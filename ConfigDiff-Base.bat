@echo off
setlocal EnableExtensions
setlocal EnableDelayedExpansion

set "BranchBaseFolderName=Configuration.Base.4.2"

set "CurrentDir=%~dp0"
if "%CurrentDir:~-1%" == "\" (
    set "CurrentDir=%CurrentDir:~0,-1%"
)
pushd "%CurrentDir%"

set "OldConfigPath=..\%BranchBaseFolderName%"
set "NewConfigPath=..\Configuration"
set "ReleaseNotesPath=ReleaseNotes.txt"
set "OutPath=ReleaseNotes\rn-{0}.txt"
set "ReleaseNotesLastBlockPath=ReleaseNotes.last.txt"
set "ConfigDiffAdditionalArgs=-csf:PostgreSql -csf:Roles -csf:VirtualFiles -lang:ru -warnAsError"

rem Default tools folder relative to "Source" project folder, can be overriden in DevEnv.bat
set "Tools=%CurrentDir%\..\tools"

:Args
if "%~1"=="" goto :Start
if "%~1"=="/batch" set "batch=1"
if "%~1"=="/old" goto :SetOld
if "%~1"=="/new" goto :SetNew
if "%~1"=="/ex" goto :SetEx
if "%~1"=="/no-ex" set "ReleaseNotesPath="
if "%~1"=="/out" goto :SetOut
shift
goto :Args

:SetOld
shift
set "OldConfigPath=%~1"
shift
goto :Args

:SetNew
shift
set "NewConfigPath=%~1"
shift
goto :Args

:SetEx
shift
set "ReleaseNotesPath=%~1"
shift
goto :Args

:SetOut
shift
set "OutPath=%~1"
shift
goto :Args

:Start
if not exist "%OldConfigPath%" (
    echo Base configuration folder not found: %OldConfigPath%
    echo To use the script, create such a folder, and put into it the configuration from the respective release build
    goto :Error
)
if not exist "%NewConfigPath%" (
    echo Current configuration folder not found: %NewConfigPath%
    goto :Error
)
if not "%ReleaseNotesPath%"=="" if not exist "%ReleaseNotesPath%" (
    echo ReleaseNotes file not found: %ReleaseNotesPath%
    goto :Error
)
if exist DevEnv.bat call DevEnv.bat
if not exist "%Tools%" (
    echo Tools folder not found: %Tools%
    goto :Error
)

if not "%batch%"=="1" (
    cls
    echo This script will compare two configuration folders
    echo;
    echo [OldConfigPath] = %OldConfigPath%
    echo [NewConfigPath] = %NewConfigPath%
    if not "%ReleaseNotesPath%"=="" (
        echo [ReleaseNotesPath] = %ReleaseNotesPath%
    )
    echo [OutPath] = %OutPath%
    echo [Tools] = %Tools%
    echo;
    echo Press any key to begin the comparison...
    pause>nul
    echo;
    echo;
    echo Comparing configuration folders
)

set "ExArg="
if not "%ReleaseNotesPath%"=="" (
    set ExArg="-ex:%ReleaseNotesLastBlockPath%"
    echo;
    echo   ^> Extracting notes for last release
    "%Tools%\tadmin" Script ExtractNotesBlock "-pp:path=%ReleaseNotesPath%" "-pp:out=%ReleaseNotesLastBlockPath%" -nologo
    if not "!ErrorLevel!"=="0" goto :Error
)

echo;
echo   ^> Comparing two folders
"%Tools%\tadmin" ConfigDiff "%OutPath%" "-old:%OldConfigPath%" "-new:%NewConfigPath%" %ExArg% %ConfigDiffAdditionalArgs% -nologo
if not "%ErrorLevel%"=="0" goto :Error

if not "%ReleaseNotesPath%"=="" (
    echo;
    echo   ^> Cleaning up
    del /Q "%ReleaseNotesLastBlockPath%"
)

if not "%batch%"=="1" (
    echo;
    echo Completed successfully.
)

:Finish
popd
if not "%batch%"=="1" pause
endlocal
goto :EOF

:Error
popd
echo;
echo Script failed with error code: %ErrorLevel% >&2
if not "%batch%"=="1" pause
endlocal
exit /b 1
