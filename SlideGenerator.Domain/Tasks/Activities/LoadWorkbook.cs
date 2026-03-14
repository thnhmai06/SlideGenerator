using ClosedXML.Excel;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace SlideGenerator.Domain.Tasks.Activities;

/// <summary>
///     Workflow step that opens a workbook file and exposes selected worksheets to downstream activities.
/// </summary>
/// <remarks>
///     <para>
///         This activity validates <see cref="WorkbookPath" />, opens the workbook with <see cref="XLWorkbook" />,
///         resolves sheets listed in <see cref="SelectedSheets" />, and stores runtime objects in
///         <c>WorkflowExecutionContext.TransientProperties</c>.
///     </para>
/// 
///     <para>
///        The following keys are written to:
///        <code>context.WorkflowExecutionContext.TransientProperties</code>
///        <list>
///           <item>+ <c>WorkbookFs</c>: the open <see cref="FileStream" /> for the workbook file.</item>
///           <item>+ <c>Workbook</c>: the opened <see cref="IXLWorkbook" /> instance.</item>
///           <item>+ <c>Worksheets</c>: <see cref="Dictionary{TKey,TValue}" /> of sheet name to <see cref="IXLWorksheet" />.</item>
///        </list>
///     </para>
/// </remarks>
public sealed class LoadWorkbook : WorkflowBase
{
    /// <summary>
    ///     Input: Full path to the workbook file.
    /// </summary>
    public Input<string> WorkbookPath { get; set; } = null!;

    /// <summary>
    ///     Input: Worksheet names to load into the runtime worksheet dictionary.
    /// </summary>
    public Input<IReadOnlyList<string>> SelectedSheets { get; set; } = null!;

    /// <summary>
    ///     Opens the workbook and stores workbook-related runtime objects in transient properties.
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