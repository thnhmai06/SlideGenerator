using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Download.Rules;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Workflows.Generating.Models.Images;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Resolves stored image files from disk for each specialized instruction.
/// </summary>
public sealed class ResolveImagePathsFromDisk(ISettingProvider settingProvider) : Activity
{
    public required Input<IReadOnlyList<SpecializedInstruction>> ImageInstructions { get; init; }
    public required Input<int> RowIndex { get; init; }
    public required Input<bool> UseEditPath { get; init; }
    public Output<IReadOnlyDictionary<SpecializedInstruction, string>> ImagePaths { get; init; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var rowIndex = context.Get(RowIndex);
        if (rowIndex <= 0)
            throw new ArgumentException("Row index must be greater than 0.");

        var instructions = context.Get(ImageInstructions) ?? [];
        if (instructions.Count == 0)
        {
            context.Set(ImagePaths, new Dictionary<SpecializedInstruction, string>());
            return ValueTask.CompletedTask;
        }

        var downloadRoot = Path.GetFullPath(settingProvider.Current.Download.DownloadFolder);
        var useEditPath = context.Get(UseEditPath);
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

        context.Set(ImagePaths, resolved);
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