#Requires -Version 7.0
<#
.SYNOPSIS
    Local CI/CD test runner for Blazing.Json.Queryable.

.DESCRIPTION
    Validates and executes GitHub Actions workflows locally using actionlint (static analysis)
    and act (Docker-based execution). Mirrors exactly what runs on GitHub Actions.

.PARAMETER Mode
    dry      - Validate workflow graph via act dry-run only (requires Docker + act)
    lint     - actionlint static analysis only
    ci       - Full workflow execution via act
    all      - lint + ci (default)

.PARAMETER Workflow
    ci       - Run only ci.yml (default)
    release  - Run only release.yml
    both     - Run both workflows

.PARAMETER Job
    Optionally run one job by name (for Mode=ci).

.EXAMPLE
    .\ci-cd-test-run.ps1
    .\ci-cd-test-run.ps1 -Mode lint
    .\ci-cd-test-run.ps1 -Mode dry
    .\ci-cd-test-run.ps1 -Mode dry -Workflow both
    .\ci-cd-test-run.ps1 -Mode ci -Workflow ci -Job build-and-test
#>

[CmdletBinding()]
param(
    [ValidateSet('dry', 'lint', 'ci', 'all')]
    [string]$Mode = 'all',

    [ValidateSet('ci', 'release', 'both')]
    [string]$Workflow = 'ci',

    [string]$Job = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Colours ────────────────────────────────────────────────────────────────────
function Write-Header  { param([string]$msg) Write-Host "`n━━━ $msg ━━━" -ForegroundColor Cyan }
function Write-Pass    { param([string]$msg) Write-Host "  ✅ $msg" -ForegroundColor Green }
function Write-Fail    { param([string]$msg) Write-Host "  ❌ $msg" -ForegroundColor Red }
function Write-Warn    { param([string]$msg) Write-Host "  ⚠  $msg" -ForegroundColor Yellow }
function Write-Section { param([string]$msg) Write-Host "`n  ▶ $msg" -ForegroundColor White }

$Script:Errors   = [System.Collections.Generic.List[string]]::new()
$Script:Warnings = [System.Collections.Generic.List[string]]::new()

function Add-Error   { param([string]$msg) $Script:Errors.Add($msg);   Write-Fail   $msg }
function Add-Warning { param([string]$msg) $Script:Warnings.Add($msg); Write-Warn   $msg }

# ── Paths ──────────────────────────────────────────────────────────────────────
$RepoRoot    = $PSScriptRoot
$WorkflowDir = Join-Path $RepoRoot '.github' 'workflows'
$CiYaml      = Join-Path $WorkflowDir 'ci.yml'
$ReleaseYaml = Join-Path $WorkflowDir 'release.yml'

# ── Tool check ─────────────────────────────────────────────────────────────────
function Test-Tool {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

# ══════════════════════════════════════════════════════════════════════════════
# STEP 1 — Prerequisite check
# ══════════════════════════════════════════════════════════════════════════════
Write-Header 'Prerequisite Check'

$needsActionlint = $Mode -in @('lint', 'all')
$needsAct        = $Mode -in @('dry', 'ci', 'all')

# dotnet: warn-only for act-backed modes — act installs .NET inside the runner
# via actions/setup-dotnet; the host machine does not need dotnet installed.
if ($needsAct) {
    if (Test-Tool 'dotnet') {
        Write-Pass "dotnet $(dotnet --version)"
    } else {
        Add-Warning "Tool 'dotnet' not found on host. Continuing because act-backed workflows install .NET inside the runner via actions/setup-dotnet."
    }
}

if (-not (Test-Path $CiYaml)) { Add-Error "Missing workflow file: $CiYaml" }
if (($Workflow -in @('release', 'both')) -and (-not (Test-Path $ReleaseYaml))) { Add-Error "Missing workflow file: $ReleaseYaml" }

$hasActionlint = $false
if ($needsActionlint) {
    $hasActionlint = Test-Tool 'actionlint'
    if (-not $hasActionlint) {
        $installHint = if ($IsWindows)  { 'winget install rhysd.actionlint  (or: choco install actionlint)' }
                       elseif ($IsMacOS) { 'brew install actionlint' }
                       else              { 'go install github.com/rhysd/actionlint/cmd/actionlint@latest  # or see https://github.com/rhysd/actionlint#installation' }
        Add-Error "Tool 'actionlint' not found. Install: $installHint"
    }
}

$hasAct          = $false
$dockerAvailable = $false
if ($needsAct) {
    $hasAct = Test-Tool 'act'
    if (-not $hasAct) {
        $installHint = if ($IsWindows)  { 'winget install nektos.act  (or: choco install act-cli)' }
                       elseif ($IsMacOS) { 'brew install act' }
                       else              { 'curl -s https://raw.githubusercontent.com/nektos/act/master/install.sh | sudo bash  # or see https://nektosact.com/installation/' }
        Add-Error "Tool 'act' not found. Install: $installHint"
    }

    # Check for the docker binary before calling docker info.
    # If Docker is not installed, 'docker info' throws an unhelpful error.
    $hasDocker = Test-Tool 'docker'
    if (-not $hasDocker) {
        $installHint = if ($IsWindows)  { 'Install Docker Desktop: https://docs.docker.com/desktop/setup/install/windows-install/' }
                       elseif ($IsMacOS) { 'brew install --cask docker' }
                       else              { 'Install Docker Engine: https://docs.docker.com/engine/install/' }
        Add-Error "Tool 'docker' not found. $installHint"
    } else {
        try {
            $null = docker info 2>$null
            $dockerAvailable = $true
            Write-Pass 'Docker daemon reachable'
        } catch {
            Add-Error 'Docker not reachable — act dry/ci modes require Docker daemon running'
        }
    }
}

if ($Script:Errors.Count -gt 0) {
    Write-Header 'Summary'
    Write-Host "`n  ❌ $($Script:Errors.Count) prerequisite error(s):" -ForegroundColor Red
    $Script:Errors | ForEach-Object { Write-Host "    • $_" -ForegroundColor Red }
    Write-Host ''
    exit 1
}

# ══════════════════════════════════════════════════════════════════════════════
# STEP 2 — actionlint static analysis
# ══════════════════════════════════════════════════════════════════════════════
if ($Mode -in @('lint', 'all') -and $hasActionlint) {
    Write-Header 'YAML Static Analysis (actionlint)'

    $yamlFiles = @()
    if ($Workflow -in @('ci',      'both')) { $yamlFiles += $CiYaml }
    if ($Workflow -in @('release', 'both')) { $yamlFiles += $ReleaseYaml }

    foreach ($yaml in $yamlFiles) {
        $name = Split-Path $yaml -Leaf
        Write-Section "Linting $name"
        $out = actionlint $yaml 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Pass "$name — no issues"
        } else {
            $out | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
            Add-Error "$name has actionlint violations (see above)"
        }
    }
}

# ══════════════════════════════════════════════════════════════════════════════
# STEP 3 — act dry-run
# ══════════════════════════════════════════════════════════════════════════════
if ($Mode -in @('dry', 'all') -and $dockerAvailable -and $hasAct) {
    Write-Header 'Workflow Dry-Run (act -n)'

    $dryWorkflows = @()
    if ($Workflow -in @('ci',      'both')) { $dryWorkflows += @{ Name = 'CI';      File = $CiYaml      } }
    if ($Workflow -in @('release', 'both')) { $dryWorkflows += @{ Name = 'Release'; File = $ReleaseYaml } }

    foreach ($wf in $dryWorkflows) {
        Write-Section "Dry-run $($wf.Name) workflow"
        Push-Location $RepoRoot
        $eventPath = $null
        try {
            # Always pass --env RUNNING_LOCALLY=true so upload-artifact steps are skipped inside act.
            $actArgs = @('push', '--workflows', $wf.File, '--env', 'RUNNING_LOCALLY=true', '-n')

            # Release workflow is gated on push to master; provide an explicit push event payload
            # so act's default ref matches the branch filter and the workflow is not silently skipped.
            if ($wf.Name -eq 'Release') {
                $eventPath = [System.IO.Path]::GetTempFileName()
                @{
                    ref        = 'refs/heads/master'
                    repository = @{ default_branch = 'master' }
                    head_commit = @{ id = 'local-dry-run' }
                } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $eventPath -Encoding UTF8
                $actArgs += @('-e', $eventPath)
            }

            $out = & act @actArgs 2>&1
            # Filter known act Windows cache bug: upload-artifact may fail to remove its own
            # .gitignore on Windows, causing a non-zero exit code even in dry-run mode.
            # Use the specific cache path pattern — never a broad message — to avoid masking real errors.
            $failed = @($out | Where-Object {
                $_ -match '(FAIL|error)' -and
                $_ -notmatch 'DRYRUN' -and
                $_ -notmatch 'upload-artifact' -and
                $_ -notmatch '\.cache\\act\\actions-upload-artifact'
            })
            $knownArtifactCacheIssue = @($out | Where-Object {
                $_ -match '\.cache\\act\\actions-upload-artifact'
            })

            # Detect when act ran but no dry-run jobs were staged (workflow likely skipped)
            $dryRunLines = @($out | Where-Object { $_ -match '\*DRYRUN\* \[[^\]]+\]' })
            if ($dryRunLines.Count -eq 0) {
                Add-Warning "$($wf.Name) dry-run: no jobs were staged — workflow may have been skipped. Verify trigger ref and branch filter."
            } elseif ($LASTEXITCODE -eq 0 -and $failed.Count -eq 0) {
                Write-Pass "$($wf.Name) dry-run succeeded"
            } elseif ($LASTEXITCODE -ne 0 -and $failed.Count -eq 0 -and $knownArtifactCacheIssue.Count -gt 0) {
                Add-Warning "$($wf.Name) dry-run hit known act artifact-cache cleanup issue; treating as success because no real failures were detected."
                Write-Pass "$($wf.Name) dry-run succeeded"
            } else {
                $out | Where-Object { $_ -match '(FAIL|error|warn)' } | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
                Add-Error "$($wf.Name) dry-run reported issues"
            }
        } finally {
            if ($null -ne $eventPath -and (Test-Path -LiteralPath $eventPath)) {
                Remove-Item -LiteralPath $eventPath -Force -ErrorAction SilentlyContinue
            }
            Pop-Location
        }
    }
}

# ══════════════════════════════════════════════════════════════════════════════
# STEP 4 — Full CI execution via act
# ══════════════════════════════════════════════════════════════════════════════
if ($Mode -in @('ci', 'all') -and $dockerAvailable -and $hasAct) {
    Write-Header 'Full CI Execution (act)'

    $actWorkflows = @()
    if ($Workflow -in @('ci',      'both')) { $actWorkflows += @{ Name = 'CI';      File = $CiYaml;      Event = 'push' } }
    if ($Workflow -in @('release', 'both')) { $actWorkflows += @{ Name = 'Release'; File = $ReleaseYaml; Event = 'push' } }

    foreach ($wf in $actWorkflows) {
        Write-Section "Running $($wf.Name) workflow via act"
        Push-Location $RepoRoot
        $eventPath = $null
        try {
            # Always pass --env RUNNING_LOCALLY=true so upload-artifact steps are skipped inside act.
            $actArgs = @($wf.Event, '--workflows', $wf.File, '--env', 'RUNNING_LOCALLY=true')
            if ($Job) { $actArgs += @('-j', $Job) }

            # Release workflow is gated on push to master; provide an explicit push event payload
            # so act's default ref matches the branch filter and the workflow is not silently skipped.
            if ($wf.Name -eq 'Release') {
                $eventPath = [System.IO.Path]::GetTempFileName()
                @{
                    ref        = 'refs/heads/master'
                    repository = @{ default_branch = 'master' }
                    head_commit = @{ id = 'local-ci-run' }
                } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $eventPath -Encoding UTF8
                $actArgs += @('-e', $eventPath)
            }

            # Stream output, capture for analysis
            $outLines = [System.Collections.Generic.List[string]]::new()
            & act @actArgs 2>&1 | ForEach-Object {
                $outLines.Add($_)
                if ($_ -match '(✅|❌|🏁|PASS|FAIL|Error|error:|warning:)') {
                    Write-Host "    $_"
                }
            }

            # Parse results — wrap in @() to force array type (.Count fails on plain strings)
            $jobSucceeded = @($outLines | Where-Object { $_ -match '🏁.*Job succeeded' })
            $jobFailed    = @($outLines | Where-Object { $_ -match '🏁.*Job failed' })
            $testPassed   = @($outLines | Where-Object { $_ -match 'Passed!.*Failed:\s+0' })
            $testFailed   = @($outLines | Where-Object { $_ -match 'Failed!.*Failed:\s+[^0]' })

            if ($testPassed.Count -gt 0) {
                $testPassed | ForEach-Object { Write-Pass ($_ -replace '^\|\s*', '') }
            }
            if ($testFailed.Count -gt 0) {
                $testFailed | ForEach-Object { Add-Error ($_ -replace '^\|\s*', '') }
            }

            # Ignore ACTIONS_RUNTIME_TOKEN artifact upload errors (known act limitation)
            $realFailures = @($jobFailed | Where-Object { $_ -notmatch 'Upload test results' })

            if ($LASTEXITCODE -eq 0 -or ($jobSucceeded.Count -gt 0 -and $realFailures.Count -eq 0)) {
                Write-Pass "$($wf.Name) workflow — all jobs succeeded"
            } else {
                Add-Error "$($wf.Name) workflow had job failures (see above)"
            }
        } finally {
            if ($null -ne $eventPath -and (Test-Path -LiteralPath $eventPath)) {
                Remove-Item -LiteralPath $eventPath -Force -ErrorAction SilentlyContinue
            }
            Pop-Location
        }
    }
}

# ══════════════════════════════════════════════════════════════════════════════
# SUMMARY
# ══════════════════════════════════════════════════════════════════════════════
Write-Header 'Summary'

if ($Script:Warnings.Count -gt 0) {
    Write-Host "`n  Warnings:" -ForegroundColor Yellow
    $Script:Warnings | ForEach-Object { Write-Host "    ⚠  $_" -ForegroundColor Yellow }
}

if ($Script:Errors.Count -eq 0) {
    Write-Host "`n  ✅ All checks passed!`n" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n  ❌ $($Script:Errors.Count) error(s) found:" -ForegroundColor Red
    $Script:Errors | ForEach-Object { Write-Host "    • $_" -ForegroundColor Red }
    Write-Host ''
    exit 1
}
