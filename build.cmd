@echo off
cls
REM "NuGet.exe" "Install" "FAKE" "-OutputDirectory" "Source\packages" "-ExcludeVersion"

.paket\paket.bootstrapper.exe
if errorlevel 1 (
  exit /b %errorlevel%
)

pushd Source
..\.paket\paket.exe install
if errorlevel 1 (
  popd
  exit /b %errorlevel%
)
popd

pushd Builder
..\.paket\paket.exe install
if errorlevel 1 (
  popd
  exit /b %errorlevel%
)

"packages\build\FAKE\tools\Fake.exe" build.fsx %1
popd
