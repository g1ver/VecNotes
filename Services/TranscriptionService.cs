using System.Diagnostics;
using System.Text.RegularExpressions;

namespace VecNotes.Services;

public interface ITranscriptionService
{
    Task<string> TranscribeAsync(string audioFilePath);
}

public partial class WhisperCppTranscriptionService(IConfiguration config) : ITranscriptionService
{
    private readonly string _binary = config["WhisperCpp:BinaryPath"] ?? "whisper-cli";
    private readonly string _model = config["WhisperCpp:ModelPath"]
        ?? throw new InvalidOperationException("WhisperCpp:ModelPath is required");

    public async Task<string> TranscribeAsync(string audioFilePath)
    {
        var wavPath = await ConvertToWavAsync(audioFilePath);
        var outputBase = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var psi = new ProcessStartInfo
        {
            FileName = _binary,
            Arguments = $"-m \"{_model}\" -f \"{wavPath}\" -otxt -of \"{outputBase}\" --no-prints",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start whisper-cli");

        await process.WaitForExitAsync();

        var outputFile = outputBase + ".txt";
        try
        {
            if (process.ExitCode != 0 || !File.Exists(outputFile))
                throw new InvalidOperationException($"whisper-cli exited with code {process.ExitCode}");

            var raw = await File.ReadAllTextAsync(outputFile);
            return CleanTranscript(raw);
        }
        finally
        {
            if (File.Exists(outputFile)) File.Delete(outputFile);
            if (wavPath != audioFilePath && File.Exists(wavPath)) File.Delete(wavPath);
        }
    }

    private static async Task<string> ConvertToWavAsync(string inputPath)
    {
        if (Path.GetExtension(inputPath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            return inputPath;

        var wavPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -i \"{inputPath}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{wavPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ffmpeg");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg exited with code {process.ExitCode}");

        return wavPath;
    }

    private static string CleanTranscript(string raw)
    {
        // Strip "[HH:MM:SS.mmm --> HH:MM:SS.mmm]" timestamp lines
        var cleaned = TimestampPattern().Replace(raw, "");
        // Collapse extra blank lines and trim
        return string.Join("\n", cleaned.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0));
    }

    [GeneratedRegex(@"\[\d{2}:\d{2}:\d{2}\.\d{3} --> \d{2}:\d{2}:\d{2}\.\d{3}\]\s*")]
    private static partial Regex TimestampPattern();
}
