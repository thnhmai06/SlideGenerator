namespace SlideGenerator.Coordinator.Models;

/// <summary>Identifies a named process-wide concurrency gate used during generation.</summary>
public enum GateType
{
    /// <summary>Gate for image download operations.</summary>
    DownloadImage,

    /// <summary>Gate for image editing operations.</summary>
    EditImage,

    /// <summary>Gate for slide editing operations.</summary>
    EditPresentation,

    /// <summary>Gate for workbook file scanning operations.</summary>
    ReadWorkbook,

    /// <summary>Gate for presentation file scanning operations.</summary>
    ReadPresentation
}