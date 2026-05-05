using SlideGenerator.Documents.Sheets.Entities;
using SlideGenerator.Documents.Sheets.Models;
using SlideGenerator.Documents.Slides.Entities;
using SlideGenerator.Documents.Slides.Models;
using SlideGenerator.Pipelines.Generating.Workflows.Models;
using Syncfusion.XlsIO;

namespace SlideGenerator.Pipelines.Generating;

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