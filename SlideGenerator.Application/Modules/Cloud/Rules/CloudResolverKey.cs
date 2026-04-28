namespace SlideGenerator.Application.Modules.Cloud.Rules;

/// <summary>
///     Represents strongly typed keys identifying supported cloud URI resolvers.
/// </summary>
public enum CloudResolverKey
{
    /// <summary>Identifies the Google Drive resolver.</summary>
    GoogleDrive,

    /// <summary>Identifies the Google Photos resolver.</summary>
    GooglePhotos,

    /// <summary>Identifies the Microsoft OneDrive resolver.</summary>
    OneDrive,

    /// <summary>Identifies the Microsoft SharePoint resolver.</summary>
    SharePoint
}