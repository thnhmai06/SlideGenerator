/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Utilities.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Sheet;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Domain.Models.Contexts;

namespace SlideGenerator.Generator.Application;

/// <summary>
///     Provides extension methods for the <see cref="GeneratingContext" /> to manage resource handles efficiently.
/// </summary>
public static class Utilities
{
    /// <param name="data">The workflow context state.</param>
    extension(GeneratingContext data)
    {
        /// <summary>
        ///     Gets an existing workbook handle from the context or opens it if not already present.
        ///     Thread-safe via ConcurrentDictionary.
        /// </summary>
        /// <param name="workbookProvider">The provider used to open new workbooks.</param>
        /// <param name="identifier">The identifier of the workbook to open.</param>
        /// <returns>A read-only handle to the opened workbook.</returns>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the workbook file does not exist.</exception>
        public IReadOnlyWorkbook GetOrOpenWorkbook(IWorkbookProvider workbookProvider, BookIdentifier identifier)
        {
            if (data.WorkbookHandles.TryGetValue(identifier, out var workbook))
                return workbook;

            if (!File.Exists(identifier.BookPath))
                throw new FileNotFoundException("Workbooks not found.", identifier.BookPath);

            var lazy = data.WorkbookFactories.GetOrAdd(identifier, id => new Lazy<IReadOnlyWorkbook>(() =>
            {
                try
                {
                    return workbookProvider.OpenWorkbookReadOnly(id);
                }
                catch
                {
                    data.WorkbookFactories.TryRemove(id, out _);
                    throw;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication));
            var opened = lazy.Value;
            data.WorkbookHandles.TryAdd(identifier, opened);
            return opened;
        }

        /// <summary>
        ///     Gets an existing template presentation handle from the context or opens it if not already present.
        ///     Thread-safe via ConcurrentDictionary.
        /// </summary>
        /// <param name="presentationProvider">The provider used to open new presentations.</param>
        /// <param name="identifier">The identifier of the presentation template to open.</param>
        /// <returns>A read-only handle to the opened presentation template.</returns>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the presentation file does not exist.</exception>
        public IReadOnlyPresentation GetOrOpenPresentation(IPresentationProvider presentationProvider,
            PresentationIdentifier identifier)
        {
            if (data.TemplateHandles.TryGetValue(identifier, out var template))
                return template;

            if (!File.Exists(identifier.PresentationPath))
                throw new FileNotFoundException("Presentations template not found.", identifier.PresentationPath);

            var lazy = data.TemplateFactories.GetOrAdd(identifier, id => new Lazy<IReadOnlyPresentation>(() =>
            {
                try
                {
                    return presentationProvider.OpenPresentationReadOnly(id);
                }
                catch
                {
                    data.TemplateFactories.TryRemove(id, out _);
                    throw;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication));
            var opened = lazy.Value;
            data.TemplateHandles.TryAdd(identifier, opened);
            return opened;
        }

        /// <summary>
        ///     Gets an existing output presentation handle from the context or opens it if not already present.
        ///     Supports lazy reopen after persistence resume.
        ///     Thread-safe via ConcurrentDictionary.
        /// </summary>
        /// <param name="presentationProvider">The provider used to open presentations.</param>
        /// <param name="identifier">The identifier of the output presentation to open.</param>
        /// <returns>A writable handle to the opened output presentation.</returns>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if the output file does not exist.</exception>
        public IPresentation GetOrOpenOutput(IPresentationProvider presentationProvider,
            PresentationIdentifier identifier)
        {
            if (data.OutputHandles.TryGetValue(identifier, out var output))
                return output;

            if (!File.Exists(identifier.PresentationPath))
                throw new FileNotFoundException("Output presentation not found.", identifier.PresentationPath);

            var lazy = data.OutputFactories.GetOrAdd(identifier, id => new Lazy<IPresentation>(() =>
            {
                try
                {
                    return presentationProvider.OpenPresentation(id);
                }
                catch
                {
                    data.OutputFactories.TryRemove(id, out _);
                    throw;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication));
            var opened = lazy.Value;
            data.OutputHandles.TryAdd(identifier, opened);
            return opened;
        }
    }
}