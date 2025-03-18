cd %~dp0
if not exist Caesar.exe cd ..
start Caesar.exe -config .\Scripts\XML\_FATP_Button_AppConfig.xml /R -release -offline

