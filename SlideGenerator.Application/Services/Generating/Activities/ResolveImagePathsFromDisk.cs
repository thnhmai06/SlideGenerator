using SlideGenerator.Application.Download.Rules;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Resolves image file paths from disk for the filtered general instructions.
/// </summary>
/// <remarks>
///     Note: In this implementation, we map the GENERAL instruction to the file path.
///     The path resolution logic uses row 1 as a placeholder for the preparation phase.
/// </remarks>
/// <param name="settingProvider">The settings provider.</param>
/// <param name="useEditPath">Whether to use edited image paths instead of downloaded ones.</param>
public sealed class ResolveImagePathsFromDisk(ISettingProvider settingProvider, bool useEditPath) : Activity
{
    /// <inheritdoc />
    public override ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var rowIndex = 1;

        var instructions =
            context.GetVariable<IReadOnlyList<GeneralInstruction>>(WorksheetContextRules.ImageInstructions) ?? [];

        if (instructions.Count == 0)
        {
            if (useEditPath)
                context.SetVariable<IReadOnlyDictionary<GeneralInstruction, string>>(
                    WorksheetContextRules.EditedImagePaths, new Dictionary<GeneralInstruction, string>());
            else
                context.SetVariable<IReadOnlyDictionary<GeneralInstruction, string>>(
                    WorksheetContextRules.DownloadedImagePaths, new Dictionary<GeneralInstruction, string>());
            return ValueTask.CompletedTask;
        }

        var downloadRoot = Path.GetFullPath(settingProvider.Current.Download.DownloadFolder);
        var resolved = new Dictionary<GeneralInstruction, string>();

        foreach (var instruction in instructions)
        {
            // We use a dummy SpecializedInstruction just to calculate the path logic
            var dummySpecialized = new SpecializedInstruction(instruction.Target, null, instruction.Edit);

            var desiredPath = useEditPath
                ? dummySpecialized.GetEditPath(downloadRoot, rowIndex)
                : dummySpecialized.GetDownloadPath(downloadRoot, rowIndex);

            var resolvedPath = useEditPath
                ? TryResolveEditedPath(desiredPath)
                : TryResolveDownloadedPath(desiredPath);

            if (string.IsNullOrWhiteSpace(resolvedPath))
                continue;

            resolved[instruction] = Path.GetFullPath(resolvedPath);
        }

        if (useEditPath)
            context.SetVariable<IReadOnlyDictionary<GeneralInstruction, string>>(
                WorksheetContextRules.EditedImagePaths, resolved);
        else
            context.SetVariable<IReadOnlyDictionary<GeneralInstruction, string>>(
                WorksheetContextRules.DownloadedImagePaths, resolved);

        return ValueTask.CompletedTask;
    }

    private static string? TryResolveEditedPath(string desiredPath)
    {
        return File.Exists(desiredPath) ? desiredPath : null;
    }

    private static string? TryResolveDownloadedPath(string desiredPathWithoutExtension)
    {
        var folder = Path.GetDirectoryName(desiredPathWithoutExtension);
        var fileName = Path.GetFileName(desiredPathWithoutExtension);
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(folder))
            return null;

        return (from candidate in Directory.EnumerateFiles(folder, $"{fileName}.*", SearchOption.TopDirectoryOnly)
            let extension = Path.GetExtension(candidate)
            where !string.Equals(extension, FileExtensionRules.TempDownload, StringComparison.OrdinalIgnoreCase)
            select candidate).FirstOrDefault();
    }
}