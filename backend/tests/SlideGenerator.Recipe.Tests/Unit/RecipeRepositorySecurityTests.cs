/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: RecipeRepositorySecurityTests.cs
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
using FluentAssertions;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Data.Sqlite;
using SlideGenerator.Recipe.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Recipe.Tests.Unit;

/// <summary>
///     Security-focused tests for <see cref="RecipeRepository" /> covering issues that exist in
///     <see cref="RecipeRepository.ImportAsync" /> but are not exercised by the happy-path tests in
///     <c>RecipeRepositoryTests</c>. These tests demonstrate:
///     <list type="bullet">
///         <item>Zip Slip (CWE-22 path traversal) when an attacker crafts entries that escape the target directory.</item>
///         <item>Missing <c>recipe.json</c> silently produces a row with <see langword="null" /> recipe content.</item>
///     </list>
/// </summary>
public sealed class RecipeRepositorySecurityTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly List<string> _cleanupDirs = [];
    private readonly List<string> _cleanupFiles = [];
    private readonly RecipeRepository _repo;

    /// <summary>
    ///     Sets up a shared-cache in-memory SQLite database with an anchor connection.
    /// </summary>
    public RecipeRepositorySecurityTests()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"memory_{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };
        _anchor = new SqliteConnection(builder.ConnectionString);
        _anchor.Open();
        _repo = new RecipeRepository(builder, new NullRecipeFileManifestExtractor());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
        foreach (var f in _cleanupFiles)
            try
            {
                if (File.Exists(f)) File.Delete(f);
            }
            catch
            {
                /* ignore */
            }

        foreach (var d in _cleanupDirs)
            try
            {
                if (Directory.Exists(d)) Directory.Delete(d, true);
            }
            catch
            {
                /* ignore */
            }
    }

    #region Edge — missing recipe.json

    /// <summary>
    ///     Verifies that <see cref="RecipeRepository.ImportAsync" /> rejects archives that do not
    ///     contain the required <c>recipe.json</c> entry by throwing <see cref="InvalidDataException" />.
    /// </summary>
    [Fact(DisplayName = "ImportAsync rejects archives missing recipe.json")]
    public async Task ImportAsync_ZipMissingRecipeJson_ThrowsInvalidDataException()
    {
        var sandboxRoot = Path.Combine(Path.GetTempPath(), "rrs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandboxRoot);
        _cleanupDirs.Add(sandboxRoot);

        var workbooksDir = Path.Combine(sandboxRoot, "workbooks");
        var presentationsDir = Path.Combine(sandboxRoot, "presentations");
        Directory.CreateDirectory(workbooksDir);
        Directory.CreateDirectory(presentationsDir);

        var zipPath = Path.Combine(sandboxRoot, "no-recipe.recipe");
        _cleanupFiles.Add(zipPath);

        await using (var fs = File.Create(zipPath))
        using (var zos = new ZipOutputStream(fs))
        {
            zos.SetLevel(0);
            await zos.PutNextEntryAsync(new ZipEntry("Workbooks/data.xlsx") { DateTime = DateTime.UtcNow });
            var data = Encoding.UTF8.GetBytes("placeholder");
            zos.Write(data, 0, data.Length);
            zos.CloseEntry();
            zos.Finish();
        }

        var act = async () => await _repo.ImportAsync(zipPath, null, workbooksDir, presentationsDir,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    #endregion

    #region BUG — Zip Slip (path traversal)

    /// <summary>
    ///     SECURITY BUG (CRITICAL, CWE-22): <see cref="RecipeRepository.ImportAsync" /> blindly
    ///     trusts the entry names inside the archive. An attacker who controls the recipe file
    ///     can place entries such as <c>Workbooks/../../escape.txt</c>; the code strips the
    ///     <c>Workbooks/</c> prefix, then calls <see cref="Path.Combine" /> with the remaining
    ///     <c>../../escape.txt</c>, producing a path <b>outside</b> <c>workbooksDirectory</c>.
    ///     The file is then written there with <see cref="File.Create" />.
    ///     <para>
    ///         This test crafts such an archive and verifies that no file is created outside the
    ///         sandbox. It <b>fails on the current implementation</b> because the file escapes.
    ///     </para>
    /// </summary>
    [Fact(DisplayName = "BUG: ImportAsync allows zip-slip path traversal via Workbooks/../ entry")]
    public async Task ImportAsync_MaliciousWorkbookEntryWithDotDot_MustNotWriteOutsideTargetDirectory()
    {
        // Arrange — build a sandbox root containing both the import target dirs and a "victim" location.
        var sandboxRoot = Path.Combine(Path.GetTempPath(), "rrs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandboxRoot);
        _cleanupDirs.Add(sandboxRoot);

        var workbooksDir = Path.Combine(sandboxRoot, "workbooks");
        var presentationsDir = Path.Combine(sandboxRoot, "presentations");
        var victimDir = Path.Combine(sandboxRoot, "victim");
        Directory.CreateDirectory(workbooksDir);
        Directory.CreateDirectory(presentationsDir);
        Directory.CreateDirectory(victimDir);

        // The relative path "../victim/pwned.txt" is interpreted from workbooksDir; the
        // resulting absolute path lands inside victimDir — that is exactly the escape.
        const string maliciousEntryName = "Workbooks/../victim/pwned.txt";
        var maliciousPayload = "attacker-controlled-content"u8.ToArray();

        var zipPath = Path.Combine(sandboxRoot, "evil.recipe");
        _cleanupFiles.Add(zipPath);

        await using (var fs = File.Create(zipPath))
        using (var zos = new ZipOutputStream(fs))
        {
            zos.SetLevel(0);
            await zos.PutNextEntryAsync(new ZipEntry("recipe.json") { DateTime = DateTime.UtcNow });
            var recipeBytes = "{}"u8.ToArray();
            zos.Write(recipeBytes, 0, recipeBytes.Length);
            zos.CloseEntry();

            await zos.PutNextEntryAsync(new ZipEntry(maliciousEntryName) { DateTime = DateTime.UtcNow });
            zos.Write(maliciousPayload, 0, maliciousPayload.Length);
            zos.CloseEntry();
            zos.Finish();
        }

        // Act — ImportAsync must reject the malicious entry. Either throws, or silently skips —
        // in either case nothing may escape the sandbox.
        var act = async () => await _repo.ImportAsync(zipPath, null, workbooksDir, presentationsDir,
            TestContext.Current.CancellationToken);
        try
        {
            await act();
        }
        catch (InvalidDataException)
        {
            /* expected after fix */
        }

        // Assert — the victim file must NOT exist; nothing must escape the workbooks sandbox.
        var escapedFile = Path.Combine(victimDir, "pwned.txt");
        File.Exists(escapedFile).Should().BeFalse(
            "ImportAsync must reject or sanitize entries whose resolved path escapes the workbooks directory");

        // Defensive cross-check: every file actually written must live under workbooksDir.
        var writtenUnderWorkbooks = Directory.Exists(workbooksDir)
            ? Directory.GetFiles(workbooksDir, "*", SearchOption.AllDirectories)
            : [];
        foreach (var w in writtenUnderWorkbooks)
            Path.GetFullPath(w).Should().StartWith(Path.GetFullPath(workbooksDir));
    }

    /// <summary>
    ///     SECURITY BUG (CRITICAL, companion): same issue, exercised through the
    ///     <c>Presentations/</c> prefix. Locks down both branches of the import switch.
    /// </summary>
    [Fact(DisplayName = "BUG: ImportAsync allows zip-slip via Presentations/../ entry")]
    public async Task ImportAsync_MaliciousPresentationEntryWithDotDot_MustNotWriteOutsideTargetDirectory()
    {
        var sandboxRoot = Path.Combine(Path.GetTempPath(), "rrs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandboxRoot);
        _cleanupDirs.Add(sandboxRoot);

        var workbooksDir = Path.Combine(sandboxRoot, "workbooks");
        var presentationsDir = Path.Combine(sandboxRoot, "presentations");
        var victimDir = Path.Combine(sandboxRoot, "victim");
        Directory.CreateDirectory(workbooksDir);
        Directory.CreateDirectory(presentationsDir);
        Directory.CreateDirectory(victimDir);

        const string maliciousEntryName = "Presentations/../victim/pwned.txt";
        var zipPath = Path.Combine(sandboxRoot, "evil2.recipe");
        _cleanupFiles.Add(zipPath);

        await using (var fs = File.Create(zipPath))
        using (var zos = new ZipOutputStream(fs))
        {
            zos.SetLevel(0);
            await zos.PutNextEntryAsync(new ZipEntry("recipe.json") { DateTime = DateTime.UtcNow });
            var recipeBytes = "{}"u8.ToArray();
            zos.Write(recipeBytes, 0, recipeBytes.Length);
            zos.CloseEntry();

            await zos.PutNextEntryAsync(new ZipEntry(maliciousEntryName) { DateTime = DateTime.UtcNow });
            zos.Write("payload"u8.ToArray(), 0, 7);
            zos.CloseEntry();
            zos.Finish();
        }

        var act = async () => await _repo.ImportAsync(zipPath, null, workbooksDir, presentationsDir,
            TestContext.Current.CancellationToken);
        try
        {
            await act();
        }
        catch (InvalidDataException)
        {
            /* expected after fix */
        }

        File.Exists(Path.Combine(victimDir, "pwned.txt")).Should().BeFalse(
            "ImportAsync must sanitize Presentations entries the same way as Workbooks entries");
    }

    #endregion
}