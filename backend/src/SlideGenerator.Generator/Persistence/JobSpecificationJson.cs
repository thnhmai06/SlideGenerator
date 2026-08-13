/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobSpecificationJson.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using SlideGenerator.Image.Cropping;

namespace SlideGenerator.Generator.Persistence;

/// <summary>
///     JSON (de)serialization support for the free-form columns of the <c>Jobs</c> table
///     (<c>UsedColumnsJson</c>/<c>TextInstructionsJson</c>/<c>ImageInstructionsJson</c> — see
///     <see cref="JobsRepository" />). Module-local duplicate of the equivalent converters used
///     by <c>SlideGenerator.Recipe</c>/<c>SlideGenerator.Stdio</c> for their own JSON boundaries — kept
///     separate rather than shared, since Generator cannot depend on Stdio and this is a small, cheap
///     duplication for a DB-storage concern distinct from the IPC wire format.
/// </summary>
internal static class JobSpecificationJson
{
    /// <summary>Shared options for serializing <see cref="Job.JobSpecification" />'s JSON columns.</summary>
    internal static readonly JsonSerializerOptions Options = Build();

    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new ReadOnlySetJsonConverterFactory());
        options.Converters.Add(new Vector2JsonConverter());
        options.Converters.Add(new RoiOptionJsonAdapter());
        return options;
    }
}

/// <summary>STJ converter mapping <see cref="IReadOnlySet{T}" /> to/from a JSON array via <see cref="HashSet{T}" />.</summary>
internal sealed class ReadOnlySetJsonConverter<T> : JsonConverter<IReadOnlySet<T>>
{
    /// <inheritdoc />
    public override IReadOnlySet<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<HashSet<T>>(ref reader, options);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlySet<T> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

/// <summary>Factory producing <see cref="ReadOnlySetJsonConverter{T}" /> for any <see cref="IReadOnlySet{T}" />.</summary>
internal sealed class ReadOnlySetJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlySet<>);

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter?)Activator.CreateInstance(typeof(ReadOnlySetJsonConverter<>).MakeGenericType(elementType));
    }
}

/// <summary>STJ converter for <see cref="Vector2" /> using lowercase <c>x</c>/<c>y</c> keys.</summary>
internal sealed class Vector2JsonConverter : JsonConverter<Vector2>
{
    /// <inheritdoc />
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var x = root.TryGetProperty("x", out var xEl) ? xEl.GetSingle() : 0f;
        var y = root.TryGetProperty("y", out var yEl) ? yEl.GetSingle() : 0f;
        return new Vector2(x, y);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}

/// <summary>STJ converter for the polymorphic <see cref="RoiOption" /> hierarchy, keyed by a <c>"type"</c> discriminator.</summary>
internal sealed class RoiOptionJsonAdapter : JsonConverter<RoiOption>
{
    /// <inheritdoc />
    public override RoiOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var typeName = root.TryGetProperty("type", out var typeProp)
            ? typeProp.GetString() ?? throw new JsonException("'type' discriminator is null.")
            : throw new JsonException("Missing 'type' discriminator in RoiOption.");

        return typeName switch
        {
            "Anchor" => ReadAnchor(root),
            "Interest" => ReadInterest(root),
            _ => throw new JsonException($"Unknown RoiOption type: '{typeName}'.")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RoiOption value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case AnchorOption anchor:
                writer.WriteString("type", "Anchor");
                writer.WriteString("anchorType", anchor.Type.ToString());
                WriteVector2(writer, "anchorRatio", anchor.Ratio);
                WriteVector2(writer, "pivot", anchor.Pivot);
                break;
            case InterestOption interest:
                writer.WriteString("type", "Interest");
                writer.WriteString("interestType", interest.Type.ToString());
                break;
            default:
                throw new JsonException($"Unsupported RoiOption subtype: {value.GetType().Name}.");
        }

        writer.WriteEndObject();
    }

    private static AnchorOption ReadAnchor(JsonElement root)
    {
        var anchorType = root.TryGetProperty("anchorType", out var anchorEl)
            ? System.Enum.Parse<AnchorType>(anchorEl.GetString()!, true)
            : AnchorType.Image;
        return new AnchorOption
        {
            Type = anchorType,
            Ratio = ReadVector2(root, "anchorRatio", Vector2.Zero),
            Pivot = ReadVector2(root, "pivot", new Vector2(0.5f, 0.5f))
        };
    }

    private static InterestOption ReadInterest(JsonElement root)
    {
        var interestType = root.TryGetProperty("interestType", out var modeEl)
            ? System.Enum.Parse<InterestType>(modeEl.GetString()!, true)
            : InterestType.Attention;
        return new InterestOption { Type = interestType };
    }

    private static Vector2 ReadVector2(JsonElement root, string propertyName, Vector2 defaultValue)
    {
        if (!root.TryGetProperty(propertyName, out var el)) return defaultValue;
        var x = el.TryGetProperty("x", out var xEl) ? xEl.GetSingle() : defaultValue.X;
        var y = el.TryGetProperty("y", out var yEl) ? yEl.GetSingle() : defaultValue.Y;
        return new Vector2(x, y);
    }

    private static void WriteVector2(Utf8JsonWriter writer, string propertyName, Vector2 v)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteNumber("x", v.X);
        writer.WriteNumber("y", v.Y);
        writer.WriteEndObject();
    }
}
