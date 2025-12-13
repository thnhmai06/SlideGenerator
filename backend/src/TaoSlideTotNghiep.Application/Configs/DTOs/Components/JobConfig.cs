namespace TaoSlideTotNghiep.Application.Configs.DTOs.Components;

public record JobConfig(int MaxConcurrentJobs, string OutputFolder, string HangfireDbPath);