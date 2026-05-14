/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: RecipeSerializer.cs
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
using System.Text.Json;
using System.Text.Json.Serialization;
using SlideGenerator.Generating.Domain.Models.Recipes;
using SlideGenerator.Image.Application.Models;

namespace SlideGenerator.Generating.Infrastructure.Services;

/// <summary>
///     Produces and consumes a canonical JSON representation of <see cref="Recipe" />.
///     Sets are sorted before serialization so that identical recipes always produce the same string,
///     enabling content-based deduplication in the database.
/// </summary>
internal static class RecipeSerializer
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        IncludeFields = true,
        Converters =
        {
            new ReadOnlySetConverterFactory(),
            new RoiOptionConverter()
        }
    };

    /// <summary>Serializes <paramref name="recipe" /> to a canonical JSON string.</summary>
    public static string Serialize(Recipe recipe)
        => JsonSerializer.Serialize(Normalize(recipe));

    /// <summary>Deserializes a <see cref="Recipe" /> from a canonical JSON string produced by <see cref="Serialize" />.</summary>
    public static Recipe Deserialize(string json)
        => JsonSerializer.Deserialize<Recipe>(json, DeserializeOptions)!;

    private static object Normalize(Recipe recipe) => new
    {
        Nodes = recipe.Nodes.Select(n => new
        {
            Sheets = n.Sheets
                .OrderBy(s => s.BookPath).ThenBy(s => s.SheetName).ThenBy(s => s.BookPassword)
                .Select(s => new { s.BookPath, s.SheetName, s.BookPassword }),
            Slide = new { n.Slide.PresentationPath, n.Slide.SlideIndex, n.Slide.PresentationPassword },
            TextInstructions = n.TextInstructions.Select(t => new
            {
                Placeholders = t.Placeholders.OrderBy(p => p),
                Columns = t.Columns.Select(c => new { c.BookPath, c.SheetName, c.ColumnName, c.BookPassword })
            }),
            ImageInstructions = n.ImageInstructions.Select(i => new
            {
                Shapes = i.Shapes
                    .OrderBy(s => s.PresentationPath)
                    .ThenBy(s => s.SlideIndex)
                    .ThenBy(s => s.ShapeName)
                    .ThenBy(s => s.PresentationPassword)
                    .Select(s => new { s.PresentationPath, s.SlideIndex, s.ShapeName, s.PresentationPassword }),
                Columns = i.Columns.Select(c => new { c.BookPath, c.SheetName, c.ColumnName, c.BookPassword }),
                EditOptions = new { RoiOption = NormalizeRoiOption(i.EditOptions.RoiOption) },
                i.FallbackImagePath
            })
        })
    };

    private static object NormalizeRoiOption(RoiOption roi) => roi switch
    {
        CenterOption c => new
        {
            Type = c.Type.ToString(),
            Pivot = new { c.Pivot.X, c.Pivot.Y },
            c.UseFaceAlignment
        },
        RuleOfThirdsOption r => new
        {
            Type = r.Type.ToString(),
            Pivot = new { r.Pivot.X, r.Pivot.Y }
        },
        _ => throw new NotSupportedException($"Unknown RoiOption type: {roi.GetType().Name}")
    };

    private sealed class ReadOnlySetConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert.IsGenericType
               && typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlySet<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var elementType = typeToConvert.GetGenericArguments()[0];
            return (JsonConverter)Activator.CreateInstance(
                typeof(ReadOnlySetConverter<>).MakeGenericType(elementType))!;
        }
    }

    private sealed class ReadOnlySetConverter<T> : JsonConverter<IReadOnlySet<T>>
    {
        public override IReadOnlySet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new HashSet<T>(JsonSerializer.Deserialize<List<T>>(ref reader, options)!);

        public override void Write(Utf8JsonWriter writer, IReadOnlySet<T> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize<IEnumerable<T>>(writer, value, options);
    }

    private sealed class RoiOptionConverter : JsonConverter<RoiOption>
    {
        public override RoiOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var type = doc.RootElement.GetProperty("Type").GetString();
            return type switch
            {
                "Center" => doc.RootElement.Deserialize<CenterOption>(options)!,
                "RuleOfThirds" => doc.RootElement.Deserialize<RuleOfThirdsOption>(options)!,
                _ => throw new JsonException($"Unknown RoiOption type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, RoiOption value, JsonSerializerOptions options)
            => throw new NotSupportedException($"{nameof(RoiOptionConverter)} is used only for deserialization.");
    }
}

