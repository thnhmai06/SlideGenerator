namespace SlideGenerator.Infrastructure.Features.Images.Exceptions;

public class NotImageFileUrl(string url)
    : ArgumentException($"URL {url} is not an valid image file.", nameof(url))
{
    public string Url { get; } = url;
}