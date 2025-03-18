cd %~dp0
if not exist Caesar.exe cd ..
start Caesar.exe -config .\Scripts\XML\_Example_AppConfig.xml /R -debug -offline -one

