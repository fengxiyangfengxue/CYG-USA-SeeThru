cd %~dp0
if not exist Caesar.exe cd ..
start Caesar.exe -config .\Scripts\XML\_FATP_Audio_AppConfig.xml /R -projects -sfis -checked

