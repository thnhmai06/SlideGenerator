using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Responses.Successes;

public record ConfigUpdateSuccess(bool Updated, string Message) : ConfigSuccess(ConfigRequestType.Update);