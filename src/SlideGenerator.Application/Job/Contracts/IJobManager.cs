using SlideGenerator.Application.Slide.DTOs.Requests.Group;
using SlideGenerator.Domain.Job.Interfaces;

namespace SlideGenerator.Application.Job.Contracts;

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