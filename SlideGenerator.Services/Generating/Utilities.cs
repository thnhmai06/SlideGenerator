using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Services.Generating.Workflows.Models;
using SlideGenerator.Slides.Entities;
using Syncfusion.XlsIO;

namespace SlideGenerator.Services.Generating;

/// <summary>
///     Provides common utility methods for the generating workflow.
/// </summary>
public static class Utilities
{
    extension(GeneratingTask data)
    {
        /// <summary>
        ///     Gets an existing workbook handle or opens it if not already present.
        /// </summary>
        public IWorkbook GetOrOpenWorkbook(ExcelEngine excelEngine, BookIdentifier identifier)
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
        ///     Gets an existing template presentation handle or opens it if not already present.
        /// </summary>
        public SfPresentation GetOrOpenTemplate(PresentationIdentifier identifier)
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
}
