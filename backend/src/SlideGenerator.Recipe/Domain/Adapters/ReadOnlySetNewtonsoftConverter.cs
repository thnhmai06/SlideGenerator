/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: ReadOnlySetNewtonsoftConverter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Newtonsoft.Json;

namespace SlideGenerator.Recipe.Domain.Adapters;

/// <summary>
///     Newtonsoft.Json converter that maps any <c>IReadOnlySet&lt;T&gt;</c> property to/from a JSON
///     array using <see cref="HashSet{T}" /> as the concrete (de)serialization target.
/// </summary>
internal sealed class ReadOnlySetNewtonsoftConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
        => objectType.IsGenericType
           && objectType.GetGenericTypeDefinition() == typeof(IReadOnlySet<>);

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var elementType = objectType.GetGenericArguments()[0];
        var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
        return serializer.Deserialize(reader, hashSetType);
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        => serializer.Serialize(writer, value);

    /// <inheritdoc />
    public override bool CanWrite => false;
}
