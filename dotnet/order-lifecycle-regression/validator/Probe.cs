using System;
using System.Diagnostics;
using System.Text.Json;

static class CandidateProbe
{
    public static T Run<T>(object request)
    {
        ProcessStartInfo info = new("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        info.ArgumentList.Add(Environment.GetEnvironmentVariable("CANDIDATE_WORKER")!);
        using Process process = Process.Start(info)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.StandardInput.Write(JsonSerializer.Serialize(request));
        process.StandardInput.Close();
        if (!process.WaitForExit(30000))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("Candidate timed out.");
        }
        if (process.ExitCode != 0) throw new InvalidOperationException($"Candidate terminated abnormally ({process.ExitCode}): {stderr.GetAwaiter().GetResult()}");
        // A successful early exit has no complete response. A printed success marker is not data.
        return JsonSerializer.Deserialize<T>(stdout.GetAwaiter().GetResult()) ?? throw new InvalidOperationException("Candidate returned no result.");
    }
}
