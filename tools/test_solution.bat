@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem Force les outils .NET, MSBuild, NuGet et Microsoft.Testing.Platform a afficher leurs messages
rem en anglais afin d'eviter les problemes d'encodage dans cmd.exe.
set "DOTNET_CLI_UI_LANGUAGE=en-US"
set "VSLANG=1033"

rem ============================================================================
rem Execute tous les tests de la solution BudgetManager :
rem   1. restaure les dependances NuGet et LibMan ;
rem   2. execute les tests ;
rem   3. genere les fichiers TRX ;
rem   4. collecte la couverture Cobertura ;
rem   5. genere et ouvre le rapport HTML.
rem
rem Emplacement attendu :
rem   <racine-du-depot>\tools\test_solution.bat
rem
rem Utilisation :
rem   tools\test_solution.bat
rem   tools\test_solution.bat --no-open
rem ============================================================================

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "ROOT_DIR=%%~fI"

set "SOLUTION_FILE=%ROOT_DIR%\BudgetManager.sln"

set "RESULTS_ROOT=%ROOT_DIR%\tests_results"
set "TEST_RESULTS_DIR=%RESULTS_ROOT%\test_runs"
set "COVERAGE_REPORT_DIR=%RESULTS_ROOT%\coverage_report"
set "COVERAGE_PATTERN=%TEST_RESULTS_DIR%\coverage.cobertura.*.xml"

set "CONFIGURATION=Debug"
set "OPEN_REPORT=1"

if /I "%~1"=="--no-open" (
    set "OPEN_REPORT=0"
)

echo.
echo ============================================================
echo BudgetManager - Tests et couverture
echo ============================================================
echo Racine        : %ROOT_DIR%
echo Solution      : %SOLUTION_FILE%
echo Configuration : %CONFIGURATION%
echo Resultats     : %RESULTS_ROOT%
echo.

rem ----------------------------------------------------------------------------
rem Verification de la solution
rem ----------------------------------------------------------------------------

if not exist "%SOLUTION_FILE%" (
    echo [ERREUR] La solution est introuvable :
    echo   %SOLUTION_FILE%
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
rem Verification de ReportGenerator
rem ----------------------------------------------------------------------------

where reportgenerator >nul 2>&1

if errorlevel 1 (
    echo [INFO] ReportGenerator n'est pas installe.
    echo [INFO] Installation de l'outil global...

    dotnet tool install --global dotnet-reportgenerator-globaltool

    if errorlevel 1 (
        echo.
        echo [ERREUR] Impossible d'installer ReportGenerator.
        echo.
        echo Essayez manuellement :
        echo   dotnet tool install --global dotnet-reportgenerator-globaltool
        exit /b 1
    )
)

rem ----------------------------------------------------------------------------
rem Nettoyage des anciens resultats
rem ----------------------------------------------------------------------------

if exist "%RESULTS_ROOT%" (
    echo [INFO] Suppression des anciens resultats...
    rmdir /s /q "%RESULTS_ROOT%"

    if exist "%RESULTS_ROOT%" (
        echo [ERREUR] Impossible de supprimer :
        echo   %RESULTS_ROOT%
        echo.
        echo Verifiez qu'aucun fichier du rapport n'est ouvert.
        exit /b 1
    )
)

mkdir "%TEST_RESULTS_DIR%" >nul 2>&1
mkdir "%COVERAGE_REPORT_DIR%" >nul 2>&1

rem ----------------------------------------------------------------------------
rem 1. Restauration
rem ----------------------------------------------------------------------------

echo.
echo [1/4] Restauration des dependances...

call "%SCRIPT_DIR%restore_solution.bat"

if errorlevel 1 (
    echo.
    echo [ERREUR] La restauration des dependances a echoue.
    exit /b 1
)

rem ----------------------------------------------------------------------------
rem 2. Compilation et execution des tests
rem ----------------------------------------------------------------------------

echo.
echo [2/4] Execution des tests et collecte de la couverture...

dotnet test ^
    --solution "%SOLUTION_FILE%" ^
    --configuration "%CONFIGURATION%" ^
    --no-restore ^
    --results-directory "%TEST_RESULTS_DIR%" ^
    -- ^
    --report-xunit-trx ^
    --coverlet ^
    --coverlet-output-format cobertura ^
    --coverlet-include "[BudgetManager.*]*"

if errorlevel 1 (
    echo.
    echo [ERREUR] La compilation ou un ou plusieurs tests ont echoue.
    echo.
    echo Les resultats disponibles se trouvent dans :
    echo   %TEST_RESULTS_DIR%
    exit /b 1
)

rem ----------------------------------------------------------------------------
rem Recherche des rapports Cobertura
rem ----------------------------------------------------------------------------

set "COVERAGE_FOUND=0"
set /a COVERAGE_COUNT=0

for %%F in ("%TEST_RESULTS_DIR%\coverage.cobertura.*.xml") do (
    set "COVERAGE_FOUND=1"
    set /a COVERAGE_COUNT+=1
    echo [INFO] Couverture trouvee : %%F
)

if "!COVERAGE_FOUND!"=="0" (
    echo.
    echo [ERREUR] Aucun rapport de couverture Cobertura n'a ete genere.
    echo.
    echo Verifiez que chaque projet de tests reference :
    echo   coverlet.MTP
    echo.
    echo Exemple :
    echo   ^<PackageReference Include="coverlet.MTP" /^>
    exit /b 1
)

echo [INFO] Nombre de rapports de couverture : !COVERAGE_COUNT!

rem ----------------------------------------------------------------------------
rem 3. Generation du rapport HTML
rem ----------------------------------------------------------------------------

echo.
echo [3/4] Generation du rapport HTML...

reportgenerator ^
    "-reports:%COVERAGE_PATTERN%" ^
    "-targetdir:%COVERAGE_REPORT_DIR%" ^
    "-reporttypes:Html;Cobertura;TextSummary" ^
    "-title:BudgetManager - Couverture de code"

if errorlevel 1 (
    echo.
    echo [ERREUR] La generation du rapport a echoue.
    exit /b 1
)

set "REPORT_FILE=%COVERAGE_REPORT_DIR%\index.html"
set "SUMMARY_FILE=%COVERAGE_REPORT_DIR%\Summary.txt"

if not exist "%REPORT_FILE%" (
    echo.
    echo [ERREUR] Le rapport HTML est introuvable :
    echo   %REPORT_FILE%
    exit /b 1
)

rem ----------------------------------------------------------------------------
rem Affichage du resume
rem ----------------------------------------------------------------------------

echo.
echo [4/4] Rapport genere avec succes.

if exist "%SUMMARY_FILE%" (
    echo.
    echo ------------------------------------------------------------
    type "%SUMMARY_FILE%"
    echo ------------------------------------------------------------
)

echo.
echo Resultats des tests :
echo   %TEST_RESULTS_DIR%
echo.
echo Rapport de couverture :
echo   %REPORT_FILE%
echo.

rem ----------------------------------------------------------------------------
rem Ouverture du rapport
rem ----------------------------------------------------------------------------

if "%OPEN_REPORT%"=="1" (
    echo [INFO] Ouverture du rapport dans le navigateur...
    start "" "%REPORT_FILE%"
) else (
    echo [INFO] Ouverture automatique desactivee.
)

echo.
echo [SUCCES] Tests et couverture termines.
echo.

exit /b 0
