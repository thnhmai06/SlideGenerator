/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: Vector2JsonConverter.cs
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

namespace SlideGenerator.Stdio.Implementations.Adapters;

/// <summary>
///     STJ converter for <see cref="Vector2" /> using lowercase <c>x</c>/<c>y</c> keys.
/// </summary>
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