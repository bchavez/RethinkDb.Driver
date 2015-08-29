@ECHO OFF
SETLOCAL

set BUILD_VERSION=0.0.0.3

IF NOT DEFINED DevEnvDir (
	IF DEFINED vs140comntools ( 
		CALL "%vs140comntools%\vsvars32.bat"
	)
)

msbuild source\Builder\Builder.csproj
if %errorlevel% neq 0 exit /b %errorlevel%

ECHO ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
ECHO        RUNNING BAU BUILDER
ECHO ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Source\Builder\bin\Debug\Builder.exe %1
if %errorlevel% neq 0 exit /b %errorlevel%
