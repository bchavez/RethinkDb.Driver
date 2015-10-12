@ECHO OFF
SETLOCAL

REM Uncomment to forcibly set the build version.
REM set FORCE_VERSION=0.0.4-alpha4

IF NOT DEFINED DevEnvDir (
	IF DEFINED vs140comntools ( 
		CALL "%vs140comntools%\vsvars32.bat"
	)
)

if exist nuget.exe (
    .\nuget.exe restore Source\RethinkDb.Driver.sln
) else (
    echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    echo      nuget.exe required
    echo ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    echo nuget.exe v3.2+ is required in the root build directory.
    echo You can download nuget.exe from here:
    echo https://dist.nuget.org/index.html
    echo Once downloaded, place it in the same folder as build.cmd
    exit /b 99
)

msbuild Source\Builder\Builder.csproj
if %errorlevel% neq 0 exit /b %errorlevel%

ECHO ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
ECHO        RUNNING BAU BUILDER
ECHO ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Source\Builder\bin\Debug\Builder.exe %1
if %errorlevel% neq 0 exit /b %errorlevel%
