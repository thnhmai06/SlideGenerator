using TaoSlideTotNghiep.Application.Base.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Base.DTOs.Requests;

/// <summary>
/// Base request class.
/// </summary>
public abstract record Request(RequestType RequestType);