/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: RoiOptionJsonAdapter.cs
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
using SlideGenerator.Image.Application.Models;

namespace SlideGenerator.Stdio.Implementations.Adapters;

/// <summary>
///     A <see cref="JsonConverter{T}" /> for the polymorphic <see cref="RoiOption" /> hierarchy.
///     Uses the <c>"type"</c> discriminator field to select between
///     <see cref="AnchorOption" /> and <see cref="InterestOption" /> during deserialization.
/// </summary>
/// <example>
///     <code>
///     {"type":"Anchor","anchorType":"Eyes","anchorRatio":{"x":0,"y":0},"pivot":{"x":0.5,"y":0.333}}
///     {"type":"Interest","interestType":"Attention"}
///     </code>
/// </example>
internal sealed class RoiOptionJsonAdapter : JsonConverter<RoiOption>
{
    /// <inheritdoc />
    public override RoiOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            throw new JsonException("Missing 'type' discriminator in RoiOption.");

        var typeName = typeProp.GetString() ?? throw new JsonException("'type' discriminator is null.");

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
            ? Enum.Parse<AnchorType>(anchorEl.GetString()!, true)
            : AnchorType.Image;

        var anchorRatio = ReadVector2(root, "anchorRatio", Vector2.Zero);
        var pivot = ReadVector2(root, "pivot", new Vector2(0.5f, 0.5f));
        return new AnchorOption { Type = anchorType, Ratio = anchorRatio, Pivot = pivot };
    }

    private static InterestOption ReadInterest(JsonElement root)
    {
        var interestType = root.TryGetProperty("interestType", out var modeEl)
            ? Enum.Parse<InterestType>(modeEl.GetString()!, true)
            : InterestType.Attention;

        return new InterestOption { Type = interestType };
    }

    private static Vector2 ReadVector2(JsonElement root, string propertyName, Vector2 defaultValue)
    {
        if (!root.TryGetProperty(propertyName, out var el))
            return defaultValue;

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