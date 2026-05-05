/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: BookType.cs
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

namespace SlideGenerator.Document.Sheet.Models;

public enum BookType
{
    Xls,
    Xlsx,
    Xlsm,
    Csv
}

public static class BookTypeExtensions
{
    public static string GetExtension(this BookType type)
    {
        return type switch
        {
            BookType.Xls => ".xls",
            BookType.Xlsx => ".xlsx",
            BookType.Xlsm => ".xlsm",
            BookType.Csv => ".csv",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static BookType FromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".xls" => BookType.Xls,
            ".xlsx" => BookType.Xlsx,
            ".xlsm" => BookType.Xlsm,
            ".csv" => BookType.Csv,
            _ => throw new ArgumentException($"Unsupported file extension: {extension}", nameof(extension))
        };
    }
}