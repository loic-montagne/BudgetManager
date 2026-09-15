@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ============================================================================
rem configure_desktop.bat
rem
rem Prepare un poste Windows pour developper et tester BudgetManager.
rem
rem Composants verifies / installes :
rem   - WinGet (requis pour les installations automatisees)
rem   - Git
rem   - SDK .NET 10
rem   - dotnet-ef (outil global, branche 10.x)
rem   - LibMan (outil global)
rem   - ReportGenerator (outil global)
rem   - WSL 2
rem   - Docker Desktop
rem
rem Visual Studio est seulement detecte et signale :
rem il n'est pas indispensable pour gerer la solution via la CLI et son
rem installation depend des workloads souhaites.
rem
rem Le script est idempotent : il peut etre relance sans reinstaller
rem les composants deja presents.
rem ============================================================================

set "DOTNET_DIR=C:\Program Files\dotnet"
set "DOTNET_EXE=%DOTNET_DIR%\dotnet.exe"
set "DOTNET_TOOLS_DIR=%USERPROFILE%\.dotnet\tools"
set "GIT_CMD_DIR=C:\Program Files\Git\cmd"
set "GIT_EXE=%GIT_CMD_DIR%\git.exe"
set "DOCKER_DIR=C:\Program Files\Docker\Docker"
set "DOCKER_BIN_DIR=%DOCKER_DIR%\resources\bin"
set "DOCKER_EXE=%DOCKER_BIN_DIR%\docker.exe"
set "DOCKER_DESKTOP_EXE=%DOCKER_DIR%\Docker Desktop.exe"

set "DOTNET_SDK_PACKAGE=Microsoft.DotNet.SDK.10"
set "DOTNET_EF_VERSION=10.*"
set "GIT_PACKAGE=Git.Git"
set "DOCKER_PACKAGE=Docker.DockerDesktop"

set "REBOOT_REQUIRED=0"
set "FAILED=0"

echo.
echo ============================================================
echo Configuration du poste de developpement BudgetManager
echo ============================================================
echo.

rem ---------------------------------------------------------------------------
rem Elevation administrateur
rem ---------------------------------------------------------------------------

set "RUNNING_ELEVATED=0"
if /I "%~1"=="--elevated" set "RUNNING_ELEVATED=1"

net session >nul 2>&1
if errorlevel 1 (
    echo [INFO] Des droits administrateur sont necessaires pour WSL et Docker.
    echo [INFO] Relance du script avec elevation...
    echo.

    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
        "Start-Process -FilePath '%~f0' -ArgumentList '--elevated' -WorkingDirectory '%CD%' -Verb RunAs"

    if errorlevel 1 (
        echo [ERREUR] La relance avec elevation a echoue ou a ete annulee.
        echo.
        pause
        exit /b 1
    )

    exit /b 0
)

rem ---------------------------------------------------------------------------
rem WinGet
rem ---------------------------------------------------------------------------

echo [1/9] Verification de WinGet...

where winget.exe >nul 2>&1
if errorlevel 1 (
    echo [ERREUR] WinGet est introuvable.
    echo          Installez ou mettez a jour "App Installer" depuis Microsoft Store,
    echo          puis relancez ce script.
    exit /b 1
)

echo [OK] WinGet est disponible.
echo.

rem ---------------------------------------------------------------------------
rem Git
rem ---------------------------------------------------------------------------

echo [2/9] Verification de Git...

where git.exe >nul 2>&1
if not errorlevel 1 (
    for /f "delims=" %%I in ('where git.exe 2^>nul') do (
        if not defined FOUND_GIT set "FOUND_GIT=%%I"
    )
)

if defined FOUND_GIT (
    set "GIT_EXE=!FOUND_GIT!"
    for %%I in ("!GIT_EXE!") do set "GIT_CMD_DIR=%%~dpI"
    echo [OK] Git est deja installe.
) else if exist "%GIT_EXE%" (
    echo [OK] Git est deja installe.
) else (
    echo [INFO] Installation de Git...
    call :WingetInstall "%GIT_PACKAGE%" "Git"
    if errorlevel 1 (
        set "FAILED=1"
        goto :summary
    )
)

if exist "%GIT_EXE%" (
    "%GIT_EXE%" --version
) else (
    git --version 2>nul
)

echo.

rem ---------------------------------------------------------------------------
rem SDK .NET 10
rem ---------------------------------------------------------------------------

echo [3/9] Verification du SDK .NET 10...

set "HAS_DOTNET10=0"

if exist "%DOTNET_EXE%" (
    "%DOTNET_EXE%" --list-sdks 2>nul | findstr /R /B "10\." >nul
    if not errorlevel 1 set "HAS_DOTNET10=1"
) else (
    where dotnet.exe >nul 2>&1
    if not errorlevel 1 (
        for /f "delims=" %%I in ('where dotnet.exe 2^>nul') do (
            if not defined FOUND_DOTNET set "FOUND_DOTNET=%%I"
        )
        if defined FOUND_DOTNET (
            set "DOTNET_EXE=!FOUND_DOTNET!"
            for %%I in ("!DOTNET_EXE!") do set "DOTNET_DIR=%%~dpI"
            "!DOTNET_EXE!" --list-sdks 2>nul | findstr /R /B "10\." >nul
            if not errorlevel 1 set "HAS_DOTNET10=1"
        )
    )
)

if "%HAS_DOTNET10%"=="1" (
    echo [OK] Le SDK .NET 10 est deja installe.
) else (
    echo [INFO] Installation du SDK .NET 10...
    call :WingetInstall "%DOTNET_SDK_PACKAGE%" ".NET 10 SDK"
    if errorlevel 1 (
        set "FAILED=1"
        goto :summary
    )

    set "DOTNET_EXE=C:\Program Files\dotnet\dotnet.exe"
    set "DOTNET_DIR=C:\Program Files\dotnet"

    if not exist "%DOTNET_EXE%" (
        echo [ERREUR] dotnet.exe reste introuvable apres installation.
        set "FAILED=1"
        goto :summary
    )

    "%DOTNET_EXE%" --list-sdks | findstr /R /B "10\." >nul
    if errorlevel 1 (
        echo [ERREUR] Aucun SDK .NET 10 n'est detecte apres installation.
        set "FAILED=1"
        goto :summary
    )
)

echo [INFO] SDK installes :
"%DOTNET_EXE%" --list-sdks
echo.

rem ---------------------------------------------------------------------------
rem dotnet-ef
rem ---------------------------------------------------------------------------

echo [4/9] Verification de dotnet-ef...

set "HAS_DOTNET_EF=0"
"%DOTNET_EXE%" tool list --global 2>nul | findstr /I /B "dotnet-ef " >nul
if not errorlevel 1 set "HAS_DOTNET_EF=1"

if "%HAS_DOTNET_EF%"=="1" (
    echo [OK] dotnet-ef est deja installe.
) else (
    echo [INFO] Installation de dotnet-ef %DOTNET_EF_VERSION%...
    "%DOTNET_EXE%" tool install --global dotnet-ef --version "%DOTNET_EF_VERSION%"
    if errorlevel 1 (
        echo [ERREUR] L'installation de dotnet-ef a echoue.
        set "FAILED=1"
        goto :summary
    )
)

rem Ajouter temporairement le dossier des outils pour cette execution uniquement.
set "PATH=%DOTNET_TOOLS_DIR%;%PATH%"

"%DOTNET_EXE%" ef --version
if errorlevel 1 (
    echo [ERREUR] dotnet-ef est installe mais ne peut pas etre execute.
    set "FAILED=1"
    goto :summary
)

echo.

rem ---------------------------------------------------------------------------
rem LibMan
rem ---------------------------------------------------------------------------

echo [5/9] Verification de LibMan...

set "HAS_LIBMAN=0"
"%DOTNET_EXE%" tool list --global 2>nul | findstr /I /B "microsoft.web.librarymanager.cli " >nul
if not errorlevel 1 set "HAS_LIBMAN=1"

if "%HAS_LIBMAN%"=="1" (
    echo [OK] LibMan est deja installe.
) else (
    echo [INFO] Installation de LibMan...
    "%DOTNET_EXE%" tool install --global Microsoft.Web.LibraryManager.Cli
    if errorlevel 1 (
        echo [ERREUR] L'installation de LibMan a echoue.
        set "FAILED=1"
        goto :summary
    )
)

set "PATH=%DOTNET_TOOLS_DIR%;%PATH%"

libman --version
if errorlevel 1 (
    echo [ERREUR] LibMan est installe mais ne peut pas etre execute.
    set "FAILED=1"
    goto :summary
)

echo.

rem ---------------------------------------------------------------------------
rem ReportGenerator
rem ---------------------------------------------------------------------------

echo [6/9] Verification de ReportGenerator...

set "HAS_REPORTGENERATOR=0"
"%DOTNET_EXE%" tool list --global 2>nul | findstr /I /B "dotnet-reportgenerator-globaltool " >nul
if not errorlevel 1 set "HAS_REPORTGENERATOR=1"

if "%HAS_REPORTGENERATOR%"=="1" (
    echo [OK] ReportGenerator est deja installe.
) else (
    echo [INFO] Installation de ReportGenerator...
    "%DOTNET_EXE%" tool install --global dotnet-reportgenerator-globaltool
    if errorlevel 1 (
        echo [ERREUR] L'installation de ReportGenerator a echoue.
        set "FAILED=1"
        goto :summary
    )
)

set "PATH=%DOTNET_TOOLS_DIR%;%PATH%"

reportgenerator --version
if errorlevel 1 (
    echo [ERREUR] ReportGenerator est installe mais ne peut pas etre execute.
    set "FAILED=1"
    goto :summary
)

echo.

rem ---------------------------------------------------------------------------
rem WSL 2
rem ---------------------------------------------------------------------------

echo [7/9] Verification de WSL 2...

where wsl.exe >nul 2>&1
if errorlevel 1 (
    echo [INFO] Installation de WSL sans distribution Linux...
    wsl.exe --install --no-distribution
    if errorlevel 1 (
        echo [ERREUR] L'installation de WSL a echoue.
        set "FAILED=1"
        goto :summary
    )
    set "REBOOT_REQUIRED=1"
) else (
    wsl.exe --status >nul 2>&1
    if errorlevel 1 (
        echo [INFO] Activation / installation de WSL 2...
        wsl.exe --install --no-distribution
        if errorlevel 1 (
            echo [ERREUR] L'installation de WSL a echoue.
            set "FAILED=1"
            goto :summary
        )
        set "REBOOT_REQUIRED=1"
    ) else (
        echo [OK] WSL est disponible.
    )
)

rem Ce parametre est sans danger si WSL est deja configure.
wsl.exe --set-default-version 2 >nul 2>&1

rem Une mise a jour peut echouer tant qu'un redemarrage n'a pas eu lieu.
if "%REBOOT_REQUIRED%"=="0" (
    echo [INFO] Mise a jour de WSL...
    wsl.exe --update
)

echo.

rem ---------------------------------------------------------------------------
rem Docker Desktop
rem ---------------------------------------------------------------------------

echo [8/9] Verification de Docker Desktop...

set "HAS_DOCKER=0"

where docker.exe >nul 2>&1
if not errorlevel 1 set "HAS_DOCKER=1"
if exist "%DOCKER_EXE%" set "HAS_DOCKER=1"
if exist "%DOCKER_DESKTOP_EXE%" set "HAS_DOCKER=1"

if "%HAS_DOCKER%"=="1" (
    echo [OK] Docker Desktop / Docker CLI est deja installe.
) else (
    echo [INFO] Installation de Docker Desktop...
    call :WingetInstall "%DOCKER_PACKAGE%" "Docker Desktop"
    if errorlevel 1 (
        set "FAILED=1"
        goto :summary
    )
)

if "%REBOOT_REQUIRED%"=="1" (
    echo [INFO] Docker Desktop est installe, mais WSL vient d'etre active.
    echo        Un redemarrage Windows est necessaire avant de valider Docker.
) else (
    rem Ajouter temporairement Docker au PATH pour cette execution.
    if exist "%DOCKER_BIN_DIR%" set "PATH=%DOCKER_BIN_DIR%;%PATH%"

    docker version >nul 2>&1
    if errorlevel 1 (
        if exist "%DOCKER_DESKTOP_EXE%" (
            echo [INFO] Demarrage de Docker Desktop...
            start "" "%DOCKER_DESKTOP_EXE%"

            rem Attente bornee : maximum ~90 secondes.
            set /a DOCKER_TRIES=0
            :docker_wait
            timeout /t 3 /nobreak >nul
            docker version >nul 2>&1
            if not errorlevel 1 goto :docker_ready
            set /a DOCKER_TRIES+=1
            if !DOCKER_TRIES! LSS 30 goto :docker_wait
        )

        echo [AVERTISSEMENT] Docker est installe mais le moteur n'est pas encore disponible.
        echo                 Lancez Docker Desktop puis executez :
        echo                 docker run --rm hello-world
        goto :after_docker_validation
    )

    :docker_ready
    echo [OK] Le moteur Docker est disponible.

    docker run --rm hello-world >nul 2>&1
    if errorlevel 1 (
        echo [AVERTISSEMENT] Docker repond, mais le test hello-world a echoue.
    ) else (
        echo [OK] Un conteneur Docker peut etre execute.
    )
)

:after_docker_validation
echo.

rem ---------------------------------------------------------------------------
rem Visual Studio : detection seulement
rem ---------------------------------------------------------------------------

echo [9/9] Verification de Visual Studio...

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if exist "%VSWHERE%" (
    for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -property displayName`) do set "VS_NAME=%%I"
    for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -property installationVersion`) do set "VS_VERSION=%%I"

    if defined VS_NAME (
        echo [OK] !VS_NAME! !VS_VERSION! detecte.
    ) else (
        echo [AVERTISSEMENT] Visual Studio Installer est present mais aucune instance n'a ete detectee.
    )
) else (
    echo [INFO] Visual Studio n'est pas detecte.
    echo        Il n'est pas indispensable aux commandes CLI de la solution.
    echo        Si vous souhaitez l'IDE, installez-le separement avec les workloads .NET adaptes.
)

echo.

rem ---------------------------------------------------------------------------
rem PATH persistant - seulement apres les installations/verifications
rem ---------------------------------------------------------------------------

echo [INFO] Configuration du PATH utilisateur...

set "PATH_DOTNET=%DOTNET_DIR%"
set "PATH_DOTNET_TOOLS=%DOTNET_TOOLS_DIR%"
set "PATH_GIT=%GIT_CMD_DIR%"
set "PATH_DOCKER=%DOCKER_BIN_DIR%"

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
    "$paths = @($env:PATH_DOTNET, $env:PATH_DOTNET_TOOLS, $env:PATH_GIT, $env:PATH_DOCKER) | Where-Object { $_ -and (Test-Path $_) };" ^
    "$current = [Environment]::GetEnvironmentVariable('Path', 'User');" ^
    "$entries = @($current -split ';' | Where-Object { $_ });" ^
    "foreach ($path in $paths) {" ^
    "    $exists = $entries | Where-Object { $_.TrimEnd('\') -ieq $path.TrimEnd('\') };" ^
    "    if (-not $exists) { $entries += $path }" ^
    "}" ^
    "[Environment]::SetEnvironmentVariable('Path', ($entries -join ';'), 'User')"

if errorlevel 1 (
    echo [ERREUR] Impossible de mettre a jour le PATH utilisateur.
    set "FAILED=1"
    goto :summary
)

rem Mise a jour de la session batch actuelle.
set "PATH=%DOTNET_DIR%;%DOTNET_TOOLS_DIR%;%GIT_CMD_DIR%;%DOCKER_BIN_DIR%;%PATH%"

:summary
echo.
echo ============================================================
echo Resume
echo ============================================================

if "%FAILED%"=="1" (
    echo [ECHEC] La configuration n'a pas pu etre terminee.
    if "%RUNNING_ELEVATED%"=="1" (
        echo.
        echo Appuyez sur une touche pour fermer cette fenetre.
        pause >nul
    )
    exit /b 1
)

echo [OK] Git
echo [OK] SDK .NET 10
echo [OK] dotnet-ef
echo [OK] LibMan
echo [OK] ReportGenerator
echo [OK] WSL 2 / activation demandee
echo [OK] Docker Desktop / installation demandee
echo [OK] PATH utilisateur configure

if "%REBOOT_REQUIRED%"=="1" (
    echo.
    echo [ACTION REQUISE] Redemarrez Windows, puis relancez ce script.
    echo                   Le second passage validera WSL et Docker.
) else (
    echo.
    echo Poste pret pour BudgetManager.
    echo.
    echo Verifications utiles :
    echo   git --version
    echo   dotnet --version
    echo   dotnet ef --version
    echo   dotnet format --version
    echo   libman --version
    echo   reportgenerator --version
    echo   docker version
    echo.
    echo Test Infrastructure :
    echo   dotnet test tests\BudgetManager.Infrastructure.Tests
)

echo.
echo Les consoles et Visual Studio deja ouverts doivent etre redemarres
echo pour recuperer le PATH utilisateur persistant.
echo.

if "%RUNNING_ELEVATED%"=="1" (
    echo Appuyez sur une touche pour fermer cette fenetre.
    pause >nul
)

exit /b 0

rem ============================================================================
rem Sous-routines
rem ============================================================================

:WingetInstall
set "PACKAGE_ID=%~1"
set "PACKAGE_NAME=%~2"

winget.exe list --id "%PACKAGE_ID%" --exact --source winget >nul 2>&1
if not errorlevel 1 (
    echo [OK] %PACKAGE_NAME% est deja enregistre par WinGet.
    exit /b 0
)

winget.exe install ^
    --id "%PACKAGE_ID%" ^
    --exact ^
    --source winget ^
    --accept-package-agreements ^
    --accept-source-agreements

if errorlevel 1 (
    echo [ERREUR] L'installation de %PACKAGE_NAME% a echoue.
    exit /b 1
)

echo [OK] %PACKAGE_NAME% installe.
exit /b 0
