Attribute VB_Name = "WindowsAPI"
Declare Function GetCurrentProcess Lib "kernel32" () As Long
Declare Function TerminateProcess Lib "kernel32" (ByVal hProcess As Long, ByVal uExitCode As Long) As Long
Declare Function SHGetFolderPath Lib "shell32.dll" Alias "SHGetFolderPathA" (ByVal hwndOwner As Long, ByVal nFolder As Long, ByVal hToken As Long, ByVal dwFlags As Long, ByVal lpszPath As String) As Long
Public Const CSIDL_APPDATA = &H1A
Public Const SHGFP_TYPE_CURRENT = 0
Public Const SHGFP_TYPE_DEFAULT = 1
Public DataFolder As String

Type FINDREPLACE
    lStructSize As Long
    hwndOwner As Long
    hInstance As Long
    flags As Long
    lpstrFindWhat As Long
    'lpstrReplaceWith As Long
    lpstrReplaceWith As Long
    wFindWhatLen As Integer
    wReplaceWithLen As Integer
    lCustData As Long
    lpfnHook As Long
    lpTemplateName As String
End Type
'Const SW_HIDE = 0    'Hides the window. Activation passes to another window.
'Const SW_MINIMIZE = 6     'Minimizes the window. Activation passes to another window.
Global Const SW_RESTORE = 9    'Displays a window at its original size and location and activates it.
'Const SW_SHOW = 5   'Displays a window at its current size and location, and activates it.
'Const SW_SHOWMAXIMIZED = 3      'Maximizes a window and activates it.
'Const SW_SHOWMINIMIZED = 2      'Minimizes a window and activates it.
'Const SW_SHOWMINNOACTIVE = 7    'Minimizes a window without changing the active window.
'Const SW_SHOWNA = 8     'Displays a window at its current size and location. Does not change the active window.
'Const SW_SHOWNOACTIVATE = 4     'Displays a window at its most recent size and location. Does not change the active window.
'Const SW_SHOWNORMAL = 1     'Same as SW_RESTORE.

Type Msg
    Hwnd As Long
    message As Long
    wParam As Long
    lParam As Long
    time As Long
    ptX As Long
    ptY As Long
End Type
Type COPYDATASTRUCT
    dwData As Long
    cbData As Long
    lpData As Long
End Type
Public Const GWL_WNDPROC = (-4)
Global Const WM_COPYDATA = &H4A
Public Declare Sub CopyMemory Lib "kernel32" Alias "RtlMoveMemory" (hpvDest As Any, hpvSource As Any, ByVal cbCopy As Long)

Private Declare Function FindText Lib "comdlg32.dll" Alias "FindTextA" (pFindreplace As Long) As Long
Private Declare Function ReplaceText Lib "comdlg32.dll" Alias "ReplaceTextA" (pFindreplace As Long) As Long
Private Declare Function RegisterWindowMessage Lib "user32" Alias "RegisterWindowMessageA" (ByVal lpString As String) As Long
Private Declare Function DispatchMessage Lib "user32" Alias "DispatchMessageA" (lpMsg As Msg) As Long
Private Declare Function GetMessage Lib "user32" Alias "GetMessageA" (lpMsg As Msg, ByVal Hwnd As Long, ByVal wMsgFilterMin As Long, ByVal wMsgFilterMax As Long) As Long
Private Declare Function TranslateMessage Lib "user32" (lpMsg As Msg) As Long
Private Declare Function IsDialogMessage Lib "user32" Alias "IsDialogMessageA" (ByVal hDlg As Long, lpMsg As Msg) As Long
Private Declare Function CopyPointer2String Lib "kernel32" Alias "lstrcpyA" (ByVal NewString As String, ByVal OldString As Long) As Long
Private Declare Function SetWindowLong Lib "user32" Alias "SetWindowLongA" (ByVal Hwnd As Long, ByVal nIndex As Long, ByVal dwNewLong As Long) As Long
Private Declare Function GetWindowLong Lib "user32" Alias "GetWindowLongA" (ByVal Hwnd As Long, ByVal nIndex As Long) As Long
Private Declare Function CallWindowProc Lib "user32" Alias "CallWindowProcA" (ByVal lpPrevWndFunc As Long, ByVal Hwnd As Long, ByVal Msg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long
Private Declare Function GetProcessHeap& Lib "kernel32" ()
Private Declare Function HeapAlloc& Lib "kernel32" (ByVal hHeap As Long, ByVal dwFlags As Long, ByVal dwBytes As Long)
Private Declare Function HeapFree Lib "kernel32" (ByVal hHeap As Long, ByVal dwFlags As Long, lpMem As Any) As Long
Private Declare Function EndDialog Lib "user32" (ByVal hDlg As Long, ByVal nResult As Long) As Long
Public Declare Function ShowWindowAsync& Lib "user32" (ByVal Hwnd As Long, ByVal nCmdShow As Long)
Public Declare Function SetForegroundWindow Lib "user32" (ByVal Hwnd As Long) As Long
Private Declare Function URLDownloadToFile Lib "urlmon" _
   Alias "URLDownloadToFileA" _
  (ByVal pCaller As Long, _
   ByVal szURL As String, _
   ByVal szFileName As String, _
   ByVal dwReserved As Long, _
   ByVal lpfnCB As Long) As Long

'Private Const GWL_WNDPROC = (-4)
Private Const HEAP_ZERO_MEMORY = &H8
Public Const FR_DIALOGTERM = &H40
Public Const FR_DOWN = &H1
Public Const FR_ENABLEHOOK = &H100
Public Const FR_ENABLETEMPLATE = &H200
Public Const FR_ENABLETEMPLATEHANDLE = &H2000
Public Const FR_FINDNEXT = &H8
Public Const FR_HIDEMATCHCASE = &H8000
Public Const FR_HIDEUPDOWN = &H4000
Public Const FR_HIDEWHOLEWORD = &H10000
Public Const FR_MATCHCASE = &H4
Public Const FR_NOMATCHCASE = &H800
Public Const FR_NOUPDOWN = &H400
Public Const FR_NOWHOLEWORD = &H1000
Public Const FR_REPLACE = &H10
Public Const FR_REPLACEALL = &H20
Public Const FR_SHOWHELP = &H80
Public Const FR_WHOLEWORD = &H2
Const WM_DESTROY = &H2
Global Kcommand As String

Const FINDMSGSTRING = "commdlg_FindReplace"
Const HELPMSGSTRING = "commdlg_help"
Const BufLength = 256

Public hDialog As Long, OldProc As Long
Dim uFindMsg As Long, uHelpMsg As Long, lHeap As Long
Public RetFrs As FINDREPLACE, TMsg As Msg
Dim arrFind() As Byte, arrReplace() As Byte
Dim objTarget As Object

Global lpPrevWndProc As Long, gHW As Long, OtherInstanceHwnd As Long
Global Hooked As Boolean

Public Declare Function SendMessageLong Lib "user32" Alias "SendMessageA" (ByVal Hwnd As Long, ByVal wMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long
'Public Declare Function FindWindowEx Lib "user32" Alias "FindWindowExA" (ByVal hWnd1 As Long, ByVal hWnd2 As Long, ByVal lpsz1 As String, ByVal lpsz2 As String) As Long


Public Declare Function Shell_NotifyIcon Lib "shell32.dll" Alias "Shell_NotifyIconA" (ByVal dwMessage As Long, lpData As NOTIFYICONDATA) As Long
Public Declare Function ShowWindow Lib "user32" (ByVal Hwnd As Long, ByVal nCmdShow As Long) As Long
Public Declare Function EnableWindow Lib "user32" (ByVal Hwnd As Long, ByVal cmd As Long) As Long
Public Declare Function FindWindow Lib "user32" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpWindowName As String) As Long
Public Declare Function GetWindow Lib "user32" (ByVal Hwnd As Long, ByVal wCmd As Long) As Long
Public Declare Function GetClassName Lib "user32" Alias "GetClassNameA" (ByVal Hwnd As Long, ByVal lpClassName As String, ByVal nMaxCount As Long) As Long
Public Type NOTIFYICONDATA
    cbSize As Long
    Hwnd As Long
    uID As Long
    uFlags As Long
    uCallbackMessage As Long
    hIcon As Long
    sTip As String * 64
    End Type
    Public Const NIM_ADD = &H0
    Public Const NIM_MODIFY = &H1
    Public Const NIM_DELETE = &H2
    Public Const NIF_MESSAGE = &H1
    Public Const NIF_ICON = &H2
    Public Const NIF_TIP = &H4
    Public Const NIF_DOALL = NIF_MESSAGE Or NIF_ICON Or NIF_TIP
    'Public Const SW_RESTORE = 9
    Public Const SW_MINIMIZE = 6
    Public Const WM_MOUSEMOVE = &H200
    Public Const WM_LBUTTONDBLCLK = &H203
    Public Const WM_RBUTTONUP = &H205

Private Declare Function GetSaveFileName Lib "comdlg32.dll" Alias "GetSaveFileNameA" (pOpenfilename As OPENFILENAME) As Long


Private Declare Function GetOpenFileName Lib "comdlg32.dll" Alias "GetOpenFileNameA" (pOpenfilename As OPENFILENAME) As Long
    Private strfileName As OPENFILENAME


Private Type OPENFILENAME
    lStructSize As Long
    hwndOwner As Long
    hInstance As Long
    lpstrFilter As String
    lpstrCustomFilter As String
    nMaxCustFilter As Long
    nFilterIndex As Long
    lpstrFile As String
    nMaxFile As Long
    lpstrFileTitle As String
    nMaxFileTitle As Long
    lpstrInitialDir As String
    lpstrTitle As String
    flags As Long
    nFileOffset As Integer
    nFileExtension As Integer
    lpstrDefExt As String
    lCustData As Long
    lpfnHook As Long
    lpTemplateName As String
    End Type



Public Const WM_USER = &H400
Public Const TB_SETSTYLE = WM_USER + 56
Public Const TB_GETSTYLE = WM_USER + 57
Public Const TBSTYLE_FLAT = &H800

Public Declare Function SendMessage Lib "user32" _
   Alias "SendMessageA" _
  (ByVal Hwnd As Long, _
   ByVal wMsg As Long, _
   ByVal wParam As Long, _
   lParam As Any) As Long

Public Declare Function FindWindowEx Lib "user32" _
   Alias "FindWindowExA" _
  (ByVal hWnd1 As Long, _
   ByVal hWnd2 As Long, _
   ByVal lpsz1 As String, _
   ByVal lpsz2 As String) As Long
Declare Function RegEnumValue Lib "advapi32.dll" Alias "RegEnumValueA" (ByVal hKey As Long, ByVal dwIndex As Long, ByVal lpValueName As String, lpcbValueName As Long, ByVal lpReserved As Long, lpType As Long, ByVal lpData As String, lpcbData As Long) As Long


Declare Function RegOpenKeyEx Lib "advapi32" Alias "RegOpenKeyExA" (ByVal hKey As Long, ByVal lpSubKey As String, ByVal ulOptions As Long, ByVal samDesired As Long, phkResult As Long) As Long


Declare Function RegSetValueEx Lib "advapi32" Alias "RegSetValueExA" (ByVal hKey As Long, ByVal lpValueName As String, ByVal Reserved As Long, ByVal dwType As Long, ByVal szData As String, ByVal cbData As Long) As Long
Declare Function RegDeleteValue Lib "advapi32" Alias "RegDeleteValueA" (ByVal hKey As Long, ByVal lpValueName As String) As Long


Declare Function RegCloseKey Lib "advapi32" (ByVal hKey As Long) As Long


Declare Function RegCreateKeyEx Lib "advapi32" Alias "RegCreateKeyExA" (ByVal hKey As Long, ByVal lpSubKey As String, ByVal Reserved As Long, ByVal lpClass As String, ByVal dwOptions As Long, ByVal samDesired As Long, lpSecurityAttributes As SECURITY_ATTRIBUTES, phkResult As Long, lpdwDisposition As Long) As Long


    #If Win32 Then
        
        Public Const HKEY_CLASSES_ROOT = &H80000000
        Public Const HKEY_CURRENT_USER = &H80000001
        Public Const HKEY_LOCAL_MACHINE = &H80000002
        Public Const HKEY_USERS = &H80000003
        Public Const KEY_ALL_ACCESS = &H3F
        Public Const REG_OPTION_NON_VOLATILE = 0&
        Public Const REG_CREATED_NEW_KEY = &H1
        Public Const REG_OPENED_EXISTING_KEY = &H2
        Public Const ERROR_SUCCESS = 0&
        Public Const REG_SZ = (1)
    #End If


Type SECURITY_ATTRIBUTES
    
    nLength As Long
    lpSecurityDescriptor As Long
    bInheritHandle As Boolean
    End Type


Declare Function SetWindowPos Lib "user32" _
    (ByVal Hwnd As Long, _
    ByVal hWndInsertAfter As Long, _
    ByVal X As Long, _
    ByVal Y As Long, _
    ByVal cx As Long, _
    ByVal cy As Long, _
    ByVal wFlags As Long) As Long
Declare Function ShellExecute Lib "shell32.dll" Alias "ShellExecuteA" (ByVal Hwnd As Long, ByVal lpOperation As String, ByVal lpFile As String, ByVal lpParameters As String, ByVal lpDirectory As String, ByVal nShowCmd As Long) As Long

Public Const HWND_TOPMOST = -1
Public Const HWND_NOTOPMOST = -2
Private Const LOCALE_SDECIMAL = 22
Private Const LOCALE_STHOUSAND = 23
Private Const WM_SETTINGCHANGE = &H1A
      
Private Const HWND_BROADCAST = &HFFFF&

Private Declare Function SetLocaleInfo Lib "kernel32" Alias "SetLocaleInfoA" (ByVal Locale As Long, ByVal LCType As Long, ByVal lpLCData As String) As Boolean
Private Declare Function PostMessage Lib "user32" Alias "PostMessageA" (ByVal Hwnd As Long, ByVal wMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long
Private Declare Function GetSystemDefaultLCID Lib "kernel32" () As Long
Private Declare Function GetLocaleInfo Lib "kernel32" Alias "GetLocaleInfoA" (ByVal Locale As Long, ByVal LCType As Long, ByVal lpLCData As String, ByVal cchData As Long) As Long

Public Function setting(ByRef digit As String, ByRef milionen As String)


Dim SeparadorDecimal As String
Dim SeparadorMiles As String
         
   Dim Symbol As String
   Dim iRet1 As Long
   Dim iRet2 As Long
   Dim lpLCDataVar As String
   Dim Pos As Integer
   Dim Locale As Long
         
   Locale = 1024
   
   iRet1 = GetLocaleInfo(Locale, LOCALE_SDECIMAL, lpLCDataVar, 0)
   Symbol = String$(iRet1, 0)
   iRet2 = GetLocaleInfo(Locale, LOCALE_SDECIMAL, Symbol, iRet1)
   Pos = InStr(Symbol, Chr$(0))
   If Pos > 0 Then
      Symbol = Left$(Symbol, Pos - 1)
      SeparadorDecimal = Symbol
   End If

   iRet1 = GetLocaleInfo(Locale, LOCALE_STHOUSAND, lpLCDataVar, 0)
   Symbol = String$(iRet1, 0)
   iRet2 = GetLocaleInfo(Locale, LOCALE_STHOUSAND, Symbol, iRet1)
   Pos = InStr(Symbol, Chr$(0))
   If Pos > 0 Then
      Symbol = Left$(Symbol, Pos - 1)
      SeparadorMiles = Symbol
   End If
    
    digit = SeparadorDecimal
    milionen = SeparadorMiles
End Function
Sub AlwaysOnTop(myfrm As Form, SetOnTop As Boolean)


    If SetOnTop Then
        lFlag = HWND_TOPMOST
    Else
        lFlag = HWND_NOTOPMOST
    End If
    SetWindowPos myfrm.Hwnd, lFlag, _
    myfrm.Left / Screen.TwipsPerPixelX, _
    myfrm.Top / Screen.TwipsPerPixelY, _
    myfrm.Width / Screen.TwipsPerPixelX, _
    myfrm.Height / Screen.TwipsPerPixelY, _
    SWP_NOACTIVATE Or SWP_SHOWWINDOW
End Sub

Public Function bSetRegValue(ByVal hKey As Long, ByVal lpszSubKey As String, ByVal sSetValue As String, ByVal sValue As String) As Boolean
    
    On Error Resume Next
    Dim phkResult As Long
    Dim lResult As Long
    Dim SA As SECURITY_ATTRIBUTES
    Dim lCreate As Long
    RegCreateKeyEx hKey, lpszSubKey, 0, "", REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, SA, phkResult, lCreate
    lResult = RegSetValueEx(phkResult, sSetValue, 0, REG_SZ, sValue, CLng(Len(sValue) + 1))
    RegCloseKey phkResult
    bSetRegValue = (lResult = ERROR_SUCCESS)
    
End Function
Public Function bDeleteRegValue(ByVal hKey As Long, ByVal lpszSubKey As String, ByVal sValue As String) As Boolean
    
    On Error Resume Next
    Dim phkResult As Long
    Dim lResult As Long
    Dim SA As SECURITY_ATTRIBUTES
    Dim lCreate As Long
    RegCreateKeyEx hKey, lpszSubKey, 0, "", REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, SA, phkResult, lCreate
    lResult = RegDeleteValue(phkResult, sValue)
    RegCloseKey phkResult
    bDeleteRegValue = (lResult = ERROR_SUCCESS)
    
End Function

Public Sub MakeDirectory(PhysicalPath As String)
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
    If Trim$(PhysicalPath) = "" Then Exit Sub
    Dim arrFolderArray() As String
    Dim strCurrentPath As String
    Dim X As Integer
    On Error GoTo ErrorMakeDirectory
    arrFolderArray() = Split(PhysicalPath, "\", , vbTextCompare)


    For X = 0 To UBound(arrFolderArray())


        If InStr(arrFolderArray(X), ":") = 0 Then
            strCurrentPath = strCurrentPath & "\" & arrFolderArray(X)
            MkDir strCurrentPath
        Else
            ' Add the Drive letter
            strCurrentPath = arrFolderArray(X)
        End If
    Next
    Exit Sub
ErrorMakeDirectory:
    Resume Next
End Sub


Public Function bGetRegValue(ByVal hKey As Long, ByVal sKey As String, ByVal sSubKey As String) As String
    
    Dim lResult As Long
    Dim phkResult As Long
    Dim dwReserved As Long
    Dim szBuffer As String
    Dim lBuffSize As Long
    Dim szBuffer2 As String
    Dim lBuffSize2 As Long
    Dim lIndex As Long
    Dim lType As Long
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
    RegCloseKey phkResult
End Function

Private Sub DialogFilter(WantedFilter As String)
    Dim intLoopCount As Integer
    strfileName.lpstrFilter = ""


    For intLoopCount = 1 To Len(WantedFilter)
        If Mid(WantedFilter, intLoopCount, 1) = "|" Then strfileName.lpstrFilter = _
        strfileName.lpstrFilter + Chr(0) Else strfileName.lpstrFilter = _
        strfileName.lpstrFilter + Mid(WantedFilter, intLoopCount, 1)
    Next intLoopCount
    strfileName.lpstrFilter = strfileName.lpstrFilter + Chr(0)
End Sub
'This is The Function To get the File Na
'     me to Open
'Even If U don't specify a Title or a Fi
'     lter it is OK


Public Function fncGetFileNametoOpen(Optional strDialogTitle As String = "Open", Optional strFilter As String = "All Files|*.*", Optional strDefaultExtention As String = "*.*") As String
    Dim lngReturnValue As Long
    Dim intRest As Integer
    strfileName.lpstrTitle = strDialogTitle
    strfileName.lpstrDefExt = strDefaultExtention
    DialogFilter (strFilter)
    strfileName.hInstance = App.hInstance
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
Public Function fncGetFileNametoSave(strFilter As String, strDefaultExtention As String, Optional strDialogTitle As String = "Save") As String
    Dim lngReturnValue As Long
    Dim intRest As Integer
    strfileName.lpstrTitle = strDialogTitle
    strfileName.lpstrDefExt = strDefaultExtention
    DialogFilter (strFilter)
    strfileName.hInstance = App.hInstance
    strfileName.lpstrFile = Chr(0) & Space(259)
    strfileName.nMaxFile = 260
    strfileName.flags = &H80000 Or &H4
    strfileName.lStructSize = Len(strfileName)
    lngReturnValue = GetSaveFileName(strfileName)
    fncGetFileNametoSave = strfileName.lpstrFile
End Function


Public Sub ButtonRelease(ctrl As Control, Container As Object)
  Container.Cls
  ctrl.Move iX, iY, 400, 400
  Container.Line (ctrl.Left - 25, ctrl.Top - 25)-(ctrl.Left + _
    ctrl.Width + 25, ctrl.Top - 25), vb3DHighlight
  Container.Line (ctrl.Left - 25, ctrl.Top - 25)-(ctrl.Left - 25, _
    ctrl.Height + ctrl.Top + 25), vb3DHighlight
  Container.Line (ctrl.Left - 25, ctrl.Top + ctrl.Height + 25)- _
    (ctrl.Left + ctrl.Width + 25, ctrl.Top + ctrl.Height + 25), _
    vb3DShadow
  Container.Line (ctrl.Left + ctrl.Width + 25, ctrl.Top + _
    ctrl.Height + 25)-(ctrl.Left + ctrl.Width + 25, ctrl.Top - 25), _
    vb3DShadow
End Sub
Public Sub ButtonPress(ctrl As Control, Container As Object)
  Container.Cls
  Container.Line (ctrl.Left - 25, ctrl.Top - 25)-(ctrl.Left + _
    ctrl.Width + 25, ctrl.Top - 25), vb3DShadow
  Container.Line (ctrl.Left - 25, ctrl.Top - 25)-(ctrl.Left - 25, _
    ctrl.Height + ctrl.Top + 25), vb3DShadow
  Container.Line (ctrl.Left - 25, ctrl.Top + ctrl.Height + 25)- _
    (ctrl.Left + ctrl.Width + 25, ctrl.Top + ctrl.Height + 25), _
    vb3DHighlight
  Container.Line (ctrl.Left + ctrl.Width + 25, ctrl.Top + ctrl.Height _
    + 25)-(ctrl.Left + ctrl.Width + 25, ctrl.Top - 25), vb3DHighlight
  iX = ctrl.Left
  iY = ctrl.Top
  ctrl.Move ctrl.Left + 30, ctrl.Top + 30, ctrl.Width - 30, _
    ctrl.Height - 30
End Sub
Public Sub HighlightBorder(ctrl As Control, Container As Object)
  Container.Line (ctrl.Left - 25, ctrl.Top - 25)-(ctrl.Left + _
    ctrl.Width + 25, ctrl.Top - 25), vb3DHighlight
  Container.Line (ctrl.Left - 25, ctrl.Top - 25)-(ctrl.Left - 25, _
    ctrl.Height + ctrl.Top + 25), vb3DHighlight
  Container.Line (ctrl.Left - 25, ctrl.Top + ctrl.Height + 25)- _
    (ctrl.Left + ctrl.Width + 25, ctrl.Top + ctrl.Height + 25), _
    vb3DShadow
  Container.Line (ctrl.Left + ctrl.Width + 25, ctrl.Top + ctrl.Height _
    + 25)-(ctrl.Left + ctrl.Width + 25, ctrl.Top - 25), vb3DShadow
End Sub

Public Sub ShowFind(fOwner As Form, objWhere As Object, lFlags As Long, sFind As String, Optional bReplace As Boolean = False, Optional sReplace As String = "")
   If hDialog > 0 Then Exit Sub
   Set objTarget = objWhere
   Dim FRS As FINDREPLACE, i As Integer
   arrFind = StrConv(sFind & Chr$(0), vbFromUnicode)
   arrReplace = StrConv(sReplace & Chr$(0), vbFromUnicode)
   With FRS
        .lStructSize = LenB(FRS) '&H20     '
        .lpstrFindWhat = VarPtr(arrFind(0))
        .wFindWhatLen = BufLength
        .lpstrReplaceWith = VarPtr(arrReplace(0))
        .wReplaceWithLen = BufLength
        .hwndOwner = fOwner.Hwnd
        .flags = lFlags
        .hInstance = App.hInstance
    End With
    lHeap = HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, FRS.lStructSize)
    CopyMemory ByVal lHeap, FRS, Len(FRS)
    uFindMsg = RegisterWindowMessage(FINDMSGSTRING)
    uHelpMsg = RegisterWindowMessage(HELPMSGSTRING)
    OldProc = SetWindowLong(fOwner.Hwnd, GWL_WNDPROC, AddressOf WndProc)
    If bReplace Then
       hDialog = ReplaceText(ByVal lHeap)
    Else
       hDialog = FindText(ByVal lHeap)
    End If
    MessageLoop
End Sub

Private Sub MessageLoop()
  Do While GetMessage(TMsg, 0&, 0&, 0&) And hDialog > 0
     If IsDialogMessage(hDialog, TMsg) = False Then
        TranslateMessage TMsg
        DispatchMessage TMsg
     End If
  Loop
End Sub

Public Function WndProc(ByVal hOwner As Long, ByVal wMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long
   Dim iRet As Long
   Select Case wMsg
      Case uFindMsg
           CopyMemory RetFrs, ByVal lParam, Len(RetFrs)
           If (RetFrs.flags And FR_DIALOGTERM) = FR_DIALOGTERM Then
              SetWindowLong hOwner, GWL_WNDPROC, OldProc
              HeapFree GetProcessHeap(), 0, lHeap
              hDialog = 0: lHeap = 0: OldProc = 0
              If objTarget.HideSelection Then objTarget.SetFocus
              Set objTarget = Nothing
           Else
              DoFindReplace RetFrs
           End If
      Case uHelpMsg
          iRet = ShellExecute(Editor.Hwnd, vbNullString, Configur3, vbNullString, "c:\", SW_SHOWNORMAL)
    Case Else
           If wMsg = WM_DESTROY Then
              EndDialog hDialog, 0&
              SetWindowLong hOwner, GWL_WNDPROC, OldProc
              HeapFree GetProcessHeap(), 0, lHeap
              hDialog = 0: lHeap = 0: OldProc = 0
              Set objTarget = Nothing
              Exit Function
           End If
           WndProc = CallWindowProc(OldProc, hOwner, wMsg, wParam, lParam)
   End Select
End Function

Private Sub DoFindReplace(fr As FINDREPLACE)
  If CheckFlags(FR_FINDNEXT, fr.flags) Then
     If CheckFlags(FR_DOWN, fr.flags) Then
        FindNextWord PointerToString(fr.lpstrFindWhat), fr.flags
     Else
        FindPrevWord PointerToString(fr.lpstrFindWhat), fr.flags
     End If
     If objTarget.HideSelection Then objTarget.SetFocus
  End If
  If CheckFlags(FR_REPLACE, fr.flags) Then ReplaceWord PointerToString(fr.lpstrFindWhat), PointerToString(fr.lpstrReplaceWith), fr.flags
  If CheckFlags(FR_REPLACEALL, fr.flags) Then ReplaceAll PointerToString(fr.lpstrFindWhat), PointerToString(fr.lpstrReplaceWith), fr.flags
End Sub

Private Function PointerToString(p As Long) As String
   Dim S As String
   S = String(BufLength, Chr$(0))
   CopyPointer2String S, p
   PointerToString = Left(S, InStr(S, Chr$(0)) - 1)
End Function

Private Function CheckFlags(flag As Long, flags As Long) As Boolean
   CheckFlags = ((flags And flag) = flag)
End Function

Function FindNextWord(sFind As String, lFlags As Long, Optional bShowMsg As Boolean = True) As Boolean
  Dim lStart As Long, pl As String, nl As String
   With objTarget
      lStart = .SelStart + 1
      If .SelLength > 0 Then lStart = lStart + 1
      Do
        lStart = InStr(lStart, .Text, sFind, IIf(CheckFlags(FR_MATCHCASE, lFlags), vbBinaryCompare, vbTextCompare))
        If lStart = 0 Then Exit Do
        If CheckFlags(FR_WHOLEWORD, lFlags) Then
           If lStart = 1 Then pl = " " Else pl = Mid$(.Text, lStart - 1, 1)
           If lStart + Len(sFind) = Len(.Text) Then nl = " " Else nl = Mid$(.Text, lStart + Len(sFind), 1)
           If ValidateWholeWord(pl, nl) Then Exit Do Else lStart = lStart + 1
        Else
           Exit Do
        End If
      Loop
      If lStart > 0 Then
         .SelStart = lStart - 1
         .SelLength = Len(sFind)
         FindNextWord = True
      Else
         FindNextWord = False
         If bShowMsg Then MsgBox nomatches, vbExclamation, findrepl
      End If
   End With
End Function

Function FindPrevWord(sFind As String, lFlags As Long) As Boolean
  Dim lStart As Long, pl As String, nl As String
   With objTarget
      lStart = .SelStart - 1
      If lStart < 0 Then lStart = 0
      Do
        lStart = InStrR(lStart, .Text, sFind, IIf(CheckFlags(FR_MATCHCASE, lFlags), vbBinaryCompare, vbTextCompare))
        If lStart <= 0 Then Exit Do
        If CheckFlags(FR_WHOLEWORD, lFlags) Then
           If lStart = 1 Then pl = " " Else pl = Mid$(.Text, lStart - 1, 1)
           If lStart + Len(sFind) = Len(.Text) Then nl = " " Else nl = Mid$(.Text, lStart + Len(sFind), 1)
           If ValidateWholeWord(pl, nl) Then Exit Do Else lStart = lStart - 1
        Else
           Exit Do
        End If
      Loop
      If lStart > 0 Then
         .SelStart = lStart - 1
         .SelLength = Len(sFind)
         FindPrevWord = True
      Else
         FindPrevWord = False
         MsgBox nomatches, vbExclamation, findrepl
      End If
   End With
End Function

Function ReplaceWord(sFind As String, sReplace As String, lFlags As Long)
  With objTarget
      If .SelText <> sFind Then
         FindNextWord sFind, lFlags
      Else
         .SelText = sReplace
         FindNextWord sFind, lFlags
      End If
  End With
End Function

Function ReplaceAll(sFind As String, sReplace As String, lFlags As Long)
  Dim nCount As Long
  With objTarget
      .SelStart = 0
      Do
         If FindNextWord(sFind, lFlags, False) Then
            .SelText = sReplace
            nCount = nCount + 1
         Else
            Exit Do
         End If
      Loop
      If nCount > 0 Then
         MsgBox textsereach & nCount & replacement, vbInformation, findrepl
      Else
         MsgBox nomatches, vbExclamation, findrepl
      End If
  End With
End Function

Private Function ValidateWholeWord(PrevLetter As String, NextLetter As String) As Boolean
   Dim sLetters As String
   ValidateWholeWord = True
   sLetters = "abcdefghijklmnoprqstuvwxyz1234567890"
   If InStr(1, sLetters, PrevLetter, vbTextCompare) Or InStr(1, sLetters, NextLetter, vbTextCompare) Then ValidateWholeWord = False
End Function

Private Function InStrR(Optional lStart As Long, Optional sTarget As String, Optional sFind As String, Optional iCompare As Integer) As Long
    Dim cFind As Long, i As Long
    cFind = Len(sFind)
    For i = lStart - cFind + 1 To 1 Step -1
        If StrComp(Mid$(sTarget, i, cFind), sFind, iCompare) = 0 Then
            InStrR = i
            Exit Function
        End If
    Next
End Function
Function fActivateWindowClass(psClassname As String, App As String) As Long
    
    Dim Hwnd As Long
    Hwnd = FindWindow(psClassname, App)
    
    If Hwnd > 0 Then
        ShowWindowAsync Hwnd, SW_RESTORE
        SetForegroundWindow Hwnd
    End If
    
    fActivateWindowClass = Hwnd
    
End Function
Public Sub Hook()
    
    gHW = Form1.Hwnd
    lpPrevWndProc = SetWindowLong(gHW, GWL_WNDPROC, AddressOf WindowProc)
    Hooked = True
    
End Sub

Public Sub Unhook()
          
    Dim temp As Long
    temp = SetWindowLong(gHW, GWL_WNDPROC, lpPrevWndProc)

End Sub

Function WindowProc(ByVal hw As Long, ByVal uMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long
          
    If uMsg = WM_COPYDATA Then
        Call MySub(lParam)
    End If
    WindowProc = CallWindowProc(lpPrevWndProc, hw, uMsg, wParam, lParam)

End Function

Sub MySub(lParam As Long)
          
    Dim cds As COPYDATASTRUCT
    Dim buf(1 To 255) As Byte, a As String

    Call CopyMemory(cds, ByVal lParam, Len(cds))

    
            Call CopyMemory(buf(1), ByVal cds.lpData, cds.cbData)
            a = StrConv(buf, vbUnicode)
            a = Left(a, InStr(1, a, Chr(0)) - 1)
           
           Call Form1.SUBCOMMAND(a)
            'MsgBox "Message received from second instance:" & vbLf & vbLf & a & _
            '    vbLf & vbLf & "This file could also be processed here!", vbInformation
   

End Sub
Public Function DownloadFile(sSourceUrl As String, _
                             SlocalFile As String) As Boolean

   Dim lngRetVal As Long
   
  'if the API returns ERROR_SUCCESS (0),
  'return True from the function
   DownloadFile = URLDownloadToFile(0&, _
                                    sSourceUrl, _
                                    SlocalFile, _
                                    0&, _
                                    0&) = ERROR_SUCCESS
   
End Function
Public Sub EndApp()
Dim uProcess As Long
uProcess = GetCurrentProcess
TerminateProcess uProcess, 0
End Sub
Public Function GetDataFolder(form4 As Form, Appdirectory As String) As String

On Error GoTo GenericFolder
Dim ReturnVal As Long
Dim PathName As String
Dim NullPos As Integer
PathName = String$(260, Chr$(32))
ReturnVal = SHGetFolderPath(form4.Hwnd, CSIDL_APPDATA, 0, SHGFP_TYPE_CURRENT, PathName)
If ReturnVal <> 0 Then GoTo GenericFolder
NullPos = InStr(PathName, vbNullChar)
If NullPos < 2 Then GoTo GenericFolder
PathName = Left$(PathName, NullPos - 1)
If Trim$(PathName) = "" Then GoTo GenericFolder
GetDataFolder = AppendPath(PathName, Appdirectory)
Exit Function
GenericFolder:
'On Windows 7 or newer this API can still fail on some systems. Never return
'an empty path, because that causes runtime error 52 when opening config files.
PathName = Environ$("APPDATA")
If Trim$(PathName) <> "" Then
                        GetDataFolder = AppendPath(PathName, Appdirectory)
                        Else
                        GetDataFolder = App.Path
                        End If
End Function

Public Function AppendPath(ByVal BasePath As String, ByVal ChildPath As String) As String
If Trim$(ChildPath) = "" Then
                        AppendPath = BasePath
                        Else
                        If Right$(BasePath, 1) = "\" Then
                                                AppendPath = BasePath + ChildPath
                                                Else
                                                AppendPath = BasePath + "\" + ChildPath
                                                End If
                        End If
End Function
