namespace SlideGenerator.Settings.Settings;

public sealed partial class Setting
{
    /// <summary>
    ///     Settings related to the execution and orchestration of generation jobs.
    ///     Controls the degree of parallelism for different stages of the pipeline.
    /// </summary>
    public sealed class JobSetting
    {
        /// <summary>Gets or sets the maximum number of concurrent image downloads.</summary>
        public int MaxParallelDownloadImage { get; set; } = 5;

        /// <summary>Gets or sets the maximum number of concurrent image editing operations.</summary>
        public int MaxParallelEditImage { get; set; } = 5;

        /// <summary>Gets or sets the maximum number of concurrent presentation editing operations (slides filling).</summary>
        public int MaxParallelEditPresentation { get; set; } = 5;

        /// <summary>Gets or sets the maximum number of concurrent workbook reading operations.</summary>
        public int MaxParallelReadWorkbook { get; set; } = 5;

        /// <summary>Gets or sets the maximum number of concurrent presentation reading operations.</summary>
        public int MaxParallelReadPresentation { get; set; } = 5;
    }
}