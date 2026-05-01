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
        public int MaxParallelDownload = 5;

        /// <summary>
        ///     Gets or sets the maximum number of concurrent image-editing flows.
        /// </summary>
        /// <remarks>
        ///     This controls runtime admission for image editing operations.
        /// </remarks>
        public int MaxParallelEditImage = 5;

        /// <summary>
        ///     Gets or sets the maximum number of concurrent slide-editing flows.
        /// </summary>
        /// <remarks>
        ///     This controls runtime admission for slide clone and slide content replacement operations.
        /// </remarks>
        public int MaxParallelEditSlide = 5;

        public int MaxParallelReadWorkbook = 5;

        public int MaxParallelReadPresentation = 5;
    }
}