using TaoSlideTotNghiep.Application.Slide.DTOs.Requests.Group;
using TaoSlideTotNghiep.Domain.Job.Interfaces;

namespace TaoSlideTotNghiep.Application.Job.Contracts;

public interface IJobManager
{
    IJobGroup CreateGroup(GenerateSlideGroupCreate request);
    IJobGroup? GetGroup(string groupId);
    IJobSheet? GetJob(string jobId);
    IReadOnlyDictionary<string, IJobGroup> GetAllGroups();

    void StartGroup(string groupId);
    void PauseGroup(string groupId);
    void ResumeGroup(string groupId);
    void CancelGroup(string groupId);

    void PauseJob(string jobId);
    void ResumeJob(string jobId);
    void CancelJob(string jobId);

    void PauseAll();
    void ResumeAll();
    void CancelAll();

    bool HasActiveJobs();
}