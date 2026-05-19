$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$uiIndex = Join-Path $root 'ui_kits/slidegenerator/index.html'
$legacyRootHtml = Join-Path $root (('Pipe' + 'line') + '.html')
$studioApp = Join-Path $root 'app/studio-app.js'
$studioData = Join-Path $root 'app/studio-data.js'

function Assert-Contains {
  param(
    [string]$Path,
    [string]$Pattern,
    [string]$Message
  )

  $content = Get-Content -Raw -Path $Path
  if ($content -notmatch $Pattern) {
    throw $Message
  }
}

function Assert-NotContains {
  param(
    [string]$Path,
    [string]$Pattern,
    [string]$Message
  )

  $content = Get-Content -Raw -Path $Path
  if ($content -match $Pattern) {
    throw $Message
  }
}

Assert-Contains $uiIndex '<title>SlideGenerator\s+—\s+Studio UI Kit</title>' 'ui_kits entry must be the canonical Studio UI Kit page.'
Assert-Contains $uiIndex '\.\./\.\./app/styles\.css' 'ui_kits entry must load the shared Studio stylesheet with rebased paths.'
Assert-Contains $uiIndex '\.\./\.\./app/studio-data\.js' 'ui_kits entry must load Studio data with rebased paths.'
Assert-Contains $uiIndex '\.\./\.\./app/studio-app\.js' 'ui_kits entry must load Studio app with rebased paths.'
Assert-Contains $uiIndex 'class="components-menu"' 'ui_kits entry must expose a compact Components link menu.'

@(
  'icons.html',
  'sidebar-items.html',
  'buttons.html',
  'cards.html',
  'inputs.html',
  'colors-primary.html',
  'type-scale.html',
  'motion.html'
) | ForEach-Object {
  Assert-Contains $uiIndex "\.\./\.\./preview/$([regex]::Escape($_))" "Components menu must link to preview/$_"
}

if (Test-Path $legacyRootHtml) {
  throw 'Legacy root HTML entry must be removed; ui_kits/slidegenerator/index.html is the canonical entry.'
}

@(
  'App.jsx',
  'Sidebar.jsx',
  'components.jsx',
  'CreateTaskScreen.jsx',
  'ProcessScreen.jsx',
  'ResultsScreen.jsx',
  'SettingsScreen.jsx',
  'AboutScreen.jsx',
  'ui-kit.css'
) | ForEach-Object {
  $path = Join-Path $root "ui_kits/slidegenerator/$_"
  if (Test-Path $path) {
    throw "Obsolete UI kit file still exists: $_"
  }
}

$retiredWordmark = 'logo-' + 'text.png'
if (Test-Path (Join-Path $root "assets/$retiredWordmark")) {
  throw 'Retired duplicate wordmark asset must be removed; use assets/app-name.png.'
}

$legacyIconDir = Join-Path $root 'assets/icons'
if (Test-Path $legacyIconDir) {
  throw 'assets/icons must be removed; use assets/app-icon.png and inline Studio SVG icons.'
}

@('assets/app-icon.png', 'assets/app-name.png') | ForEach-Object {
  if (-not (Test-Path (Join-Path $root $_))) {
    throw "Required brand asset missing: $_"
  }
}

@('assets/logo-icon.png', 'assets/logo-name.png') | ForEach-Object {
  if (Test-Path (Join-Path $root $_)) {
    throw "Retired brand asset still exists: $_"
  }
}

$trackedTextFiles = @(
  'README.md',
  'SKILL.md',
  'Splash.html',
  'About.html',
  'ui_kits/slidegenerator/index.html',
  'ui_kits/slidegenerator/README.md',
  'preview/icons.html',
  'preview/sidebar-items.html',
  'app/studio-data.js',
  'app/studio-app.js'
)

$trackedTextFiles | ForEach-Object {
  $path = Join-Path $root $_
  $retiredTerms = '(Pipe' + 'line)|pipe' + 'line-data|pipe' + 'line-app|logo-' + 'text|logo-icon|logo-name|assets/icons'
  Assert-NotContains $path $retiredTerms "$_ contains retired Studio terminology or duplicate wordmark references."
}

$studioAppContent = Get-Content -Raw -Path $studioApp
$renderHierarchyIndex = $studioAppContent.IndexOf('function renderHierarchy(readonly)')
$extIndex = $studioAppContent.IndexOf('const ext = EXTENSIONS.find(e => e.id === c.extension);', [Math]::Max(0, $renderHierarchyIndex))
if ($renderHierarchyIndex -lt 0 -or $extIndex -lt 0 -or ($extIndex - $renderHierarchyIndex) -gt 240) {
  throw 'renderHierarchy must resolve the selected extension in its own scope.'
}

Assert-Contains $studioData "name: 'Run #042'" 'Mock run name must use Run terminology.'

Write-Host 'Studio UI kit checks passed.'
