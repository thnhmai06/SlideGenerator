namespace SlideGenerator.Domain.Settings.Entities;

public partial class Setting
{
    public sealed class ImageSetting
    {
        public FaceSetting Face = new();
        public SaliencySetting Saliency = new();

        public sealed class FaceSetting
        {
            public float Confidence = 0.7f;
            public int MaxDimension = 1280;
            public bool UnionAll = false;
        }

        public sealed class SaliencySetting
        {
            public float PaddingBottom = 0.0f;
            public float PaddingLeft = 0.0f;
            public float PaddingRight = 0.0f;
            public float PaddingTop = 0.0f;
        }
    }
}