/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: YamlSerializer.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ISerializer = SlideGenerator.Settings.Application.Abstractions.ISerializer;

namespace SlideGenerator.Settings.Infrastructure.Services;

/// <summary>
///     Implements the <see cref="ISerializer" /> abstraction using the YAML format.
///     Uses CamelCase naming conventions and ignores unmatched properties during deserialization.
/// </summary>
internal class YamlSerializer : ISerializer
{
    /// <inheritdoc />
    public string FileExtension => ".yaml";

    /// <inheritdoc />
    public T Deserialize<T>(string source)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<T>(source);
    }

    /// <inheritdoc />
    public string Serialize<T>(T obj)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(obj);
    }
}