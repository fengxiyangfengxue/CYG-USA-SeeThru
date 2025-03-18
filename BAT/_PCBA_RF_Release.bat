cd %~dp0
if not exist Caesar.exe cd ..
start Caesar.exe -config .\Scripts\XML\_PCBA_RF_AppConfig.xml /R -projects -sfis -checked

