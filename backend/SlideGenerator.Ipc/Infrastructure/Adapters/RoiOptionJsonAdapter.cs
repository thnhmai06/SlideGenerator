/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: RoiOptionJsonAdapter.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using SlideGenerator.Image.Application.Models;

namespace SlideGenerator.Ipc.Infrastructure.Adapters;

/// <summary>
///     A <see cref="JsonConverter{T}" /> for the polymorphic <see cref="RoiOption" /> hierarchy.
///     Uses the <c>"type"</c> discriminator field to select between
///     <see cref="CenterOption" /> and <see cref="RuleOfThirdsOption" /> during deserialization.
/// </summary>
/// <example>
///     <code>
///     {"type":"Center","pivot":{"x":0.5,"y":0.5},"useFaceAlignment":true}
///     {"type":"RuleOfThirds","pivot":{"x":0.5,"y":0.333}}
///     </code>
/// </example>
public sealed class RoiOptionJsonAdapter : JsonConverter<RoiOption>
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
            "Center" => ReadCenter(root),
            "RuleOfThirds" => ReadRuleOfThirds(root),
            _ => throw new JsonException($"Unknown RoiOption type: '{typeName}'.")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RoiOption value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type.ToString());

        switch (value)
        {
            case CenterOption center:
                WritePivot(writer, center.Pivot);
                writer.WriteBoolean("useFaceAlignment", center.UseFaceAlignment);
                break;
            case RuleOfThirdsOption thirds:
                WritePivot(writer, thirds.Pivot);
                break;
            default:
                throw new JsonException($"Unsupported RoiOption subtype: {value.GetType().Name}.");
        }

        writer.WriteEndObject();
    }

    private static CenterOption ReadCenter(JsonElement root)
    {
        var pivot = ReadPivot(root);
        var useFaceAlignment = root.TryGetProperty("useFaceAlignment", out var fa) && fa.GetBoolean();
        return new CenterOption { Pivot = pivot, UseFaceAlignment = useFaceAlignment };
    }

    private static RuleOfThirdsOption ReadRuleOfThirds(JsonElement root)
    {
        var pivot = ReadPivot(root);
        return new RuleOfThirdsOption { Pivot = pivot };
    }

    private static Vector2 ReadPivot(JsonElement root)
    {
        if (!root.TryGetProperty("pivot", out var pivotEl))
            return new Vector2(0.5f, 0.5f);

        var x = pivotEl.TryGetProperty("x", out var xEl) ? xEl.GetSingle() : 0.5f;
        var y = pivotEl.TryGetProperty("y", out var yEl) ? yEl.GetSingle() : 0.5f;
        return new Vector2(x, y);
    }

    private static void WritePivot(Utf8JsonWriter writer, Vector2 pivot)
    {
        writer.WriteStartObject("pivot");
        writer.WriteNumber("x", pivot.X);
        writer.WriteNumber("y", pivot.Y);
        writer.WriteEndObject();
    }
}