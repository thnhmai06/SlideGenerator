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
            ///     Padding ratio for top side of detected faces (0-1). Default is 0.15.
            /// </summary>
            public float PaddingTop { get; init; } = 0.15f;

            /// <summary>
            ///     Padding ratio for bottom side of detected faces (0-1). Default is 0.15.
            /// </summary>
            public float PaddingBottom { get; init; } = 0.15f;

            /// <summary>
            ///     Padding ratio for left side of detected faces (0-1). Default is 0.15.
            /// </summary>
            public float PaddingLeft { get; init; } = 0.15f;

            /// <summary>
            ///     Padding ratio for right side of detected faces (0-1). Default is 0.15.
            /// </summary>
            public float PaddingRight { get; init; } = 0.15f;

            /// <summary>
            ///     If true, union all detected faces; otherwise use the best single face. Default is true.
            /// </summary>
            public bool UnionAll { get; init; } = false;
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