using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Iris.Api.Services;

/// <summary>
/// Kills the Vite dev server process on app shutdown.
/// Prevents the node process holding port 5173 from surviving after VS stops debugging.
/// </summary>
public sealed class SpaProcessManager : IHostedService
{
    private const int SpaPort = 5173;

    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SpaProcessManager> _logger;

    public SpaProcessManager(IHostApplicationLifetime lifetime, ILogger<SpaProcessManager> logger)
    {
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStopping.Register(KillSpaProcess);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void KillSpaProcess()
    {
        try
        {
            var pid = GetPidOnPort(SpaPort);
            if (pid is null)
                return;

            var process = Process.GetProcessById(pid.Value);
            _logger.LogInformation("Stopping Vite dev server (PID {Pid}) on port {Port}", pid, SpaPort);
            process.Kill(entireProcessTree: true);
            process.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not stop Vite dev server — it may have already exited.");
        }
    }

    private static int? GetPidOnPort(int port)
    {
        // netstat -ano lists TCP connections with PID; find the one listening on our port
        var psi = new ProcessStartInfo("netstat", "-ano")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi);
        if (proc is null) return null;

        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        // Match lines like: TCP   0.0.0.0:5173   ...   LISTENING   12345
        var pattern = new Regex($@"TCP\s+[^\s]+:{port}\s+[^\s]+\s+LISTENING\s+(\d+)", RegexOptions.IgnoreCase);
        var match = pattern.Match(output);
        if (!match.Success) return null;

        return int.TryParse(match.Groups[1].Value, out var pid) ? pid : null;
    }
}
