namespace Audiola.Models;

/// <summary>Serialisierbarer Projektzustand (Manifest in der .audiola-ZIP).</summary>
public sealed class ProjectDto
{
    public int Version { get; set; } = 1;
    public double MasterVolume { get; set; } = 1.0;
    public double PixelsPerSecond { get; set; } = 40;
    public bool SnapEnabled { get; set; } = true;
    public double GridSeconds { get; set; } = 0.25;
    public List<ProjectTrackDto> Tracks { get; set; } = [];
    public List<ProjectEqBandDto> Eq { get; set; } = [];

    /// <summary>Index der zuletzt ausgewählten Spur (-1 = keine).</summary>
    public int SelectedTrackIndex { get; set; } = -1;

    /// <summary>Aktuelle Mastering-Einstellungen (null = keine gespeichert).</summary>
    public MasteringSettings? Mastering { get; set; }

    /// <summary>Name des gewählten Mastering-Profils (optional).</summary>
    public string? MasteringProfile { get; set; }

    /// <summary>Spatial-Audio-Positionen/Layout (null = nicht eingerichtet).</summary>
    public ProjectSpatialDto? Spatial { get; set; }
}

/// <summary>Gespeicherter Spatial-Audio-Zustand (3D-Positionen + Ausgabe-Layout).</summary>
public sealed class ProjectSpatialDto
{
    public string Layout { get; set; } = "7.1.4 (Atmos-Bett)";
    public double RoomAmount { get; set; } = 0.18;
    public List<ProjectSpatialSourceDto> Sources { get; set; } = [];
}

public sealed class ProjectSpatialSourceDto
{
    public string Name { get; set; } = "";
    public double AzimuthDeg { get; set; }
    public double ElevationDeg { get; set; }
    public double Distance { get; set; } = 1.0;
    public double GainDb { get; set; }
    public bool Muted { get; set; }
}

public sealed class ProjectTrackDto
{
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "#5B8CFF";
    public double Volume { get; set; } = 1.0;
    public double Pan { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsMuted { get; set; }
    public bool IsSolo { get; set; }
    public List<ProjectClipDto> Clips { get; set; } = [];

    /// <summary>Transkript dieser Spur als LRC (zeitgestempelt) — wird beim Export eingebettet.</summary>
    public string? Lrc { get; set; }
}

public sealed class ProjectClipDto
{
    /// <summary>Beim Speichern relativer Pfad im ZIP (media/…), zur Laufzeit absoluter Pfad.</summary>
    public string Media { get; set; } = "";
    public double SourceTotalSeconds { get; set; }
    public double TimelineOffsetSeconds { get; set; }
    public double SourceStartSeconds { get; set; }
    public double LengthSeconds { get; set; }
    public double GainDb { get; set; }
    public double FadeInSeconds { get; set; }
    public double FadeOutSeconds { get; set; }
}

public sealed class ProjectEqBandDto
{
    public string Type { get; set; } = "Peaking";
    public double Frequency { get; set; }
    public double GainDb { get; set; }
    public double Q { get; set; } = 1.0;
    public string ColorHex { get; set; } = "#5B8CFF";
}
