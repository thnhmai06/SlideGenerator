using SlideGenerator.Services.Generating.Models;

namespace SlideGenerator.Services.Generating.Services;

public static class ValidationService
{
    /// <summary>
    ///     Resolves the list of sheet names to be used for slide generation based on the user's selection or the available
    ///     templates.
    /// </summary>
    /// <remarks>
    ///     If the user has not selected any sheets, all available template keys are returned to ensure
    ///     that slide generation includes all possible sheets.
    /// </remarks>
    /// <param name="request">
    ///     The request containing the selected sheets and the template mapping used to determine which
    ///     sheets to include.
    /// </param>
    /// <returns>
    ///     A read-only list of strings representing the selected sheet names. If no sheets are selected, returns the keys
    ///     from the template map.
    /// </returns>
    public static IReadOnlyList<string> ResolveSelectedSheets(SlidesGenerateRequest request)
    {
        return request.Book.SelectedSheets is { Count: > 0 }
            ? request.Book.SelectedSheets
            : request.SheetToSlideMap.Keys.ToList();
    }

    /// <summary>
    ///     Validates the specified slide generation request to ensure that all required templates and data files are
    ///     present and correctly configured.`
    /// </summary>
    /// <remarks>
    ///     Call this method before processing a slide generation request to verify that all necessary
    ///     files and configuration values are valid. This helps prevent runtime errors due to missing files or invalid
    ///     template indices.
    /// </remarks>
    /// <param name="request">
    ///     The slide generation request containing the template mappings and sheet data to validate. Cannot
    ///     be null.
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the request does not contain at least one template, or if any
    ///     template's index is less than 1.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    ///     Thrown if the specified sheet data file or any template file does not exist at
    ///     the provided file path.
    /// </exception>
    public static void ValidateRequest(SlidesGenerateRequest request)
    {
        if (request.SheetToSlideMap.Count == 0)
            throw new InvalidOperationException("At least one template is required.");

        if (!File.Exists(request.Book.FilePath))
            throw new FileNotFoundException("Sheet data file not found.", request.Book.FilePath);

        foreach (var template in request.SheetToSlideMap.Values)
        {
            if (!File.Exists(template.FilePath))
                throw new FileNotFoundException("Template file not found.", template.FilePath);
            if (template.Index < 1)
                throw new InvalidOperationException("TemplateSlideIndex must be >= 1.");
        }
    }
}