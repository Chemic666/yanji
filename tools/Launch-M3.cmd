@echo off
setlocal
cd /d "%~dp0"
start "YanJi M3 isolated preview" "%~dp0YanJi.exe" --data-dir "%~dp0m3-sandbox-data"
endlocal
