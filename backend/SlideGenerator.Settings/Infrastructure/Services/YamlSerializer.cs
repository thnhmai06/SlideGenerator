/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: YamlSerializer.cs
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