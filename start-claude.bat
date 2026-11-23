@echo off
setlocal EnableExtensions

rem ==========================================================================
rem  Double-click launcher for Claude Code
rem  Opens a new PowerShell window in THIS file's folder and runs "claude".
rem  The window stays open after claude exits (-NoExit), so you can rerun it.
rem  Messages are English-only on purpose (avoids broken Korean in cmd).
rem ==========================================================================

rem Folder this .bat lives in. Passed via an environment variable so that
rem spaces, apostrophes or a drive root (D:\) in the path are handled safely.
set "CLAUDE_LAUNCH_DIR=%~dp0"

start "Claude Code" powershell -NoExit -Command "$Host.UI.RawUI.WindowTitle = 'Claude Code'; Set-Location -LiteralPath $env:CLAUDE_LAUNCH_DIR; $c = Get-Command claude -ErrorAction SilentlyContinue; if (-not $c) { $p = Join-Path $env:USERPROFILE '.local\bin\claude.exe'; if (Test-Path -LiteralPath $p) { $c = $p } }; if ($c) { & $c } else { Write-Host 'claude was not found. Run install-claude-code.bat first.' -ForegroundColor Red }"

endlocal
exit /b 0
