using System.Collections.ObjectModel;
using System.IO;
using Audiola.Dsp;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singola.Services;

namespace Singola.ViewModels;

/// <summary>Ein Mitspieler: Name, Bühnenfarbe, Mikrofon und Live-Zustand (Pegel/Ton/Punkte).</summary>
public sealed partial class PlayerSlot : ObservableObject
{
    private static readonly string[] Palette = ["#FF4FA3", "#4FD8FF", "#B56BFF", "#3DDC84"];
    public static string ColorFor(int index) => Palette[index % Palette.Length];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _colorHex = Palette[0];
    [ObservableProperty] private int _deviceNumber;
    [ObservableProperty] private int _score;       // Punkte im aktuellen Song
    [ObservableProperty] private int _totalScore;  // Punkte über alle Runden der Show
    [ObservableProperty] private float _level;
    [ObservableProperty] private string _note = "–";
    [ObservableProperty] private int _place;       // 1..n im Endstand
    [ObservableProperty] private bool _isHit;      // trifft gerade die Ziel-Note (Glow)
    [ObservableProperty] private string _comboText = "";
    internal int Combo;

    internal double LastMidi;
}

/// <summary>
/// Der komplette Spielablauf: Song wählen (Lyrics kommen automatisch), Spieler mit je eigenem
/// Mikrofon anlegen, singen (Live-Pitch + Punkte), Endstand. Eine Runde = ein Song; Playlisten
/// und Mehrrunden-Duelle setzen darauf auf.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings = new SettingsService();
    public readonly KaraokeEngine Engine = new();

    // ---- Screens ----
    [ObservableProperty] private bool _isSetup = true;
    [ObservableProperty] private bool _isSinging;
    [ObservableProperty] private bool _isFinished;

    // ---- Song ----
    [ObservableProperty] private string? _songPath;
    [ObservableProperty] private string _songTitle = "";
    [ObservableProperty] private string _songStatus = "Zieh einen Song hierher — MP3, WAV, OGG, FLAC, M4A … oder ein .audiola-Projekt.";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasSong;
    [ObservableProperty] private bool _hasLyrics;
    private LoadedSong? _song;
    private IReadOnlyList<LyricLine> _lyrics = [];

    /// <summary>Referenz-Melodie fürs Notenband (leer = freies Singen mit Aktivitäts-Score).</summary>
    public IReadOnlyList<MelodyNote> Melody => _song?.Melody ?? [];
    private int _melodyIndex;

    // ---- Einstellungen (geteilt mit Audiola: %APPDATA%\Audiola\settings.json) ----
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
        _settings.Current.ElevenLabsApiKey = string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey.Trim();
        _settings.Save();
        IsSettingsOpen = false;
        if (HasSong && !HasLyrics)
            SongStatus = "Key gespeichert — lade den Song neu, um den Songtext erkennen zu lassen.";
    }

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    // ---- Spieler ----
    public ObservableCollection<PlayerSlot> Players { get; } = [];

    // ---- Sing-Zustand ----
    [ObservableProperty] private string _lyricPrev = "";
    [ObservableProperty] private string _lyricCurrent = "";
    [ObservableProperty] private string _lyricNext = "";
    [ObservableProperty] private string _timeText = "0:00 / 0:00";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _isPaused;
    private int _lyricIndex = -1;

    // ---- Ergebnis ----
    public ObservableCollection<PlayerSlot> Ranking { get; } = [];

    // ---- Playlisten + Show (mehrere Runden bis zum Sieger) ----
    public ObservableCollection<Playlist> Playlists { get; } = new(PlaylistStore.Load());
    [ObservableProperty] private Playlist? _selectedPlaylist;
    [ObservableProperty] private string _newPlaylistName = "";
    /// <summary>0 = nur der geladene Song, 1 = ganze Playlist, 2 = X zufällige daraus.</summary>
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
        foreach (var s in value?.SongPaths ?? []) SelectedPlaylistSongs.Add(s);
        OnPropertyChanged(nameof(CanStart));
    }

    partial void OnPlayModeChanged(int value) => OnPropertyChanged(nameof(CanStart));

    [RelayCommand]
    private void CreatePlaylist()
    {
        var name = string.IsNullOrWhiteSpace(NewPlaylistName) ? $"Playlist {Playlists.Count + 1}" : NewPlaylistName.Trim();
        var pl = new Playlist { Name = name };
        Playlists.Add(pl);
        SelectedPlaylist = pl;
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

    /// <summary>Fügt den aktuell geladenen Song der gewählten Playlist hinzu.</summary>
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

    public MainViewModel()
    {
        AddPlayer();
        Engine.PlaybackEnded += (_, _) =>
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(FinishRound);
    }

    public bool CanStart => !IsLoading && Players.Count > 0
        && Players.Select(p => p.DeviceNumber).Distinct().Count() == Players.Count
        && (PlayMode == 0 ? HasSong : SelectedPlaylist?.SongPaths.Count > 0);

    partial void OnHasSongChanged(bool value) => OnPropertyChanged(nameof(CanStart));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(CanStart));
    /// <summary>Von der View bei Mic-/Spieleränderungen aufgerufen (Geräte müssen eindeutig sein).</summary>
    public void RevalidateStart() => OnPropertyChanged(nameof(CanStart));

    [RelayCommand]
    private void AddPlayer()
    {
        if (Players.Count >= 4) return;
        var i = Players.Count;
        Players.Add(new PlayerSlot
        {
            Name = $"Spieler {i + 1}",
            ColorHex = PlayerSlot.ColorFor(i),
            DeviceNumber = Math.Min(i, Math.Max(0, NAudio.Wave.WaveInEvent.DeviceCount - 1)),
        });
        RevalidateStart();
    }

    [RelayCommand]
    private void RemovePlayer(PlayerSlot? slot)
    {
        if (slot is null || Players.Count <= 1) return;
        Players.Remove(slot);
        RevalidateStart();
    }

    public async Task LoadSongAsync(string path)
    {
        if (!SongLoader.IsSupported(path)) { SongStatus = "Dieses Format kenne ich nicht."; return; }
        IsLoading = true; HasSong = false;
        SongTitle = Path.GetFileNameWithoutExtension(path);
        var progress = new Progress<string>(s => SongStatus = s);
        try
        {
            _song = await SongLoader.LoadAsync(path, progress, _settings);
            _lyrics = LrcParser.Parse(_song.Lrc);

            // Fließtext-Lyrics ohne Zeitstempel: Zeilen gleichmäßig über den Song verteilen.
            if (_lyrics.Count == 0 && !string.IsNullOrWhiteSpace(_song.Lrc) && _song.DurationSeconds > 10)
            {
                var lines = _song.Lrc!.Replace("\r\n", "\n").Split('\n')
                    .Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                if (lines.Count > 0)
                {
                    var span = _song.DurationSeconds * 0.86;                    // Intro/Outro grob aussparen
                    var offset = _song.DurationSeconds * 0.07;
                    _lyrics = lines.Select((l, i) =>
                        new LyricLine(offset + span * i / lines.Count, l)).ToList();
                }
            }
            SongPath = path;
            SongTitle = _song.Title;
            HasLyrics = _lyrics.Count > 0;
            OnPropertyChanged(nameof(Melody));

            var parts = new List<string>();
            parts.Add(HasLyrics ? $"{_lyrics.Count} Textzeilen" : "ohne Songtext (Key in den Einstellungen hinterlegen)");
            parts.Add(_song.HasMelody ? $"Notenband: {_song.MelodySource}" : "ohne Notenband");
            SongStatus = "Bereit — " + string.Join(" · ", parts) + ". Lasst die Show beginnen!";
            HasSong = true;
        }
        catch (Exception ex)
        {
            SongStatus = "Song konnte nicht geladen werden: " + ex.Message;
        }
        finally { IsLoading = false; }
    }

    /// <summary>Startet die Show: baut die Song-Queue nach Spielmodus und singt Runde 1.</summary>
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
        if (_queue.Count == 0) { SongStatus = "Keine (vorhandenen) Songs im gewählten Modus."; return; }

        _queueIndex = 0;
        foreach (var p in Players) p.TotalScore = 0;
        await PlayQueueEntryAsync();
    }

    /// <summary>Lädt den Song an _queueIndex und startet die Runde (Songs kommen meist aus dem Cache).</summary>
    private async Task PlayQueueEntryAsync()
    {
        var path = _queue[_queueIndex];
        if (!string.Equals(path, SongPath, StringComparison.OrdinalIgnoreCase) || _song is null)
        {
            IsLoadingNext = true;
            try { await LoadSongAsync(path); }
            finally { IsLoadingNext = false; }
            if (_song is null || !HasSong) { SongStatus = $"Song übersprungen: {Path.GetFileName(path)}"; return; }
        }

        foreach (var p in Players) { p.Score = 0; p.LastMidi = 0; p.Note = "–"; p.Combo = 0; p.ComboText = ""; p.IsHit = false; }
        _lyricIndex = -1;
        _melodyIndex = 0;
        LyricPrev = ""; LyricCurrent = HasLyrics ? "…" : "🎤 Sing einfach los!"; LyricNext = "";
        IsPaused = false;
        RoundText = _queue.Count > 1 ? $"Runde {_queueIndex + 1} von {_queue.Count}" : "";

        Engine.Start(_song!.PlayablePath, Players.Select(p => p.DeviceNumber).ToList());
        IsSetup = false; IsFinished = false; IsSinging = true;
    }

    /// <summary>Nächster Song der Show (vom Zwischenstand-Screen).</summary>
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
        if (IsPaused) { Engine.Resume(); IsPaused = false; }
        else { Engine.Pause(); IsPaused = true; }
    }

    [RelayCommand]
    private void StopRound() => FinishRound();

    [RelayCommand]
    private void BackToSetup()
    {
        Engine.Stop();
        IsSinging = false; IsFinished = false; IsSetup = true;
    }

    private void FinishRound()
    {
        if (!IsSinging) return;
        Engine.Stop();
        foreach (var p in Players) p.TotalScore += p.Score;

        Ranking.Clear();
        var place = 1;
        foreach (var p in Players.OrderByDescending(p => p.TotalScore)) { p.Place = place++; Ranking.Add(p); }

        HasNextSong = _queueIndex + 1 < _queue.Count;
        RoundText = _queue.Count > 1
            ? (HasNextSong ? $"Zwischenstand nach Runde {_queueIndex + 1} von {_queue.Count}" : $"Endstand nach {_queue.Count} Runden")
            : "";
        IsSinging = false; IsFinished = true;
    }

    /// <summary>Pro Anzeige-Takt und Spieler: (Index, Songzeit, MIDI-Ton oder 0 bei Stille) — für die Bühnen-Linien.</summary>
    public event Action<int, double, double>? PitchSampled;

    /// <summary>Anzeige-Takt (~20×/s): Pitch/Pegel je Spieler lesen, Punkte vergeben, Lyrics weiterschalten.</summary>
    public void Tick()
    {
        if (!IsSinging || IsPaused) return;
        var t = Engine.PositionSeconds;
        var dur = Math.Max(1, Engine.DurationSeconds);
        Progress = Math.Clamp(t / dur, 0, 1);
        TimeText = $"{(int)t / 60}:{(int)t % 60:00} / {(int)dur / 60}:{(int)dur % 60:00}";

        // Lyrics mitführen.
        if (_lyrics.Count > 0)
        {
            var idx = _lyricIndex;
            while (idx + 1 < _lyrics.Count && _lyrics[idx + 1].TimeSeconds <= t) idx++;
            if (idx != _lyricIndex)
            {
                _lyricIndex = idx;
                LyricPrev = idx > 0 ? _lyrics[idx - 1].Text : "";
                LyricCurrent = idx >= 0 ? _lyrics[idx].Text : "…";
                LyricNext = idx + 1 < _lyrics.Count ? _lyrics[idx + 1].Text : "";
            }
        }

        // Aktive Ziel-Note des Notenbands bestimmen (Melodie ist zeitlich sortiert).
        var melody = Melody;
        MelodyNote? target = null;
        if (melody.Count > 0)
        {
            while (_melodyIndex < melody.Count && melody[_melodyIndex].End < t) _melodyIndex++;
            if (_melodyIndex < melody.Count && melody[_melodyIndex].Start <= t) target = melody[_melodyIndex];
        }

        for (var i = 0; i < Players.Count; i++)
        {
            var p = Players[i];
            var (hz, level) = Engine.ReadPlayer(i);
            p.Level = level;

            var singing = hz > 60 && level > 0.02f;
            var midi = singing ? PitchDetector.HzToMidi(hz) : 0;
            p.Note = singing ? PitchDetector.MidiToName(midi) : "–";
            PitchSampled?.Invoke(i, t, singing ? midi : 0);

            if (target is { } n)
            {
                // SingStar-Modus: Punkte gibt es fürs TREFFEN der Ziel-Note (Oktave egal).
                var hit = singing && Math.Abs(PitchDetector.CentsOffOctaveless(hz, PitchDetector.MidiToHz(n.Midi))) <= 120;
                p.IsHit = hit;
                if (hit)
                {
                    p.Combo++;
                    p.Score += 3 + Math.Min(2, p.Combo / 40);   // langer Streak = kleiner Bonus
                }
                else p.Combo = 0;
            }
            else
            {
                // Kein Notenband (oder Pause zwischen Noten): Aktivität + gehaltene Töne zählen.
                p.IsHit = false;
                if (melody.Count == 0 && singing)
                {
                    var stable = p.LastMidi > 0 && Math.Abs(midi - p.LastMidi) < 0.7;
                    p.Score += stable ? 3 : 1;
                }
                if (melody.Count > 0) p.Combo = 0;
            }
            p.ComboText = p.Combo >= 20 ? $"×{p.Combo / 20 * 20} 🔥" : "";
            p.LastMidi = singing ? midi : 0;
        }
    }
}
