@echo on

call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\vsvars32.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"
msbuild VSSonarQubeExtension2012.sln /p:VisualStudioVersion=12.0 /p:Vsix=Vs2012 /p:VsFolder=vs12 /p:SolutionFile=VSSonarQubeExtension2012.sln /p:Configuration=Release /v:diagnostic > buildlog2012.txt

