/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: ISerializer.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Settings.Application.Abstractions;

/// <summary>
///     Provides an abstraction for serializing and deserializing objects.
/// </summary>
public interface ISerializer
{
    /// <summary>
    ///     Gets the standard file extension (e.g., .json, .yaml) associated with this serializer.
    /// </summary>
    public string FileExtension { get; }

    /// <summary>
    ///     Serializes an object into its string representation.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A string representation of the object.</returns>
    public string Serialize<T>(T obj);

    /// <summary>
    ///     Deserializes a string into an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type of the deserialized object.</typeparam>
    /// <param name="source">The string representation to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    public T Deserialize<T>(string source);
}