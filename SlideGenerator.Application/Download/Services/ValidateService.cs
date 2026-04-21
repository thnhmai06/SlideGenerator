namespace SlideGenerator.Application.Download.Services;

/// Review by @thnhmai06 at 04/03/2026 22:01:09 GMT+7
public sealed class ValidateService
{
    public static async Task<bool> IsImageUri(Uri uri, HttpClient httpClient)
    {
        var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        return response.IsSuccessStatusCode
               && response.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true;
    }
}