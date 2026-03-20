using System.Collections.Concurrent;
using ClosedXML.Excel;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Tasks.Models.Sheet;
using SlideGenerator.Framework.Sheet.Services;

namespace SlideGenerator.Domain.Tasks.Activities;

using WorkbookRegistry = ConcurrentDictionary<string, (FileStream fs, XLWorkbook workbook)>;

/// <summary>
///     Loads a collection of Excel workbooks into runtime memory for downstream workflow activities.
/// </summary>
/// <remarks>
///     This activity initializes a transient workbook registry at
///     <c>WorkflowExecutionContext.TransientProperties["Workbooks"]</c>, then iterates input items with
///     <see cref="ParallelForEach{T}"/> to open each file concurrently.
/// </remarks>
public sealed class LoadWorkbooks : WorkflowBase
{
    /// <summary>
    ///     Gets or sets workbook descriptors that should be opened for the current workflow run.
    /// </summary>
    /// <remarks>
    ///     Empty, null, or non-existing paths are skipped silently by design.
    /// </remarks>
    public Input<ICollection<WorkbookIdentifier>> Workbooks { get; set; } = null!;

    /// <summary>
    ///     Builds the runtime activity graph.
    /// </summary>
    /// <remarks>
    ///     Execution order is:
    ///     <list type="number">
    ///         <item><description>Create the transient workbook registry.</description></item>
    ///         <item><description>Run <see cref="LoadWorkbook"/> for each input item in parallel.</description></item>
    ///     </list>
    ///     Inside <see cref="LoadWorkbook"/>, the current iteration item is read from Elsa variable
    ///     <c>"CurrentValue"</c>.
    /// </remarks>
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence()
        {
            Activities =
            {
                InitializeRegistry,
                new ParallelForEach<WorkbookIdentifier>(context => context.Get(Workbooks) ?? [])
                {
                    Body = LoadWorkbook,
                    Name = "IterateWorkbooks"
                }
            },
            Name = "LoadWorkbooks"
        };
    }
    
    /// <summary>
    ///     Initializes the transient workbook registry used by this workflow step.
    /// </summary>
    private static Inline InitializeRegistry => new(context =>
    {
        context.WorkflowExecutionContext.TransientProperties["Workbooks"] = new WorkbookRegistry();
    })
    {
        Name = "InitializeRegistry"
    };

    /// <summary>
    ///     Opens the current workbook item and stores it in the transient registry.
    /// </summary>
    /// <remarks>
    ///     Registry key priority is workbook internal name, then file name without extension.
    /// </remarks>
    private static Inline LoadWorkbook => new(context =>
    {
        var workbook = context.GetVariable<WorkbookIdentifier>("CurrentValue");

        if (workbook is null || string.IsNullOrEmpty(workbook.FilePath))
            return;
        if (!File.Exists(workbook.FilePath))
            return;

        var fs = new FileStream(workbook.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var xlWorkbook = new XLWorkbook(fs);
        var workbookName = xlWorkbook.GetName() ?? Path.GetFileNameWithoutExtension(workbook.FilePath);

        var openLog = (WorkbookRegistry)context.WorkflowExecutionContext.TransientProperties["Workbooks"];
        openLog[workbookName] = (fs, xlWorkbook);
    })
    {
        Name = "LoadWorkbook"
    };
}