using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Audiola.Services;

/// <summary>
/// Registriert die <c>.audiola</c>-Dateiendung für den aktuellen Benutzer (HKCU — keine
/// Adminrechte nötig): Doppelklick öffnet Audiola mit dem Projekt, die Dateien tragen das
/// App-Icon. Läuft idempotent bei jedem Start und aktualisiert damit auch den Exe-Pfad
/// nach App-Updates.
/// </summary>
public static class FileAssociation
{
    private const string ProgId = "Audiola.Project";

    public static void EnsureRegistered()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe) || !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return; // z. B. dotnet-Host ohne AppHost — dann nicht registrieren

            var command = $"\"{exe}\" \"%1\"";

            using var classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes");

            // Schon aktuell? Dann nichts anfassen (kein unnötiges Registry-Geschreibe).
            using (var check = classes.OpenSubKey($@"{ProgId}\shell\open\command"))
                if (check?.GetValue("") as string == command)
                    return;

            using (var ext = classes.CreateSubKey(".audiola"))
                ext.SetValue("", ProgId);

            using (var prog = classes.CreateSubKey(ProgId))
            {
                prog.SetValue("", "Audiola-Projekt");
                using (var icon = prog.CreateSubKey("DefaultIcon"))
                    icon.SetValue("", $"\"{exe}\",0");
                using (var open = prog.CreateSubKey(@"shell\open\command"))
                    open.SetValue("", command);
            }

            // Explorer über die Änderung informieren (Icon/Verknüpfung sofort sichtbar).
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }
        catch { /* Best effort — Verknüpfung ist Komfort, kein Muss. */ }
    }

    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const int SHCNF_IDLIST = 0x0000;

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
}
