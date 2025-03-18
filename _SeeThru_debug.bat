cd %~dp0
if not exist Caesar.exe cd ..
start Caesar.exe -config .\Scripts\XML\_FATP_SeeThru_AppConfig.xml /R -debug -offline -one