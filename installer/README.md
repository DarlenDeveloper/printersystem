# Windows installer

The installer is a self-contained 64-bit Setup executable for Windows 10/11. Recipients do not need to install .NET separately.

Build it on Windows PowerShell from the repository root:

```powershell
.\installer\build-installer.ps1 -Version 1.0.0
```

Output:

- `artifacts\installer\ChequeMate-1.0.0-win-x64-Setup.exe`
- `artifacts\installer\ChequeMate-1.0.0-win-x64-Setup.exe.sha256`

The setup installs for all users under Program Files and therefore requests administrator approval. It creates Start Menu and Desktop shortcuts and appears in Windows Installed Apps for upgrade or removal. NSIS is required only on the build machine.

The setup is unsigned until a code-signing certificate is supplied. Windows SmartScreen may warn recipients about an unsigned download; do not bypass this for public distribution. Sign both `ChequeMate.App.exe` before packaging and the final setup executable for a trusted release.
