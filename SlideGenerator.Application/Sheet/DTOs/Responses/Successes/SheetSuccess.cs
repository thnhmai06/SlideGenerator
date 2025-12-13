using SlideGenerator.Application.Base.DTOs;
using SlideGenerator.Application.Base.DTOs.Enums;
using SlideGenerator.Application.Base.DTOs.Responses;
using SlideGenerator.Application.Sheet.DTOs.Enums;

namespace SlideGenerator.Application.Sheet.DTOs.Responses.Successes;

public abstract record SheetSuccess(string FilePath, SheetRequestType Type) : SuccessResponse(RequestType.Sheet),
    IFilePathBased;