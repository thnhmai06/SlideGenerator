using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Models;

/// <summary>
///     Carries the context for a single download or edit task within a row iteration.
///     Passed as the ForEach item to <see cref="Activities.DownloadImage" /> and <see cref="Activities.EditImage" />.
/// </summary>
/// <param name="Worksheet">The worksheet this task belongs to.</param>
/// <param name="RowIndex">The 1-based row index being processed.</param>
/// <param name="DownloadItem">The image instruction to download; <see langword="null" /> for edit tasks.</param>
/// <param name="EditItem">
///     The resolved (instruction, downloaded-path) pair to edit; <see langword="null" /> for download tasks.
/// </param>
public record RowTask(
    WorksheetIdentifier Worksheet,
    int RowIndex,
    GeneralInstruction? DownloadItem = null,
    KeyValuePair<SpecializedInstruction, string>? EditItem = null);