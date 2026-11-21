param([switch]$Test, [switch]$UiTest)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) {
        $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    }
    if (-not (Test-Path -LiteralPath $compiler)) {
        throw 'The .NET Framework C# compiler was not found.'
    }

    if (Get-Command py -CommandType Application -ErrorAction SilentlyContinue) {
        & py -3 scripts\build_demo_data.py
    } elseif (Get-Command python -CommandType Application -ErrorAction SilentlyContinue) {
        & python scripts\build_demo_data.py
    } else {
        throw 'Python 3 is required to generate the bundled demo data.'
    }
    if ($LASTEXITCODE -ne 0) { throw 'Demo data generation failed.' }

    New-Item -ItemType Directory -Force -Path dist | Out-Null
    $exe = 'dist\oii背词.exe'
    $sources = (Get-ChildItem -LiteralPath src -Filter '*.cs' -File | Sort-Object Name).FullName
    $options = @(
        '/nologo', '/target:winexe', '/optimize+', '/utf8output', '/codepage:65001'
        '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll'
        '/reference:System.Core.dll', '/reference:System.IO.Compression.dll'
        '/reference:System.IO.Compression.FileSystem.dll', '/reference:System.Xml.dll'
        '/reference:System.Xml.Linq.dll', '/win32manifest:Vocabulary.manifest'
        '/win32icon:assets\oii背词.ico', '/resource:data\words.gz,words.gz'
        "/out:$exe"
    )
    & $compiler @options @sources
    if ($LASTEXITCODE -ne 0) { throw 'Windows build failed.' }
    Write-Host "Build completed: $exe"

    if ($Test -or $UiTest) {
        $testRoot = Join-Path ([IO.Path]::GetTempPath()) ('oii-test-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $testRoot | Out-Null
        $tests = @('self-test')
        if ($UiTest) { $tests += 'sentence-ui-test', 'audit-ui-test', 'study-ui-test', 'study-ui-test' }
        try {
            for ($i = 0; $i -lt $tests.Count; $i++) {
                $testName = $tests[$i]
                $report = Join-Path $testRoot "$i.txt"
                $profile = Join-Path $testRoot $testName
                $process = Start-Process -FilePath (Resolve-Path -LiteralPath $exe) -PassThru `
                    -ArgumentList "--$testName", ('"{0}"' -f $report), ('"{0}"' -f $profile)
                if (-not $process.WaitForExit(30000)) {
                    $process.Kill()
                    $process.WaitForExit()
                    throw "$testName timed out."
                }
                if (Test-Path -LiteralPath $report) { Get-Content -Encoding UTF8 -LiteralPath $report }
                if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) {
                    throw "$testName failed (exit code $($process.ExitCode))."
                }
            }
        } finally {
            $resolved = (Resolve-Path -LiteralPath $testRoot).Path
            if ($resolved -eq [IO.Path]::GetFullPath($testRoot)) {
                Remove-Item -LiteralPath $resolved -Recurse -Force
            }
        }
    }
} finally {
    Pop-Location
}
