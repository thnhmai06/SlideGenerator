using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Services.Generating.Workflows.Models;
using SlideGenerator.Slides.Entities;
using Syncfusion.XlsIO;

namespace SlideGenerator.Services.Generating;

/// <summary>
///     Provides extension methods for the <see cref="GeneratingTask"/> to manage resource handles efficiently.
/// </summary>
public static class Utilities
{
    /// <summary>
    ///     Gets an existing workbook handle from the task state or opens it if not already present.
    ///     Thread-safe via ConcurrentDictionary.
    /// </summary>
    /// <param name="data">The workflow task state.</param>
    /// <param name="excelEngine">The Excel engine to use for opening the workbook.</param>
    /// <param name="identifier">The identifier of the workbook to open.</param>
    /// <returns>A handle to the opened workbook.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the workbook file does not exist.</exception>
    public static IWorkbook GetOrOpenWorkbook(this GeneratingTask data, ExcelEngine excelEngine, BookIdentifier identifier)
    {
        if (data.WorkbookHandles.TryGetValue(identifier.BookPath, out var workbook))
            return workbook;

        if (!File.Exists(identifier.BookPath))
            throw new FileNotFoundException("Workbook not found.", identifier.BookPath);

        workbook = excelEngine.Excel.Workbooks.Open(identifier.BookPath, ExcelParseOptions.Default, true, identifier.BookPassword);
        data.WorkbookHandles.TryAdd(identifier.BookPath, workbook);

        return workbook;
    }

    /// <summary>
    ///     Gets an existing template presentation handle from the task state or opens it if not already present.
    ///     Thread-safe via ConcurrentDictionary.
    /// </summary>
    /// <param name="data">The workflow task state.</param>
    /// <param name="identifier">The identifier of the presentation template to open.</param>
    /// <returns>A wrapper around the opened presentation template.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the presentation file does not exist.</exception>
    public static SfPresentation GetOrOpenTemplate(this GeneratingTask data, PresentationIdentifier identifier)
    {
        if (data.TemplateHandles.TryGetValue(identifier.PresentationPath, out var template))
            return template;

        if (!File.Exists(identifier.PresentationPath))
            throw new FileNotFoundException("Presentation template not found.", identifier.PresentationPath);

        template = new SfPresentation(identifier.PresentationPath, false, identifier.PresentationPassword);
        data.TemplateHandles.TryAdd(identifier.PresentationPath, template);

        return template;
    }
}
