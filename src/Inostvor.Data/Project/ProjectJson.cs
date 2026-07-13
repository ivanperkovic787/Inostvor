using System.Text.Json;
using System.Text.Json.Serialization;
using Inostvor.Kernel.Primitives;

namespace Inostvor.Data.Project;

/// <summary>
/// Jedine JSON opcije za format projekta — FIKSIRANE (enumi kao stringovi,
/// uvlačenje, ignoriranje null): stabilnost formata kroz godine je ugovor.
///
/// ISegment je polimorfan (LineSeg/ArcSeg) pa nosi diskriminator "$kind" —
/// nužno za serijalizaciju ToolpathCachea.
/// </summary>
internal static class ProjectJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new SegmentJsonConverter() },
    };
}

/// <summary>Polimorfna serijalizacija segmenata (linija/luk) za cache putanje.</summary>
internal sealed class SegmentJsonConverter : JsonConverter<ISegment>
{
    public override ISegment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var kind = root.GetProperty("$kind").GetString();

        switch (kind)
        {
            case "line":
                return new LineSeg(ReadPoint(root, "start"), ReadPoint(root, "end"));

            case "arc":
                return new ArcSeg(
                    ReadPoint(root, "center"),
                    root.GetProperty("radius").GetDouble(),
                    root.GetProperty("startAngle").GetDouble(),
                    root.GetProperty("sweepAngle").GetDouble());

            default:
                throw new JsonException($"Nepoznata vrsta segmenta: '{kind}'.");
        }
    }

    public override void Write(Utf8JsonWriter writer, ISegment value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        switch (value)
        {
            case LineSeg line:
                writer.WriteString("$kind", "line");
                WritePoint(writer, "start", line.StartPoint);
                WritePoint(writer, "end", line.EndPoint);
                break;

            case ArcSeg arc:
                writer.WriteString("$kind", "arc");
                WritePoint(writer, "center", arc.Center);
                writer.WriteNumber("radius", arc.Radius);
                writer.WriteNumber("startAngle", arc.StartAngle);
                writer.WriteNumber("sweepAngle", arc.SweepAngle);
                break;

            default:
                throw new JsonException($"Segment tipa {value.GetType().Name} nije podržan u cacheu.");
        }

        writer.WriteEndObject();
    }

    private static Point2 ReadPoint(JsonElement root, string name)
    {
        var element = root.GetProperty(name);
        return new Point2(element.GetProperty("x").GetDouble(), element.GetProperty("y").GetDouble());
    }

    private static void WritePoint(Utf8JsonWriter writer, string name, Point2 point)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("x", point.X);
        writer.WriteNumber("y", point.Y);
        writer.WriteEndObject();
    }
}
