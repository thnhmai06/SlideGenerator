using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Requests;

public record ConfigGet() : ConfigRequest(ConfigRequestType.Get);