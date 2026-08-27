/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: DesignSystemTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Machine-checkable half of the frontend-overhaul blueprint's Visual Quality Gate (blueprint §7.0.A).
///     Parses <c>Resources/Primitives.axaml</c>/<c>Semantic.axaml</c> directly with <see cref="XDocument" /> —
///     no Avalonia runtime needed, since the tokens are plain hex/XML — and greps view XAML under
///     <c>Features/</c>/<c>Shell/</c> for violations of the token contract. Every rule here has a matching
///     line in the blueprint's §7.0.A table; keep the two in sync if either changes.
/// </summary>
public sealed class DesignSystemTests
{
    private static readonly string DesktopSrc = FindDesktopSrc();
    private static readonly XNamespace Av = "https://github.com/avaloniaui";
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    ///     Pre-existing violations not yet in scope for the phase currently landing them — each entry is an
    ///     explicit, reviewable exception (blueprint §7.0.C: "ghi rõ thành gap tường minh, không âm thầm bỏ
    ///     qua"), never a silent skip. Remove an entry the moment its owning phase actually fixes the line;
    ///     a stale entry here would hide a regression instead of tracking one.
    /// </summary>
    // Empty as of P4b — every entry tracked here since P1 (SlideCanvasView's unpadded label chip,
    // RecipeEditorView's/RecipesView's literal "..." loading text) has been fixed at its owning phase and
    // removed rather than re-pinned to a new line number. Add an entry back only for a genuinely new,
    // out-of-scope-for-now violation — never to paper over a regression.
    private static readonly HashSet<string> KnownViolations = [];

    private static string FindDesktopSrc([CallerFilePath] string here = "")
    {
        var dir = Path.GetDirectoryName(here)!;
        while (!File.Exists(Path.Combine(dir, "SlideGenerator.slnx")))
            dir = Path.GetDirectoryName(dir) ?? throw new InvalidOperationException(
                $"Could not locate SlideGenerator.slnx by walking up from {here}");
        return Path.Combine(dir, "src", "SlideGenerator.Desktop");
    }

    private static IEnumerable<string> ViewFiles()
    {
        return Directory.EnumerateFiles(Path.Combine(DesktopSrc, "Features"), "*.axaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(DesktopSrc, "Shell"), "*.axaml", SearchOption.AllDirectories));
    }

    private static IEnumerable<(int LineNo, string Line)> Lines(string file)
    {
        return File.ReadLines(file).Select((line, i) => (i + 1, line));
    }

    private static string RelativePath(string file)
    {
        return Path.GetRelativePath(DesktopSrc, file).Replace('\\', '/');
    }

    /// <summary>Reports <paramref name="offenders" /> unless every one of them is a tracked <see cref="KnownViolations" /> entry.</summary>
    private static void AssertNoUntrackedOffenders(List<(string Key, string Message)> offenders, string because)
    {
        var untracked = offenders.Where(o => !KnownViolations.Contains(o.Key)).Select(o => o.Message).ToList();
        untracked.Should().BeEmpty(string.Join("; ", untracked) + " | " + because);
    }

    #region Contrast (WCAG AA, blueprint §7.0.A "Contrast" row)

    private static Dictionary<string, string> ParsePrimitives()
    {
        var doc = XDocument.Load(Path.Combine(DesktopSrc, "Resources", "Primitives.axaml"));
        return doc.Descendants(Av + "Color").ToDictionary(e => e.Attribute(X + "Key")!.Value, e => e.Value.Trim());
    }

    private static Dictionary<string, Dictionary<string, string>> ParseSemanticThemeBrushes(
        IReadOnlyDictionary<string, string> primitives)
    {
        var doc = XDocument.Load(Path.Combine(DesktopSrc, "Resources", "Semantic.axaml"));
        var result = new Dictionary<string, Dictionary<string, string>>();
        foreach (var themeDict in doc.Descendants(Av + "ResourceDictionary.ThemeDictionaries")
                     .Elements(Av + "ResourceDictionary"))
        {
            var themeName = themeDict.Attribute(X + "Key")!.Value;
            var brushes = themeDict.Elements(Av + "SolidColorBrush").ToDictionary(
                b => b.Attribute(X + "Key")!.Value,
                b => ResolveColor(b.Attribute("Color")!.Value, primitives));
            result[themeName] = brushes;
        }

        return result;
    }

    private static string ResolveColor(string raw, IReadOnlyDictionary<string, string> primitives)
    {
        var match = Regex.Match(raw, @"\{StaticResource\s+(\w+)\}");
        return match.Success ? primitives[match.Groups[1].Value] : raw;
    }

    private static (double R, double G, double B) ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 8) hex = hex[2..]; // drop leading alpha (AARRGGBB) — none of the current tokens use it
        return (
            Convert.ToInt32(hex[..2], 16) / 255.0,
            Convert.ToInt32(hex[2..4], 16) / 255.0,
            Convert.ToInt32(hex[4..6], 16) / 255.0);
    }

    private static double RelativeLuminance(double channel)
    {
        return channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }

    private static double ContrastRatio(string hexA, string hexB)
    {
        var (ar, ag, ab) = ParseHex(hexA);
        var (br, bg, bb) = ParseHex(hexB);
        var lumA = 0.2126 * RelativeLuminance(ar) + 0.7152 * RelativeLuminance(ag) + 0.0722 * RelativeLuminance(ab);
        var lumB = 0.2126 * RelativeLuminance(br) + 0.7152 * RelativeLuminance(bg) + 0.0722 * RelativeLuminance(bb);
        var (lighter, darker) = lumA >= lumB ? (lumA, lumB) : (lumB, lumA);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>Every (text brush, surface brush) pair actually used together in the app — see the app's own views.</summary>
    private static readonly (string Text, string Surface)[] ContrastPairs =
    [
        ("TextPrimaryBrush", "SurfaceBackgroundBrush"),
        ("TextPrimaryBrush", "SurfaceBrush"),
        ("TextPrimaryBrush", "SurfaceMutedBrush"),
        ("TextSecondaryBrush", "SurfaceBackgroundBrush"),
        ("TextSecondaryBrush", "SurfaceBrush"),
        ("TextSecondaryBrush", "SurfaceMutedBrush"),
        ("TextMutedBrush", "SurfaceBackgroundBrush"),
        ("TextMutedBrush", "SurfaceBrush"),
        ("TextMutedBrush", "SurfaceMutedBrush"),
        ("TextOnAccentBrush", "AccentBrush"),
        // Border.pill.success/.warning/.danger (Controls.axaml) — Saved/Unsaved badge, conflict badges, status
        // pills — all use TextOnAccentBrush over these 3 backgrounds, same as AccentBrush above.
        ("TextOnAccentBrush", "SuccessBrush"),
        ("TextOnAccentBrush", "WarningBrush"),
        ("TextOnAccentBrush", "DangerBrush"),
        // Border.pill.info (Controls.axaml) — AccentBrush text over AccentMutedBrush background.
        ("AccentBrush", "AccentMutedBrush")
    ];

    /// <summary>
    ///     Every text/surface brush pair the app actually renders together must clear WCAG AA (4.5:1) in both
    ///     theme variants. Caught 5 real defects on first run: <c>TextMutedBrush</c> failed against
    ///     <c>SurfaceBackgroundBrush</c>/<c>SurfaceMutedBrush</c> in light (4.35/4.04:1) and against
    ///     <c>SurfaceBrush</c>/<c>SurfaceMutedBrush</c> in dark (4.39/4.00:1) — i.e. every list row's hover
    ///     background (<c>Border.hairline-row:pointerover</c>) dropped below AA in both themes — and white
    ///     <c>TextOnAccentBrush</c> on dark's <c>AccentBrush</c> (<c>BrandDodger</c>) was only 3.24:1.
    /// </summary>
    [Fact]
    public void ThemeBrushPairs_MeetWcagAaContrast_InBothVariants()
    {
        var themes = ParseSemanticThemeBrushes(ParsePrimitives());
        var failures = new List<string>();

        foreach (var (themeName, brushes) in themes)
        foreach (var (textKey, surfaceKey) in ContrastPairs)
        {
            var ratio = ContrastRatio(brushes[textKey], brushes[surfaceKey]);
            if (ratio < 4.5)
                failures.Add($"{themeName}: {textKey} on {surfaceKey} = {ratio:F2}:1 (need >= 4.5:1)");
        }

        failures.Should().BeEmpty();
    }

    #endregion

    #region View XAML hygiene (blueprint §7.0.A remaining rows)

    [Fact]
    public void ViewXaml_ContainsNoRawHexColors()
    {
        var pattern = new Regex("#[0-9A-Fa-f]{6,8}");
        var offenders = new List<(string Key, string Message)>();
        foreach (var file in ViewFiles())
        foreach (var (lineNo, line) in Lines(file))
            if (pattern.IsMatch(line))
                offenders.Add(($"{RelativePath(file)}:{lineNo}", $"{RelativePath(file)}:{lineNo}: {line.Trim()}"));

        AssertNoUntrackedOffenders(offenders,
            "colors must come from Resources/Primitives.axaml via Semantic.axaml brushes, not literal hex in views");
    }

    [Fact]
    public void ViewXaml_ContainsNoLiteralDuration()
    {
        var pattern = new Regex("Duration=\"0:");
        var offenders = new List<(string Key, string Message)>();
        foreach (var file in ViewFiles())
        foreach (var (lineNo, line) in Lines(file))
            if (pattern.IsMatch(line))
                offenders.Add(($"{RelativePath(file)}:{lineNo}", $"{RelativePath(file)}:{lineNo}: {line.Trim()}"));

        AssertNoUntrackedOffenders(offenders,
            "every duration must come from {DynamicResource Motion*}, or be built in code (like MainWindow/" +
            "ShellView's page transitions) reading the same resource, so Appearance.ReducedMotion can zero it");
    }

    /// <summary>
    ///     Literal numeric <c>Margin</c>/<c>Padding</c>/<c>Spacing</c> attribute values must land on the 4px
    ///     grid, so gaps read as intentional design decisions rather than accidental one-offs.
    /// </summary>
    [Fact]
    public void ViewXaml_MarginPaddingSpacingLiterals_AreOnFourPxGrid()
    {
        var attrPattern = new Regex(@"(?:Margin|Padding|Spacing)=""([0-9,.\s]+)""");
        var offenders = new List<(string Key, string Message)>();
        foreach (var file in ViewFiles())
        foreach (var (lineNo, line) in Lines(file))
        foreach (Match m in attrPattern.Matches(line))
        {
            var parts = m.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
                if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v % 4 != 0)
                    offenders.Add(($"{RelativePath(file)}:{m.Value}",
                        $"{RelativePath(file)}:{lineNo}: {m.Value} (not a multiple of 4)"));
        }

        AssertNoUntrackedOffenders(offenders, "spacing must stay on the 4px grid");
    }

    /// <summary><c>CornerRadius="0"</c> (square corners — e.g. flush caption buttons) is a deliberate, allowed opt-out; any other literal is not.</summary>
    [Fact]
    public void ViewXaml_CornerRadiusIsAlwaysATokenReference()
    {
        var pattern = new Regex(@"CornerRadius=""(?!\{StaticResource)([^""]+)""");
        var offenders = new List<(string Key, string Message)>();
        foreach (var file in ViewFiles())
        foreach (var (lineNo, line) in Lines(file))
        foreach (Match m in pattern.Matches(line))
            if (m.Groups[1].Value != "0")
                offenders.Add(($"{RelativePath(file)}:{lineNo}:CornerRadius",
                    $"{RelativePath(file)}:{lineNo}: CornerRadius=\"{m.Groups[1].Value}\""));

        AssertNoUntrackedOffenders(offenders, "radius must be {StaticResource Radius*}, not a literal");
    }

    [Fact]
    public void ViewXaml_ContainsNoPlaceholderMarkers()
    {
        string[] markers = ["PlaceholderPageView", "Text=\"TODO", "Text=\"Coming soon", "Text=\"...\""];
        var offenders = new List<(string Key, string Message)>();
        foreach (var file in ViewFiles())
        {
            foreach (var (lineNo, line) in Lines(file))
            foreach (var marker in markers)
                if (line.Contains(marker, StringComparison.Ordinal))
                    offenders.Add(($"{RelativePath(file)}:{lineNo}:{marker}",
                        $"{RelativePath(file)}:{lineNo}: contains '{marker}'"));
        }

        AssertNoUntrackedOffenders(offenders, "no page/loading state may fall back to a bare placeholder string");
    }

    /// <summary>
    ///     A hand-drawn divider line must be a <c>Border</c> hairline via <c>BorderBrush</c> (see
    ///     <c>Controls.axaml</c>'s <c>Border.hairline-row</c>), never a solid-filled <c>Rectangle</c> standing
    ///     in for a rule. The one legitimate <c>Rectangle</c> in the app is the canvas shape-overlay
    ///     (<c>Rectangle.shape-overlay</c> in <c>SlideCanvasView</c>) — anything else is exempt from this
    ///     specific opt-out.
    /// </summary>
    [Fact]
    public void ViewXaml_NoBareRectangleUsedAsADivider()
    {
        var openTag = new Regex(@"<Rectangle\b(?![^>]*Classes=""shape-overlay"")");
        var offenders = new List<(string Key, string Message)>();
        foreach (var file in ViewFiles())
        foreach (var (lineNo, line) in Lines(file))
            if (openTag.IsMatch(line))
                offenders.Add(($"{RelativePath(file)}:{lineNo}:Rectangle",
                    $"{RelativePath(file)}:{lineNo}: bare <Rectangle> — use a Border.hairline-row instead"));

        AssertNoUntrackedOffenders(offenders, "a divider is a Border hairline, not a filled Rectangle");
    }

    #endregion
}
