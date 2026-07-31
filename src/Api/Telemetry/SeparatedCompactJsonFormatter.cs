using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace HekCoreApi.Api.Telemetry;

/// <summary>
/// Wraps <see cref="CompactJsonFormatter"/> and appends a visual separator line after each JSON
/// event, so a human scrolling technical-*.log/errors-*.log in Notepad can see where one request's
/// entry ends and the next begins - the raw JSON alone reads as one giant unbroken wall of text.
/// </summary>
public sealed class SeparatedCompactJsonFormatter : ITextFormatter
{
    private const string Separator = "================================================================================";

    private readonly CompactJsonFormatter _inner = new();

    public void Format(LogEvent logEvent, TextWriter output)
    {
        _inner.Format(logEvent, output);
        output.Write(Separator);
        output.Write(Environment.NewLine);
    }
}
