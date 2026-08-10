using System.Collections.ObjectModel;
using System.IO;
using Audiola.Dsp;
using Audiola.Services;
using Audiola.Services.Audio;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singola.Services;

namespace Singola.ViewModels;

/// <summary>A player, their selected microphone, and the live karaoke score.</summary>
public sealed partial class PlayerSlot : ObservableObject
{
    private static readonly string[] Palette = ["#FF4FA3", "#4FD8FF", "#B56BFF", "#3DDC84"];
    public static string ColorFor(int index) => Palette[index % Palette.Length];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _colorHex = Palette[0];
    [ObservableProperty] private AudioInputDevice? _inputDevice;
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _totalScore;
    [ObservableProperty] private float _level;
    [ObservableProperty] private string _note = "–";
    [ObservableProperty] private int _place;
    [ObservableProperty] private bool _isHit;
    [ObservableProperty] private string _comboText = "";
    internal int Combo;
    internal double LastMidi;

    public IReadOnlyList<AudioInputDevice> InputDevices { get; init; } = [];
    public string DeviceId => InputDevice?.Id ?? "";
}

/// <summary>Runs the karaoke round and delegates platform audio I/O to <see cref="IAudioPlatform"/>.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings = new SettingsService();
    private readonly IAudioPlatform _audio;
    public readonly KaraokeEngine Engine;

    [ObservableProperty] private bool _isSetup = true;
    [ObservableProperty] private bool _isSinging;
    [ObservableProperty] private bool _isFinished;

    [ObservableProperty] private string? _songPath;
    [ObservableProperty] private string _songTitle = "";
    [ObservableProperty] private string _songStatus = "Zieh einen Song hierher — MP3, WAV, OGG, FLAC, M4A … oder ein .audiola-Projekt.";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasSong;
    [ObservableProperty] private bool _hasLyrics;
    private LoadedSong? _song;
    private IReadOnlyList<LyricLine> _lyrics = [];

    public IReadOnlyList<MelodyNote> Melody => _song?.Melody ?? [];
    private int _melodyIndex;

    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private string _apiKey = "";

    [RelayCommand]
    private void OpenSettings()
    {
        ApiKey = _settings.Current.ElevenLabsApiKey ?? "";
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settings.Current.ElevenLabsApiKey = string.IsNullOrWhiteSpace(ApiKey) ? "" : ApiKey.Trim();
        _settings.Save();
        IsSettingsOpen = false;
        if (HasSong && !HasLyrics)
            SongStatus = "Key gespeichert — lade den Song neu, um den Songtext erkennen zu lassen.";
    }

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    public ObservableCollection<PlayerSlot> Players { get; } = [];
    public IReadOnlyList<AudioInputDevice> InputDevices { get; }

    [ObservableProperty] private string _lyricPrev = "";
    [ObservableProperty] private string _lyricCurrent = "";
    [ObservableProperty] private string _lyricNext = "";
    [ObservableProperty] private string _timeText = "0:00 / 0:00";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _isPaused;
    private int _lyricIndex = -1;

    public ObservableCollection<PlayerSlot> Ranking { get; } = [];

    public ObservableCollection<Playlist> Playlists { get; } = new(PlaylistStore.Load());
    [ObservableProperty] private Playlist? _selectedPlaylist;
    [ObservableProperty] private string _newPlaylistName = "";
    [ObservableProperty] private int _playMode;
    [ObservableProperty] private int _randomCount = 3;
    public ObservableCollection<string> SelectedPlaylistSongs { get; } = [];

    private List<string> _queue = [];
    private int _queueIndex;
    [ObservableProperty] private string _roundText = "";
    [ObservableProperty] private bool _hasNextSong;
    [ObservableProperty] private bool _isLoadingNext;

    partial void OnSelectedPlaylistChanged(Playlist? value)
    {
        SelectedPlaylistSongs.Clear();
        foreach (var song in value?.SongPaths ?? []) SelectedPlaylistSongs.Add(song);
        OnPropertyChanged(nameof(CanStart));
    }

    partial void OnPlayModeChanged(int value) => OnPropertyChanged(nameof(CanStart));

    [RelayCommand]
    private void CreatePlaylist()
    {
        var name = string.IsNullOrWhiteSpace(NewPlaylistName) ? $"Playlist {Playlists.Count + 1}" : NewPlaylistName.Trim();
        var playlist = new Playlist { Name = name };
        Playlists.Add(playlist);
        SelectedPlaylist = playlist;
        NewPlaylistName = "";
        PlaylistStore.Save(Playlists);
    }

    [RelayCommand]
    private void DeletePlaylist()
    {
        if (SelectedPlaylist is null) return;
        Playlists.Remove(SelectedPlaylist);
        SelectedPlaylist = Playlists.FirstOrDefault();
        PlaylistStore.Save(Playlists);
    }

    [RelayCommand]
    private void AddSongToPlaylist()
    {
        if (SelectedPlaylist is null || SongPath is null) return;
        if (!SelectedPlaylist.SongPaths.Contains(SongPath, StringComparer.OrdinalIgnoreCase))
        {
            SelectedPlaylist.SongPaths.Add(SongPath);
            SelectedPlaylistSongs.Add(SongPath);
            PlaylistStore.Save(Playlists);
        }
    }

    [RelayCommand]
    private void RemoveSongFromPlaylist(string? path)
    {
        if (SelectedPlaylist is null || path is null) return;
        SelectedPlaylist.SongPaths.Remove(path);
        SelectedPlaylistSongs.Remove(path);
        PlaylistStore.Save(Playlists);
    }

    public MainViewModel(IAudioPlatform audio)
    {
        _audio = audio;
        InputDevices = _audio.GetInputDevices();
        Engine = new KaraokeEngine(_audio);
        AddPlayer();
        Engine.PlaybackEnded += (_, _) => Dispatcher.UIThread.Post(FinishRound);
    }

    public bool CanStart => !IsLoading && Players.Count > 0 && Players.All(player => !string.IsNullOrWhiteSpace(player.DeviceId))
        && Players.Select(player => player.DeviceId).Distinct(StringComparer.Ordinal).Count() == Players.Count
        && (PlayMode == 0 ? HasSong : SelectedPlaylist?.SongPaths.Count > 0);

    partial void OnHasSongChanged(bool value) => OnPropertyChanged(nameof(CanStart));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(CanStart));
    public void RevalidateStart() => OnPropertyChanged(nameof(CanStart));

    [RelayCommand]
    private void AddPlayer()
    {
        if (Players.Count >= 4) return;
        var index = Players.Count;
        var player = new PlayerSlot
        {
            Name = $"Spieler {index + 1}",
            ColorHex = PlayerSlot.ColorFor(index),
            InputDevices = InputDevices,
            InputDevice = InputDevices.ElementAtOrDefault(index) ?? InputDevices.FirstOrDefault(),
        };
        player.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(PlayerSlot.InputDevice)) RevalidateStart();
        };
        Players.Add(player);
        RevalidateStart();
    }

    [RelayCommand]
    private void RemovePlayer(PlayerSlot? player)
    {
        if (player is null || Players.Count <= 1) return;
        Players.Remove(player);
        RevalidateStart();
    }

    public async Task LoadSongAsync(string path)
    {
        if (!SongLoader.IsSupported(path)) { SongStatus = "Dieses Format kenne ich nicht."; return; }
        IsLoading = true;
        HasSong = false;
        SongTitle = Path.GetFileNameWithoutExtension(path);
        var progress = new Progress<string>(status => SongStatus = status);
        try
        {
            _song = await SongLoader.LoadAsync(path, progress, _settings);
            _lyrics = LrcParser.Parse(_song.Lrc);

            if (_lyrics.Count == 0 && !string.IsNullOrWhiteSpace(_song.Lrc) && _song.DurationSeconds > 10)
            {
                var lines = _song.Lrc.Replace("\r\n", "\n").Split('\n')
                    .Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
                if (lines.Count > 0)
                {
                    var span = _song.DurationSeconds * .86;
                    var offset = _song.DurationSeconds * .07;
                    _lyrics = lines.Select((line, index) =>
                        new LyricLine(offset + span * index / lines.Count, line)).ToList();
                }
            }

            SongPath = path;
            SongTitle = _song.Title;
            HasLyrics = _lyrics.Count > 0;
            OnPropertyChanged(nameof(Melody));
            var parts = new List<string>
            {
                HasLyrics ? $"{_lyrics.Count} Textzeilen" : "ohne Songtext (Key in den Einstellungen hinterlegen)",
                _song.HasMelody ? $"Notenband: {_song.MelodySource}" : "ohne Notenband",
            };
            SongStatus = "Bereit — " + string.Join(" · ", parts) + ". Lasst die Show beginnen!";
            HasSong = true;
        }
        catch (Exception ex)
        {
            SongStatus = "Song konnte nicht geladen werden: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartRoundAsync()
    {
        if (!CanStart) return;

        _queue = PlayMode switch
        {
            1 when SelectedPlaylist is not null => [.. SelectedPlaylist.SongPaths],
            2 when SelectedPlaylist is not null =>
                [.. SelectedPlaylist.SongPaths.OrderBy(_ => Random.Shared.Next()).Take(Math.Max(1, RandomCount))],
            _ => SongPath is null ? [] : [SongPath],
        };
        _queue = _queue.Where(File.Exists).ToList();
        if (_queue.Count == 0)
        {
            SongStatus = "Keine (vorhandenen) Songs im gewählten Modus.";
            return;
        }

        _queueIndex = 0;
        foreach (var player in Players) player.TotalScore = 0;
        await PlayQueueEntryAsync();
    }

    private async Task PlayQueueEntryAsync()
    {
        var path = _queue[_queueIndex];
        if (!string.Equals(path, SongPath, StringComparison.OrdinalIgnoreCase) || _song is null)
        {
            IsLoadingNext = true;
            try
            {
                await LoadSongAsync(path);
            }
            finally
            {
                IsLoadingNext = false;
            }
            if (_song is null || !HasSong)
            {
                SongStatus = $"Song übersprungen: {Path.GetFileName(path)}";
                return;
            }
        }

        foreach (var player in Players)
        {
            player.Score = 0;
            player.LastMidi = 0;
            player.Note = "–";
            player.Combo = 0;
            player.ComboText = "";
            player.IsHit = false;
        }
        _lyricIndex = -1;
        _melodyIndex = 0;
        LyricPrev = "";
        LyricCurrent = HasLyrics ? "…" : "🎤 Sing einfach los!";
        LyricNext = "";
        IsPaused = false;
        RoundText = _queue.Count > 1 ? $"Runde {_queueIndex + 1} von {_queue.Count}" : "";

        try
        {
            Engine.Start(_song!.PlayablePath, Players.Select(player => player.DeviceId).ToList());
            IsSetup = false;
            IsFinished = false;
            IsSinging = true;
        }
        catch (Exception ex)
        {
            SongStatus = "Audio konnte nicht gestartet werden: " + ex.Message;
            Engine.Stop();
        }
    }

    [RelayCommand]
    private async Task NextSongAsync()
    {
        if (_queueIndex + 1 >= _queue.Count) return;
        _queueIndex++;
        await PlayQueueEntryAsync();
    }

    [RelayCommand]
    private void TogglePause()
    {
        if (!IsSinging) return;
        if (IsPaused)
        {
            Engine.Resume();
            IsPaused = false;
        }
        else
        {
            Engine.Pause();
            IsPaused = true;
        }
    }

    [RelayCommand]
    private void StopRound() => FinishRound();

    [RelayCommand]
    private void BackToSetup()
    {
        Engine.Stop();
        IsSinging = false;
        IsFinished = false;
        IsSetup = true;
    }

    private void FinishRound()
    {
        if (!IsSinging) return;
        Engine.Stop();
        foreach (var player in Players) player.TotalScore += player.Score;

        Ranking.Clear();
        var place = 1;
        foreach (var player in Players.OrderByDescending(player => player.TotalScore))
        {
            player.Place = place++;
            Ranking.Add(player);
        }

        HasNextSong = _queueIndex + 1 < _queue.Count;
        RoundText = _queue.Count > 1
            ? (HasNextSong ? $"Zwischenstand nach Runde {_queueIndex + 1} von {_queue.Count}" : $"Endstand nach {_queue.Count} Runden")
            : "";
        IsSinging = false;
        IsFinished = true;
    }

    public event Action<int, double, double>? PitchSampled;

    /// <summary>Called at the display rate; preserves the original pitch and scoring thresholds.</summary>
    public void Tick()
    {
        if (!IsSinging || IsPaused) return;
        var time = Engine.PositionSeconds;
        var duration = Math.Max(1, Engine.DurationSeconds);
        Progress = Math.Clamp(time / duration, 0, 1);
        TimeText = $"{(int)time / 60}:{(int)time % 60:00} / {(int)duration / 60}:{(int)duration % 60:00}";

        if (_lyrics.Count > 0)
        {
            var index = _lyricIndex;
            while (index + 1 < _lyrics.Count && _lyrics[index + 1].TimeSeconds <= time) index++;
            if (index != _lyricIndex)
            {
                _lyricIndex = index;
                LyricPrev = index > 0 ? _lyrics[index - 1].Text : "";
                LyricCurrent = index >= 0 ? _lyrics[index].Text : "…";
                LyricNext = index + 1 < _lyrics.Count ? _lyrics[index + 1].Text : "";
            }
        }

        var melody = Melody;
        MelodyNote? target = null;
        if (melody.Count > 0)
        {
            while (_melodyIndex < melody.Count && melody[_melodyIndex].End < time) _melodyIndex++;
            if (_melodyIndex < melody.Count && melody[_melodyIndex].Start <= time) target = melody[_melodyIndex];
        }

        for (var index = 0; index < Players.Count; index++)
        {
            var player = Players[index];
            var (hz, level) = Engine.ReadPlayer(index);
            player.Level = level;

            var singing = hz > 60 && level > .02f;
            var midi = singing ? PitchDetector.HzToMidi(hz) : 0;
            player.Note = singing ? PitchDetector.MidiToName(midi) : "–";
            PitchSampled?.Invoke(index, time, singing ? midi : 0);

            if (target is { } note)
            {
                var hit = singing &&
                    Math.Abs(PitchDetector.CentsOffOctaveless(hz, PitchDetector.MidiToHz(note.Midi))) <= 120;
                player.IsHit = hit;
                if (hit)
                {
                    player.Combo++;
                    player.Score += 3 + Math.Min(2, player.Combo / 40);
                }
                else
                {
                    player.Combo = 0;
                }
            }
            else
            {
                player.IsHit = false;
                if (melody.Count == 0 && singing)
                {
                    var stable = player.LastMidi > 0 && Math.Abs(midi - player.LastMidi) < .7;
                    player.Score += stable ? 3 : 1;
                }
                if (melody.Count > 0) player.Combo = 0;
            }

            player.ComboText = player.Combo >= 20 ? $"×{player.Combo / 20 * 20} 🔥" : "";
            player.LastMidi = singing ? midi : 0;
        }
    }
}
