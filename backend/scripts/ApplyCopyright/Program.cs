/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: ApplyCopyright
 * File: Program.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using System.Text;
using System.Text.RegularExpressions;

const string year = "2026";
const string author = "Thành Mai (thnhmai06)";
const string solutionName = "SlideGenerator";
const string repoUrl = "https://github.com/thnhmai06/SlideGenerator";

var options = Options.Parse(args);
var root = Path.GetFullPath(options.Root);
var files = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
    .Where(file => !IsIgnored(root, file))
    .Order(StringComparer.OrdinalIgnoreCase)
    .ToArray();

var changedCount = 0;

foreach (var file in files)
{
    var projectName = GetProjectName(file) ?? solutionName;
    var fileName = Path.GetFileName(file);
    var content = await File.ReadAllTextAsync(file, Encoding.UTF8).ConfigureAwait(false);
    var header = CreateHeader(projectName, fileName, DetectNewLine(content));
    var nextContent = header + RemoveExistingCopyrightHeaders(content);
    var changed = content != nextContent;

    Console.WriteLine($"{(changed ? "Changed" : "Unchanged")} {file}");

    if (!changed) continue;

    changedCount++;

    if (!options.Check)
    {
        await File.WriteAllTextAsync(file, nextContent, new UTF8Encoding(false)).ConfigureAwait(false);
    }
}

Console.WriteLine(options.Check
    ? $"Checked {files.Length} file(s). {changedCount} file(s) need updates."
    : $"Scanned {files.Length} file(s). Updated {changedCount} file(s).");

return options.Check && changedCount > 0 ? 1 : 0;

static string CreateHeader(string projectName, string fileName, string newLine)
{
    var lines = new[]
    {
        "/*",
        $" * Copyright (C) {year} {author}",
        " *",
        $" * Solution: {solutionName}",
        $" * Project: {projectName}",
        $" * File: {fileName}",
        " *",
        $" * This file is part of this solution. You can find the full source code here: {repoUrl}",
        " *",
        " * This program is free software: you can redistribute it and/or modify",
        " * it under the terms of the GNU Affero General Public License as published by",
        " * the Free Software Foundation, version 3.",
        " *",
        " * This program is distributed in the hope that it will be useful,",
        " * but WITHOUT ANY WARRANTY; without even the implied warranty of",
        " * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the",
        " * GNU Affero General Public License for more details.",
        " */",
        string.Empty
    };

    return string.Join(newLine, lines) + newLine;
}

static string? GetProjectName(string filePath)
{
    var directory = Path.GetDirectoryName(filePath);

    while (!string.IsNullOrWhiteSpace(directory))
    {
        var project = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (project is not null) return Path.GetFileNameWithoutExtension(project);

        var parent = Directory.GetParent(directory);
        if (parent is null || string.Equals(parent.FullName, directory, StringComparison.OrdinalIgnoreCase)) break;

        directory = parent.FullName;
    }

    return null;
}

static string RemoveExistingCopyrightHeaders(string content)
{
    content = content.TrimStart('\uFEFF');
    while (TryStripCopyrightBlock(ref content) || TryStripCopyrightLine(ref content)) { }
    return content.TrimStart();
}

static bool TryStripCopyrightBlock(ref string content)
{
    var next = Regex.Replace(
        content,
        @"^\s*/\*[\s\S]*?\*/\s*",
        match => match.Value.Contains("Copyright", StringComparison.OrdinalIgnoreCase) ? string.Empty : match.Value,
        RegexOptions.None,
        TimeSpan.FromSeconds(1));
    if (next == content) return false;
    content = next;
    return true;
}

static bool TryStripCopyrightLine(ref string content)
{
    var next = Regex.Replace(
        content,
        @"^\s*(?://[^\r\n]*(?:\r?\n|$))+",
        match => match.Value.Contains("Copyright", StringComparison.OrdinalIgnoreCase) ? string.Empty : match.Value,
        RegexOptions.None,
        TimeSpan.FromSeconds(1));
    if (next == content) return false;
    content = next;
    return true;
}

static string DetectNewLine(string content)
{
    return content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
}

static bool IsIgnored(string root, string filePath)
{
    var relative = Path.GetRelativePath(root, filePath);
    var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
           || segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
}

internal sealed record Options(string Root, bool Check)
{
    public static Options Parse(string[] args)
    {
        var root = Directory.GetCurrentDirectory();
        var check = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--check":
                    check = true;
                    break;
                case "--root":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException($"Unknown or incomplete argument: {args[i]}");
                    root = args[++i];
                    break;
                default:
                    throw new ArgumentException($"Unknown or incomplete argument: {args[i]}");
            }
        }

        return new Options(root, check);
    }
}
