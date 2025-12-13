using TaoSlideTotNghiep.Application.Base.DTOs.Enums;
using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Requests;

public abstract record ConfigRequest(ConfigRequestType Type) : Base.DTOs.Requests.Request(RequestType.Config);