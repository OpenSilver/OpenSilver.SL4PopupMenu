@ECHO off

SETLOCAL

IF "%~1" == "--help" (
	GOTO :help
)

IF "%~1" == "-h" (
	GOTO :help
)

SET BUILD_DIR=%~dp0
SET SRC_DIR=%~dp0..\
SET CFG=Release

REM Define the escape character for colored text
FOR /F %%a IN ('"prompt $E$S & echo on & for %%b in (1) do rem"') DO SET "ESC=%%a"

REM Define the PackageVersion and OpenSilverPkgVersion variables
IF "%~1" == "" (
	SET /P PackageVersion="%ESC%[92mOpenSilver.SL4PopupMenu version:%ESC%[0m "
	SET /P OpenSilverPkgVersion="%ESC%[92mOpenSilver version:%ESC%[0m "
) ELSE (
	SET PackageVersion=%1
	if "%~2" == "" (
		SET OpenSilverPkgVersion=%1
	) ELSE (
		SET OpenSilverPkgVersion=%2
	)
)

ECHO. 
ECHO %ESC%[95mBuilding %ESC%[0m%CFG% %ESC%[95mconfiguration%ESC%[0m
ECHO. 
msbuild %SRC_DIR%\SL4PopupMenuDemo.Opensilver.sln -p:Configuration=%CFG%;OpenSilverVersion=%OpenSilverPkgVersion% -verbosity:minimal -restore

ECHO. 
ECHO %ESC%[95mPacking %ESC%[0mOpenSilver.SL4PopupMenu %ESC%[95mNuGet package%ESC%[0m
ECHO. 
%BUILD_DIR%\nuget.exe pack %BUILD_DIR%\nuspec\OpenSilver.SL4PopupMenu.nuspec -OutputDirectory "%BUILD_DIR%\output" -Properties "PackageVersion=%PackageVersion%;OpenSilverDependencyVersion=%OpenSilverPkgVersion%;Configuration=%CFG%"

EXIT /b

:help
ECHO [1] OpenSilver.SL4PopupMenu NuGet package Version
ECHO [2] OpenSilver Version

ENDLOCAL