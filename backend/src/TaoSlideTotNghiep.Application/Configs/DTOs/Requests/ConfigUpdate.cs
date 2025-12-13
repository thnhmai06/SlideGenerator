using TaoSlideTotNghiep.Application.Configs.DTOs.Components;
using TaoSlideTotNghiep.Application.Configs.DTOs.Enums;

namespace TaoSlideTotNghiep.Application.Configs.DTOs.Requests;

public record ConfigUpdate(
    ServerConfig? Server,
    DownloadConfig? Download,
    JobConfig? Job) : ConfigRequest(ConfigRequestType.Update);