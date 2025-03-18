cd %~dp0
if not exist Caesar.exe cd ..
start Caesar.exe -config .\Scripts\XML\_FATP_SuperCal_AppConfig.xml /R -Release-offline