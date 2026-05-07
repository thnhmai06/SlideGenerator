$YEAR = "2026"
$AUTHOR = "Thành Mai"
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
        $csproj = Get-ChildItem -Path $dir -Filter "*.csproj" -ErrorAction SilentlyContinue
        if ($csproj) {
            return $csproj[0].BaseName
        }
        $parent = Split-Path $dir -Parent
        if ($parent -eq $dir) { break }
        $dir = $parent
    }
    return "SlideGenerator"
}

$files = Get-ChildItem -Recurse -Filter "*.cs" -Exclude "bin","obj"
foreach ($file in $files) {
    $filePath = $file.FullName
    if ($filePath -match "\\bin\\" -or $filePath -match "\\obj\\") { continue }
    
    $projectName = Get-ProjectName $filePath
    $fileName = $file.Name
    $header = $template -f $projectName, $fileName
    
    # Read with UTF8 to preserve characters
    $content = Get-Content $filePath -Raw -Encoding utf8
    if (-not $content.StartsWith("/*`r`n * Copyright")) {
        Write-Host "Applying header to $filePath"
        $newContent = $header + $content
        # Write with UTF8 with BOM for C# files
        $newContent | Out-File -FilePath $filePath -Encoding utf8
    } else {
        Write-Host "Header already exists in $filePath"
    }
}

# JetBrains Header Template does not apply on exists files, that's so suck.