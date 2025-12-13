using TaoSlideTotNghiep.Application.Configs.DTOs.Components;
using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Responses.Successes;

public record ConfigGetSuccess(
    ServerConfig Server,
    DownloadConfig Download,
    JobConfig Job) : ConfigSuccess(ConfigRequestType.Get);