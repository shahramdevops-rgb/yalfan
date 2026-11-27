<#
.SYNOPSIS
    Runs every check CI runs and prints only a summary and what failed.

.DESCRIPTION
    The steps of .github/workflows/ci.yml for .NET and the panel: format, lint, build, tests and
    the panel's screenshots and behaviour tests (ADR 0028, ADR 0030).
    Each step's full output goes to artifacts/check/<step>.log. The console gets one line per
    step with the test counts, and for a failed step only what is needed to fix it: compiler
    messages once each, failed tests grouped by their error. Why (ADR 0017): one failing test run
    printed 4,277 lines, the same error 104 times, because Docker was not running.

    As in CI, every step runs even when an earlier one failed; a step is skipped only when what it
    needs is missing (the tests need the build, the panel steps need node_modules).

    Works in Windows PowerShell 5.1 and PowerShell 7. Keep this file ASCII: Windows PowerShell
    reads a file without a byte order mark in the ANSI code page.

.PARAMETER Only
    dotnet or panel to run one side; both by default.

.EXAMPLE
    ./scripts/check.ps1

.EXAMPLE
    ./scripts/check.ps1 -Only dotnet
#>
[CmdletBinding()]
param(
    [ValidateSet('all', 'dotnet', 'panel')]
    [string]$Only = 'all'
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$logDir = Join-Path $repoRoot 'artifacts\check'
$ansi = "$([char]27)\[[0-9;?]*[A-Za-z]"
$maxLines = 40
$maxLineLength = 300
$results = [System.Collections.Generic.List[object]]::new()
# Roots cut from paths in the output, so `D:\...\Yalfan\src\X.cs` prints as `src\X.cs`.
$roots = [System.Collections.Generic.List[string]]::new()
$roots.Add($repoRoot)

function Invoke-Logged {
    # Runs a command in a directory and saves everything it printed to artifacts/check/<Name>.log.
    param([string]$Name, [string]$Directory, [string]$Command, [string[]]$Arguments)

    $log = Join-Path $logDir "$Name.log"
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $lines = @("'$Command' was not found on PATH.")
    $exitCode = 127
    if (Get-Command $Command -ErrorAction SilentlyContinue) {
        Push-Location -LiteralPath $Directory
        # PowerShell wraps every stderr line in an error record: with 'Stop' the first one would end
        # the script, and the record of an empty line prints as its exception's type name, so the
        # text comes from Exception.Message.
        $ErrorActionPreference = 'Continue'
        try {
            $lines = @(& $Command @Arguments 2>&1 | ForEach-Object {
                    $text = if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { $_ }
                    ([string]$text) -replace $ansi, ''
                })
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = 'Stop'
            Pop-Location
        }
    }
    [IO.File]::WriteAllLines($log, [string[]]$lines)
    [pscustomobject]@{ ExitCode = $exitCode; Lines = $lines; Elapsed = $watch.Elapsed; Log = $log }
}

function Write-Step {
    param([string]$Status, [string]$Name, $Run, [string]$Note, [string[]]$Details)

    $time = ''
    if ($Run) {
        $seconds = [int][Math]::Round($Run.Elapsed.TotalSeconds)
        $time = if ($seconds -lt 60) { "${seconds}s" } else { "$([Math]::Floor($seconds / 60))m$(($seconds % 60).ToString('00'))s" }
    }
    $line = $Status.PadRight(6) + $Name.PadRight(15) + $time.PadLeft(6)
    if ($Note) { $line += "  $Note" }
    $color = switch ($Status) { 'PASS' { 'Green' } 'FAIL' { 'Red' } default { 'Yellow' } }
    Write-Host $line -ForegroundColor $color
    foreach ($detail in @($Details)) {
        if ($null -ne $detail) { Write-Host ('      ' + (Format-Line $detail)) }
    }
    if ($Status -eq 'FAIL' -and $Run) { Write-Host ('      log: ' + (Format-Line $Run.Log)) }
    $results.Add([pscustomobject]@{ Name = $Name; Status = $Status })
}

function Format-Line([string]$Line) {
    foreach ($root in $roots) { $Line = $Line -replace [regex]::Escape("$root\"), '' }
    if ($Line.Length -gt $maxLineLength) { $Line = $Line.Substring(0, $maxLineLength) + ' ...' }
    return $Line
}

function Get-Shortened([string[]]$Lines) {
    # Without blank lines, npm's "> script" banners and Vitest's passed files; head and tail when
    # still long.
    $passed = [string][char]0x2713
    $kept = @($Lines | Where-Object { $_.Trim() -and $_ -notmatch '^> ' -and -not $_.TrimStart().StartsWith($passed) })
    if ($kept.Count -le $maxLines) { return $kept }
    $tail = 10
    $head = $maxLines - $tail
    return @($kept[0..($head - 1)]) + "... $($kept.Count - $maxLines) lines left out, see the log ..." +
        @($kept[($kept.Count - $tail)..($kept.Count - 1)])
}

function Get-Diagnostics([string[]]$Lines) {
    # Compiler, analyzer and format messages, each once: MSBuild repeats every one in its summary.
    $found = @($Lines |
        Where-Object { $_ -match ': (error|warning) [A-Za-z]+[0-9]*\s*:' } |
        ForEach-Object { ($_ -replace '\s*\[[^\[\]]+\]\s*$', '').Trim() } |
        Select-Object -Unique)
    if ($found.Count -eq 0) { return @(Get-Shortened $Lines) }
    if ($found.Count -le $maxLines) { return $found }
    return @($found[0..($maxLines - 1)]) + "... $($found.Count - $maxLines) more, see the log ..."
}

function Get-ShortTestName([string]$FullName) {
    # Namespace.Class.Method(arguments) -> Class.Method(arguments)
    $paren = $FullName.IndexOf('(')
    $method = if ($paren -ge 0) { $FullName.Substring(0, $paren) } else { $FullName }
    $arguments = if ($paren -ge 0) { $FullName.Substring($paren) } else { '' }
    return (@($method.Split('.') | Select-Object -Last 2) -join '.') + $arguments
}

function Get-TestReport([string[]]$Lines) {
    # Microsoft.Testing.Platform prints "failed <test> (<time>)", then "from <dll>", the error and
    # its stack trace, all indented; the counts come last. Tests with the same error are grouped,
    # so a broken shared fixture prints once, not once per test.
    $counts = @{}
    $groups = [ordered]@{}
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match '^\s+(total|failed|succeeded|skipped):\s+(\d+)\s*$') {
            $counts[$Matches[1]] = [int]$Matches[2]
            continue
        }
        if ($Lines[$i] -notmatch '^failed (.+) \([^()]*\)\s*$') { continue }
        $test = Get-ShortTestName $Matches[1]
        $message = [System.Collections.Generic.List[string]]::new()
        $frame = $null
        $inStack = $false
        for ($j = $i + 1; $j -lt $Lines.Count -and $Lines[$j] -match '^\s'; $j++) {
            $next = $Lines[$j].Trim()
            if ($next.StartsWith('at ')) {
                $inStack = $true
                # The first frame in our own code says where to look.
                if (-not $frame -and $next -match '^at Yalfan\..+ in .+:\d+$') { $frame = $next }
            }
            elseif (-not $inStack -and $next -and -not $next.StartsWith('from ') -and $message.Count -lt 8) {
                $message.Add($next)
            }
        }
        $key = $message -join "`n"
        if (-not $groups.Contains($key)) {
            $groups[$key] = [pscustomobject]@{
                Tests = [System.Collections.Generic.List[string]]::new()
                Message = $message
                Frame = $frame
            }
        }
        $groups[$key].Tests.Add($test)
    }
    # No counts: the test run itself broke; the caller shows the output instead.
    if ($null -eq $counts['total']) { return $null }

    $summary = "$($counts['total']) total, $($counts['succeeded']) passed, $($counts['failed']) failed"
    if ($counts['skipped']) { $summary += ", $($counts['skipped']) skipped" }
    $details = [System.Collections.Generic.List[string]]::new()
    $shown = 0
    foreach ($group in $groups.Values) {
        if ($shown -eq 10) {
            $details.Add("... $($groups.Count - $shown) more different errors, see the log ...")
            break
        }
        $names = @($group.Tests | Select-Object -First 3) -join ', '
        if ($group.Tests.Count -gt 3) { $names += ", +$($group.Tests.Count - 3) more" }
        $details.Add("$($group.Tests.Count) x $names")
        foreach ($line in $group.Message) { $details.Add("  $line") }
        if ($group.Frame) { $details.Add("  $($group.Frame)") }
        $shown++
    }
    [pscustomobject]@{ Summary = $summary; Details = $details.ToArray() }
}

function Get-ScreensReport([string[]]$Lines) {
    # Playwright's list reporter ends with a block per failed test, headed
    # "  1) [phone] > screens/pages.spec.ts:44:3 > contractors ---", then "Error: ..." and the reason
    # on the next line ("578 pixels ... are different."). Each failed picture prints as its page,
    # its width and that reason; the counts are Playwright's own "28 passed" lines.
    $counts = @($Lines | Where-Object { $_ -match '^\s+\d+ (passed|failed|flaky|did not run)' } | ForEach-Object { $_.Trim() })
    $details = [System.Collections.Generic.List[string]]::new()
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -notmatch '^\s+\d+\) \[(\w+)\] \S+ \S+ \S+ (\S+)') { continue }
        $page = "$($Matches[2]) ($($Matches[1]))"
        $reason = @()
        for ($j = $i + 1; $j -lt $Lines.Count -and $Lines[$j] -notmatch '^\s+\d+\) \['; $j++) {
            if ($Lines[$j] -notmatch '^\s*Error: (.+)$') { continue }
            $reason = @($Matches[1].Trim())
            $next = @($Lines[($j + 1)..([Math]::Min($j + 3, $Lines.Count - 1))] | Where-Object { $_.Trim() } | Select-Object -First 1)
            if ($next.Count -gt 0) { $reason += $next[0].Trim() }
            break
        }
        $details.Add("$page`: $($reason -join ' ')")
    }
    [pscustomobject]@{ Summary = ($counts -join ', '); Details = $details.ToArray() }
}

function Test-Docker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { return $false }
    $ErrorActionPreference = 'Continue'
    & docker info --format '{{.ServerVersion}}' 2>&1 | Out-Null
    return $LASTEXITCODE -eq 0
}

function Invoke-DotnetChecks {
    $format = Invoke-Logged -Name 'dotnet-format' -Directory $repoRoot -Command 'dotnet' `
        -Arguments @('format', 'whitespace', 'Yalfan.slnx', '--verify-no-changes')
    if ($format.ExitCode -eq 0) { Write-Step -Status PASS -Name 'dotnet format' -Run $format }
    else {
        Write-Step -Status FAIL -Name 'dotnet format' -Run $format -Note 'fix: dotnet format whitespace Yalfan.slnx' `
            -Details (Get-Diagnostics $format.Lines)
    }

    $build = Invoke-Logged -Name 'dotnet-build' -Directory $repoRoot -Command 'dotnet' -Arguments @('build', 'Yalfan.slnx')
    if ($build.ExitCode -eq 0) { Write-Step -Status PASS -Name 'dotnet build' -Run $build }
    else {
        Write-Step -Status FAIL -Name 'dotnet build' -Run $build -Details (Get-Diagnostics $build.Lines)
        Write-Step -Status SKIP -Name 'dotnet test' -Note 'needs a passing build'
        return
    }

    # Checked first because without Docker every integration test fails with the same long error.
    if (-not (Test-Docker)) {
        Write-Step -Status FAIL -Name 'dotnet test' -Note 'Docker is not running' `
            -Details @('The integration tests start PostgreSQL with Testcontainers. Start Docker Desktop and run again.')
        return
    }
    $test = Invoke-Logged -Name 'dotnet-test' -Directory $repoRoot -Command 'dotnet' `
        -Arguments @('test', '--solution', 'Yalfan.slnx', '--no-build')
    $report = Get-TestReport $test.Lines
    $status = if ($test.ExitCode -eq 0) { 'PASS' } else { 'FAIL' }
    if ($null -eq $report) {
        $details = if ($status -eq 'FAIL') { Get-Shortened $test.Lines } else { $null }
        Write-Step -Status $status -Name 'dotnet test' -Run $test -Details $details
    }
    else {
        Write-Step -Status $status -Name 'dotnet test' -Run $test -Note $report.Summary -Details $report.Details
    }
}

function Get-SubstRoot {
    # Vite and Vitest cannot run from a path that contains '#' (ADR 0014). A subst drive letter
    # hides it: an existing one when it covers the checkout, otherwise a free letter that is
    # removed again at the end.
    if ($repoRoot -notmatch '#') { return @{ Root = $repoRoot; Drive = $null } }
    foreach ($mapping in @(subst)) {
        if ($mapping -notmatch '^([A-Z]):\\: => (.+)$') { continue }
        $letter = $Matches[1]
        $target = $Matches[2].TrimEnd('\')
        if ($repoRoot -eq $target -or $repoRoot.StartsWith("$target\", [StringComparison]::OrdinalIgnoreCase)) {
            return @{ Root = "${letter}:" + $repoRoot.Substring($target.Length); Drive = $null }
        }
    }
    $used = @([IO.Directory]::GetLogicalDrives() | ForEach-Object { $_.Substring(0, 1).ToUpperInvariant() })
    $free = @('Z', 'Y', 'X', 'W', 'V', 'U', 'T', 'S', 'R', 'Q' | Where-Object { $used -notcontains $_ })
    if ($free.Count -eq 0) { throw "No free drive letter for subst; the panel cannot run from '$repoRoot'." }
    & subst "$($free[0]):" $repoRoot | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "subst $($free[0]): '$repoRoot' failed." }
    return @{ Root = "$($free[0]):"; Drive = "$($free[0]):" }
}

function Invoke-PanelChecks {
    $names = @('panel lint', 'panel format', 'panel build', 'panel test', 'panel screens')
    $panelOnDisk = Join-Path $repoRoot 'web\panel'
    $npm = if ($env:OS -eq 'Windows_NT') { 'npm.cmd' } else { 'npm' }
    $subst = Get-SubstRoot
    try {
        $panel = "$($subst.Root)\web\panel"
        if ($subst.Root -ne $repoRoot) { $roots.Add($subst.Root) }

        # npm ci only when package-lock.json is newer than the last install, because it deletes
        # and rewrites node_modules every time.
        $installed = Join-Path $panelOnDisk 'node_modules\.package-lock.json'
        $lock = Join-Path $panelOnDisk 'package-lock.json'
        if (-not (Test-Path -LiteralPath $installed) -or
            (Get-Item -LiteralPath $installed).LastWriteTimeUtc -lt (Get-Item -LiteralPath $lock).LastWriteTimeUtc) {
            $install = Invoke-Logged -Name 'panel-install' -Directory $panel -Command $npm -Arguments @('ci')
            if ($install.ExitCode -ne 0) {
                Write-Step -Status FAIL -Name 'panel install' -Run $install -Details (Get-Shortened $install.Lines)
                foreach ($name in $names) { Write-Step -Status SKIP -Name $name -Note 'needs npm ci' }
                return
            }
            Write-Step -Status PASS -Name 'panel install' -Run $install
        }

        $steps = @(
            @{ Name = $names[0]; Log = 'panel-lint'; Arguments = @('run', 'lint'); Fix = '' },
            @{ Name = $names[1]; Log = 'panel-format'; Arguments = @('run', 'format:check'); Fix = 'fix: npm run format (in web/panel)' },
            @{ Name = $names[2]; Log = 'panel-build'; Arguments = @('run', 'build'); Fix = '' },
            @{ Name = $names[3]; Log = 'panel-test'; Arguments = @('test'); Fix = '' }
        )
        foreach ($step in $steps) {
            $run = Invoke-Logged -Name $step.Log -Directory $panel -Command $npm -Arguments $step.Arguments
            if ($run.ExitCode -eq 0) {
                # Vitest's own count line, e.g. "Tests 12 passed (12)".
                $counts = @($run.Lines | Where-Object { $_ -match '^\s*Tests\s+\d' } | Select-Object -Last 1)
                $note = if ($counts.Count -gt 0) { $counts[0].Trim() -replace '\s+', ' ' } else { '' }
                Write-Step -Status PASS -Name $step.Name -Run $run -Note $note
            }
            else {
                Write-Step -Status FAIL -Name $step.Name -Run $run -Note $step.Fix -Details (Get-Shortened $run.Lines)
            }
        }

        # The screenshots of the main pages, compared inside the pinned Playwright image (ADR 0028).
        if (-not (Test-Docker)) {
            Write-Step -Status FAIL -Name $names[4] -Note 'Docker is not running' `
                -Details @('The screenshots run in the Playwright image. Start Docker Desktop and run again.')
            return
        }
        $screens = Invoke-Logged -Name 'panel-screens' -Directory $panel -Command $npm -Arguments @('run', 'screens')
        $report = Get-ScreensReport $screens.Lines
        if ($screens.ExitCode -eq 0) { Write-Step -Status PASS -Name $names[4] -Run $screens -Note $report.Summary }
        elseif ($report.Details.Count -gt 0) {
            Write-Step -Status FAIL -Name $names[4] -Run $screens -Note $report.Summary -Details ($report.Details + @(
                    'pictures: artifacts\check\screens (expected, actual, diff)',
                    'if the change is intended: npm run screens:update (in web/panel), then review the pictures in git'))
        }
        else {
            Write-Step -Status FAIL -Name $names[4] -Run $screens -Details (Get-Shortened $screens.Lines)
        }
    }
    finally {
        if ($subst.Drive) { & subst $subst.Drive /D | Out-Null }
    }
}

# Tools print UTF-8; read it as such so Persian test names stay readable in the logs.
$originalEncoding = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false) } catch { Write-Verbose 'No console to set.' }
try {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    Get-ChildItem -LiteralPath $logDir -Filter '*.log' | Remove-Item -Force
    if ($Only -ne 'panel') { Invoke-DotnetChecks }
    if ($Only -ne 'dotnet') { Invoke-PanelChecks }
}
finally {
    try { [Console]::OutputEncoding = $originalEncoding } catch { Write-Verbose 'No console to reset.' }
}

$notPassed = @($results | Where-Object { $_.Status -ne 'PASS' })
if ($notPassed.Count -eq 0) {
    Write-Host "All $($results.Count) checks passed." -ForegroundColor Green
    exit 0
}
$list = @($notPassed | ForEach-Object { $_.Name }) -join ', '
Write-Host "$($notPassed.Count) of $($results.Count) checks did not pass: $list. Logs: artifacts\check\" -ForegroundColor Red
exit 1
