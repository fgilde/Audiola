using System.Runtime.InteropServices;

namespace Audiola.Services;

/// <summary>Centralizes application and virtual-environment paths for every desktop platform.</summary>
public static class AppPaths
{
    public static string LocalDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Audiola");

    public static string RoamingDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Audiola");

    public static string PythonExecutable(string environmentDirectory) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(environmentDirectory, "Scripts", "python.exe")
            : Path.Combine(environmentDirectory, "bin", "python");
}
