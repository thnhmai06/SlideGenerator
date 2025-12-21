using SlideGenerator.Application.Base.DTOs.Responses;

namespace SlideGenerator.Application.Slide.DTOs.Responses.Successes.Group;

/// <summary>
///     Response for group creation.
/// </summary>
public sealed record SlideGroupCreateSuccess(
    string GroupId,
    string OutputFolder,
    IReadOnlyDictionary<string, string> JobIds)
    : Response("groupcreate");