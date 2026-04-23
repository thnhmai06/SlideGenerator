using SlideGenerator.Application.Download.Rules;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Resolves stored image files from disk for each specialized instruction and writes the result to the context.
/// </summary>
public sealed class ResolveImagePathsFromDisk(ISettingProvider settingProvider, bool useEditPath) : Activity
{
    /// <inheritdoc />
    public override ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        int rowIndex = 1;

        var instructions = context.GetVariable<IReadOnlyList<SpecializedInstruction>>(WorksheetContextRules.ImageInstructions) ?? [];
        if (instructions.Count == 0)
        {
            if (useEditPath)
                context.SetVariable<IReadOnlyDictionary<SpecializedInstruction, string>>(WorksheetContextRules.EditedImagePaths, new Dictionary<SpecializedInstruction, string>());
            else
                context.SetVariable<IReadOnlyDictionary<SpecializedInstruction, string>>(WorksheetContextRules.DownloadedImagePaths, new Dictionary<SpecializedInstruction, string>());
            return ValueTask.CompletedTask;
        }

        var downloadRoot = Path.GetFullPath(settingProvider.Current.Download.DownloadFolder);
        var resolved = new Dictionary<SpecializedInstruction, string>();

        foreach (var instruction in instructions)
        {
            var desiredPath = useEditPath
                ? instruction.GetEditPath(downloadRoot, rowIndex)
                : instruction.GetDownloadPath(downloadRoot, rowIndex);

            var resolvedPath = useEditPath
                ? TryResolveEditedPath(desiredPath)
                : TryResolveDownloadedPath(desiredPath);

            if (string.IsNullOrWhiteSpace(resolvedPath))
                continue;

            resolved[instruction] = Path.GetFullPath(resolvedPath);
        }

        if (useEditPath)
            context.SetVariable<IReadOnlyDictionary<SpecializedInstruction, string>>(WorksheetContextRules.EditedImagePaths, resolved);
        else
            context.SetVariable<IReadOnlyDictionary<SpecializedInstruction, string>>(WorksheetContextRules.DownloadedImagePaths, resolved);

        return ValueTask.CompletedTask;
    }

    private static string? TryResolveEditedPath(string desiredPath) =>
        File.Exists(desiredPath) ? desiredPath : null;

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
