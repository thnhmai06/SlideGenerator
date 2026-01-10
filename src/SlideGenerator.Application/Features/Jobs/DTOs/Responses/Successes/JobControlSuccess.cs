using System.Text.Json.Serialization;
using SlideGenerator.Application.Common.Base.DTOs.Responses;
using SlideGenerator.Application.Features.Slides.DTOs.Enums;
using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Application.Features.Jobs.DTOs.Responses.Successes;

/// <summary>
///     Response for job control.
/// </summary>
public sealed record JobControlSuccess(
    [property: JsonPropertyName("TaskId")] string JobId,
    [property: JsonPropertyName("TaskType")]
    JobType JobType,
    ControlAction Action)
    : Response("jobcontrol");