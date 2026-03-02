namespace SlideGenerator.Features.Configs.Entities;

public partial class Config
{
    public sealed class ImageConfig
    {
        public FaceConfig Face = new();
        public SaliencyConfig Saliency = new();

        public sealed class FaceConfig
        {
            public float Confidence = 0.7f;
            public int MaxDimension = 1280;
            public bool UnionAll = false;
        }

        public sealed class SaliencyConfig
        {
            public float PaddingBottom = 0.0f;
            public float PaddingLeft = 0.0f;
            public float PaddingRight = 0.0f;
            public float PaddingTop = 0.0f;
        }
    }
}