using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog.Events;
using Serilog.Formatting;

namespace WideEvents.Sample.Api;

public sealed class PrettyJsonFormatter : ITextFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var record = new Dictionary<string, object?>
        {
            ["timestamp"] = logEvent.Timestamp,
            ["level"] = logEvent.Level.ToString(),
            ["category"] = GetScalar(logEvent, "SourceContext"),
        };

        if (logEvent.Properties.TryGetValue("WideEvent", out var wideEvent))
        {
            record["message"] = "WideEvent";
            record["wide_event"] = ToObject(wideEvent);
        }
        else
        {
            record["message"] = logEvent.RenderMessage();
        }

        if (logEvent.Exception is not null)
            record["exception"] = logEvent.Exception.ToString();

        output.WriteLine(JsonSerializer.Serialize(record, Options));
    }

    private static string? GetScalar(LogEvent ev, string name) =>
        ev.Properties.TryGetValue(name, out var v) && v is ScalarValue s
            ? s.Value?.ToString()
            : null;

    private static object? ToObject(LogEventPropertyValue value) => value switch
    {
        ScalarValue scalar => scalar.Value,
        SequenceValue seq => seq.Elements.Select(ToObject).ToArray(),
        StructureValue structure => structure.Properties
            .ToDictionary(p => p.Name, p => ToObject(p.Value)),
        DictionaryValue dict => dict.Elements.ToDictionary(
            kv => kv.Key.Value?.ToString() ?? string.Empty,
            kv => ToObject(kv.Value)),
        _ => value.ToString(),
    };
}
