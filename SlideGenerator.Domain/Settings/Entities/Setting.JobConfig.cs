namespace SlideGenerator.Domain.Settings.Entities;

public sealed partial class Setting
{
    public static readonly string DatabasePath = Path.Combine(AppContext.BaseDirectory, "Jobs.db");

    public sealed class JobSetting
    {
        /// <summary>
        ///     Gets or sets the maximum number of concurrent image-processing flows.
        /// </summary>
        /// <remarks>
        ///     This controls runtime admission for image download and image editing operations.
        /// </remarks>
        public int MaxConcurrentPreparingFlows = 5;

        /// <summary>
        ///     Gets or sets the maximum number of concurrent slide-editing flows.
        /// </summary>
        /// <remarks>
        ///     This controls runtime admission for slide clone and slide content replacement operations.
        /// </remarks>
        public int MaxConcurrentEditingFlows = 5;

        public int MaxRetries = 3;
    }
}