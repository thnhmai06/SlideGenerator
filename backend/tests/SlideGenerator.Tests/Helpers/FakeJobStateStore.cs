using SlideGenerator.Domain.Features.Jobs.Enums;
using SlideGenerator.Domain.Features.Jobs.Interfaces;
using SlideGenerator.Domain.Features.Jobs.States;

namespace SlideGenerator.Tests.Helpers;

internal sealed class FakeJobStateStore : IJobStateStore
{
    private readonly Dictionary<string, GroupJobState> _groups = new();
    private readonly Dictionary<string, List<JobLogEntry>> _logs = new();
    private readonly Dictionary<string, SheetJobState> _sheets = new();

    public Task SaveGroupAsync(GroupJobState state, CancellationToken cancellationToken)
    {
        _groups[state.Id] = state;
        return Task.CompletedTask;
    }

    public Task SaveSheetAsync(SheetJobState state, CancellationToken cancellationToken)
    {
        _sheets[state.Id] = state;
        return Task.CompletedTask;
    }

    public Task<GroupJobState?> GetGroupAsync(string groupId, CancellationToken cancellationToken)
    {
        _groups.TryGetValue(groupId, out var state);
        return Task.FromResult(state);
    }

    public Task<SheetJobState?> GetSheetAsync(string sheetId, CancellationToken cancellationToken)
    {
        _sheets.TryGetValue(sheetId, out var state);
        return Task.FromResult(state);
    }

    public Task<IReadOnlyList<GroupJobState>> GetActiveGroupsAsync(CancellationToken cancellationToken)
    {
        var result = _groups.Values.Where(g => IsActive(g.Status)).ToList();
        return Task.FromResult<IReadOnlyList<GroupJobState>>(result);
    }

    public Task<IReadOnlyList<GroupJobState>> GetAllGroupsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<GroupJobState>>(_groups.Values.ToList());
    }

    public Task AppendJobLogAsync(JobLogEntry entry, CancellationToken cancellationToken)
    {
        return AppendJobLogsAsync([entry], cancellationToken);
    }

    public Task AppendJobLogsAsync(IReadOnlyCollection<JobLogEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
            return Task.CompletedTask;

        foreach (var entry in entries)
        {
            if (!_logs.TryGetValue(entry.JobId, out var list))
            {
                list = new List<JobLogEntry>();
                _logs[entry.JobId] = list;
            }

            list.Add(entry);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JobLogEntry>> GetJobLogsAsync(string jobId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<JobLogEntry>>(
            _logs.TryGetValue(jobId, out var list) ? list : []);
    }

    public Task<IReadOnlyList<SheetJobState>> GetSheetsByGroupAsync(string groupId,
        CancellationToken cancellationToken)
    {
        var result = _sheets.Values.Where(s => s.GroupId == groupId).ToList();
        return Task.FromResult<IReadOnlyList<SheetJobState>>(result);
    }

    public Task RemoveGroupAsync(string groupId, CancellationToken cancellationToken)
    {
        _groups.Remove(groupId);
        foreach (var sheetId in _sheets.Values.Where(s => s.GroupId == groupId).Select(s => s.Id))
        {
            _sheets.Remove(sheetId);
            _logs.Remove(sheetId);
        }

        return Task.CompletedTask;
    }

    public Task RemoveSheetAsync(string sheetId, CancellationToken cancellationToken)
    {
        _sheets.Remove(sheetId);
        _logs.Remove(sheetId);
        return Task.CompletedTask;
    }

    private static bool IsActive(GroupStatus status)
    {
        return status is GroupStatus.Pending or GroupStatus.Running or GroupStatus.Paused;
    }
}