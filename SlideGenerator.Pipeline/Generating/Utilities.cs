/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Pipeline
 * File: Utilities.cs
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

using SlideGenerator.Document.Sheet.Entities;
using SlideGenerator.Document.Sheet.Models;
using SlideGenerator.Document.Slide.Entities;
using SlideGenerator.Document.Slide.Models;
using SlideGenerator.Pipeline.Generating.Workflows.Models;
using Syncfusion.XlsIO;

namespace SlideGenerator.Pipeline.Generating;

/// <summary>
///     Provides extension methods for the <see cref="GeneratingTask" /> to manage resource handles efficiently.
/// </summary>
public static class Utilities
{
    /// <param name="data">The workflow task state.</param>
    extension(GeneratingTask data)
    {
        /// <summary>
        ///     Gets an existing workbook handle from the task state or opens it if not already present.
        ///     Thread-safe via ConcurrentDictionary.
        /// </summary>
        /// <param name="excelEngine">The Excel engine to use for opening the workbook.</param>
        /// <param name="identifier">The identifier of the workbook to open.</param>
        /// <param name="isWritable">Whether the workbook should be opened in read/write mode.</param>
        /// <returns>A handle to the opened workbook.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the workbook file does not exist.</exception>
        public SfWorkbook GetOrOpenWorkbook(ExcelEngine excelEngine, BookIdentifier identifier, bool isWritable = false)
        {
            if (data.WorkbookHandles.TryGetValue(identifier, out var workbook))
                return workbook;

            if (!File.Exists(identifier.BookPath))
                throw new FileNotFoundException("Workbook not found.", identifier.BookPath);

            workbook = new SfWorkbook(excelEngine, identifier, isWritable);
            data.WorkbookHandles.TryAdd(identifier, workbook);

            return workbook;
        }

        /// <summary>
        ///     Gets an existing template presentation handle from the task state or opens it if not already present.
        ///     Thread-safe via ConcurrentDictionary.
        /// </summary>
        /// <param name="identifier">The identifier of the presentation template to open.</param>
        /// <param name="isWritable">Whether the presentation should be opened in read/write mode.</param>
        /// <returns>A wrapper around the opened presentation template.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the presentation file does not exist.</exception>
        public SfPresentation GetOrOpenPresentation(PresentationIdentifier identifier, bool isWritable = false)
        {
            if (data.TemplateHandles.TryGetValue(identifier, out var template))
                return template;

            if (!File.Exists(identifier.PresentationPath))
                throw new FileNotFoundException("Presentation template not found.", identifier.PresentationPath);

            template = new SfPresentation(identifier, isWritable);
            data.TemplateHandles.TryAdd(identifier, template);

            return template;
        }
    }
}