namespace SlideGenerator.Domain.Configs;

public sealed partial class Config
{
    public sealed class ImageConfig
    {
        public FaceConfig Face { get; init; } = new();
        public SaliencyConfig Saliency { get; init; } = new();

        public sealed class FaceConfig
        {
            /// <summary>
            ///     Minimum face detection confidence score (0-1). Default is 0.7.
            /// </summary>
            public float Confidence { get; init; } = 0.7f;

            /// <summary>
            ///     If true, union all detected faces; otherwise use the best single face. Default is <see langword="false" />.
            /// </summary>
            public bool UnionAll { get; init; } = false;

            /// <summary>
            ///     Maximum dimension (width or height) for face detection image.
            ///     If the image is larger, it will be resized maintaining aspect ratio.
            ///     Default is 1280.
            /// </summary>
            public int MaxDimension { get; init; } = 1280;
        }

        public sealed class SaliencyConfig
        {
            /// <summary>
            ///     Padding ratio for top side of saliency anchor (0-1). Default is 0.0.
            /// </summary>
            public float PaddingTop { get; init; } = 0.0f;

            /// <summary>
            ///     Padding ratio for bottom side of saliency anchor (0-1). Default is 0.0.
            /// </summary>
            public float PaddingBottom { get; init; } = 0.0f;

            /// <summary>
            ///     Padding ratio for left side of saliency anchor (0-1). Default is 0.0.
            /// </summary>
            public float PaddingLeft { get; init; } = 0.0f;

            /// <summary>
            ///     Padding ratio for right side of saliency anchor (0-1). Default is 0.0.
            /// </summary>
            public float PaddingRight { get; init; } = 0.0f;
        }
    }
}