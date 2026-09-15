@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem Force les outils .NET, MSBuild, NuGet et Microsoft.Testing.Platform a afficher leurs messages
rem en anglais afin d'eviter les problemes d'encodage dans cmd.exe.
set "DOTNET_CLI_UI_LANGUAGE=en-US"
set "VSLANG=1033"

rem ============================================================================
rem Execute tout ou partie des tests de la solution BudgetManager.
rem
rem Sans selecteur de tests, tous les tests sont executes.
rem Les selecteurs peuvent etre combines :
rem   --domain
rem   --application
rem   --infrastructure
rem   --web
rem   --js
rem
rem Autres options :
rem   --all       execute explicitement tous les tests
rem   --no-open   n'ouvre pas le rapport HTML .NET
rem   --help      affiche l'aide
rem
rem Exemples :
rem   tools\test_solution.bat
rem   tools\test_solution.bat --js
rem   tools\test_solution.bat --web --js --no-open
rem   tools\test_solution.bat --domain --application
rem ============================================================================

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "ROOT_DIR=%%~fI"

set "SOLUTION_FILE=%ROOT_DIR%\BudgetManager.sln"
set "DOMAIN_TEST_PROJECT=%ROOT_DIR%\tests\BudgetManager.Domain.Tests\BudgetManager.Domain.Tests.csproj"
set "APPLICATION_TEST_PROJECT=%ROOT_DIR%\tests\BudgetManager.Application.Tests\BudgetManager.Application.Tests.csproj"
set "INFRASTRUCTURE_TEST_PROJECT=%ROOT_DIR%\tests\BudgetManager.Infrastructure.Tests\BudgetManager.Infrastructure.Tests.csproj"
set "WEB_TEST_PROJECT=%ROOT_DIR%\tests\BudgetManager.Web.Tests\BudgetManager.Web.Tests.csproj"
set "JS_TEST_DIR=%ROOT_DIR%\tests\BudgetManager.Web.JsTests"
set "JS_PACKAGE_LOCK=%JS_TEST_DIR%\package-lock.json"

set "RESULTS_ROOT=%ROOT_DIR%\tests_results"
set "TEST_RESULTS_DIR=%RESULTS_ROOT%\test_runs"
set "COVERAGE_REPORT_DIR=%RESULTS_ROOT%\coverage_report"
set "COVERAGE_PATTERN=%TEST_RESULTS_DIR%\coverage.cobertura.*.xml"
set "JS_COVERAGE_DIR=%JS_TEST_DIR%\coverage"

set "CONFIGURATION=Debug"
set "OPEN_REPORT=1"

set "RUN_DOMAIN=0"
set "RUN_APPLICATION=0"
set "RUN_INFRASTRUCTURE=0"
set "RUN_WEB=0"
set "RUN_JS=0"
set "HAS_SELECTOR=0"

:parse_args
if "%~1"=="" goto args_parsed

if /I "%~1"=="--domain" (
    set "RUN_DOMAIN=1"
    set "HAS_SELECTOR=1"
    shift
    goto parse_args
)
if /I "%~1"=="--application" (
    set "RUN_APPLICATION=1"
    set "HAS_SELECTOR=1"
    shift
    goto parse_args
)
if /I "%~1"=="--infrastructure" (
    set "RUN_INFRASTRUCTURE=1"
    set "HAS_SELECTOR=1"
    shift
    goto parse_args
)
if /I "%~1"=="--web" (
    set "RUN_WEB=1"
    set "HAS_SELECTOR=1"
    shift
    goto parse_args
)
if /I "%~1"=="--js" (
    set "RUN_JS=1"
    set "HAS_SELECTOR=1"
    shift
    goto parse_args
)
if /I "%~1"=="--all" (
    set "RUN_DOMAIN=1"
    set "RUN_APPLICATION=1"
    set "RUN_INFRASTRUCTURE=1"
    set "RUN_WEB=1"
    set "RUN_JS=1"
    set "HAS_SELECTOR=1"
    shift
    goto parse_args
)
if /I "%~1"=="--no-open" (
    set "OPEN_REPORT=0"
    shift
    goto parse_args
)
if /I "%~1"=="--help" goto show_help
if /I "%~1"=="-h" goto show_help
if /I "%~1"=="/?" goto show_help

echo [ERREUR] Parametre inconnu : %~1
echo.
goto show_help_error

:args_parsed
if "%HAS_SELECTOR%"=="0" (
    set "RUN_DOMAIN=1"
    set "RUN_APPLICATION=1"
    set "RUN_INFRASTRUCTURE=1"
    set "RUN_WEB=1"
    set "RUN_JS=1"
)

set "RUN_DOTNET=0"
if "%RUN_DOMAIN%"=="1" set "RUN_DOTNET=1"
if "%RUN_APPLICATION%"=="1" set "RUN_DOTNET=1"
if "%RUN_INFRASTRUCTURE%"=="1" set "RUN_DOTNET=1"
if "%RUN_WEB%"=="1" set "RUN_DOTNET=1"

echo.
echo ============================================================
echo BudgetManager - Tests et couverture
echo ============================================================
echo Racine        : %ROOT_DIR%
echo Configuration : %CONFIGURATION%
echo.
echo Groupes selectionnes :
if "%RUN_DOMAIN%"=="1" ( echo   [X] Domain ) else ( echo   [ ] Domain )
if "%RUN_APPLICATION%"=="1" ( echo   [X] Application ) else ( echo   [ ] Application )
if "%RUN_INFRASTRUCTURE%"=="1" ( echo   [X] Infrastructure ) else ( echo   [ ] Infrastructure )
if "%RUN_WEB%"=="1" ( echo   [X] Web ^(.NET^) ) else ( echo   [ ] Web ^(.NET^) )
if "%RUN_JS%"=="1" ( echo   [X] Web ^(JavaScript^) )  else ( echo   [ ] Web ^(JavaScript^)  )
echo.

if not exist "%SOLUTION_FILE%" (
    echo [ERREUR] La solution est introuvable :
    echo   %SOLUTION_FILE%
    exit /b 1
)

if "%RUN_DOTNET%"=="1" (
    where dotnet >nul 2>&1
    if errorlevel 1 (
        echo [ERREUR] Le SDK .NET est introuvable.
        echo Executez tools\configure_desktop.bat puis relancez ce script.
        exit /b 1
    )

    where reportgenerator >nul 2>&1
    if errorlevel 1 (
        echo [INFO] ReportGenerator n'est pas installe.
        echo [INFO] Installation de l'outil global...
        dotnet tool install --global dotnet-reportgenerator-globaltool
        if errorlevel 1 (
            echo [ERREUR] Impossible d'installer ReportGenerator.
            exit /b 1
        )
    )
)

if "%RUN_JS%"=="1" (
    where node.exe >nul 2>&1
    if errorlevel 1 (
        echo [ERREUR] Node.js est introuvable.
        echo Executez tools\configure_desktop.bat puis relancez ce script.
        exit /b 1
    )

    where npm.cmd >nul 2>&1
    if errorlevel 1 (
        echo [ERREUR] npm est introuvable.
        echo Executez tools\configure_desktop.bat puis relancez ce script.
        exit /b 1
    )

    if not exist "%JS_PACKAGE_LOCK%" (
        echo [ERREUR] Le fichier npm lock est introuvable :
        echo   %JS_PACKAGE_LOCK%
        exit /b 1
    )
)

rem Nettoyage uniquement des resultats qui vont etre regeneres.
if "%RUN_DOTNET%"=="1" (
    if exist "%RESULTS_ROOT%" rmdir /s /q "%RESULTS_ROOT%"
    mkdir "%TEST_RESULTS_DIR%" >nul 2>&1
    mkdir "%COVERAGE_REPORT_DIR%" >nul 2>&1
)

if "%RUN_JS%"=="1" (
    if exist "%JS_COVERAGE_DIR%" rmdir /s /q "%JS_COVERAGE_DIR%"
)

rem ----------------------------------------------------------------------------
rem Restauration
rem ----------------------------------------------------------------------------

if "%RUN_DOTNET%"=="1" (
    echo.
    echo [RESTORE] Dependances .NET et bibliotheques clientes...
    call "%SCRIPT_DIR%restore_solution.bat"
    if errorlevel 1 (
        echo.
        echo [ERREUR] La restauration des dependances .NET / LibMan a echoue.
        exit /b 1
    )
)

if "%RUN_JS%"=="1" (
    echo.
    echo [RESTORE] Dependances JavaScript avec npm ci...
    pushd "%JS_TEST_DIR%"
    call npm.cmd ci
    set "NPM_CI_EXIT=!ERRORLEVEL!"
    popd
    if not "!NPM_CI_EXIT!"=="0" (
        echo.
        echo [ERREUR] La restauration des dependances JavaScript a echoue.
        exit /b 1
    )
)

rem ----------------------------------------------------------------------------
rem Tests .NET selectionnes
rem ----------------------------------------------------------------------------

if "%RUN_DOMAIN%"=="1" call :run_dotnet_tests "Domain" "%DOMAIN_TEST_PROJECT%"
if errorlevel 1 exit /b 1

if "%RUN_APPLICATION%"=="1" call :run_dotnet_tests "Application" "%APPLICATION_TEST_PROJECT%"
if errorlevel 1 exit /b 1

if "%RUN_INFRASTRUCTURE%"=="1" call :run_dotnet_tests "Infrastructure" "%INFRASTRUCTURE_TEST_PROJECT%"
if errorlevel 1 exit /b 1

if "%RUN_WEB%"=="1" call :run_dotnet_tests "Web" "%WEB_TEST_PROJECT%"
if errorlevel 1 exit /b 1

rem ----------------------------------------------------------------------------
rem Tests JavaScript et couverture
rem ----------------------------------------------------------------------------

if "%RUN_JS%"=="1" (
    echo.
    echo [TEST] JavaScript et couverture...

    pushd "%JS_TEST_DIR%"
    call npm.cmd run test:coverage
    set "NPM_TEST_EXIT=!ERRORLEVEL!"
    popd

    if not "!NPM_TEST_EXIT!"=="0" (
        echo.
        echo [ERREUR] Un ou plusieurs tests JavaScript ont echoue.
        exit /b 1
    )

)

rem ----------------------------------------------------------------------------
rem Rapport de couverture .NET pour les projets selectionnes
rem ----------------------------------------------------------------------------

if "%RUN_DOTNET%"=="1" (
    set "COVERAGE_FOUND=0"
    set /a COVERAGE_COUNT=0

    for %%F in ("%TEST_RESULTS_DIR%\coverage.cobertura.*.xml") do (
        if exist "%%F" (
            set "COVERAGE_FOUND=1"
            set /a COVERAGE_COUNT+=1
            echo [INFO] Couverture trouvee : %%F
        )
    )

    if "!COVERAGE_FOUND!"=="0" (
        echo.
        echo [ERREUR] Aucun rapport de couverture Cobertura .NET n'a ete genere.
        exit /b 1
    )

    echo.
    echo [REPORT] Generation du rapport HTML .NET...

    reportgenerator ^
        "-reports:%COVERAGE_PATTERN%" ^
        "-targetdir:%COVERAGE_REPORT_DIR%" ^
        "-reporttypes:Html;Cobertura;TextSummary" ^
        "-title:BudgetManager - Couverture de code"

    if errorlevel 1 (
        echo.
        echo [ERREUR] La generation du rapport .NET a echoue.
        exit /b 1
    )

    set "REPORT_FILE=%COVERAGE_REPORT_DIR%\index.html"
    set "SUMMARY_FILE=%COVERAGE_REPORT_DIR%\Summary.txt"

    if not exist "!REPORT_FILE!" (
        echo.
        echo [ERREUR] Le rapport HTML .NET est introuvable :
        echo   !REPORT_FILE!
        exit /b 1
    )

    if exist "!SUMMARY_FILE!" (
        echo.
        echo ------------------------------------------------------------
        type "!SUMMARY_FILE!"
        echo ------------------------------------------------------------
    )

    echo.
    echo Rapport de couverture .NET :
    echo   !REPORT_FILE!

    if "%OPEN_REPORT%"=="1" (
        echo [INFO] Ouverture du rapport .NET dans le navigateur...
        start "" "!REPORT_FILE!"
    ) else (
        echo [INFO] Ouverture automatique desactivee.
    )
)

if "%RUN_JS%"=="1" (
    set "JS_REPORT_FILE=%JS_COVERAGE_DIR%\index.html"

    echo.
    echo Rapport de couverture JavaScript :
    echo   !JS_REPORT_FILE!

    if not exist "!JS_REPORT_FILE!" (
        echo.
        echo [ERREUR] Le rapport HTML JavaScript est introuvable :
        echo   !JS_REPORT_FILE!
        exit /b 1
    )

    if "%OPEN_REPORT%"=="1" (
        echo [INFO] Ouverture du rapport JavaScript dans le navigateur...
        start "" "!JS_REPORT_FILE!"
    ) else (
        echo [INFO] Ouverture automatique desactivee.
    )
)

echo.
echo ============================================================
echo [SUCCES] Tous les groupes de tests selectionnes ont reussi.
echo ============================================================
echo.
exit /b 0

:run_dotnet_tests
set "TEST_GROUP=%~1"
set "TEST_PROJECT=%~2"

echo.
echo [TEST] %TEST_GROUP% .NET et couverture...

dotnet test "%TEST_PROJECT%" ^
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
    echo [ERREUR] La compilation ou un ou plusieurs tests %TEST_GROUP% ont echoue.
    echo Resultats disponibles :
    echo   %TEST_RESULTS_DIR%
    exit /b 1
)

exit /b 0

:show_help
echo.
echo BudgetManager - test_solution.bat
echo.
echo Utilisation :
echo   tools\test_solution.bat [selecteurs] [options]
echo.
echo Selecteurs combinables :
echo   --domain          tests BudgetManager.Domain.Tests
echo   --application     tests BudgetManager.Application.Tests
echo   --infrastructure  tests BudgetManager.Infrastructure.Tests
echo   --web             tests BudgetManager.Web.Tests
echo   --js              tests BudgetManager.Web.JsTests
echo   --all             tous les groupes
echo.
echo Sans selecteur, tous les groupes sont executes.
echo.
echo Options :
echo   --no-open         ne pas ouvrir le rapport HTML .NET
echo   --help            afficher cette aide
echo.
echo Exemples :
echo   tools\test_solution.bat --js
echo   tools\test_solution.bat --web --js --no-open
echo   tools\test_solution.bat --domain --application
echo.
exit /b 0

:show_help_error
call :show_help
exit /b 1
