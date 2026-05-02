namespace SlideGenerator.Settings.Settings;

public sealed partial class Setting
{
    public sealed class JobSetting
    {
        public int MaxParallelDownloadImage { get; set; } = 5;
        public int MaxParallelEditImage { get; set; } = 5;
        public int MaxParallelEditPresentation { get; set; } = 5;
        public int MaxParallelReadWorkbook { get; set; } = 5;
        public int MaxParallelReadPresentation { get; set; } = 5;
    }
}