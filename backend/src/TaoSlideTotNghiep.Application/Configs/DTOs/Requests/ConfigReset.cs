using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Requests;

public record ConfigReset() : ConfigRequest(ConfigRequestType.Reset);