cd\broncode
upx freesyscal.exe
upx syscalEditor.exe
Cd\Program Files\Windows NT\Accessories\
wordpad.exe c:\broncode\readme.rtf
cd\broncode
cd\Program Files\Inno Setup 4\
Compil32.exe /cc "c:\broncode\syscalculcator.iss" 
cd\broncode\Output
setup.exe
c:\progra~1\winzip\wzzip -a c:\broncode\Output\sys172.zip setup.exe
ftp -i -s:c:\broncode\syscalftp.txt tcsoftware.com
	