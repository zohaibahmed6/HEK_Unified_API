using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Admin.Controllers;

public sealed record CallLogEntry(string System, string Timestamp, string CorrelationId, string Text);

/// <summary>
/// Internal-only read endpoint for the frontend dashboard's "Logs" tab - shows the plain-English
/// per-call summary lines (<see cref="HekCoreApi.Api.Telemetry.RoutingSummaryFormatter.BuildCallLogLine"/>)
/// so a non-technical reader can see "who called, for which patient, what happened, from which
/// server/db" without a terminal or Docker CLI. Not a real domain operation - same justification as
/// <c>HisoCompatController</c>'s <c>[ApiExplorerSettings(IgnoreApi = true)]</c> internal-tooling
/// pattern, so no MediatR round-trip.
///
/// Reads across all 4 per-system readable-log folders (logs/{system}/readable-*.log, see
/// Program.cs's UseSerilog callback) - one flat "recent" view spanning HISO/KARO/ERMS/COL, optionally
/// narrowed with <c>?system=</c>. Each entry's <see cref="CallLogEntry.CorrelationId"/> matches the
/// same request's full detail in that system's technical-*.log/errors-*.log (both JSON, both carry
/// "CorrelationId") - the link between "what a non-technical reader sees" and "what Claude needs for
/// production troubleshooting."
/// </summary>
[ApiController]
[Route("admin/logs")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class LogsController : ControllerBase
{
    // Matches Program.cs's readable-file outputTemplate:
    // "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{CorrelationId}] {Message:lj}{NewLine}"
    private static readonly Regex LineFormat = new(
        @"^(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[(?<id>[^\]]*)\] (?<rest>.*)$",
        RegexOptions.Compiled);
    private const string Marker = "[CallLog] ";
    private static readonly string[] Systems = ["hiso", "karo", "erms", "col"];

    [HttpGet("recent")]
    public IActionResult GetRecent([FromQuery] int take = 50, [FromQuery] string? system = null)
    {
        var logsRoot = Path.IsPathRooted("logs") ? "logs" : Path.Combine(AppContext.BaseDirectory, "logs");
        var systemsToRead = string.IsNullOrWhiteSpace(system) ? Systems : Systems.Where(s => s.Equals(system, StringComparison.OrdinalIgnoreCase));

        var entries = new List<CallLogEntry>();
        foreach (var sys in systemsToRead)
        {
            var directory = Path.Combine(logsRoot, sys);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            // Today's file plus a couple of rolled predecessors is enough for "recent".
            var files = Directory.GetFiles(directory, "readable-*.log")
                .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
                .Take(3);

            foreach (var file in files)
            {
                string[] lines;
                try
                {
                    using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                }
                catch (IOException)
                {
                    // File mid-write/rolled away between the directory listing and the read - skip it,
                    // this is a best-effort "recent activity" view, not an audit-grade guarantee.
                    continue;
                }

                foreach (var rawLine in lines)
                {
                    var line = rawLine.TrimEnd('\r');
                    var match = LineFormat.Match(line);
                    if (!match.Success)
                    {
                        continue;
                    }

                    var rest = match.Groups["rest"].Value;
                    var markerIndex = rest.IndexOf(Marker, StringComparison.Ordinal);
                    if (markerIndex < 0)
                    {
                        continue;
                    }

                    entries.Add(new CallLogEntry(sys, match.Groups["ts"].Value, match.Groups["id"].Value, rest[(markerIndex + Marker.Length)..]));
                }
            }
        }

        var ordered = entries
            .OrderByDescending(e => e.Timestamp, StringComparer.Ordinal) // ISO-ish timestamps sort correctly as strings
            .Take(take);

        return Ok(new { entries = ordered });
    }
}
