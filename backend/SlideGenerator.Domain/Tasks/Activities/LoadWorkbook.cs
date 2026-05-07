using ClosedXML.Excel;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Framework.Sheet.Services;

namespace SlideGenerator.Domain.Tasks.Activities;

/// <summary>
///     Workflow step to open an Excel workbook and prepare a worksheet dictionary.
///     Opens the workbook file and creates a dictionary mapping sheet names to IXLWorksheet instances
///     for the specified list of sheets.
/// </summary>
/// <remarks>
///     <para>
///         This workflow performs the following steps:
///     </para>
///     <list type="number">
///         <item>
///             <description>
///                 <strong>Validate Inputs:</strong> Checks that WorkbookPath is provided and not empty.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Open Workbook:</strong> Calls <see cref="WorkbookService.OpenWorkbook" /> to open
///                 the Excel file in read-only mode. Supports both .xlsx and other Excel formats.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Build Worksheet Dictionary:</strong> Iterates through the SelectedSheets list
///                 and retrieves each worksheet from the workbook. Creates a dictionary mapping sheet names
///                 to IXLWorksheet instances.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Store in TransientProperties:</strong> Stores both the Workbook instance and
///                 Worksheets dictionary in workflow context's TransientProperties for use by downstream tasks.
///             </description>
///         </item>
///     </list>
///     <para>
///         The Workbook instance is not persisted to workflow state - it exists only in runtime memory
///         (TransientProperties) to avoid serialization issues with large Excel objects. Downstream tasks
///         can retrieve these objects from the same TransientProperties.
///     </para>
/// </remarks>
public sealed class LoadWorkbook : WorkflowBase
{
    /// <summary>
    ///     Input: Full path to the Excel workbook file (.xlsx, .xls, etc.).
    /// </summary>
    public Input<string> WorkbookPath { get; set; } = null!;

    /// <summary>
    ///     Input: List of worksheet names to load from the workbook.
    /// </summary>
    public Input<IReadOnlyList<string>> SelectedSheets { get; set; } = null!;

    /// <summary>
    ///     Loads the workbook and builds a worksheet dictionary stored in workflow context's TransientProperties.
    /// </summary>
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Inline(context =>
        {
            // Step 1: Validate and get workbook path
            var path = context.Get(WorkbookPath);
            if (string.IsNullOrEmpty(path))
                return;

            // Step 2: Open workbook
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var workbook = new XLWorkbook(fs);

            // Step 3: Get list of selected sheet names
            var selectedSheets = context.Get(SelectedSheets);
            if (selectedSheets == null)
                return;

            // Step 4: Build worksheet dictionary by iterating selected sheets
            var worksheetDict = new Dictionary<string, IXLWorksheet>();
            foreach (var sheetName in selectedSheets)
                if (workbook.Worksheets.TryGetWorksheet(sheetName, out var worksheet))
                    worksheetDict[sheetName] = worksheet;

            // Step 5: Store in TransientProperties (not persisted to state)
            context.WorkflowExecutionContext.TransientProperties["WorkbookFs"] = fs;
            context.WorkflowExecutionContext.TransientProperties["Workbook"] = workbook;
            context.WorkflowExecutionContext.TransientProperties["Worksheets"] = worksheetDict;
        })
        {
            Name = "LoadWorkbook"
        };
    }
}