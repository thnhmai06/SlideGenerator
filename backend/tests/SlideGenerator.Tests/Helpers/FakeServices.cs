using SlideGenerator.Application.Job.Contracts;
using SlideGenerator.Application.Job.Contracts.Collections;
using SlideGenerator.Application.Sheet;
using SlideGenerator.Application.Slide;
using SlideGenerator.Application.Slide.DTOs.Requests.Group;
using SlideGenerator.Domain.Job.Components;
using SlideGenerator.Domain.Job.Entities;
using SlideGenerator.Domain.Job.Enums;
using SlideGenerator.Domain.Job.Interfaces;
using SlideGenerator.Domain.Sheet.Interfaces;
using SlideGenerator.Domain.Slide;

namespace SlideGenerator.Tests.Helpers;

internal sealed class FakeSheetService : ISheetService
{
    private readonly Dictionary<string, ISheetBook> _workbooks = new();

    public FakeSheetService(ISheetBook workbook)
    {
        _workbooks[workbook.FilePath] = workbook;
    }

    public ISheetBook OpenFile(string filePath)
    {
        if (_workbooks.TryGetValue(filePath, out var book))
            return book;

        var sheet = new TestSheet("Sheet1", 1);
        var created = new TestSheetBook(filePath, sheet);
        _workbooks[filePath] = created;
        return created;
    }

    public IReadOnlyDictionary<string, int> GetSheetsInfo(ISheetBook group) => group.GetSheetsInfo();

    public IReadOnlyList<string?> GetHeaders(ISheetBook group, string tableName)
    {
        return group.Worksheets.TryGetValue(tableName, out var sheet)
            ? sheet.Headers
            : [];
    }

    public Dictionary<string, string?> GetRow(ISheetBook group, string tableName, int rowNumber)
    {
        return group.Worksheets.TryGetValue(tableName, out var sheet)
            ? sheet.GetRow(rowNumber)
            : new Dictionary<string, string?>();
    }
}

internal sealed class FakeSlideTemplateManager : ISlideTemplateManager
{
    private readonly Dictionary<string, ITemplatePresentation> _templates = new();

    public FakeSlideTemplateManager(ITemplatePresentation template)
    {
        _templates[template.FilePath] = template;
    }

    public bool AddTemplate(string filepath)
    {
        if (_templates.ContainsKey(filepath))
            return false;
        _templates[filepath] = new TestTemplatePresentation(filepath);
        return true;
    }

    public bool RemoveTemplate(string filepath)
    {
        return _templates.Remove(filepath);
    }

    public ITemplatePresentation GetTemplate(string filepath)
    {
        return _templates[filepath];
    }
}

internal sealed class FakeActiveJobCollection : IActiveJobCollection
{
    private readonly Dictionary<string, JobGroup> _groups = new();
    private readonly Dictionary<string, JobSheet> _sheets = new();

    public IJobGroup CreateGroup(GenerateSlideGroupCreate request)
    {
        var workbook = CreateWorkbook();
        var template = new TestTemplatePresentation(request.GetTemplatePath());
        var outputRoot = string.IsNullOrWhiteSpace(request.GetOutputPath())
            ? Path.GetTempPath()
            : request.GetOutputPath();
        var outputFolder = new DirectoryInfo(outputRoot);
        var group = new JobGroup(workbook, template, outputFolder, [], []);

        var requestedSheets = request.SheetNames ?? request.CustomSheet;
        var sheetNames = requestedSheets?.Length > 0
            ? requestedSheets
            : workbook.Worksheets.Keys.ToArray();

        foreach (var sheetName in sheetNames)
        {
            if (!workbook.Worksheets.ContainsKey(sheetName))
                continue;

            var outputPath = Path.Combine(outputFolder.FullName, $"{sheetName}.pptx");
            var sheet = group.AddJob(sheetName, outputPath);
            _sheets[sheet.Id] = sheet;
        }

        _groups[group.Id] = group;
        return group;
    }

    public void StartGroup(string groupId)
    {
        if (_groups.TryGetValue(groupId, out var group))
            group.SetStatus(GroupStatus.Running);
    }

    public void PauseGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;
        foreach (var sheet in group.InternalJobs.Values)
            sheet.SetStatus(SheetJobStatus.Paused);
        group.SetStatus(GroupStatus.Paused);
    }

    public void ResumeGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;
        foreach (var sheet in group.InternalJobs.Values.Where(s => s.Status == SheetJobStatus.Paused))
            sheet.SetStatus(SheetJobStatus.Running);
        group.SetStatus(GroupStatus.Running);
    }

    public void CancelGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;
        foreach (var sheet in group.InternalJobs.Values)
            sheet.SetStatus(SheetJobStatus.Cancelled);
        group.SetStatus(GroupStatus.Cancelled);
    }

    public void PauseSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var sheet))
            sheet.SetStatus(SheetJobStatus.Paused);
    }

    public void ResumeSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var sheet))
            sheet.SetStatus(SheetJobStatus.Running);
    }

    public void CancelSheet(string sheetId)
    {
        if (_sheets.TryGetValue(sheetId, out var sheet))
            sheet.SetStatus(SheetJobStatus.Cancelled);
    }

    public void PauseAll()
    {
        foreach (var group in _groups.Values)
            PauseGroup(group.Id);
    }

    public void ResumeAll()
    {
        foreach (var group in _groups.Values)
            ResumeGroup(group.Id);
    }

    public void CancelAll()
    {
        foreach (var group in _groups.Values)
            CancelGroup(group.Id);
    }

    public bool HasActiveJobs => _groups.Values.Any(g =>
        g.Status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused);

    public IReadOnlyDictionary<string, IJobGroup> GetRunningGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Running)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetPausedGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Paused)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetPendingGroups()
    {
        return _groups.Where(kv => kv.Value.Status == GroupStatus.Pending)
            .ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public IJobGroup? GetGroup(string groupId) => _groups.GetValueOrDefault(groupId);
    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups() =>
        _groups.ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    public int GroupCount => _groups.Count;
    public IJobSheet? GetSheet(string sheetId) => _sheets.GetValueOrDefault(sheetId);
    public IReadOnlyDictionary<string, IJobSheet> GetAllSheets() =>
        _sheets.ToDictionary(kv => kv.Key, kv => (IJobSheet)kv.Value);
    public int SheetCount => _sheets.Count;
    public bool ContainsGroup(string groupId) => _groups.ContainsKey(groupId);
    public bool ContainsSheet(string sheetId) => _sheets.ContainsKey(sheetId);
    public bool IsEmpty => _groups.Count == 0;

    private static ISheetBook CreateWorkbook()
    {
        var sheet1 = new TestSheet("Sheet1", 3);
        var sheet2 = new TestSheet("Sheet2", 2);
        return new TestSheetBook("book.xlsx", sheet1, sheet2);
    }
}

internal sealed class FakeCompletedJobCollection : ICompletedJobCollection
{
    public IJobGroup? GetGroup(string groupId) => null;
    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups() => new Dictionary<string, IJobGroup>();
    public int GroupCount => 0;
    public IJobSheet? GetSheet(string sheetId) => null;
    public IReadOnlyDictionary<string, IJobSheet> GetAllSheets() => new Dictionary<string, IJobSheet>();
    public int SheetCount => 0;
    public bool ContainsGroup(string groupId) => false;
    public bool ContainsSheet(string sheetId) => false;
    public bool IsEmpty => true;
    public bool RemoveGroup(string groupId) => false;
    public bool RemoveSheet(string sheetId) => false;
    public void ClearAll()
    {
    }
    public IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups() => new Dictionary<string, IJobGroup>();
    public IReadOnlyDictionary<string, IJobGroup> GetFailedGroups() => new Dictionary<string, IJobGroup>();
    public IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups() => new Dictionary<string, IJobGroup>();
}

internal sealed class FakeJobManager : IJobManager
{
    public FakeJobManager(IActiveJobCollection active)
    {
        Active = active;
        Completed = new FakeCompletedJobCollection();
    }

    public IActiveJobCollection Active { get; }
    public ICompletedJobCollection Completed { get; }

    public IJobGroup? GetGroup(string groupId)
    {
        return Active.GetGroup(groupId);
    }

    public IJobSheet? GetSheet(string sheetId)
    {
        return Active.GetSheet(sheetId);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
    {
        return Active.GetAllGroups();
    }
}
