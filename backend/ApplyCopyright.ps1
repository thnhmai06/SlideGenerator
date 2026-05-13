$YEAR = "2026"
$AUTHOR = "Thành Mai (thnhmai06)"
$SOLUTION_NAME = "SlideGenerator"
$REPO_URL = "https://github.com/thnhmai06/SlideGenerator"

$template = @"
/*
 * Copyright (C) ${YEAR} ${AUTHOR}
 *
 * Solution: ${SOLUTION_NAME}
 * Project: {0}
 * File: {1}
 *
 * This file is part of this solution. You can find the full source code here: ${REPO_URL}
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

"@

function Get-ProjectName($filePath) {
    $dir = Split-Path $filePath -Parent

    while ($dir -and (Test-Path $dir)) {
        $csproj = Get-ChildItem -Path $dir -Filter "*.csproj" -File -ErrorAction SilentlyContinue

        if ($csproj) {
            return $csproj[0].BaseName
        }

        $parent = Split-Path $dir -Parent

        if ($parent -eq $dir) {
            break
        }

        $dir = $parent
    }

    return $SOLUTION_NAME
}

function Remove-ExistingCopyrightHeaders($content) {
    # Remove UTF-8 BOM if it exists in the string
    $content = $content -replace "^\uFEFF", ""

    $changed = $true

    while ($changed) {
        $changed = $false

        # Remove block comment at the top if it contains "Copyright"
        #
        # Matches:
        # /*
        #  * Copyright ...
        #  */
        $blockPattern = "^\s*/\*[\s\S]*?\*/\s*"

        $newContent = [regex]::Replace(
            $content,
            $blockPattern,
            {
                param($match)

                if ($match.Value -match "(?i)Copyright") {
                    return ""
                }

                return $match.Value
            },
            1
        )

        if ($newContent -ne $content) {
            $content = $newContent
            $changed = $true
            continue
        }

        # Remove consecutive line comments at the top if they contain "Copyright"
        #
        # Matches:
        # // Copyright ...
        # // ...
        $linePattern = "^\s*(?://[^\r\n]*(?:\r?\n|$))+"

        $newContent = [regex]::Replace(
            $content,
            $linePattern,
            {
                param($match)

                if ($match.Value -match "(?i)Copyright") {
                    return ""
                }

                return $match.Value
            },
            1
        )

        if ($newContent -ne $content) {
            $content = $newContent
            $changed = $true
            continue
        }
    }

    return $content.TrimStart()
}

$files = Get-ChildItem -Recurse -Filter "*.cs" -File |
    Where-Object {
        $_.FullName -notmatch "\\bin\\" -and
        $_.FullName -notmatch "\\obj\\"
    }

foreach ($file in $files) {
    $filePath = $file.FullName

    $projectName = Get-ProjectName $filePath
    $fileName = $file.Name

    $header = $template -f $projectName, $fileName

    $content = Get-Content $filePath -Raw -Encoding utf8
    $contentWithoutOldHeaders = Remove-ExistingCopyrightHeaders $content

    $newContent = $header + $contentWithoutOldHeaders

    Write-Host "Updating copyright header in $filePath"

    $newContent | Out-File -FilePath $filePath -Encoding utf8
}

Write-Host "Done."