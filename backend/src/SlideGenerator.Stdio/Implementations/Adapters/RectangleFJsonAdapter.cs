/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: RectangleFJsonAdapter.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlideGenerator.Stdio.Implementations.Adapters;

/// <summary>
///     A <see cref="JsonConverter{T}" /> for <see cref="RectangleF" /> that serializes to and
///     from a JSON object with <c>x</c>, <c>y</c>, <c>width</c>, and <c>height</c> fields
///     expressed in points (pt).
/// </summary>
/// <example>
///     <code>
///     {"x":50.0,"y":80.0,"width":200.0,"height":200.0}
///     </code>
/// </example>
internal sealed class RectangleFJsonAdapter : JsonConverter<RectangleF>
{
    /// <inheritdoc />
    public override RectangleF Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var x = root.TryGetProperty("x", out var xEl) ? xEl.GetSingle() : 0f;
        var y = root.TryGetProperty("y", out var yEl) ? yEl.GetSingle() : 0f;
        var width = root.TryGetProperty("width", out var wEl) ? wEl.GetSingle() : 0f;
        var height = root.TryGetProperty("height", out var hEl) ? hEl.GetSingle() : 0f;

        return new RectangleF(x, y, width, height);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RectangleF value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("width", value.Width);
        writer.WriteNumber("height", value.Height);
        writer.WriteEndObject();
    }
}