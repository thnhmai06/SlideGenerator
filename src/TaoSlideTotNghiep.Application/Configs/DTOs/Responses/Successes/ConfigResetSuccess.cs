using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Responses.Successes;

public record ConfigResetSuccess(bool Reset, string Message) : ConfigSuccess(ConfigRequestType.Reset);