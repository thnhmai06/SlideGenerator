/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: NodeJsonConverter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using SlideGenerator.Recipe.Domain.Models.Graphs;

namespace SlideGenerator.Recipe.Infrastructure.Adapters;

/// <summary>
///     Polymorphic STJ converter for <see cref="Node" /> — discriminates on the <c>type</c> field
///     mapped to <see cref="NodeType" />.
/// </summary>
public sealed class NodeJsonConverter : JsonConverter<Node>
{
    /// <inheritdoc />
    public override Node? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp)) return null;
        if (!Enum.TryParse<NodeType>(typeProp.GetString(), true, out var nodeType)) return null;

        var raw = root.GetRawText();
        return nodeType switch
        {
            NodeType.Workbook => JsonSerializer.Deserialize<WorkbookNode>(raw, options),
            NodeType.Worksheet => JsonSerializer.Deserialize<WorksheetNode>(raw, options),
            NodeType.Presentation => JsonSerializer.Deserialize<PresentationNode>(raw, options),
            NodeType.Slide => JsonSerializer.Deserialize<SlideNode>(raw, options),
            NodeType.Map => JsonSerializer.Deserialize<MapNode>(raw, options),
            NodeType.Comment => JsonSerializer.Deserialize<CommentNode>(raw, options),
            _ => null
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}