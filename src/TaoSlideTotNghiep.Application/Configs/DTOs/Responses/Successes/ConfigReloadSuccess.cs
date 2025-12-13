using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Responses.Successes;

public record ConfigReloadSuccess(bool Reloaded, string Message) : ConfigSuccess(ConfigRequestType.Reload);