# DEBUG
SetCompress off
!warning "Compression is off"

!include "MUI2.nsh"

!define INSTALLER_SOURCES "source\"
!define APP_NAME "Projects Sync"

!define INSTDIR_REG_ROOT "HKLM"
!define INSTDIR_REG_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
!include AdvUninstLog.nsh

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
Name "${APP_NAME}"
Unicode True

!define REG_KEY_PATH "Software\${APP_NAME}"
InstallDirRegKey HKCU "${REG_KEY_PATH}" ""

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!insertmacro INTERACTIVE_UNINSTALL
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

Function WaitForAppClosed APP_EXE
	
	${nsProcess::FindProcess} "${APP_EXE}" $R0

	${If} $R0 == 0
		DetailPrint "${APP_EXE} is running. Closing it down"
		${nsProcess::CloseProcess} "${APP_EXE}" $R0
		DetailPrint "Waiting for ${APP_EXE} to close"
		Sleep 2000  
	${Else}
		DetailPrint "${APP_EXE} was not found to be running"        
	${EndIf}    

	${nsProcess::Unload}

FunctionEnd

Function .onInit
	;prepare log always within .onInit function
	!insertmacro UNINSTALL.LOG_PREPARE_INSTALL
FunctionEnd

Function .onInstSuccess
	 ;create/update log always within .onInstSuccess function
	 !insertmacro UNINSTALL.LOG_UPDATE_INSTALL
FunctionEnd

Function UN.onInit
	 ;begin uninstall, could be added on top of uninstall section instead
	 !insertmacro UNINSTALL.LOG_BEGIN_UNINSTALL
FunctionEnd

Section "-Core"
	SetOutPath "$INSTDIR"
	
	CreateDirectory '$SMPROGRAMS\${APP_NAME}'
	CreateShortcut '$SMPROGRAMS\${APP_NAME}\uninstall.lnk' '${UNINST_EXE}'
	WriteRegStr ${INSTDIR_REG_ROOT} "${INSTDIR_REG_KEY}" "InstallDir" "$INSTDIR"
	WriteRegStr ${INSTDIR_REG_ROOT} "${INSTDIR_REG_KEY}" "DisplayName" "${APP_NAME}"
	WriteRegStr ${INSTDIR_REG_ROOT} "${INSTDIR_REG_KEY}" "UninstallString" "${UNINST_EXE}"
	
SectionEnd

!define LINK_PATH_CLI '$SMPROGRAMS\${APP_NAME}\prsync.lnk'
Section "Command-line utilities" SecCLI
	!define OUT_PATH_CLI "$INSTDIR\CLI"

	SetOutPath "${OUT_PATH_CLI}"
	
	!insertmacro UNINSTALL.LOG_OPEN_INSTALL
	File /r "${SRC_PATH_CLI}\*"
	!insertmacro UNINSTALL.LOG_CLOSE_INSTALL

	CreateShortcut '${LINK_PATH_CLI}' '${OUT_PATH_CLI}\prsync.exe'

	WriteRegStr HKCU "${REG_KEY_PATH}\CLI" "" ${OUT_PATH_CLI}
SectionEnd

!define LINK_PATH_GUI '$SMPROGRAMS\${APP_NAME}\Projects Sync.lnk'
Section "Graphical Interface" SecGUI
	!define OUT_PATH_GUI "$INSTDIR\GUI"
	
	SetOutPath "${OUT_PATH_GUI}"
	
	!insertmacro UNINSTALL.LOG_OPEN_INSTALL
	File /r "${SRC_PATH_GUI}\*"
	!insertmacro UNINSTALL.LOG_CLOSE_INSTALL
	
	CreateShortcut '${LINK_PATH_GUI}' '${OUT_PATH_GUI}\ProjectsSyncAv.Desktop.exe'
	
	WriteRegStr HKCU "${REG_KEY_PATH}\GUI" "" ${OUT_PATH_GUI}
SectionEnd

LangString DESC_SecGUI ${LANG_ENGLISH} "Graphical user interface (GUI) for ProjectsSync. This program will start in the tray on every system boot."
LangString DESC_SecCLI ${LANG_ENGLISH} "Command line interface for ProjectsSync."
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${SecGUI} $(DESC_SecGUI)
	!insertmacro MUI_DESCRIPTION_TEXT ${SecCLI} $(DESC_SecCLI)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section UnInstall


	!insertmacro UNINSTALL.LOG_UNINSTALL "$INSTDIR"
	!insertmacro UNINSTALL.LOG_UNINSTALL "$APPDATA\${APP_NAME}"
	!insertmacro UNINSTALL.LOG_END_UNINSTALL
	
	Delete "$SMPROGRAMS\${APP_NAME}\uninstall.lnk"
	Delete "${LINK_PATH_CLI}"
	Delete "${LINK_PATH_GUI}"
	RmDir "$SMPROGRAMS\${APP_NAME}"
	
	DeleteRegValue ${INSTDIR_REG_ROOT} "${INSTDIR_REG_KEY}" "InstallDir"
	DeleteRegValue ${INSTDIR_REG_ROOT} "${INSTDIR_REG_KEY}" "DisplayName"
	DeleteRegValue ${INSTDIR_REG_ROOT} "${INSTDIR_REG_KEY}" "UninstallString"
	DeleteRegKey /ifempty ${INSTDIR_REG_ROOT} "${INSTDIR_REG_KEY}"

SectionEnd