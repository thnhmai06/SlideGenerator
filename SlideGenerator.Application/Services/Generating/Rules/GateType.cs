namespace SlideGenerator.Application.Services.Generating.Rules;

/// <summary>Identifies a named process-wide concurrency gate used during generation.</summary>
public enum GateType
{
    /// <summary>Gate for worksheet-level processing flows.</summary>
    Worksheet,

    /// <summary>Gate for image download operations.</summary>
    Download,

    /// <summary>Gate for image editing operations.</summary>
    EditImage,

    /// <summary>Gate for slide editing operations.</summary>
    EditSlide,

    /// <summary>Gate for workbook file scanning operations.</summary>
    ScanWorkbook,

    /// <summary>Gate for presentation file scanning operations.</summary>
    ScanPresentation
}