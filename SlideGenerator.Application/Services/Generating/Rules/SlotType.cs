namespace SlideGenerator.Application.Services.Generating.Rules;

/// <summary>Identifies a named process-wide concurrency gate used during generation.</summary>
public enum SlotType
{
    /// <summary>Gate for image download operations.</summary>
    Download,

    /// <summary>Gate for image editing operations.</summary>
    EditImage,

    /// <summary>Gate for slide editing operations.</summary>
    EditSlide
}