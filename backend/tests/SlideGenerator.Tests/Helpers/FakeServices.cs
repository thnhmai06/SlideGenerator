using System.Drawing;
using SlideGenerator.Application.Common.Utilities;
using SlideGenerator.Application.Features.Images;
using SlideGenerator.Application.Features.Jobs.Contracts;
using SlideGenerator.Application.Features.Jobs.Contracts.Collections;
using SlideGenerator.Application.Features.Jobs.DTOs.Requests;
using SlideGenerator.Application.Features.Sheets;
using SlideGenerator.Application.Features.Slides;
using SlideGenerator.Domain.Features.Images.Enums;
using SlideGenerator.Domain.Features.Jobs.Entities;
using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Jobs.Interfaces;
using SlideGenerator.Domain.Features.Sheets.Interfaces;
using SlideGenerator.Domain.Features.Slides;

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

    public IReadOnlyDictionary<string, int> GetSheetsInfo(ISheetBook group)
    {
        return group.GetSheetsInfo();
    }

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

    public void CancelAndRemoveGroup(string groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return;
        foreach (var sheet in group.InternalJobs.Values)
            _sheets.Remove(sheet.Id);
        _groups.Remove(groupId);
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

    public void CancelAndRemoveSheet(string sheetId)
    {
        if (_sheets.Remove(sheetId, out var sheet))
            if (_groups.TryGetValue(sheet.GroupId, out var group))
                group.RemoveJob(sheet.Id);
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

    public IJobGroup? GetGroup(string groupId)
    {
        return _groups.GetValueOrDefault(groupId);
    }

    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
    {
        return _groups.ToDictionary(kv => kv.Key, kv => (IJobGroup)kv.Value);
    }

    public IEnumerable<IJobGroup> EnumerateGroups()
    {
        return _groups.Values;
    }

    public int GroupCount => _groups.Count;

    public IJobSheet? GetSheet(string sheetId)
    {
        return _sheets.GetValueOrDefault(sheetId);
    }

    public IReadOnlyDictionary<string, IJobSheet> GetAllSheets()
    {
        return _sheets.ToDictionary(kv => kv.Key, kv => (IJobSheet)kv.Value);
    }

    public IEnumerable<IJobSheet> EnumerateSheets()
    {
        return _sheets.Values;
    }

    public int SheetCount => _sheets.Count;

    public bool ContainsGroup(string groupId)
    {
        return _groups.ContainsKey(groupId);
    }

    public bool ContainsSheet(string sheetId)
    {
        return _sheets.ContainsKey(sheetId);
    }

    public bool IsEmpty => _groups.Count == 0;

    public IJobGroup? GetGroupByOutputPath(string outputFolderPath)
    {
        var normalized = OutputPathUtils.NormalizeOutputFolderPath(outputFolderPath);
        return _groups.Values.FirstOrDefault(group =>
            string.Equals(group.OutputFolder.FullName, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public IJobGroup CreateGroup(JobCreate request)
    {
        var workbook = CreateWorkbook();
        var template = new TestTemplatePresentation(request.TemplatePath);
        var outputRoot = string.IsNullOrWhiteSpace(request.OutputPath)
            ? Path.GetTempPath()
            : request.OutputPath;
        var fullOutputPath = Path.GetFullPath(outputRoot);
        var outputFolderPath = OutputPathUtils.NormalizeOutputFolderPath(fullOutputPath);
        var outputFolder = new DirectoryInfo(outputFolderPath);
        var group = new JobGroup(workbook, template, outputFolder, [], []);

        string[] sheetNames;
        if (request.JobType == JobType.Sheet)
        {
            if (string.IsNullOrWhiteSpace(request.SheetName))
                throw new InvalidOperationException("SheetName is required for sheet jobs.");
            sheetNames = [request.SheetName];
        }
        else
        {
            sheetNames = request.SheetNames?.Length > 0
                ? request.SheetNames
                : workbook.Worksheets.Keys.ToArray();
        }

        var outputOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (HasPptxExtension(fullOutputPath) && sheetNames.Length == 1)
            outputOverrides[sheetNames[0]] = fullOutputPath;

        foreach (var sheetName in sheetNames)
        {
            if (!workbook.Worksheets.ContainsKey(sheetName))
                continue;

            var outputPath = outputOverrides.TryGetValue(sheetName, out var overridePath)
                ? overridePath
                : Path.Combine(outputFolder.FullName, $"{sheetName}.pptx");
            var sheet = group.AddJob(sheetName, outputPath);
            _sheets[sheet.Id] = sheet;
        }

        _groups[group.Id] = group;
        return group;
    }

    private static ISheetBook CreateWorkbook()
    {
        var sheet1 = new TestSheet("Sheet1", 3);
        var sheet2 = new TestSheet("Sheet2", 2);
        return new TestSheetBook("book.xlsx", sheet1, sheet2);
    }

    private static bool HasPptxExtension(string path)
    {
        return string.Equals(Path.GetExtension(path), ".pptx", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class FakeCompletedJobCollection : ICompletedJobCollection
{
    public IJobGroup? GetGroup(string groupId)
    {
        return null;
    }

    public IReadOnlyDictionary<string, IJobGroup> GetAllGroups()
    {
        return new Dictionary<string, IJobGroup>();
    }

    public IEnumerable<IJobGroup> EnumerateGroups()
    {
        return Array.Empty<IJobGroup>();
    }

    public int GroupCount => 0;

    public IJobSheet? GetSheet(string sheetId)
    {
        return null;
    }

    public IReadOnlyDictionary<string, IJobSheet> GetAllSheets()
    {
        return new Dictionary<string, IJobSheet>();
    }

    public IEnumerable<IJobSheet> EnumerateSheets()
    {
        return Array.Empty<IJobSheet>();
    }

    public int SheetCount => 0;

    public bool ContainsGroup(string groupId)
    {
        return false;
    }

    public bool ContainsSheet(string sheetId)
    {
        return false;
    }

    public bool IsEmpty => true;

    public bool RemoveGroup(string groupId)
    {
        return false;
    }

    public bool RemoveSheet(string sheetId)
    {
        return false;
    }

    public void ClearAll()
    {
    }

    public IReadOnlyDictionary<string, IJobGroup> GetSuccessfulGroups()
    {
        return new Dictionary<string, IJobGroup>();
    }

    public IReadOnlyDictionary<string, IJobGroup> GetFailedGroups()
    {
        return new Dictionary<string, IJobGroup>();
    }

    public IReadOnlyDictionary<string, IJobGroup> GetCancelledGroups()
    {
        return new Dictionary<string, IJobGroup>();
    }
}

internal sealed class FakeJobManager(IActiveJobCollection active) : IJobManager
{
    public IActiveJobCollection Active { get; } = active;
    public ICompletedJobCollection Completed { get; } = new FakeCompletedJobCollection();

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

internal sealed class FakeImageService : IImageService
{
    public bool IsFaceModelAvailable { get; private set; }

    public Task<byte[]> CropImageAsync(string filePath, Size size, ImageRoiType roiType, ImageCropType cropType)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    public Task<bool> InitFaceModelAsync()
    {
        IsFaceModelAvailable = true;
        return Task.FromResult(true);
    }

    public Task<bool> DeInitFaceModelAsync()
    {
        IsFaceModelAvailable = false;
        return Task.FromResult(true);
    }
}