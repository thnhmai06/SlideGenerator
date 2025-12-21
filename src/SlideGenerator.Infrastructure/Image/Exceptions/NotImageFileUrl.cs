namespace SlideGenerator.Infrastructure.Image.Exceptions;

public class NotImageFileUrl(string url)
    : ArgumentException($"URL {url} is not an valid image file.", nameof(url))
{
    public string Url { get; } = url;
}