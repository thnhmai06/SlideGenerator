namespace SlideGenerator.Domain.Settings.Entities;

public partial class Setting
{
    /// <summary>
    ///     Represents the configuration settings for image processing operations.
    /// </summary>
    public sealed class ImageSetting
    {
        /// <summary>
        ///     Gets or sets the configuration for face detection and alignment.
        /// </summary>
        public FaceSetting Face = new();

        /// <summary>
        ///     Gets or sets the configuration for saliency detection padding.
        /// </summary>
        public SaliencySetting Saliency = new();

        /// <summary>
        ///     Represents the configuration settings for face detection.
        /// </summary>
        public sealed class FaceSetting
        {
            /// <summary>
            ///     Gets or sets the minimum confidence threshold for accepting a detected face.
            /// </summary>
            public float Confidence = 0.7f;

            /// <summary>
            ///     Gets or sets the maximum pixel dimension allowed for face detection scaling.
            /// </summary>
            public int MaxDimension = 1280;

            /// <summary>
            ///     Gets or sets a value indicating whether to unite all detected faces into a single bounding box.
            /// </summary>
            public bool UnionAll = false;
        }

        /// <summary>
        ///     Represents the configuration settings for padding applied around salient regions.
        /// </summary>
        public sealed class SaliencySetting
        {
            /// <summary>Gets or sets the relative bottom padding.</summary>
            public float PaddingBottom = 0.0f;

            /// <summary>Gets or sets the relative left padding.</summary>
            public float PaddingLeft = 0.0f;

            /// <summary>Gets or sets the relative right padding.</summary>
            public float PaddingRight = 0.0f;

            /// <summary>Gets or sets the relative top padding.</summary>
            public float PaddingTop = 0.0f;
        }
    }
}
