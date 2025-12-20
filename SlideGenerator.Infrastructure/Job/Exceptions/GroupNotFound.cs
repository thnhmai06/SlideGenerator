namespace SlideGenerator.Infrastructure.Job.Exceptions;

public class GroupNotFound(string groupId)
    : InvalidOperationException($"Group '{groupId}' not found.")
{
    public string GroupId { get; } = groupId;
}