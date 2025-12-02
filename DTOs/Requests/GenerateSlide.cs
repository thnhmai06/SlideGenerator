using presentation.Models.Enum;

namespace presentation.DTOs.Requests;

public record GenerateSlideTextConfig(string Pattern, params string[] Columns);

public record GenerateSlideImageConfig(uint ShapeId, params string[] Columns);

// ? Group
public record GenerateSlideCreate(
    string TemplatePath,
    string SpreadsheetPath,
    GenerateSlideTextConfig[] TextConfigs,
    GenerateSlideImageConfig[] ImageConfigs,
    string Path, // Save Path
    string[]? CustomSheet) : Request.Create, IPathBased;

public record GenerateSlideGroupControl(string Path, ControlState State) : Request.Control(State), IPathBased;

// ? Job
public record GenerateSlideJobControl(string JobId, ControlState State) : Request.Control(State), IJobBased;
