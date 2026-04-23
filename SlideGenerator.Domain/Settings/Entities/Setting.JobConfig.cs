namespace SlideGenerator.Domain.Settings.Entities;

public sealed partial class Setting
{
    /// <summary>
    ///     Gets the absolute path to the SQLite database used for job storage.
    /// </summary>
    public static readonly string DatabasePath = Path.Combine(AppContext.BaseDirectory, "Jobs.db");

    /// <summary>
    ///     Represents the configuration settings for background job execution.
    /// </summary>
    public sealed class JobSetting
    {
        /// <summary>
        ///     Gets or sets the maximum number of concurrent image-download flows.
        /// </summary>
        /// <remarks>
        ///     This controls runtime admission for image download operations.
        /// </remarks>
        public int MaxConcurrentDownloadFlows = 5;

        /// <summary>
        ///     Gets or sets the maximum number of concurrent image-editing flows.
        /// </summary>
        /// <remarks>
        ///     This controls runtime admission for image editing operations.
        /// </remarks>
        public int MaxConcurrentImageEditingFlows = 5;

        /// <summary>
        ///     Gets or sets the maximum number of concurrent slide-editing flows.
        /// </summary>
        /// <remarks>
        ///     This controls runtime admission for slide clone and slide content replacement operations.
        /// </remarks>
        public int MaxConcurrentSlideEditingFlows = 5;

        /// <summary>
        ///     Gets or sets the maximum number of times a failing job will be retried.
        /// </summary>
        public int MaxRetries = 3;
    }
}