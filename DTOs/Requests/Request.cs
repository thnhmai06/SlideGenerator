using presentation.Models.Enum;

namespace presentation.DTOs.Requests;

public enum RequestType
{
    Create,
    Control,
    Status
}

public abstract record Request
{
    public RequestType Type { get; init; }

    private Request(RequestType type) { Type = type; }

    public abstract record Create() : Request(RequestType.Create);
    public abstract record Control(ControlState? State = null) : Request(RequestType.Control);
    public abstract record Status() : Request(RequestType.Status);
}