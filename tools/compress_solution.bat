@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ============================================================================
rem compress_solution.bat
rem
rem Cree tmp\BudgetManager.zip directement depuis les fichiers source.
rem Aucun staging/copie intermediaire de toute la solution.
rem
rem Utilisation :
rem   tools\compress_solution.bat
rem   tools\compress_solution.bat BudgetManager.Application
rem   tools\compress_solution.bat BudgetManager.Application BudgetManager.Web
rem ============================================================================

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "ROOT=%%~fI"

set "TMP_DIR=%ROOT%\tmp"
set "TEMP_GIT_DIR=%TMP_DIR%\compress_solution_git"
set "TEMP_SOLUTION_DIR=%TMP_DIR%\compress_solution_sln"
set "TEMP_SOLUTION=%TEMP_SOLUTION_DIR%\BudgetManager.sln"
set "FILE_LIST=%TMP_DIR%\compress_solution_files.txt"
set "OUTPUT=%TMP_DIR%\BudgetManager.zip"
set "IGNORED_PROJECTS=%*"

echo.
echo === Compression directe de BudgetManager ===
echo Racine : "%ROOT%"
echo Sortie : "%OUTPUT%"
if defined IGNORED_PROJECTS (
    echo Projets ignores : %IGNORED_PROJECTS%
) else (
    echo Projets ignores : aucun
)
echo.

where git >nul 2>&1
if errorlevel 1 (
    echo ERREUR : Git est introuvable dans le PATH.
    exit /b 1
)

where powershell >nul 2>&1
if errorlevel 1 (
    echo ERREUR : Windows PowerShell est introuvable dans le PATH.
    exit /b 1
)

if defined IGNORED_PROJECTS (
    where dotnet >nul 2>&1
    if errorlevel 1 (
        echo ERREUR : dotnet est introuvable dans le PATH.
        exit /b 1
    )
)

if not exist "%ROOT%\BudgetManager.sln" (
    echo ERREUR : BudgetManager.sln est introuvable.
    exit /b 1
)

if not exist "%ROOT%\.gitignore" (
    echo ERREUR : .gitignore est introuvable.
    exit /b 1
)

if not exist "%TMP_DIR%" mkdir "%TMP_DIR%" >nul 2>&1

call :Cleanup

if exist "%OUTPUT%" del /F /Q "%OUTPUT%"

echo Calcul de la liste des fichiers...

git -C "%ROOT%" rev-parse --is-inside-work-tree >nul 2>&1

if not errorlevel 1 (
    git -C "%ROOT%" ls-files --cached --others --exclude-standard > "%FILE_LIST%"
    if errorlevel 1 (
        echo ERREUR : impossible d'obtenir la liste des fichiers Git.
        call :Cleanup
        exit /b 1
    )
) else (
    mkdir "%TEMP_GIT_DIR%" >nul 2>&1

    git --git-dir="%TEMP_GIT_DIR%" --work-tree="%ROOT%" init -q
    if errorlevel 1 (
        echo ERREUR : impossible d'initialiser le depot Git temporaire.
        call :Cleanup
        exit /b 1
    )

    git --git-dir="%TEMP_GIT_DIR%" --work-tree="%ROOT%" ^
        ls-files --others --exclude-standard > "%FILE_LIST%"

    if errorlevel 1 (
        echo ERREUR : impossible d'obtenir la liste des fichiers non ignores.
        call :Cleanup
        exit /b 1
    )
)

rem ---------------------------------------------------------------------------
rem Cree uniquement une copie temporaire de BudgetManager.sln si des projets
rem doivent etre retires. Aucun autre fichier n'est copie.
rem ---------------------------------------------------------------------------

if defined IGNORED_PROJECTS (
    mkdir "%TEMP_SOLUTION_DIR%" >nul 2>&1
    copy /Y "%ROOT%\BudgetManager.sln" "%TEMP_SOLUTION%" >nul

    echo Mise a jour de la copie temporaire de BudgetManager.sln...

    for %%P in (%IGNORED_PROJECTS%) do (
        set "PROJECT_NAME=%%~P"
        set "SRC_PROJECT=src\!PROJECT_NAME!\!PROJECT_NAME!.csproj"
        set "TEST_PROJECT=tests\!PROJECT_NAME!.Tests\!PROJECT_NAME!.Tests.csproj"

        echo   - !PROJECT_NAME!

        dotnet sln "%TEMP_SOLUTION%" remove "!SRC_PROJECT!" >nul 2>&1
        dotnet sln "%TEMP_SOLUTION%" remove "!TEST_PROJECT!" >nul 2>&1
    )
)

echo.
echo Creation directe de l'archive...

set "PS_ROOT=%ROOT%"
set "PS_OUTPUT=%OUTPUT%"
set "PS_FILE_LIST=%FILE_LIST%"
set "PS_IGNORED_PROJECTS=%IGNORED_PROJECTS%"
set "PS_TEMP_SOLUTION=%TEMP_SOLUTION%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference = 'Stop';" ^
    "Add-Type -AssemblyName System.IO.Compression;" ^
    "Add-Type -AssemblyName System.IO.Compression.FileSystem;" ^
    "$root = [IO.Path]::GetFullPath($env:PS_ROOT).TrimEnd('\');" ^
    "$output = $env:PS_OUTPUT;" ^
    "$ignoredProjects = [string]$env:PS_IGNORED_PROJECTS;$projects = @($ignoredProjects.Split(' ', [StringSplitOptions]::RemoveEmptyEntries));" ^
    "$tempSolution = $env:PS_TEMP_SOLUTION;" ^
    "" ^
    "function Is-ProjectExcluded([string] $relativePath) {" ^
    "    $normalized = $relativePath.Replace('\','/').TrimStart('/');" ^
    "    foreach ($project in $projects) {" ^
    "        if ($normalized.StartsWith(('src/' + $project + '/'), [StringComparison]::OrdinalIgnoreCase)) { return $true };" ^
    "        if ($normalized.StartsWith(('tests/' + $project + '.Tests/'), [StringComparison]::OrdinalIgnoreCase)) { return $true };" ^
    "    };" ^
    "    return $false;" ^
    "};" ^
    "" ^
    "$stream = [IO.File]::Open($output, [IO.FileMode]::CreateNew);" ^
    "try {" ^
    "    $zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $false);" ^
    "    try {" ^
    "        $count = 0;" ^
    "        foreach ($relative in [IO.File]::ReadLines($env:PS_FILE_LIST)) {" ^
    "            if ([string]::IsNullOrWhiteSpace($relative)) { continue };" ^
    "            $relative = $relative.Replace('\','/');" ^
    "            if ($relative.StartsWith('tmp/', [StringComparison]::OrdinalIgnoreCase)) { continue };" ^
    "            if (Is-ProjectExcluded $relative) { continue };" ^
    "            if ($projects.Count -gt 0 -and $relative.Equals('BudgetManager.sln', [StringComparison]::OrdinalIgnoreCase)) { continue };" ^
    "            $source = Join-Path $root ($relative.Replace('/','\'));" ^
    "            if (-not [IO.File]::Exists($source)) { continue };" ^
    "            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(" ^
    "                $zip," ^
    "                $source," ^
    "                $relative," ^
    "                [IO.Compression.CompressionLevel]::Fastest) | Out-Null;" ^
    "            $count++;" ^
    "        };" ^
    "" ^
    "        if ($projects.Count -gt 0) {" ^
    "            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(" ^
    "                $zip," ^
    "                $tempSolution," ^
    "                'BudgetManager.sln'," ^
    "                [IO.Compression.CompressionLevel]::Fastest) | Out-Null;" ^
    "            $count++;" ^
    "        };" ^
    "" ^
    "        Write-Host ('Fichiers archives : ' + $count);" ^
    "    } finally {" ^
    "        $zip.Dispose();" ^
    "    };" ^
    "} finally {" ^
    "    $stream.Dispose();" ^
    "};"

if errorlevel 1 (
    echo ERREUR : la creation de l'archive a echoue.
    call :Cleanup
    exit /b 1
)

call :Cleanup

echo.
echo Archive creee avec succes :
echo "%OUTPUT%"
echo.

exit /b 0

:Cleanup
if exist "%TEMP_GIT_DIR%" (
    rmdir /S /Q "%TEMP_GIT_DIR%" >nul 2>&1
)

if exist "%TEMP_SOLUTION_DIR%" (
    rmdir /S /Q "%TEMP_SOLUTION_DIR%" >nul 2>&1
)

if exist "%FILE_LIST%" (
    del /F /Q "%FILE_LIST%" >nul 2>&1
)

exit /b 0
