rem set current directory to support double click
cd /d %~dp0
rem run installutil
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil /i "WebEntrypoint.exe"
pause