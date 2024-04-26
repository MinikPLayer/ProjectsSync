!include "MUI2.nsh"

!define INSTALLER_SOURCES "source\"
!define PROJECT_NAME "Projects Sync"

    !define !IfNExist `!insertmacro _!IfExist "n"`
    !macro _!IfExist _OP _FilePath
        !ifdef !IfExistIsTrue
            !undef !IfExistIsTrue
        !endif
        !tempfile "!IfExistTmp"
        !system `IF EXIST "${_FilePath}" Echo !define "!IfExistIsTrue" > "${!IfExistTmp}"`
        !include /NONFATAL "${!IfExistTmp}"
        !delfile /NONFATAL "${!IfExistTmp}"
        !undef !IfExistTmp
        !if${_OP}def !IfExistIsTrue
    !macroend

	!macro !UnZip _ZipPath _DestPath
		!system 'cmd /C "del /F /S /Q ${_DestPath} && rmdir /q /s ${_DestPath}"'
		!system 'powershell.exe -Command "Expand-Archive ${_ZipPath} -DestinationPath ${_DestPath} "' = 0
	!macroend

# Check for sources
${!IfNExist} "${INSTALLER_SOURCES}"
	!error "Build directory is missing. Use release-build script before running this installer script."
!endif

!define SRC_PATH_CLI "${INSTALLER_SOURCES}\prsync-cli"
${!IfNExist} "${SRC_PATH_CLI}"
	!error "Projects Sync CLI folder is missing. Rebuild using release-build script and try again."
!endif

!define SRC_PATH_GUI "${INSTALLER_SOURCES}\prsync-gui"
${!IfNExist} "${SRC_PATH_GUI}"
	!error "Projects Sync GUI folder is missing. Rebuild using release-build script and try again."
!endif


# Define output variables
Outfile "ProjectsSyncInstaller.exe"
InstallDir "$PROGRAMFILES64\ProjectsSync"
Name "${PROJECT_NAME}"
Unicode True

!define REG_KEY_PATH "Software\${PROJECT_NAME}"
InstallDirRegKey HKCU "${REG_KEY_PATH}" ""

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "Command-line utilities" SecCLI
	!define OUT_PATH_CLI "$INSTDIR\CLI"

	SetOutPath "${OUT_PATH_CLI}"
	File /r "${SRC_PATH_CLI}\*"

	WriteRegStr HKCU "${REG_KEY_PATH}\CLI" "" ${OUT_PATH_CLI}
SectionEnd

Section "Graphical Interface" SecGUI
	!define OUT_PATH_GUI "$INSTDIR\GUI"
	
	SetOutPath "${OUT_PATH_GUI}"
	File /r "${SRC_PATH_GUI}\*"
	
	WriteRegStr HKCU "${REG_KEY_PATH}\GUI" "" ${OUT_PATH_GUI}
SectionEnd

LangString DESC_SecGUI ${LANG_ENGLISH} "Graphical user interface (GUI) for ProjectsSync. This program will start in the tray on every system boot."
LangString DESC_SecCLI ${LANG_ENGLISH} "Command line interface for ProjectsSync."
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${SecGUI} $(DESC_SecGUI)
	!insertmacro MUI_DESCRIPTION_TEXT ${SecCLI} $(DESC_SecCLI)
!insertmacro MUI_FUNCTION_DESCRIPTION_END