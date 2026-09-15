@echo off
setlocal EnableExtensions EnableDelayedExpansion

rem ============================================================================
rem Nettoie la solution en supprimant récursivement les dossiers :
rem   bin, obj, packages, .vs
rem
rem Emplacement attendu :
rem   <racine-du-depot>\tools\clean_solution.bat
rem ============================================================================

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "ROOT_DIR=%%~fI"

echo.
echo === Nettoyage de la solution ===
echo Racine : %ROOT_DIR%
echo.

set /a DELETED_COUNT=0

for %%D in (bin obj packages .vs TestResults tests_results) do (
    echo Recherche des dossiers "%%D"...

    for /d /r "%ROOT_DIR%" %%P in (%%D) do (
        if exist "%%P" (
            echo   Suppression : %%P
            rmdir /s /q "%%P"

            if exist "%%P" (
                echo   [ERREUR] Impossible de supprimer : %%P
            ) else (
                set /a DELETED_COUNT+=1
            )
        )
    )
)

echo.
echo Nettoyage termine.
echo Nombre de dossiers supprimes : !DELETED_COUNT!
echo.

exit /b 0