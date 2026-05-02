@echo off
setlocal EnableExtensions
setlocal EnableDelayedExpansion

set "BranchBaseName=4.2"

set "CurrentDir=%~dp0"
if "%CurrentDir:~-1%" == "\" (
    set "CurrentDir=%CurrentDir:~0,-1%"
)
pushd "%CurrentDir%"

set "ConfigPath=..\Configuration"
set "CommitConfigPath=..\Configuration.Base.commit"
set "OutPath=ReleaseNotes\rn-cfg-{0}.txt"
set "BaseCommit="
set "CopyParams=/Q /R /Y"

rem Default tools folder relative to "Source" project folder, can be overriden in DevEnv.bat
set "Tools=%CurrentDir%\..\tools"

:Args
if "%~1"=="" goto :Start
if "%~1"=="/batch" set "batch=1"
if "%~1"=="/commit" goto :SetCommit
if "%~1"=="/out" goto :SetOut
shift
goto :Args

:SetCommit
shift
set "BaseCommit=%~1"
shift
goto :Args

:SetOut
shift
set "OutPath=%~1"
shift
goto :Args

:Start
if not exist "%ConfigPath%" (
    echo Current configuration folder not found: %ConfigPath%
    goto :Error
)
if exist DevEnv.bat call DevEnv.bat
if not exist "%Tools%" (
    echo Tools folder not found: %Tools%
    goto :Error
)

if not "%batch%"=="1" (
    cls
    echo This script will compare configuration folder between current and other commits
    echo;
    echo Please, make sure that there is NO UNCOMMITTED CHANGES in the configuration folder
    echo;
)

if not "%BaseCommit%"=="" goto :BaseCommitInputCompleted
if "%batch%"=="1" goto :BaseCommitInputCompleted
set /P BaseCommit="Base commit hash, or its branch name [%BranchBaseName%]: "
echo;

:BaseCommitInputCompleted
if "%BaseCommit%"=="" set "BaseCommit=%BranchBaseName%"
if not "%batch%"=="1" (
    echo [BaseCommit] = %BaseCommit%
    echo [ConfigPath] = %ConfigPath%
    echo [OutPath] = %OutPath%
    echo [Tools] = %Tools%
    echo;
    echo Press any key to begin the comparison...
    pause>nul
    echo;
    echo;
    echo Comparing configuration folders
)

echo;
echo   ^> Preparing
rd /S /Q "%CommitConfigPath%">nul 2>&1
git restore "%ConfigPath%"
if not "%ErrorLevel%"=="0" goto :Error
git clean -fd "%ConfigPath%"
if not "%ErrorLevel%"=="0" goto :Error

echo;
echo   ^> Resolving configuration from commit %BaseCommit%
git restore -s "%BaseCommit%" "%ConfigPath%"
if not "%ErrorLevel%"=="0" goto :Error
xcopy "%ConfigPath%\" "%CommitConfigPath%\" /S %CopyParams%>nul
git restore "%ConfigPath%"
if not "%ErrorLevel%"=="0" goto :Error
git clean -fd "%ConfigPath%"
if not "%ErrorLevel%"=="0" goto :Error

call ConfigDiff-Base.bat /batch /no-ex /old "%CommitConfigPath%" /new "%ConfigPath%" /out "%OutPath%"
if not "%ErrorLevel%"=="0" goto :Error

echo;
echo   ^> Cleaning up
rd /S /Q "%CommitConfigPath%"

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
