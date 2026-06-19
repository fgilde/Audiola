using System.Diagnostics;

namespace Audiola.Services;

/// <summary>
/// Zentrale GPU-/CUDA-Erkennung für ALLE Python-Engines (qwen-tts, Whisper, seed-vc, audio-separator).
/// Eine einzige Quelle der Wahrheit, damit nicht an mehreren Stellen unterschiedlich entschieden wird,
/// ob CUDA-Builds installiert werden — der Fehler, der seed-vc/Stem-Trennung versehentlich auf die CPU
/// gezwungen hat.
/// </summary>
public static class GpuDetect
{
    /// <summary>CUDA-Wheel-Index (an Haupt-venv-torch 2.x+cu121 und aktuelle NVIDIA-Treiber ausgerichtet).</summary>
    public const string CudaIndexUrl = "https://download.pytorch.org/whl/cu121";

    /// <summary>
    /// Soll CUDA installiert/genutzt werden? „cuda“ erzwingt es, „cpu“/„directml“ schalten es ab,
    /// „auto“ (Standard) nutzt CUDA, sobald eine NVIDIA-GPU vorhanden ist.
    /// </summary>
    public static bool ShouldUseCuda(string? device)
    {
        if (string.Equals(device, "cuda", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.IsNullOrWhiteSpace(device) || string.Equals(device, "auto", StringComparison.OrdinalIgnoreCase))
            return HasNvidiaGpu();
        return false; // cpu / directml
    }

    /// <summary>Prüft per <c>nvidia-smi</c>, ob eine NVIDIA-GPU vorhanden ist (schnell, kein torch nötig).</summary>
    public static bool HasNvidiaGpu()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi", RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            psi.ArgumentList.Add("--query-gpu=name");
            psi.ArgumentList.Add("--format=csv,noheader");
            using var p = Process.Start(psi);
            if (p is null) return false;
            if (!p.WaitForExit(5000)) { try { p.Kill(); } catch { } return false; }
            return p.ExitCode == 0;
        }
        catch { return false; }
    }
}
