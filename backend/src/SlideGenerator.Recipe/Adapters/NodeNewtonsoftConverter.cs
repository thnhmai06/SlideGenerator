/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: NodeNewtonsoftConverter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Recipe.Adapters;

/// <summary>
///     Newtonsoft.Json polymorphic converter for <see cref="Node" />.
///     Discriminates concrete type via the <c>type</c> field mapped to <see cref="NodeType" />.
///     Handles both <see cref="CanvasNode" /> subtypes and <see cref="ChildNode" /> subtypes.
///     Used by WorkflowCore to persist <see cref="Recipe.Nodes" /> to SQLite.
///     Uses <c>[ThreadStatic]</c> guards on <see cref="CanRead" /> / <see cref="CanWrite" />
///     to break recursion that would otherwise arise from the <c>[JsonConverter]</c> attribute
///     on the base <see cref="Node" /> type.
/// </summary>
internal sealed class NodeNewtonsoftConverter : JsonConverter<Node>
{
    [ThreadStatic]
    private static bool _reading;

    [ThreadStatic]
    private static bool _writing;

    /// <inheritdoc />
    public override bool CanRead => !_reading;

    /// <inheritdoc />
    public override bool CanWrite => !_writing;

    /// <inheritdoc />
    public override Node? ReadJson(
        JsonReader reader, Type objectType, Node? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        var typeToken = obj.GetValue("type", StringComparison.OrdinalIgnoreCase)?.Value<string>();
        if (typeToken == null || !Enum.TryParse<NodeType>(typeToken, true, out var nodeType))
            return null;

        _reading = true;
        try
        {
            return nodeType switch
            {
                NodeType.Workbook => obj.ToObject<WorkbookNode>(serializer),
                NodeType.Presentation => obj.ToObject<PresentationNode>(serializer),
                NodeType.Map => obj.ToObject<MapNode>(serializer),
                NodeType.Comment => obj.ToObject<CommentNode>(serializer),
                NodeType.Worksheet => obj.ToObject<WorksheetNode>(serializer),
                NodeType.Slide => obj.ToObject<SlideNode>(serializer),
                _ => null
            };
        }
        finally
        {
            _reading = false;
        }
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, Node? value, JsonSerializer serializer)
    {
        if (value == null) { writer.WriteNull(); return; }

        _writing = true;
        try { serializer.Serialize(writer, value); }
        finally { _writing = false; }
    }
}
