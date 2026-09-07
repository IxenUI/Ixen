[CmdletBinding()]
param(
    [string] $Configuration = 'Debug',
    [switch] $Rebuild,
    [switch] $SkipFigures,
    [switch] $SkipDemo
)

$ErrorActionPreference = 'Continue'
$env:VSLANG = '1033'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

$framework = $PSScriptRoot
$workspace = Split-Path -Parent $framework

$script:Results = New-Object System.Collections.ArrayList

function Record([string] $verdict, [string] $name, [string] $detail)
{
    $null = $script:Results.Add([pscustomobject]@{ Verdict = $verdict; Name = $name; Detail = $detail })

    $colour = 'Green'

    if ($verdict -eq 'FAIL')
    {
        $colour = 'Red'
    }

    if ($verdict -eq 'SKIP')
    {
        $colour = 'Yellow'
    }

    Write-Host ('  {0,-4} {1,-30} {2}' -f $verdict, $name, $detail) -ForegroundColor $colour
}

function Verdicts([string] $verdict)
{
    return @($script:Results | Where-Object { $_.Verdict -eq $verdict }).Count
}

function Show([object[]] $lines, [int] $keep)
{
    $shown = @($lines | Select-Object -First $keep)

    foreach ($line in $shown)
    {
        Write-Host ('       ' + $line)
    }
}

function FindMsBuild
{
    $installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'

    if (-not (Test-Path $installer))
    {
        return $null
    }

    $found = @(& $installer -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe')

    if ($LASTEXITCODE -ne 0 -or $found.Count -eq 0)
    {
        return $null
    }

    return $found[0]
}

function TestCount([object[]] $lines)
{
    $line = @($lines | Where-Object { $_ -match 'Passed:\s+(\d+)' })

    if ($line.Count -eq 0)
    {
        return -1
    }

    if ($line[$line.Count - 1] -match 'Passed:\s+(\d+)')
    {
        return [int] $Matches[1]
    }

    return -1
}

function RunSuite([string] $name, [string] $project)
{
    $out = @(& dotnet test (Join-Path $framework $project) --no-build --nologo -c $Configuration 2>&1)
    $code = $LASTEXITCODE
    $count = TestCount $out

    if ($code -eq 0)
    {
        Record 'OK' $name ('{0} tests' -f $count)
        return
    }

    Show @($out | Where-Object { $_ -match '^\s+Failed |: error ' }) 12
    Record 'FAIL' $name ('exit {0}, {1} passed' -f $code, $count)
}

Write-Host ''
Write-Host ('Ixen verify - ' + $Configuration + ' - ' + $framework)
Write-Host ''

$msbuild = FindMsBuild
$built = $false

if ($null -eq $msbuild)
{
    Record 'FAIL' 'msbuild is available' 'vswhere found no MSBuild carrying the C++ component'
}
else
{
    $out = @(& $msbuild (Join-Path $framework 'Ixen.sln') '-t:Restore' ('-p:Configuration=' + $Configuration) '-nologo' '-v:m' 2>&1)
    $code = $LASTEXITCODE
    $advisories = @($out | Where-Object { $_ -match ': warning NU' })

    if ($code -eq 0)
    {
        $detail = 'msbuild restores what the dotnet cli cannot evaluate'

        if ($advisories.Count -gt 0)
        {
            Show $advisories 6
            $detail = '{0} nuget advisories, above, not fatal' -f $advisories.Count
        }

        Record 'OK' 'packages restore' $detail
    }
    else
    {
        Show @($out | Where-Object { $_ -match ': error ' }) 8
        Record 'FAIL' 'packages restore' ('exit ' + $code)
    }

    $target = '-t:Build'

    if ($Rebuild)
    {
        $target = '-t:Rebuild'
    }

    $out = @(& $msbuild (Join-Path $framework 'Ixen.sln') $target ('-p:Configuration=' + $Configuration) '-warnaserror' '-nologo' '-v:m' 2>&1)
    $code = $LASTEXITCODE

    if ($code -eq 0)
    {
        $built = $true
        Record 'OK' 'solution, warning-free' '10 projects, C++ and Android included'
    }
    else
    {
        Show @($out | Where-Object { $_ -match ': (error|warning) ' }) 12
        Record 'FAIL' 'solution, warning-free' ('exit ' + $code)
    }
}

if ($built)
{
    RunSuite 'core tests' 'UnitTests\Ixen.Core.UT\Ixen.Core.UT.csproj'
    RunSuite 'controls tests' 'UnitTests\Ixen.Controls.UT\Ixen.Controls.UT.csproj'
}
else
{
    Record 'SKIP' 'core tests' 'the solution did not build'
    Record 'SKIP' 'controls tests' 'the solution did not build'
}

$harness = @(Get-ChildItem -Path $framework -Filter *.cs -Recurse |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
    Where-Object { Select-String -Path $_.FullName -Pattern 'GetAllocatedBytesForCurrentThread' -Quiet })

if ($harness.Count -eq 1 -and $harness[0].Name -eq 'Allocations.cs')
{
    Record 'OK' 'one allocation harness' 'Allocations.cs'
}
else
{
    Record 'FAIL' 'one allocation harness' (@($harness | ForEach-Object { $_.Name }) -join ', ')
}

$tools = Join-Path $workspace 'Tools\Ixen.Docs\Ixen.Docs.csproj'
$docs = Join-Path $workspace 'Documentation'
$toolBuilt = $false

if (-not (Test-Path $tools))
{
    Record 'SKIP' 'Ixen.Docs, warning-free' 'Tools is not beside Framework'
}
elseif (-not $built)
{
    Record 'SKIP' 'Ixen.Docs, warning-free' 'the solution did not build'
}
else
{
    $out = @(& dotnet build $tools -c $Configuration --nologo -warnaserror 2>&1)
    $code = $LASTEXITCODE

    if ($code -eq 0)
    {
        $toolBuilt = $true
        Record 'OK' 'Ixen.Docs, warning-free' 'the figure renderer, a real consumer of Core'
    }
    else
    {
        Show @($out | Where-Object { $_ -match ': (error|warning) ' }) 8
        Record 'FAIL' 'Ixen.Docs, warning-free' ('exit ' + $code)
    }
}

if ($SkipFigures)
{
    Record 'SKIP' 'figures byte-identical' 'asked to skip'
}
elseif (-not (Test-Path $docs))
{
    Record 'SKIP' 'figures byte-identical' 'Documentation is not beside Framework'
}
elseif (-not $toolBuilt)
{
    Record 'SKIP' 'figures byte-identical' 'Ixen.Docs was not built'
}
else
{
    $out = @(& dotnet run --project $tools -c $Configuration --no-build 2>&1)
    $code = $LASTEXITCODE

    if ($code -ne 0)
    {
        Show @($out | Select-Object -Last 8) 8
        Record 'FAIL' 'figures byte-identical' ('the renderer failed, exit ' + $code)
    }
    else
    {
        $status = @(& git -C $docs status --porcelain -- images 2>&1)
        $moved = @($status | Where-Object { $_ -notmatch '^\?\?' })
        $added = @($status | Where-Object { $_ -match '^\?\?' })
        $total = @(Get-ChildItem (Join-Path $docs 'images') -Filter *.png).Count

        if ($moved.Count -eq 0)
        {
            $detail = '{0} figures' -f $total

            if ($added.Count -gt 0)
            {
                $detail = '{0} figures, {1} of them new and untracked' -f $total, $added.Count
            }

            Record 'OK' 'figures byte-identical' $detail
        }
        else
        {
            Show $moved 12
            Record 'FAIL' 'figures byte-identical' ('{0} of {1} moved' -f $moved.Count, $total)
        }
    }
}

$demo = Join-Path $workspace 'Demo App\Ixen.DemoApp\Ixen.DemoApp.csproj'
$reference = Join-Path $framework 'ci\demo-frame.md5'

if ($SkipDemo)
{
    Record 'SKIP' 'demo frame unchanged' 'asked to skip'
}
elseif (-not (Test-Path $demo))
{
    Record 'SKIP' 'demo frame unchanged' 'Demo App is not beside Framework'
}
elseif (-not $toolBuilt)
{
    Record 'SKIP' 'demo frame unchanged' 'Ixen.Docs was not built'
}
elseif (-not (Test-Path $reference))
{
    Record 'FAIL' 'demo frame unchanged' ('no reference hash at ' + $reference)
}
else
{
    $out = @(& dotnet build $demo -c $Configuration --nologo -warnaserror 2>&1)
    $code = $LASTEXITCODE

    if ($code -ne 0)
    {
        Show @($out | Where-Object { $_ -match ': (error|warning) ' }) 8
        Record 'FAIL' 'demo frame unchanged' ('the demo did not build, exit ' + $code)
    }
    else
    {
        $expected = ((Get-Content $reference -TotalCount 1) -replace '\s', '').ToUpperInvariant()
        $dll = Join-Path $workspace ('Demo App\Ixen.DemoApp\bin\' + $Configuration + '\net10.0\Ixen.DemoApp.dll')
        $shot = Join-Path ([System.IO.Path]::GetTempPath()) 'ixen-verify-demo.png'

        $out = @(& dotnet run --project $tools -c $Configuration --no-build -- --render $dll 1282 753 $shot --component MainComponent 2>&1)
        $code = $LASTEXITCODE

        if ($code -ne 0 -or -not (Test-Path $shot))
        {
            Show @($out | Select-Object -Last 8) 8
            Record 'FAIL' 'demo frame unchanged' ('the render failed, exit ' + $code)
        }
        else
        {
            $actual = (Get-FileHash $shot -Algorithm MD5).Hash.ToUpperInvariant()

            if ($actual -eq $expected)
            {
                Record 'OK' 'demo frame unchanged' $actual
            }
            else
            {
                Record 'FAIL' 'demo frame unchanged' ('{0}, the reference says {1}' -f $actual, $expected)
            }

            Remove-Item $shot -ErrorAction SilentlyContinue
        }
    }
}

Record 'SKIP' 'vs extension' 'net472 and the VSSDK, and its six MEF warnings are pre-existing'

$passed = Verdicts 'OK'
$failed = Verdicts 'FAIL'
$skipped = Verdicts 'SKIP'

Write-Host ''
Write-Host ('verify : {0} passed, {1} failed, {2} skipped' -f $passed, $failed, $skipped)

if ($skipped -gt 0)
{
    Write-Host 'a green run claims only what it checked - the SKIP lines say what it did not'
}

Write-Host ''

if ($failed -gt 0)
{
    exit 1
}

exit 0
