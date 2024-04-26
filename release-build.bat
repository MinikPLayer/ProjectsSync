@echo off
setlocal enabledelayedexpansion

goto :main

:build_and_pack
    set "PROJECT_DIR=%~1"
    set "PROJECT_NAME=%~2"
    set "DOTNET_VERSION=%~3"

    echo Building %PROJECT_NAME%...
    cd /d "%SCRIPT_DIR%\%PROJECT_DIR%"
    dotnet clean
    rd /s /q .\bin
    rd /s /q .\obj
    set VERSION=
    dotnet build -c Release /p:FileVersion="%FILE_VERSION%" || call :error "Error building project %PROJECT_NAME%"
    cd bin\Release\%DOTNET_VERSION%

	rem Write version.txt file
    if exist "%VERSION_FILE_NAME%" del "%VERSION_FILE_NAME%"
    echo %FILE_VERSION% > "%VERSION_FILE_NAME%"
    set "ZIP_FILE_NAME=%PROJECT_NAME%_%FILE_VERSION%_windows.zip"
    if exist "%ZIP_FILE_NAME%" del "%ZIP_FILE_NAME%"

	rem Copy files to the installer directory
	set "INSTALLER_OUTPUT_PROJECT_DIR=%INSTALLER_OUTPUT_DIR%\%PROJECT_NAME%\"
	if not exist "%INSTALLER_OUTPUT_PROJECT_DIR%" mkdir "%INSTALLER_OUTPUT_PROJECT_DIR%"
	xcopy * "%INSTALLER_OUTPUT_PROJECT_DIR%" /S
	
	rem Create zip file.
	powershell Compress-Archive -Path * -DestinationPath "%ZIP_FILE_NAME%"
    copy "%ZIP_FILE_NAME%" "%BUILD_OUTPUT_DIR%"

	
    exit /b

:error
    set "ERROR_MSG=%~1"
    echo "ERROR: %ERROR_MSG%"
    PAUSE
    exit

:main
    set "SCRIPT_DIR=%~dp0"
    if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"
    if "%SCRIPT_DIR%"=="" (
        echo Cannot determine script location. Exiting
        exit /b 1
    )
    set "SCRIPT_PATH=%0"

    if "%~1"=="" (
        echo Usage: release-build.bat ^<version_string^>
        exit /b 3
    )

    if "%~2" == "" (
	    cmd /C "%SCRIPT_PATH% %~1 1"
	    exit /B %ERRORLEVEL%
    )

    set "FILE_VERSION=%~1"
    set "BUILD_OUTPUT_DIR=%SCRIPT_DIR%\build"
    if exist "%BUILD_OUTPUT_DIR%" rmdir /s /q "%BUILD_OUTPUT_DIR%"
    mkdir "%BUILD_OUTPUT_DIR%"

    set "INSTALLER_OUTPUT_DIR=%SCRIPT_DIR%\Installer\source"
    if exist "%INSTALLER_OUTPUT_DIR%" rmdir /s /q "%INSTALLER_OUTPUT_DIR%"
    mkdir "%INSTALLER_OUTPUT_DIR%"

    set "VERSION_FILE_NAME=version.txt"

    call :build_and_pack "ProjectsSyncLib" "prsync-lib" "net7.0"
    call :build_and_pack "ProjectsSyncLibCLI" "prsync-cli" "net8.0"
    call :build_and_pack "ProjectsSyncAv.Desktop" "prsync-gui" "net7.0"
