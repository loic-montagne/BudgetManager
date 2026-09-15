@echo off
setlocal EnableExtensions

rem Force les outils .NET, MSBuild et NuGet a afficher leurs messages
rem en anglais afin d'eviter les problemes d'encodage dans cmd.exe.
set "DOTNET_CLI_UI_LANGUAGE=en-US"
set "VSLANG=1033"

rem ============================================================================
rem Restaure toutes les dependances de la solution BudgetManager :
rem   1. restaure les packages NuGet ;
rem   2. restaure les bibliotheques clientes gerees par LibMan.
rem
rem Emplacement attendu :
rem   <racine-du-depot>\tools\restore_solution.bat
rem
rem Utilisation :
rem   tools\restore_solution.bat
rem ============================================================================

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "ROOT_DIR=%%~fI"

set "SOLUTION_FILE=%ROOT_DIR%\BudgetManager.sln"
set "WEB_DIR=%ROOT_DIR%\src\BudgetManager.Web"
set "LIBMAN_FILE=%WEB_DIR%\libman.json"

echo.
echo ============================================================
echo BudgetManager - Restauration des dependances
echo ============================================================
echo Racine   : %ROOT_DIR%
echo Solution : %SOLUTION_FILE%
echo LibMan   : %LIBMAN_FILE%
echo.

rem ----------------------------------------------------------------------------
rem Verification des fichiers
rem ----------------------------------------------------------------------------

if not exist "%SOLUTION_FILE%" (
    echo [ERREUR] La solution est introuvable :
    echo   %SOLUTION_FILE%
    exit /b 1
)

if not exist "%LIBMAN_FILE%" (
    echo [ERREUR] Le fichier LibMan est introuvable :
    echo   %LIBMAN_FILE%
    exit /b 1
)

rem ----------------------------------------------------------------------------
rem Verification du SDK .NET
rem ----------------------------------------------------------------------------

where dotnet >nul 2>&1

if errorlevel 1 (
    echo [ERREUR] Le SDK .NET est introuvable.
    echo Installez le SDK .NET puis relancez ce script.
    exit /b 1
)

rem ----------------------------------------------------------------------------
rem Verification de LibMan
rem ----------------------------------------------------------------------------

where libman >nul 2>&1

if errorlevel 1 (
    echo [ERREUR] LibMan est introuvable.
    echo.
    echo Installez LibMan puis relancez ce script :
    echo   dotnet tool install --global Microsoft.Web.LibraryManager.Cli
    exit /b 1
)

rem ----------------------------------------------------------------------------
rem 1. Restauration NuGet
rem ----------------------------------------------------------------------------

echo.
echo [1/2] Restauration des packages NuGet...

dotnet restore "%SOLUTION_FILE%"

if errorlevel 1 (
    echo.
    echo [ERREUR] La restauration des packages NuGet a echoue.
    exit /b 1
)

rem ----------------------------------------------------------------------------
rem 2. Restauration LibMan
rem ----------------------------------------------------------------------------

echo.
echo [2/2] Restauration des bibliotheques clientes...

pushd "%WEB_DIR%"

libman restore
set "LIBMAN_EXIT_CODE=%ERRORLEVEL%"

popd

if not "%LIBMAN_EXIT_CODE%"=="0" (
    echo.
    echo [ERREUR] La restauration des bibliotheques LibMan a echoue.
    exit /b %LIBMAN_EXIT_CODE%
)

rem ----------------------------------------------------------------------------
rem Termine
rem ----------------------------------------------------------------------------

echo.
echo ============================================================
echo [SUCCES] Toutes les dependances ont ete restaurees.
echo ============================================================
echo.
echo Packages NuGet :
echo   %SOLUTION_FILE%
echo.
echo Bibliotheques clientes :
echo   %WEB_DIR%\wwwroot\lib
echo.

exit /b 0