Unicode True
SetCompressor /SOLID lzma

!ifndef APP_VERSION
  !define APP_VERSION "1.0.0"
!endif
!ifndef APP_FILE_VERSION
  !define APP_FILE_VERSION "1.0.0.0"
!endif
!ifndef PUBLISH_DIR
  !error "PUBLISH_DIR must point to the self-contained win-x64 publish directory."
!endif
!ifndef OUTPUT_FILE
  !define OUTPUT_FILE "ChequeMate-Setup.exe"
!endif

!define APP_NAME "ChequeMate"
!define APP_PUBLISHER "ChequeMate"
!define APP_EXE "ChequeMate.App.exe"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\ChequeMate"

Name "${APP_NAME} ${APP_VERSION}"
OutFile "${OUTPUT_FILE}"
InstallDir "$PROGRAMFILES64\ChequeMate"
InstallDirRegKey HKLM "Software\ChequeMate" "InstallLocation"
RequestExecutionLevel admin
ManifestDPIAware true

VIProductVersion "${APP_FILE_VERSION}"
VIAddVersionKey /LANG=1033 "ProductName" "${APP_NAME}"
VIAddVersionKey /LANG=1033 "CompanyName" "${APP_PUBLISHER}"
VIAddVersionKey /LANG=1033 "FileDescription" "ChequeMate Windows installer"
VIAddVersionKey /LANG=1033 "FileVersion" "${APP_FILE_VERSION}"
VIAddVersionKey /LANG=1033 "ProductVersion" "${APP_VERSION}"

Page directory
Page instfiles
UninstPage uninstConfirm
UninstPage instfiles

Section "Install" SEC_MAIN
  SetShellVarContext all
  SetRegView 64
  SetOutPath "$INSTDIR"

  ; Replacing an existing installation in the same folder performs an in-place upgrade.
  File /r /x "*.pdb" "${PUBLISH_DIR}/*.*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\ChequeMate"
  CreateShortcut "$SMPROGRAMS\ChequeMate\ChequeMate.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
  CreateShortcut "$DESKTOP\ChequeMate.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0

  WriteRegStr HKLM "Software\ChequeMate" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayName" "${APP_NAME}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "Publisher" "${APP_PUBLISHER}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "UninstallString" '$"$INSTDIR\Uninstall.exe$"'
  WriteRegStr HKLM "${UNINSTALL_KEY}" "QuietUninstallString" '$"$INSTDIR\Uninstall.exe$" /S'
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoRepair" 1
SectionEnd

Section "Uninstall"
  SetShellVarContext all
  SetRegView 64

  Delete "$DESKTOP\ChequeMate.lnk"
  Delete "$SMPROGRAMS\ChequeMate\ChequeMate.lnk"
  RMDir "$SMPROGRAMS\ChequeMate"

  ; User templates, queue data, and backups live under LocalAppData and are preserved.
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "${UNINSTALL_KEY}"
  DeleteRegKey HKLM "Software\ChequeMate"
SectionEnd
