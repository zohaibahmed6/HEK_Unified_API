using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Api.Telemetry;

/// <summary>
/// FR-12 (call-flow traceability): turns a resolved <see cref="CallRoutingInfo"/> into a single
/// plain-English sentence, aimed at a non-technical reader - not a raw header/trace-tag dump. Maps
/// the internal environment/db-server identifiers used across the tenant/HISO-session registries to
/// friendly labels. Used only for the human-readable summary; the raw values still ride on their own
/// individual `X-Hek-Routing-*` headers for anyone who wants the precise underlying identifiers.
/// </summary>
public static class RoutingSummaryFormatter
{
    private static readonly Dictionary<string, string> FriendlyEnvironments = new(StringComparer.OrdinalIgnoreCase)
    {
        ["local"] = "Local Development",
        ["-"] = "Local Development",
        ["south"] = "SOUTH (remote production)",
        ["dev"] = "Development",
        ["prod"] = "Production",
    };

    private static readonly Dictionary<string, string> FriendlySystemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hiso"] = "HISO",
        ["karo"] = "KARO",
        ["erms"] = "ERMS",
        ["col"] = "COL",
    };

    public static string BuildSummary(string system, CallRoutingInfo routing)
    {
        var friendlySystem = FriendlySystemNames.GetValueOrDefault(system, system.ToUpperInvariant());
        var friendlyEnvironment = FriendlyEnvironments.GetValueOrDefault(routing.Environment, routing.Environment);

        return $"This {friendlySystem} call was for practice {routing.PracticeId} and was routed to the " +
               $"{friendlyEnvironment} environment (database server {routing.DbServerHost}, database {routing.DbName}).";
    }

    /// <summary>
    /// FR-12/FR-6 combined into one readable log line per call: who called, for which patient, what
    /// was asked for, what came back, and which server/database actually served it. Deliberately logs
    /// field NAMES returned (e.g. "nhi, birthDate, given"), never field VALUES - actual patient data
    /// (NHI, name, DOB) must never land in plain-text logs per the project's NZ HIPC compliance
    /// decision (see PROJECT_STATUS.md - errors/logs must not carry PHI).
    /// </summary>
    public static string BuildCallLogLine(
        string system,
        string action,
        string? patientId,
        string? encounterId,
        bool succeeded,
        string? failureReason,
        IReadOnlyCollection<string>? fieldsReturned,
        CallRoutingInfo? routing)
    {
        var friendlySystem = FriendlySystemNames.GetValueOrDefault(system, system.ToUpperInvariant());
        var who = $"{friendlySystem} {action}";
        var patient = string.IsNullOrWhiteSpace(patientId) ? "an unspecified patient" : $"patient {patientId}";
        var encounter = string.IsNullOrWhiteSpace(encounterId) ? "" : $" (encounter {encounterId})";

        var outcome = succeeded
            ? (fieldsReturned is { Count: > 0 }
                ? $"succeeded and returned {fieldsReturned.Count} field(s): {string.Join(", ", fieldsReturned)}"
                : "succeeded")
            : $"failed ({failureReason ?? "no reason given"})";

        var routedTo = routing is null
            ? "routing could not be determined"
            : $"served from database server {routing.DbServerHost} (database {routing.DbName}), " +
              $"{FriendlyEnvironments.GetValueOrDefault(routing.Environment, routing.Environment)} environment";

        return $"{who}: called for {patient}{encounter} — {outcome} — {routedTo}.";
    }

    /// <summary>
    /// Field NAMES only (never values - see <see cref="BuildCallLogLine"/> remarks) of an object's
    /// non-null properties, for use as the <c>fieldsReturned</c> argument above. Reflection-based so
    /// it can't drift from whatever a repository/query result record actually exposes.
    /// </summary>
    public static string[] GetNonNullPropertyNames(object? obj)
    {
        if (obj is null)
        {
            return [];
        }

        return obj.GetType().GetProperties()
            .Where(p => p.GetValue(obj) is not null)
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..])
            .ToArray();
    }
}
