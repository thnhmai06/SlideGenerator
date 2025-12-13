using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Requests;

/// <summary>
/// Base sheet request.
/// </summary>
public abstract record SheetRequest(SheetRequestType Type, string FilePath)
    : Base.DTOs.Requests.Request(RequestType.Sheet),
        IFilePathBased;