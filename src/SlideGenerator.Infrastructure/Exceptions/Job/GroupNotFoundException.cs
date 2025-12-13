namespace SlideGenerator.Infrastructure.Exceptions.Job;

public class GroupNotFoundException(string groupId)
    : InvalidOperationException($"Group '{groupId}' not found.")
{
    public string GroupId { get; } = groupId;
}