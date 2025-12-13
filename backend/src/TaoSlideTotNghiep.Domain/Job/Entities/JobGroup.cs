using System.Collections.Concurrent;
using TaoSlideTotNghiep.Domain.Job.Interfaces;
using TaoSlideTotNghiep.Domain.Sheet.Enums;
using TaoSlideTotNghiep.Domain.Sheet.Interfaces;
using TaoSlideTotNghiep.Domain.Slide.Components;
using TaoSlideTotNghiep.Domain.Slide.Interfaces;

namespace TaoSlideTotNghiep.Domain.Job.Entities;

public class JobGroup(
    ISheetBook workbook,
    ITemplatePresentation template,
    string outputFolder,
    TextConfig[] textConfigs,
    ImageConfig[] imageConfigs)
    : IJobGroup
{
    private readonly ConcurrentDictionary<string, JobSheet> _jobs = new();

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public ISheetBook Workbook { get; } = workbook;
    public ITemplatePresentation Template { get; } = template;
    public string OutputFolder { get; } = outputFolder;
    public GroupStatus Status { get; private set; } = GroupStatus.Pending;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; private set; }

    public float Progress
    {
        get
        {
            if (_jobs.IsEmpty) return 0;
            return _jobs.Values.Sum(j => j.Progress) / _jobs.Count;
        }
    }

    public IReadOnlyDictionary<string, IJobSheet> Jobs =>
        field ??= new ReadOnlyJobs(_jobs);

    public IReadOnlyDictionary<string, JobSheet> InternalJobs => _jobs;

    public TextConfig[] TextConfigs { get; } = textConfigs;
    public ImageConfig[] ImageConfigs { get; } = imageConfigs;

    public JobSheet AddJob(string sheetName, string outputPath)
    {
        var worksheet = Workbook.Sheets[sheetName];
        var job = new JobSheet(this, worksheet, outputPath);
        _jobs[job.Id] = job;
        return job;
    }

    public JobSheet? GetJob(string jobId)
    {
        return _jobs.GetValueOrDefault(jobId);
    }

    public void SetStatus(GroupStatus status)
    {
        Status = status;
        if (status is GroupStatus.Completed or GroupStatus.Failed or GroupStatus.Cancelled)
            FinishedAt = DateTime.UtcNow;
    }

    public void UpdateStatus()
    {
        var jobs = _jobs.Values;

        if (jobs.All(j => j.Status == SheetJobStatus.Completed))
            SetStatus(GroupStatus.Completed);
        else if (jobs.Any(j => j.Status == SheetJobStatus.Failed))
            SetStatus(GroupStatus.Failed);
        else if (jobs.All(j => j.Status == SheetJobStatus.Cancelled))
            SetStatus(GroupStatus.Cancelled);
        else if (jobs.Any(j => j.Status == SheetJobStatus.Paused) &&
                 jobs.All(j => j.Status is SheetJobStatus.Paused or SheetJobStatus.Pending or SheetJobStatus.Completed))
            SetStatus(GroupStatus.Paused);
        else if (jobs.Any(j => j.Status == SheetJobStatus.Running))
            SetStatus(GroupStatus.Running);
    }

    private sealed class ReadOnlyJobs(ConcurrentDictionary<string, JobSheet> source)
        : IReadOnlyDictionary<string, IJobSheet>
    {
        public IJobSheet this[string key] => source[key];
        public IEnumerable<string> Keys => source.Keys;
        public IEnumerable<IJobSheet> Values => source.Values;
        public int Count => source.Count;

        public bool ContainsKey(string key)
        {
            return source.ContainsKey(key);
        }

        public bool TryGetValue(string key, out IJobSheet value)
        {
            if (source.TryGetValue(key, out var job))
            {
                value = job;
                return true;
            }

            value = null!;
            return false;
        }

        public IEnumerator<KeyValuePair<string, IJobSheet>> GetEnumerator()
        {
            return source.Select(kv => new KeyValuePair<string, IJobSheet>(kv.Key, kv.Value)).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}