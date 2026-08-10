namespace Audiola.Services;

/// <summary>
/// Wendet das App-Theme ("Light"/"Dark") LIVE an, ohne Neustart. Die Palette selbst liegt
/// im Host (WPF: DawColors.*.xaml + WPF-UI; Avalonia: DawColors.*.axaml + FluentTheme).
/// </summary>
public interface IAppTheme
{
    bool IsLight { get; }

    void Apply(string? theme);
}
