namespace Audiola.Services;

using System.Diagnostics;
using System.IO;
using System.Text;

public abstract class PythonVariationProviderBase : IAudioVariationProvider
{
    private readonly string _pythonExe;
    private readonly string _scriptPath;
    private readonly IReadOnlyList<AudioVariation> _variations;

    protected PythonVariationProviderBase(
        string name,
        string pythonExe,
        string scriptPath,
        IReadOnlyList<AudioVariation> variations)
    {
        Name = name;
        _pythonExe = pythonExe;
        _scriptPath = scriptPath;
        _variations = variations;
    }

    public bool ScriptExists => File.Exists(_scriptPath);

    public string Name { get; }

    public IReadOnlyList<AudioVariation> GetVariations() => _variations;

    public async Task<float[]> ApplyAsync(
        string variationId,
        float[] interleavedStereo,
        int sampleRate,
        CancellationToken ct = default)
    {
        if (interleavedStereo.Length % 2 != 0)
            throw new ArgumentException("Buffer must be interleaved stereo: L,R,L,R,...", nameof(interleavedStereo));

        if (!_variations.Any(v => v.Id == variationId))
            throw new ArgumentException($"Unknown variation id: {variationId}", nameof(variationId));

        var tempRoot = Path.Combine(Path.GetTempPath(), "audio_variation_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var inputWav = Path.Combine(tempRoot, "input.wav");
        var outputDir = Path.Combine(tempRoot, "out");

        try
        {
            WriteStereoFloatWav(inputWav, interleavedStereo, sampleRate);

            var result = await RunPythonAsync(inputWav, outputDir, variationId, ct);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Python variation failed with exit code {result.ExitCode}\n\nSTDOUT:\n{result.StdOut}\n\nSTDERR:\n{result.StdErr}");
            }

            var expectedPrefix = variationId + "_";
            var outputWav = Directory
                .EnumerateFiles(outputDir, "*.wav")
                .FirstOrDefault(p => Path.GetFileName(p).StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase));

            if (outputWav is null)
            {
                throw new FileNotFoundException(
                    $"Python script did not create a WAV output for variation '{variationId}' in '{outputDir}'.");
            }

            return ReadStereoFloatWav(outputWav);
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, recursive: true);
            }
            catch
            {
                // Ignore temp cleanup errors.
            }
        }
    }

    private async Task<ProcessResult> RunPythonAsync(
        string inputWav,
        string outputDir,
        string variationId,
        CancellationToken ct)
    {
        Directory.CreateDirectory(outputDir);

        var psi = new ProcessStartInfo
        {
            FileName = _pythonExe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        psi.ArgumentList.Add(_scriptPath);
        psi.ArgumentList.Add(inputWav);
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(outputDir);
        psi.ArgumentList.Add("--formats");
        psi.ArgumentList.Add("wav");
        psi.ArgumentList.Add("--only");
        psi.ArgumentList.Add(variationId);
        psi.ArgumentList.Add("--continue-on-error");

        using var process = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdout.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static void WriteStereoFloatWav(string path, float[] samples, int sampleRate)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs, Encoding.ASCII);

        var byteRate = sampleRate * 2 * 4;
        var blockAlign = 2 * 4;
        var dataSize = samples.Length * 4;

        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((ushort)3); // IEEE float
        bw.Write((ushort)2); // stereo
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((ushort)blockAlign);
        bw.Write((ushort)32);

        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);

        foreach (var sample in samples)
            bw.Write(sample);
    }

    private static float[] ReadStereoFloatWav(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs, Encoding.ASCII);

        var riff = new string(br.ReadChars(4));
        if (riff != "RIFF")
            throw new InvalidDataException("Not a RIFF file.");

        br.ReadInt32();

        var wave = new string(br.ReadChars(4));
        if (wave != "WAVE")
            throw new InvalidDataException("Not a WAVE file.");

        ushort audioFormat = 0;
        ushort channels = 0;
        ushort bitsPerSample = 0;
        long dataPosition = -1;
        int dataSize = 0;

        while (fs.Position < fs.Length)
        {
            var chunkId = new string(br.ReadChars(4));
            var chunkSize = br.ReadInt32();

            if (chunkId == "fmt ")
            {
                audioFormat = br.ReadUInt16();
                channels = br.ReadUInt16();
                br.ReadInt32(); // sample rate
                br.ReadInt32(); // byte rate
                br.ReadUInt16(); // block align
                bitsPerSample = br.ReadUInt16();

                fs.Position += chunkSize - 16;
            }
            else if (chunkId == "data")
            {
                dataPosition = fs.Position;
                dataSize = chunkSize;
                fs.Position += chunkSize;
            }
            else
            {
                fs.Position += chunkSize;
            }

            if (chunkSize % 2 == 1)
                fs.Position++;
        }

        if (channels != 2)
            throw new InvalidDataException($"Expected stereo WAV, got {channels} channels.");

        if (dataPosition < 0)
            throw new InvalidDataException("No data chunk found.");

        fs.Position = dataPosition;

        if (audioFormat == 3 && bitsPerSample == 32)
        {
            var count = dataSize / 4;
            var result = new float[count];

            for (var i = 0; i < count; i++)
                result[i] = br.ReadSingle();

            return result;
        }

        if (audioFormat == 1 && bitsPerSample == 24)
        {
            var count = dataSize / 3;
            var result = new float[count];

            for (var i = 0; i < count; i++)
            {
                var b0 = br.ReadByte();
                var b1 = br.ReadByte();
                var b2 = br.ReadByte();

                var value = b0 | (b1 << 8) | (b2 << 16);
                if ((value & 0x800000) != 0)
                    value |= unchecked((int)0xFF000000);

                result[i] = value / 8388608f;
            }

            return result;
        }

        if (audioFormat == 1 && bitsPerSample == 16)
        {
            var count = dataSize / 2;
            var result = new float[count];

            for (var i = 0; i < count; i++)
                result[i] = br.ReadInt16() / 32768f;

            return result;
        }

        throw new InvalidDataException($"Unsupported WAV format. AudioFormat={audioFormat}, Bits={bitsPerSample}");
    }

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
}