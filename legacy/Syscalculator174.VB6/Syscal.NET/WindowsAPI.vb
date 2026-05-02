Option Strict Off
Option Explicit On
Module WindowsAPI
	Declare Function GetCurrentProcess Lib "kernel32" () As Integer
	Declare Function TerminateProcess Lib "kernel32" (ByVal hProcess As Integer, ByVal uExitCode As Integer) As Integer
	Declare Function SHGetFolderPath Lib "shell32.dll"  Alias "SHGetFolderPathA"(ByVal hwndOwner As Integer, ByVal nFolder As Integer, ByVal hToken As Integer, ByVal dwFlags As Integer, ByVal lpszPath As String) As Integer
	Public Const CSIDL_APPDATA As Integer = &H1A
	Public Const SHGFP_TYPE_CURRENT As Short = 0
	Public Const SHGFP_TYPE_DEFAULT As Short = 1
	Public DataFolder As String
	
	Structure FINDREPLACE
		Dim lStructSize As Integer
		Dim hwndOwner As Integer
		Dim hInstance As Integer
		Dim flags As Integer
		Dim lpstrFindWhat As Integer
		'lpstrReplaceWith As Long
		Dim lpstrReplaceWith As Integer
		Dim wFindWhatLen As Short
		Dim wReplaceWithLen As Short
		Dim lCustData As Integer
		Dim lpfnHook As Integer
		Dim lpTemplateName As String
	End Structure
	'Const SW_HIDE = 0    'Hides the window. Activation passes to another window.
	'Const SW_MINIMIZE = 6     'Minimizes the window. Activation passes to another window.
	Public Const SW_RESTORE As Short = 9 'Displays a window at its original size and location and activates it.
	'Const SW_SHOW = 5   'Displays a window at its current size and location, and activates it.
	'Const SW_SHOWMAXIMIZED = 3      'Maximizes a window and activates it.
	'Const SW_SHOWMINIMIZED = 2      'Minimizes a window and activates it.
	'Const SW_SHOWMINNOACTIVE = 7    'Minimizes a window without changing the active window.
	'Const SW_SHOWNA = 8     'Displays a window at its current size and location. Does not change the active window.
	'Const SW_SHOWNOACTIVATE = 4     'Displays a window at its most recent size and location. Does not change the active window.
	'Const SW_SHOWNORMAL = 1     'Same as SW_RESTORE.
	
	Structure Msg
		Dim Hwnd As Integer
		Dim message As Integer
		Dim wParam As Integer
		Dim lParam As Integer
		Dim time As Integer
		Dim ptX As Integer
		Dim ptY As Integer
	End Structure
	Structure COPYDATASTRUCT
		Dim dwData As Integer
		Dim cbData As Integer
		Dim lpData As Integer
	End Structure
	Public Const GWL_WNDPROC As Short = (-4)
	Public Const WM_COPYDATA As Integer = &H4A
	'UPGRADE_ISSUE: Declaring a parameter 'As Any' is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="FAE78A8D-8978-4FD4-8208-5B7324A8F795"'
	'UPGRADE_ISSUE: Declaring a parameter 'As Any' is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="FAE78A8D-8978-4FD4-8208-5B7324A8F795"'
	Public Declare Sub CopyMemory Lib "kernel32"  Alias "RtlMoveMemory"(ByRef hpvDest As Any, ByRef hpvSource As Any, ByVal cbCopy As Integer)
	
	Private Declare Function FindText Lib "comdlg32.dll"  Alias "FindTextA"(ByRef pFindreplace As Integer) As Integer
	Private Declare Function ReplaceText Lib "comdlg32.dll"  Alias "ReplaceTextA"(ByRef pFindreplace As Integer) As Integer
	Private Declare Function RegisterWindowMessage Lib "user32"  Alias "RegisterWindowMessageA"(ByVal lpString As String) As Integer
	'UPGRADE_WARNING: Structure Msg may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="C429C3A5-5D47-4CD9-8F51-74A1616405DC"'
	Private Declare Function DispatchMessage Lib "user32"  Alias "DispatchMessageA"(ByRef lpMsg As Msg) As Integer
	'UPGRADE_WARNING: Structure Msg may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="C429C3A5-5D47-4CD9-8F51-74A1616405DC"'
	Private Declare Function GetMessage Lib "user32"  Alias "GetMessageA"(ByRef lpMsg As Msg, ByVal Hwnd As Integer, ByVal wMsgFilterMin As Integer, ByVal wMsgFilterMax As Integer) As Integer
	'UPGRADE_WARNING: Structure Msg may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="C429C3A5-5D47-4CD9-8F51-74A1616405DC"'
	Private Declare Function TranslateMessage Lib "user32" (ByRef lpMsg As Msg) As Integer
	'UPGRADE_WARNING: Structure Msg may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="C429C3A5-5D47-4CD9-8F51-74A1616405DC"'
	Private Declare Function IsDialogMessage Lib "user32"  Alias "IsDialogMessageA"(ByVal hDlg As Integer, ByRef lpMsg As Msg) As Integer
	Private Declare Function CopyPointer2String Lib "kernel32"  Alias "lstrcpyA"(ByVal NewString As String, ByVal OldString As Integer) As Integer
	Private Declare Function SetWindowLong Lib "user32"  Alias "SetWindowLongA"(ByVal Hwnd As Integer, ByVal nIndex As Integer, ByVal dwNewLong As Integer) As Integer
	Private Declare Function GetWindowLong Lib "user32"  Alias "GetWindowLongA"(ByVal Hwnd As Integer, ByVal nIndex As Integer) As Integer
	Private Declare Function CallWindowProc Lib "user32"  Alias "CallWindowProcA"(ByVal lpPrevWndFunc As Integer, ByVal Hwnd As Integer, ByVal Msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
	Private Declare Function GetProcessHeap Lib "kernel32" () As Integer
	Private Declare Function HeapAlloc Lib "kernel32" (ByVal hHeap As Integer, ByVal dwFlags As Integer, ByVal dwBytes As Integer) As Integer
	'UPGRADE_ISSUE: Declaring a parameter 'As Any' is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="FAE78A8D-8978-4FD4-8208-5B7324A8F795"'
	Private Declare Function HeapFree Lib "kernel32" (ByVal hHeap As Integer, ByVal dwFlags As Integer, ByRef lpMem As Any) As Integer
	Private Declare Function EndDialog Lib "user32" (ByVal hDlg As Integer, ByVal nResult As Integer) As Integer
	Public Declare Function ShowWindowAsync Lib "user32" (ByVal Hwnd As Integer, ByVal nCmdShow As Integer) As Integer
	Public Declare Function SetForegroundWindow Lib "user32" (ByVal Hwnd As Integer) As Integer
	Private Declare Function URLDownloadToFile Lib "urlmon"  Alias "URLDownloadToFileA"(ByVal pCaller As Integer, ByVal szURL As String, ByVal szFileName As String, ByVal dwReserved As Integer, ByVal lpfnCB As Integer) As Integer
	
	'Private Const GWL_WNDPROC = (-4)
	Private Const HEAP_ZERO_MEMORY As Integer = &H8
	Public Const FR_DIALOGTERM As Integer = &H40
	Public Const FR_DOWN As Integer = &H1
	Public Const FR_ENABLEHOOK As Integer = &H100
	Public Const FR_ENABLETEMPLATE As Integer = &H200
	Public Const FR_ENABLETEMPLATEHANDLE As Integer = &H2000
	Public Const FR_FINDNEXT As Integer = &H8
	Public Const FR_HIDEMATCHCASE As Integer = &H8000
	Public Const FR_HIDEUPDOWN As Integer = &H4000
	Public Const FR_HIDEWHOLEWORD As Integer = &H10000
	Public Const FR_MATCHCASE As Integer = &H4
	Public Const FR_NOMATCHCASE As Integer = &H800
	Public Const FR_NOUPDOWN As Integer = &H400
	Public Const FR_NOWHOLEWORD As Integer = &H1000
	Public Const FR_REPLACE As Integer = &H10
	Public Const FR_REPLACEALL As Integer = &H20
	Public Const FR_SHOWHELP As Integer = &H80
	Public Const FR_WHOLEWORD As Integer = &H2
	Const WM_DESTROY As Integer = &H2
	Public Kcommand As String
	
	Const FINDMSGSTRING As String = "commdlg_FindReplace"
	Const HELPMSGSTRING As String = "commdlg_help"
	Const BufLength As Short = 256
	
	Public hDialog, OldProc As Integer
	Dim uHelpMsg, uFindMsg, lHeap As Integer
	Public RetFrs As FINDREPLACE
	Public TMsg As Msg
	Dim arrFind() As Byte
	Dim arrReplace() As Byte
	Dim objTarget As Object
	
	Public gHW, lpPrevWndProc, OtherInstanceHwnd As Integer
	Public Hooked As Boolean
	
	Public Declare Function SendMessageLong Lib "user32"  Alias "SendMessageA"(ByVal Hwnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
	'Public Declare Function FindWindowEx Lib "user32" Alias "FindWindowExA" (ByVal hWnd1 As Long, ByVal hWnd2 As Long, ByVal lpsz1 As String, ByVal lpsz2 As String) As Long
	
	
	'UPGRADE_WARNING: Structure NOTIFYICONDATA may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="C429C3A5-5D47-4CD9-8F51-74A1616405DC"'
	Public Declare Function Shell_NotifyIcon Lib "shell32.dll"  Alias "Shell_NotifyIconA"(ByVal dwMessage As Integer, ByRef lpData As NOTIFYICONDATA) As Integer
	Public Declare Function ShowWindow Lib "user32" (ByVal Hwnd As Integer, ByVal nCmdShow As Integer) As Integer
	Public Declare Function EnableWindow Lib "user32" (ByVal Hwnd As Integer, ByVal cmd As Integer) As Integer
	Public Declare Function FindWindow Lib "user32"  Alias "FindWindowA"(ByVal lpClassName As String, ByVal lpWindowName As String) As Integer
	Public Declare Function GetWindow Lib "user32" (ByVal Hwnd As Integer, ByVal wCmd As Integer) As Integer
	Public Declare Function GetClassName Lib "user32"  Alias "GetClassNameA"(ByVal Hwnd As Integer, ByVal lpClassName As String, ByVal nMaxCount As Integer) As Integer
	Public Structure NOTIFYICONDATA
		Dim cbSize As Integer
		Dim Hwnd As Integer
		Dim uID As Integer
		Dim uFlags As Integer
		Dim uCallbackMessage As Integer
		Dim hIcon As Integer
		'UPGRADE_WARNING: Fixed-length string size must fit in the buffer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="3C1E4426-0B80-443E-B943-0627CD55D48B"'
		<VBFixedString(64),System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray,SizeConst:=64)> Public sTip() As Char
	End Structure
	Public Const NIM_ADD As Integer = &H0
	Public Const NIM_MODIFY As Integer = &H1
	Public Const NIM_DELETE As Integer = &H2
	Public Const NIF_MESSAGE As Integer = &H1
	Public Const NIF_ICON As Integer = &H2
	Public Const NIF_TIP As Integer = &H4
	Public Const NIF_DOALL As Boolean = NIF_MESSAGE Or NIF_ICON Or NIF_TIP
	'Public Const SW_RESTORE = 9
	Public Const SW_MINIMIZE As Short = 6
	Public Const WM_MOUSEMOVE As Integer = &H200
	Public Const WM_LBUTTONDBLCLK As Integer = &H203
	Public Const WM_RBUTTONUP As Integer = &H205
	
	'UPGRADE_WARNING: Structure OPENFILENAME may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="C429C3A5-5D47-4CD9-8F51-74A1616405DC"'
	Private Declare Function GetSaveFileName Lib "comdlg32.dll"  Alias "GetSaveFileNameA"(ByRef pOpenfilename As OPENFILENAME) As Integer
	
	
	'UPGRADE_WARNING: Structure OPENFILENAME may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="C429C3A5-5D47-4CD9-8F51-74A1616405DC"'
	Private Declare Function GetOpenFileName Lib "comdlg32.dll"  Alias "GetOpenFileNameA"(ByRef pOpenfilename As OPENFILENAME) As Integer
	Private strfileName As OPENFILENAME
	
	
	Private Structure OPENFILENAME
		Dim lStructSize As Integer
		Dim hwndOwner As Integer
		Dim hInstance As Integer
		Dim lpstrFilter As String
		Dim lpstrCustomFilter As String
		Dim nMaxCustFilter As Integer
		Dim nFilterIndex As Integer
		Dim lpstrFile As String
		Dim nMaxFile As Integer
		Dim lpstrFileTitle As String
		Dim nMaxFileTitle As Integer
		Dim lpstrInitialDir As String
		Dim lpstrTitle As String
		Dim flags As Integer
		Dim nFileOffset As Short
		Dim nFileExtension As Short
		Dim lpstrDefExt As String
		Dim lCustData As Integer
		Dim lpfnHook As Integer
		Dim lpTemplateName As String
	End Structure
	
	
	
	Public Const WM_USER As Integer = &H400
	Public Const TB_SETSTYLE As Decimal = WM_USER + 56
	Public Const TB_GETSTYLE As Decimal = WM_USER + 57
	Public Const TBSTYLE_FLAT As Integer = &H800
	
	'UPGRADE_ISSUE: Declaring a parameter 'As Any' is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="FAE78A8D-8978-4FD4-8208-5B7324A8F795"'
	Public Declare Function SendMessage Lib "user32"  Alias "SendMessageA"(ByVal Hwnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByRef lParam As Any) As Integer
	
	Public Declare Function FindWindowEx Lib "user32"  Alias "FindWindowExA"(ByVal hWnd1 As Integer, ByVal hWnd2 As Integer, ByVal lpsz1 As String, ByVal lpsz2 As String) As Integer
	Declare Function RegEnumValue Lib "advapi32.dll"  Alias "RegEnumValueA"(ByVal hKey As Integer, ByVal dwIndex As Integer, ByVal lpValueName As String, ByRef lpcbValueName As Integer, ByVal lpReserved As Integer, ByRef lpType As Integer, ByVal lpData As String, ByRef lpcbData As Integer) As Integer
	
	
	Declare Function RegOpenKeyEx Lib "advapi32"  Alias "RegOpenKeyExA"(ByVal hKey As Integer, ByVal lpSubKey As String, ByVal ulOptions As Integer, ByVal samDesired As Integer, ByRef phkResult As Integer) As Integer
	
	
	Declare Function RegSetValueEx Lib "advapi32"  Alias "RegSetValueExA"(ByVal hKey As Integer, ByVal lpValueName As String, ByVal Reserved As Integer, ByVal dwType As Integer, ByVal szData As String, ByVal cbData As Integer) As Integer
	Declare Function RegDeleteValue Lib "advapi32"  Alias "RegDeleteValueA"(ByVal hKey As Integer, ByVal lpValueName As String) As Integer
	
	
	Declare Function RegCloseKey Lib "advapi32" (ByVal hKey As Integer) As Integer
	
	
	'UPGRADE_WARNING: Structure SECURITY_ATTRIBUTES may require marshalling attributes to be passed as an argument in this Declare statement. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="C429C3A5-5D47-4CD9-8F51-74A1616405DC"'
	Declare Function RegCreateKeyEx Lib "advapi32"  Alias "RegCreateKeyExA"(ByVal hKey As Integer, ByVal lpSubKey As String, ByVal Reserved As Integer, ByVal lpClass As String, ByVal dwOptions As Integer, ByVal samDesired As Integer, ByRef lpSecurityAttributes As SECURITY_ATTRIBUTES, ByRef phkResult As Integer, ByRef lpdwDisposition As Integer) As Integer
	
	
#If Win32 Then
	
	Public Const HKEY_CLASSES_ROOT As Integer = &H80000000
	Public Const HKEY_CURRENT_USER As Integer = &H80000001
	Public Const HKEY_LOCAL_MACHINE As Integer = &H80000002
	Public Const HKEY_USERS As Integer = &H80000003
	Public Const KEY_ALL_ACCESS As Integer = &H3F
	Public Const REG_OPTION_NON_VOLATILE As Short = 0
	Public Const REG_CREATED_NEW_KEY As Integer = &H1
	Public Const REG_OPENED_EXISTING_KEY As Integer = &H2
	Public Const ERROR_SUCCESS As Short = 0
	Public Const REG_SZ As Short = (1)
#End If
	
	
	Structure SECURITY_ATTRIBUTES
		Dim nLength As Integer
		Dim lpSecurityDescriptor As Integer
		Dim bInheritHandle As Boolean
	End Structure
	
	
	Declare Function SetWindowPos Lib "user32" (ByVal Hwnd As Integer, ByVal hWndInsertAfter As Integer, ByVal X As Integer, ByVal Y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal wFlags As Integer) As Integer
	Declare Function ShellExecute Lib "shell32.dll"  Alias "ShellExecuteA"(ByVal Hwnd As Integer, ByVal lpOperation As String, ByVal lpFile As String, ByVal lpParameters As String, ByVal lpDirectory As String, ByVal nShowCmd As Integer) As Integer
	
	Public Const HWND_TOPMOST As Short = -1
	Public Const HWND_NOTOPMOST As Short = -2
	Private Const LOCALE_SDECIMAL As Short = 22
	Private Const LOCALE_STHOUSAND As Short = 23
	Private Const WM_SETTINGCHANGE As Integer = &H1A
	
	Private Const HWND_BROADCAST As Integer = &HFFFF
	
	Private Declare Function SetLocaleInfo Lib "kernel32"  Alias "SetLocaleInfoA"(ByVal Locale As Integer, ByVal LCType As Integer, ByVal lpLCData As String) As Boolean
	Private Declare Function PostMessage Lib "user32"  Alias "PostMessageA"(ByVal Hwnd As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
	Private Declare Function GetSystemDefaultLCID Lib "kernel32" () As Integer
	Private Declare Function GetLocaleInfo Lib "kernel32"  Alias "GetLocaleInfoA"(ByVal Locale As Integer, ByVal LCType As Integer, ByVal lpLCData As String, ByVal cchData As Integer) As Integer
	
	Public Function setting(ByRef digit As String, ByRef milionen As String) As Object
		
		
		Dim SeparadorDecimal As String
		Dim SeparadorMiles As String
		
		Dim Symbol As String
		Dim iRet1 As Integer
		Dim iRet2 As Integer
		Dim lpLCDataVar As String
		Dim Pos As Short
		Dim Locale As Integer
		
		Locale = 1024
		
		iRet1 = GetLocaleInfo(Locale, LOCALE_SDECIMAL, lpLCDataVar, 0)
		Symbol = New String(Chr(0), iRet1)
		iRet2 = GetLocaleInfo(Locale, LOCALE_SDECIMAL, Symbol, iRet1)
		Pos = InStr(Symbol, Chr(0))
		If Pos > 0 Then
			Symbol = Left(Symbol, Pos - 1)
			SeparadorDecimal = Symbol
		End If
		
		iRet1 = GetLocaleInfo(Locale, LOCALE_STHOUSAND, lpLCDataVar, 0)
		Symbol = New String(Chr(0), iRet1)
		iRet2 = GetLocaleInfo(Locale, LOCALE_STHOUSAND, Symbol, iRet1)
		Pos = InStr(Symbol, Chr(0))
		If Pos > 0 Then
			Symbol = Left(Symbol, Pos - 1)
			SeparadorMiles = Symbol
		End If
		
		digit = SeparadorDecimal
		milionen = SeparadorMiles
	End Function
	Sub AlwaysOnTop(ByRef myfrm As System.Windows.Forms.Form, ByRef SetOnTop As Boolean)
		Dim SWP_SHOWWINDOW As Object
		Dim SWP_NOACTIVATE As Object
		Dim lFlag As Object
		
		
		If SetOnTop Then
			'UPGRADE_WARNING: Couldn't resolve default property of object lFlag. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			lFlag = HWND_TOPMOST
		Else
			'UPGRADE_WARNING: Couldn't resolve default property of object lFlag. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			lFlag = HWND_NOTOPMOST
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object lFlag. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		SetWindowPos(myfrm.Handle.ToInt32, lFlag, VB6.PixelsToTwipsX(myfrm.Left) / VB6.TwipsPerPixelX, VB6.PixelsToTwipsY(myfrm.Top) / VB6.TwipsPerPixelY, VB6.PixelsToTwipsX(myfrm.Width) / VB6.TwipsPerPixelX, VB6.PixelsToTwipsY(myfrm.Height) / VB6.TwipsPerPixelY, SWP_NOACTIVATE Or SWP_SHOWWINDOW)
	End Sub
	
	Public Function bSetRegValue(ByVal hKey As Integer, ByVal lpszSubKey As String, ByVal sSetValue As String, ByVal sValue As String) As Boolean
		
		On Error Resume Next
		Dim phkResult As Integer
		Dim lResult As Integer
		Dim SA As SECURITY_ATTRIBUTES
		Dim lCreate As Integer
		RegCreateKeyEx(hKey, lpszSubKey, 0, "", REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, SA, phkResult, lCreate)
		lResult = RegSetValueEx(phkResult, sSetValue, 0, REG_SZ, sValue, CInt(Len(sValue) + 1))
		RegCloseKey(phkResult)
		bSetRegValue = (lResult = ERROR_SUCCESS)
		
	End Function
	Public Function bDeleteRegValue(ByVal hKey As Integer, ByVal lpszSubKey As String, ByVal sValue As String) As Boolean
		
		On Error Resume Next
		Dim phkResult As Integer
		Dim lResult As Integer
		Dim SA As SECURITY_ATTRIBUTES
		Dim lCreate As Integer
		RegCreateKeyEx(hKey, lpszSubKey, 0, "", REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, SA, phkResult, lCreate)
		lResult = RegDeleteValue(phkResult, sValue)
		RegCloseKey(phkResult)
		bDeleteRegValue = (lResult = ERROR_SUCCESS)
		
	End Function
	
	Public Sub MakeDirectory(ByRef PhysicalPath As String)
		'---------------------------------------
		'     ---------------------
		' Procedure Name: MakeDirectory
		' Author: Erik Bartlow
		' Purpose: Extends the ability of the st
		'     andard MKDir function
		'by parsing through an entire path throu
		'     gh to
		'the : character.
		' Date: July,30 2002 @ 18:53:46
		'---------------------------------------
		'     ---------------------
		Dim arrFolderArray() As String
		Dim strCurrentPath As String
		Dim X As Short
		On Error GoTo ErrorMakeDirectory
		arrFolderArray = Split(PhysicalPath, "\",  , CompareMethod.Text)
		
		
		For X = 0 To UBound(arrFolderArray)
			
			
			If InStr(arrFolderArray(X), ":") = 0 Then
				strCurrentPath = strCurrentPath & "\" & arrFolderArray(X)
				MkDir(strCurrentPath)
			Else
				' Add the Drive letter
				strCurrentPath = arrFolderArray(X)
			End If
		Next 
		Exit Sub
ErrorMakeDirectory: 
		Resume Next
	End Sub
	
	
	Public Function bGetRegValue(ByVal hKey As Integer, ByVal sKey As String, ByVal sSubKey As String) As String
		Dim bFound As Object
		
		Dim lResult As Integer
		Dim phkResult As Integer
		Dim dwReserved As Integer
		Dim szBuffer As String
		Dim lBuffSize As Integer
		Dim szBuffer2 As String
		Dim lBuffSize2 As Integer
		Dim lIndex As Integer
		Dim lType As Integer
		Dim sCompKey As String
		
		lIndex = 0
		lResult = RegOpenKeyEx(hKey, sKey, 0, 1, phkResult)
		
		
		Do While lResult = ERROR_SUCCESS And Not (bFound)
			szBuffer = Space(255)
			lBuffSize = Len(szBuffer)
			szBuffer2 = Space(255)
			lBuffSize2 = Len(szBuffer2)
			lResult = RegEnumValue(phkResult, lIndex, szBuffer, lBuffSize, dwReserved, lType, szBuffer2, lBuffSize2)
			
			
			If (lResult = ERROR_SUCCESS) Then
				sCompKey = Left(szBuffer, lBuffSize)
				
				
				If (sCompKey = sSubKey) Then
					bGetRegValue = Left(szBuffer2, lBuffSize2 - 1)
				End If
			End If
			lIndex = lIndex + 1
			
		Loop 
		RegCloseKey(phkResult)
	End Function
	
	Private Sub DialogFilter(ByRef WantedFilter As String)
		Dim intLoopCount As Short
		strfileName.lpstrFilter = ""
		
		
		For intLoopCount = 1 To Len(WantedFilter)
			If Mid(WantedFilter, intLoopCount, 1) = "|" Then strfileName.lpstrFilter = strfileName.lpstrFilter & Chr(0) Else strfileName.lpstrFilter = strfileName.lpstrFilter & Mid(WantedFilter, intLoopCount, 1)
		Next intLoopCount
		strfileName.lpstrFilter = strfileName.lpstrFilter & Chr(0)
	End Sub
	'This is The Function To get the File Na
	'     me to Open
	'Even If U don't specify a Title or a Fi
	'     lter it is OK
	
	
	Public Function fncGetFileNametoOpen(Optional ByRef strDialogTitle As String = "Open", Optional ByRef strFilter As String = "All Files|*.*", Optional ByRef strDefaultExtention As String = "*.*") As String
		Dim lngReturnValue As Integer
		Dim intRest As Short
		strfileName.lpstrTitle = strDialogTitle
		strfileName.lpstrDefExt = strDefaultExtention
		DialogFilter((strFilter))
		strfileName.hInstance = VB6.GetHInstance.ToInt32
		strfileName.lpstrFile = Chr(0) & Space(259)
		strfileName.nMaxFile = 260
		strfileName.flags = &H4
		strfileName.lStructSize = Len(strfileName)
		lngReturnValue = GetOpenFileName(strfileName)
		fncGetFileNametoOpen = strfileName.lpstrFile
	End Function
	'This Function Returns the Save File Nam
	'     e
	'Remember, U have to Specify a Filter an
	'     d default Extention for this
	Public Function fncGetFileNametoSave(ByRef strFilter As String, ByRef strDefaultExtention As String, Optional ByRef strDialogTitle As String = "Save") As String
		Dim lngReturnValue As Integer
		Dim intRest As Short
		strfileName.lpstrTitle = strDialogTitle
		strfileName.lpstrDefExt = strDefaultExtention
		DialogFilter((strFilter))
		strfileName.hInstance = VB6.GetHInstance.ToInt32
		strfileName.lpstrFile = Chr(0) & Space(259)
		strfileName.nMaxFile = 260
		strfileName.flags = &H80000 Or &H4
		strfileName.lStructSize = Len(strfileName)
		lngReturnValue = GetSaveFileName(strfileName)
		fncGetFileNametoSave = strfileName.lpstrFile
	End Function
	
	
	Public Sub ButtonRelease(ByRef ctrl As System.Windows.Forms.Control, ByRef Container As Object)
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Cls. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Cls()
		ctrl.SetBounds(VB6.TwipsToPixelsX(iX), VB6.TwipsToPixelsY(iY), VB6.TwipsToPixelsX(400), VB6.TwipsToPixelsY(400))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) - 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) - 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlLightLight))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) - 25) - (VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Height) + VB6.PixelsToTwipsY(ctrl.Top) + 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlLightLight))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlDark))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) - 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlDark))
	End Sub
	Public Sub ButtonPress(ByRef ctrl As System.Windows.Forms.Control, ByRef Container As Object)
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Cls. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Cls()
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) - 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) - 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlDark))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) - 25) - (VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Height) + VB6.PixelsToTwipsY(ctrl.Top) + 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlDark))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlLightLight))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) - 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlLightLight))
		iX = VB6.PixelsToTwipsX(ctrl.Left)
		iY = VB6.PixelsToTwipsY(ctrl.Top)
		ctrl.SetBounds(VB6.TwipsToPixelsX(VB6.PixelsToTwipsX(ctrl.Left) + 30), VB6.TwipsToPixelsY(VB6.PixelsToTwipsY(ctrl.Top) + 30), VB6.TwipsToPixelsX(VB6.PixelsToTwipsX(ctrl.Width) - 30), VB6.TwipsToPixelsY(VB6.PixelsToTwipsY(ctrl.Height) - 30))
	End Sub
	Public Sub HighlightBorder(ByRef ctrl As System.Windows.Forms.Control, ByRef Container As Object)
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) - 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) - 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlLightLight))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) - 25) - (VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Height) + VB6.PixelsToTwipsY(ctrl.Top) + 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlLightLight))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) - 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlDark))
		'UPGRADE_WARNING: Couldn't resolve default property of object Container.Line. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Container.Line((VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) + VB6.PixelsToTwipsY(ctrl.Height) + 25) - (VB6.PixelsToTwipsX(ctrl.Left) + VB6.PixelsToTwipsX(ctrl.Width) + 25, VB6.PixelsToTwipsY(ctrl.Top) - 25), System.Drawing.ColorTranslator.ToOle(System.Drawing.SystemColors.ControlDark))
	End Sub
	
	Public Sub ShowFind(ByRef fOwner As System.Windows.Forms.Form, ByRef objWhere As Object, ByRef lFlags As Integer, ByRef sFind As String, Optional ByRef bReplace As Boolean = False, Optional ByRef sReplace As String = "")
		If hDialog > 0 Then Exit Sub
		objTarget = objWhere
		Dim FRS As FINDREPLACE
		Dim i As Short
		'UPGRADE_ISSUE: Constant vbFromUnicode was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="55B59875-9A95-4B71-9D6A-7C294BF7139D"'
		'UPGRADE_TODO: Code was upgraded to use System.Text.UnicodeEncoding.Unicode.GetBytes() which may not have the same behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="93DD716C-10E3-41BE-A4A8-3BA40157905B"'
		arrFind = System.Text.UnicodeEncoding.Unicode.GetBytes(StrConv(sFind & Chr(0), vbFromUnicode))
		'UPGRADE_ISSUE: Constant vbFromUnicode was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="55B59875-9A95-4B71-9D6A-7C294BF7139D"'
		'UPGRADE_TODO: Code was upgraded to use System.Text.UnicodeEncoding.Unicode.GetBytes() which may not have the same behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="93DD716C-10E3-41BE-A4A8-3BA40157905B"'
		arrReplace = System.Text.UnicodeEncoding.Unicode.GetBytes(StrConv(sReplace & Chr(0), vbFromUnicode))
		With FRS
			'UPGRADE_ISSUE: LenB function is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="367764E5-F3F8-4E43-AC3E-7FE0B5E074E2"'
			.lStructSize = LenB(FRS) '&H20     '
			'UPGRADE_ISSUE: VarPtr function is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="367764E5-F3F8-4E43-AC3E-7FE0B5E074E2"'
			.lpstrFindWhat = VarPtr(arrFind(0))
			.wFindWhatLen = BufLength
			'UPGRADE_ISSUE: VarPtr function is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="367764E5-F3F8-4E43-AC3E-7FE0B5E074E2"'
			.lpstrReplaceWith = VarPtr(arrReplace(0))
			.wReplaceWithLen = BufLength
			.hwndOwner = fOwner.Handle.ToInt32
			.flags = lFlags
			.hInstance = VB6.GetHInstance.ToInt32
		End With
		lHeap = HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, FRS.lStructSize)
		'UPGRADE_WARNING: Couldn't resolve default property of object FRS. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		CopyMemory(lHeap, FRS, Len(FRS))
		uFindMsg = RegisterWindowMessage(FINDMSGSTRING)
		uHelpMsg = RegisterWindowMessage(HELPMSGSTRING)
		'UPGRADE_WARNING: Add a delegate for AddressOf WndProc Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="E9E157F7-EF0C-4016-87B7-7D7FBBC6EE08"'
		OldProc = SetWindowLong(fOwner.Handle.ToInt32, GWL_WNDPROC, AddressOf WndProc)
		If bReplace Then
			hDialog = ReplaceText(lHeap)
		Else
			hDialog = FindText(lHeap)
		End If
		MessageLoop()
	End Sub
	
	Private Sub MessageLoop()
		Do While GetMessage(TMsg, 0, 0, 0) And hDialog > 0
			If IsDialogMessage(hDialog, TMsg) = False Then
				TranslateMessage(TMsg)
				DispatchMessage(TMsg)
			End If
		Loop 
	End Sub
	
	Public Function WndProc(ByVal hOwner As Integer, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
		Dim SW_SHOWNORMAL As Object
		Dim iRet As Integer
		Select Case wMsg
			Case uFindMsg
				'UPGRADE_WARNING: Couldn't resolve default property of object RetFrs. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				CopyMemory(RetFrs, lParam, Len(RetFrs))
				If (RetFrs.flags And FR_DIALOGTERM) = FR_DIALOGTERM Then
					SetWindowLong(hOwner, GWL_WNDPROC, OldProc)
					HeapFree(GetProcessHeap(), 0, lHeap)
					hDialog = 0 : lHeap = 0 : OldProc = 0
					'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.HideSelection. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SetFocus. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If objTarget.HideSelection Then objTarget.SetFocus()
					'UPGRADE_NOTE: Object objTarget may not be destroyed until it is garbage collected. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6E35BFF6-CD74-4B09-9689-3E1A43DF8969"'
					objTarget = Nothing
				Else
					DoFindReplace(RetFrs)
				End If
			Case uHelpMsg
				'UPGRADE_WARNING: Couldn't resolve default property of object SW_SHOWNORMAL. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				iRet = ShellExecute(Editor.Handle.ToInt32, vbNullString, Configur3, vbNullString, "c:\", SW_SHOWNORMAL)
			Case Else
				If wMsg = WM_DESTROY Then
					EndDialog(hDialog, 0)
					SetWindowLong(hOwner, GWL_WNDPROC, OldProc)
					HeapFree(GetProcessHeap(), 0, lHeap)
					hDialog = 0 : lHeap = 0 : OldProc = 0
					'UPGRADE_NOTE: Object objTarget may not be destroyed until it is garbage collected. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6E35BFF6-CD74-4B09-9689-3E1A43DF8969"'
					objTarget = Nothing
					Exit Function
				End If
				WndProc = CallWindowProc(OldProc, hOwner, wMsg, wParam, lParam)
		End Select
	End Function
	
	Private Sub DoFindReplace(ByRef fr As FINDREPLACE)
		If CheckFlags(FR_FINDNEXT, fr.flags) Then
			If CheckFlags(FR_DOWN, fr.flags) Then
				FindNextWord(PointerToString(fr.lpstrFindWhat), fr.flags)
			Else
				FindPrevWord(PointerToString(fr.lpstrFindWhat), fr.flags)
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.HideSelection. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SetFocus. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If objTarget.HideSelection Then objTarget.SetFocus()
		End If
		If CheckFlags(FR_REPLACE, fr.flags) Then ReplaceWord(PointerToString(fr.lpstrFindWhat), PointerToString(fr.lpstrReplaceWith), fr.flags)
		If CheckFlags(FR_REPLACEALL, fr.flags) Then ReplaceAll(PointerToString(fr.lpstrFindWhat), PointerToString(fr.lpstrReplaceWith), fr.flags)
	End Sub
	
	Private Function PointerToString(ByRef p As Integer) As String
		Dim S As String
		S = New String(Chr(0), BufLength)
		CopyPointer2String(S, p)
		PointerToString = Left(S, InStr(S, Chr(0)) - 1)
	End Function
	
	Private Function CheckFlags(ByRef flag As Integer, ByRef flags As Integer) As Boolean
		CheckFlags = ((flags And flag) = flag)
	End Function
	
	Function FindNextWord(ByRef sFind As String, ByRef lFlags As Integer, Optional ByRef bShowMsg As Boolean = True) As Boolean
		Dim lStart As Integer
		Dim pl, nl As String
		With objTarget
			'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelStart. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			lStart = .SelStart + 1
			'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelLength. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If .SelLength > 0 Then lStart = lStart + 1
			Do 
				'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				lStart = InStr(lStart, .Text, sFind, IIf(CheckFlags(FR_MATCHCASE, lFlags), CompareMethod.Binary, CompareMethod.Text))
				If lStart = 0 Then Exit Do
				If CheckFlags(FR_WHOLEWORD, lFlags) Then
					If lStart = 1 Then
						pl = " "
					Else
						'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						pl = Mid(.Text, lStart - 1, 1)
					End If
					'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If lStart + Len(sFind) = Len(.Text) Then
						nl = " "
					Else
						'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						nl = Mid(.Text, lStart + Len(sFind), 1)
					End If
					If ValidateWholeWord(pl, nl) Then Exit Do Else lStart = lStart + 1
				Else
					Exit Do
				End If
			Loop 
			If lStart > 0 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelStart. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				.SelStart = lStart - 1
				'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelLength. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				.SelLength = Len(sFind)
				FindNextWord = True
			Else
				FindNextWord = False
				If bShowMsg Then MsgBox(nomatches, MsgBoxStyle.Exclamation, findrepl)
			End If
		End With
	End Function
	
	Function FindPrevWord(ByRef sFind As String, ByRef lFlags As Integer) As Boolean
		Dim lStart As Integer
		Dim pl, nl As String
		With objTarget
			'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelStart. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			lStart = .SelStart - 1
			If lStart < 0 Then lStart = 0
			Do 
				'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				lStart = InStrR(lStart, .Text, sFind, IIf(CheckFlags(FR_MATCHCASE, lFlags), CompareMethod.Binary, CompareMethod.Text))
				If lStart <= 0 Then Exit Do
				If CheckFlags(FR_WHOLEWORD, lFlags) Then
					If lStart = 1 Then
						pl = " "
					Else
						'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						pl = Mid(.Text, lStart - 1, 1)
					End If
					'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If lStart + Len(sFind) = Len(.Text) Then
						nl = " "
					Else
						'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						nl = Mid(.Text, lStart + Len(sFind), 1)
					End If
					If ValidateWholeWord(pl, nl) Then Exit Do Else lStart = lStart - 1
				Else
					Exit Do
				End If
			Loop 
			If lStart > 0 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelStart. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				.SelStart = lStart - 1
				'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelLength. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				.SelLength = Len(sFind)
				FindPrevWord = True
			Else
				FindPrevWord = False
				MsgBox(nomatches, MsgBoxStyle.Exclamation, findrepl)
			End If
		End With
	End Function
	
	Function ReplaceWord(ByRef sFind As String, ByRef sReplace As String, ByRef lFlags As Integer) As Object
		With objTarget
			'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelText. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If .SelText <> sFind Then
				FindNextWord(sFind, lFlags)
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelText. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				.SelText = sReplace
				FindNextWord(sFind, lFlags)
			End If
		End With
	End Function
	
	Function ReplaceAll(ByRef sFind As String, ByRef sReplace As String, ByRef lFlags As Integer) As Object
		Dim nCount As Integer
		With objTarget
			'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelStart. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			.SelStart = 0
			Do 
				If FindNextWord(sFind, lFlags, False) Then
					'UPGRADE_WARNING: Couldn't resolve default property of object objTarget.SelText. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					.SelText = sReplace
					nCount = nCount + 1
				Else
					Exit Do
				End If
			Loop 
			If nCount > 0 Then
				MsgBox(textsereach & nCount & replacement, MsgBoxStyle.Information, findrepl)
			Else
				MsgBox(nomatches, MsgBoxStyle.Exclamation, findrepl)
			End If
		End With
	End Function
	
	Private Function ValidateWholeWord(ByRef PrevLetter As String, ByRef NextLetter As String) As Boolean
		Dim sLetters As String
		ValidateWholeWord = True
		sLetters = "abcdefghijklmnoprqstuvwxyz1234567890"
		If InStr(1, sLetters, PrevLetter, CompareMethod.Text) Or InStr(1, sLetters, NextLetter, CompareMethod.Text) Then ValidateWholeWord = False
	End Function
	
	Private Function InStrR(Optional ByRef lStart As Integer = 0, Optional ByRef sTarget As String = "", Optional ByRef sFind As String = "", Optional ByRef iCompare As Short = 0) As Integer
		Dim cFind, i As Integer
		cFind = Len(sFind)
		For i = lStart - cFind + 1 To 1 Step -1
			If StrComp(Mid(sTarget, i, cFind), sFind, iCompare) = 0 Then
				InStrR = i
				Exit Function
			End If
		Next 
	End Function
	Function fActivateWindowClass(ByRef psClassname As String, ByRef App As String) As Integer
		
		Dim Hwnd As Integer
		Hwnd = FindWindow(psClassname, App)
		
		If Hwnd > 0 Then
			ShowWindowAsync(Hwnd, SW_RESTORE)
			SetForegroundWindow(Hwnd)
		End If
		
		fActivateWindowClass = Hwnd
		
	End Function
	Public Sub Hook()
		
		gHW = Form1.Handle.ToInt32
		'UPGRADE_WARNING: Add a delegate for AddressOf WindowProc Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="E9E157F7-EF0C-4016-87B7-7D7FBBC6EE08"'
		lpPrevWndProc = SetWindowLong(gHW, GWL_WNDPROC, AddressOf WindowProc)
		Hooked = True
		
	End Sub
	
	Public Sub Unhook()
		
		Dim temp As Integer
		temp = SetWindowLong(gHW, GWL_WNDPROC, lpPrevWndProc)
		
	End Sub
	
	Function WindowProc(ByVal hw As Integer, ByVal uMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
		
		If uMsg = WM_COPYDATA Then
			Call MySub(lParam)
		End If
		WindowProc = CallWindowProc(lpPrevWndProc, hw, uMsg, wParam, lParam)
		
	End Function
	
	Sub MySub(ByRef lParam As Integer)
		
		Dim cds As COPYDATASTRUCT
		'UPGRADE_WARNING: Lower bound of array buf was changed from 1 to 0. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="0F1C9BE1-AF9D-476E-83B1-17D43BECFF20"'
		Dim buf(255) As Byte
		Dim a As String
		
		'UPGRADE_WARNING: Couldn't resolve default property of object cds. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Call CopyMemory(cds, lParam, Len(cds))
		
		
		Call CopyMemory(buf(1), cds.lpData, cds.cbData)
		'UPGRADE_ISSUE: Constant vbUnicode was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="55B59875-9A95-4B71-9D6A-7C294BF7139D"'
		a = StrConv(System.Text.UnicodeEncoding.Unicode.GetString(buf), vbUnicode)
		a = Left(a, InStr(1, a, Chr(0)) - 1)
		
		Call Form1.SUBCOMMAND(a)
		'MsgBox "Message received from second instance:" & vbLf & vbLf & a & _
		''    vbLf & vbLf & "This file could also be processed here!", vbInformation
		
		
	End Sub
	Public Function DownloadFile(ByRef sSourceUrl As String, ByRef SlocalFile As String) As Boolean
		
		Dim lngRetVal As Integer
		
		'if the API returns ERROR_SUCCESS (0),
		'return True from the function
		DownloadFile = URLDownloadToFile(0, sSourceUrl, SlocalFile, 0, 0) = ERROR_SUCCESS
		
	End Function
	Public Sub EndApp()
		Dim uProcess As Integer
		uProcess = GetCurrentProcess
		TerminateProcess(uProcess, 0)
	End Sub
	Public Function GetDataFolder(ByRef form4 As System.Windows.Forms.Form, ByRef Appdirectory As String) As Object
		Dim retval As Object
		
		On Error GoTo GenericFolder
		Dim ReturnVal As Integer
		Dim PathName As String
		PathName = New String(Chr(32), 260)
		'UPGRADE_WARNING: Couldn't resolve default property of object retval. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		retval = SHGetFolderPath(form4.Handle.ToInt32, CSIDL_APPDATA, 0, SHGFP_TYPE_CURRENT, PathName)
		PathName = Left(PathName, InStr(PathName, vbNullChar) - 1)
		'UPGRADE_WARNING: Couldn't resolve default property of object GetDataFolder. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		GetDataFolder = PathName & "\" & Appdirectory
		Exit Function
GenericFolder: 
		'Since Windows XP\2000 is not installed
		' we don't have this api so just use the A ' pp.path
		'UPGRADE_WARNING: Couldn't resolve default property of object GetDataFolder. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Err.Number = 453 Then GetDataFolder = My.Application.Info.DirectoryPath
	End Function
End Module