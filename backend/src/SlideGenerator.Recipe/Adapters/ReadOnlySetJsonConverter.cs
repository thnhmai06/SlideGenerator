/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: ReadOnlySetJsonConverter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlideGenerator.Recipe.Adapters;

/// <summary>
///     STJ converter that maps <see cref="IReadOnlySet{T}" /> to/from a JSON array using
///     <see cref="HashSet{T}" /> as the concrete deserialisation target.
/// </summary>
internal sealed class ReadOnlySetJsonConverter<T> : JsonConverter<IReadOnlySet<T>>
{
    /// <inheritdoc />
    public override IReadOnlySet<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<HashSet<T>>(ref reader, options);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlySet<T> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

/// <summary>
///     Factory that produces <see cref="ReadOnlySetJsonConverter{T}" /> instances for any
///     <see cref="IReadOnlySet{T}" /> type encountered during (de)serialization.
/// </summary>
public sealed class ReadOnlySetJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType
           && typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlySet<>);

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ReadOnlySetJsonConverter<>).MakeGenericType(elementType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
