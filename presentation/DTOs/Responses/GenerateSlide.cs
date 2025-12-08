using presentation.Models.Enum;

namespace presentation.DTOs.Responses;

// ? Group

public record GenerateSlideCreate(string Path, Dictionary<string, string> JobIds) : Response.Create, IPathBased;

public record GenerateSlideGroupFinish(string Path, bool Success) : Response.Finish(Success), IPathBased;

public record GenerateSlideGroupStatus(string Path, float Percent, string? Message = null) : Response.Status(Percent, Message), IPathBased;

public record GenerateSlideGroupControl(string Path, ControlState State) : Response.Control(State), IPathBased;

public record GenerateSlideGroupError : Response.Error, IPathBased
{
    public string Path { get; init; }
    public GenerateSlideGroupError(string path, Exception exception) : base(exception)
    {
        Path = path;
    }
}

// ? Job
public record GenerateSlideJobFinish(string JobId, bool Success) : Response.Finish(Success), IJobBased;
public record GenerateSlideJobStatus(string JobId, float Percent, uint Current, uint Total, string Message) : Response.Status(Percent, Message), IJobBased;

public record GenerateSlideJobControl(string JobId, ControlState State) : Response.Control(State), IJobBased;

public record GenerateSlideError : Response.Error, IJobBased
{
    public string JobId { get; init; }

    public GenerateSlideError(string jobId, Exception exception) : base(exception)
    {
        JobId = jobId;
    }
}