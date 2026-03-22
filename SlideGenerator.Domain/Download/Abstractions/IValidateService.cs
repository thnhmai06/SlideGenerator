namespace SlideGenerator.Domain.Download.Abstractions;

public interface IValidateService
{
    Task<bool> IsImageUri(Uri uri, HttpClient httpClient);
}