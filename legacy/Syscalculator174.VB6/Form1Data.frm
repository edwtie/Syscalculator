VERSION 5.00
Begin VB.Form Form1 
   BorderStyle     =   1  'Fixed Single
   Caption         =   "Syscalculator Euro Edition"
   ClientHeight    =   2970
   ClientLeft      =   1275
   ClientTop       =   1800
   ClientWidth     =   5160
   FillColor       =   &H8000000F&
   Icon            =   "Form1Data.frx":0000
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   2970
   ScaleWidth      =   5160
   ShowInTaskbar   =   0   'False
   Begin VB.CheckBox Digitchek 
      Caption         =   "Digit"
      Height          =   375
      Left            =   120
      TabIndex        =   12
      Top             =   2400
      Width           =   3015
   End
   Begin VB.ComboBox Combo1 
      Height          =   315
      ItemData        =   "Form1Data.frx":030A
      Left            =   2520
      List            =   "Form1Data.frx":030C
      Sorted          =   -1  'True
      TabIndex        =   11
      Text            =   "Combo1"
      Top             =   140
      Width           =   2415
   End
   Begin VB.OptionButton Option2 
      Height          =   255
      Left            =   120
      TabIndex        =   9
      Top             =   1920
      Width           =   255
   End
   Begin VB.OptionButton Option1 
      Height          =   255
      Left            =   120
      TabIndex        =   8
      Top             =   1080
      Value           =   -1  'True
      Width           =   255
   End
   Begin VB.TextBox Text2 
      Height          =   285
      Left            =   1080
      TabIndex        =   1
      Top             =   1920
      Width           =   2895
   End
   Begin VB.TextBox Text1 
      Height          =   285
      Left            =   1080
      TabIndex        =   0
      Top             =   1080
      Width           =   2895
   End
   Begin VB.Line Line11 
      BorderColor     =   &H80000014&
      X1              =   80
      X2              =   110
      Y1              =   110
      Y2              =   110
   End
   Begin VB.Line Line10 
      BorderColor     =   &H80000010&
      X1              =   80
      X2              =   120
      Y1              =   520
      Y2              =   520
   End
   Begin VB.Line Line9 
      BorderColor     =   &H80000010&
      X1              =   100
      X2              =   100
      Y1              =   520
      Y2              =   90
   End
   Begin VB.Line Line8 
      BorderColor     =   &H80000014&
      X1              =   80
      X2              =   80
      Y1              =   520
      Y2              =   100
   End
   Begin VB.Image Image3 
      Height          =   405
      Left            =   1200
      Picture         =   "Form1Data.frx":030E
      Stretch         =   -1  'True
      Top             =   120
      Width           =   405
   End
   Begin VB.Image Image2 
      Height          =   405
      Left            =   720
      Picture         =   "Form1Data.frx":09F8
      Stretch         =   -1  'True
      Top             =   120
      Width           =   405
   End
   Begin VB.Line Line7 
      BorderColor     =   &H80000014&
      X1              =   5050
      X2              =   5050
      Y1              =   10
      Y2              =   610
   End
   Begin VB.Line Line6 
      BorderColor     =   &H80000014&
      X1              =   0
      X2              =   5060
      Y1              =   610
      Y2              =   610
   End
   Begin VB.Label Label7 
      Caption         =   "Location:"
      Height          =   255
      Left            =   1680
      TabIndex        =   10
      Top             =   150
      Width           =   1095
   End
   Begin VB.Line Line5 
      BorderColor     =   &H80000014&
      X1              =   10
      X2              =   10
      Y1              =   10
      Y2              =   600
   End
   Begin VB.Line Line4 
      BorderColor     =   &H80000010&
      X1              =   5040
      X2              =   5040
      Y1              =   0
      Y2              =   600
   End
   Begin VB.Line Line3 
      BorderColor     =   &H80000010&
      X1              =   0
      X2              =   0
      Y1              =   0
      Y2              =   600
   End
   Begin VB.Line Line1 
      BorderColor     =   &H80000010&
      Index           =   1
      X1              =   0
      X2              =   5040
      Y1              =   600
      Y2              =   600
   End
   Begin VB.Line Line2 
      BorderColor     =   &H80000014&
      X1              =   10
      X2              =   5030
      Y1              =   10
      Y2              =   10
   End
   Begin VB.Line Line1 
      BorderColor     =   &H80000010&
      Index           =   0
      X1              =   0
      X2              =   5040
      Y1              =   0
      Y2              =   0
   End
   Begin VB.Image Image1 
      Appearance      =   0  'Flat
      Height          =   405
      Left            =   240
      Picture         =   "Form1Data.frx":10E2
      Stretch         =   -1  'True
      Tag             =   "56"
      Top             =   120
      Width           =   405
   End
   Begin VB.Label Label6 
      Height          =   255
      Left            =   4080
      TabIndex        =   7
      Top             =   1920
      Width           =   735
   End
   Begin VB.Label Label5 
      Height          =   255
      Left            =   4080
      TabIndex        =   6
      Top             =   1080
      Width           =   735
   End
   Begin VB.Label Label4 
      Caption         =   "Label4"
      Height          =   255
      Left            =   1080
      TabIndex        =   5
      Top             =   1560
      Width           =   2895
   End
   Begin VB.Label Label3 
      Caption         =   "Label3"
      Height          =   255
      Left            =   1080
      TabIndex        =   4
      Top             =   720
      Width           =   2895
   End
   Begin VB.Label Label2 
      Height          =   255
      Left            =   480
      TabIndex        =   3
      Top             =   1920
      Width           =   615
   End
   Begin VB.Label Label1 
      Height          =   255
      Left            =   480
      TabIndex        =   2
      Top             =   1080
      Width           =   495
   End
   Begin VB.Menu File 
      Caption         =   "&File"
      Begin VB.Menu openconv 
         Caption         =   "Open &Conversion"
      End
      Begin VB.Menu Exit 
         Caption         =   "&Exit"
      End
   End
   Begin VB.Menu edit 
      Caption         =   "&Edit"
      Begin VB.Menu ecut 
         Caption         =   "cut"
         Shortcut        =   ^X
      End
      Begin VB.Menu ecopy 
         Caption         =   "copy"
         Shortcut        =   ^C
      End
      Begin VB.Menu epaste 
         Caption         =   "paste"
         Shortcut        =   ^V
      End
      Begin VB.Menu Delete 
         Caption         =   "Delete"
         Shortcut        =   {DEL}
      End
      Begin VB.Menu eselect 
         Caption         =   "Select All"
         Shortcut        =   ^A
      End
      Begin VB.Menu delall 
         Caption         =   "Delete All "
         Enabled         =   0   'False
      End
   End
   Begin VB.Menu Config 
      Caption         =   "Config"
      Begin VB.Menu Switchclick 
         Caption         =   "Switch"
      End
      Begin VB.Menu Altop 
         Caption         =   "Always on Top"
      End
      Begin VB.Menu Indo 
         Caption         =   "Introducation Option"
         Checked         =   -1  'True
      End
      Begin VB.Menu Mtray 
         Caption         =   "Tray"
      End
      Begin VB.Menu startup 
         Caption         =   "Startup"
      End
   End
   Begin VB.Menu Tool 
      Caption         =   "&Tool"
      Begin VB.Menu Wizard 
         Caption         =   "WizardExpress"
      End
      Begin VB.Menu cal 
         Caption         =   "Calculcator"
      End
      Begin VB.Menu Opties 
         Caption         =   "Opties"
      End
   End
   Begin VB.Menu Help 
      Caption         =   "&Help"
      Begin VB.Menu Help2 
         Caption         =   "&Help"
         Shortcut        =   {F1}
      End
      Begin VB.Menu help1 
         Caption         =   "&Bugreports"
      End
      Begin VB.Menu urllaunch 
         Caption         =   "Tiedragon.com"
      End
      Begin VB.Menu About 
         Caption         =   "&About"
      End
   End
End
Attribute VB_Name = "Form1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Dim bOverForm As Boolean
Dim change As Integer


Private Sub about_Click()
Load frmAbout
frmAbout.Show (0)
End Sub

Sub Altop_Click()
Dim test1 As Boolean
If Altop.Checked = True Then
      test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Ontop", "0")
      Call WindowsAPI.AlwaysOnTop(Form1, False)
      If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, False)
      If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, False)
      Altop.Checked = False
      TOPilse = False
Else
      test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Ontop", "1")
      Call WindowsAPI.AlwaysOnTop(Form1, True)
      If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, True)
      If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, True)
      Altop.Checked = True
      TOPilse = True
      Exit Sub
      End If
End Sub

Private Sub cal_Click()
standard.Show
End Sub

Private Sub delall_Click()
Form1.Text1.Text = ""
Form1.Text2.Text = ""
End Sub

Private Sub Digitchek_Click()
If Digitchek.Value = 1 Then
      test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Digit", "1")
      booldigit = True

      Exit Sub
      End If
If Digitchek.Value = 0 Then
      test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Digit", "0")
      booldigit = False

      Exit Sub
      End If
End Sub


Private Sub Combo1_Click()
Dim i, e As Integer
Dim a As Boolean
If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, False)
Dim fout As Integer
Dim nodid As Integer
nodid = Combo1.ListIndex
a = OpenNOD(filestring(Combo1.ListIndex) + ".nod", 1)
If a = False Then
                    If nodid < defaultID Then defaultID = defaultID - 1
                    If nodid = defaultID Then If nodid = 0 Then defaultID = 0 Else defaultID = nodid - 1
                    For o = nodid To max
                    If o = max Then
                                    Combo1.RemoveItem (max)
                                    filestring(max) = ""
                                    max = max - 1
                                    Call saveconfig
                                    If max > -1 Then
                                                    
                                                    If Combo1.ListIndex <= 0 Then Combo1.ListIndex = 0: Exit Sub Else Combo1.ListIndex = Combo1.ListIndex - 1
                                                    Exit Sub
                                                    Else
                                                    If max = -1 Then Form1.Hide: Form3.Show: Exit Sub
                                                    Combo1.ListIndex = 0
                                                    Exit Sub
                                                    End If
                                    End If
                    Combo1.List(o) = Combo1.List(o + 1)
                    filestring(o) = filestring(o + 1)
                    Next o
                                    
                    End If
If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, True)

If intro = True Then
                     If Not (Getindro() = "") Then Show (0): Form2.Show (0)
                     End If
If ActiveForm2 = True Then WizardExpress.Show
If tray = True Then
                        Tic.sTip = Form1.Caption & vbNullChar
                        rc = Shell_NotifyIcon(NIM_MODIFY, Tic)
                        End If
Caption = Getappname()
If switch1 = False Then Label1.Caption = Getsym1() Else Label1.Caption = Getsym2()
If switch1 = False Then Label2.Caption = Getsym2() Else Label2.Caption = Getsym1()
If switch1 = False Then Label5.Caption = Getsym3() Else Label5.Caption = Getsym4()
If switch1 = False Then Label6.Caption = Getsym4() Else Label6.Caption = Getsym3()
If switch1 = False Then Label3.Caption = Getask1() Else Label3.Caption = Getask2()
If switch1 = False Then Label4.Caption = Getask2() Else Label4.Caption = Getask1()
End Sub
Private Sub saveconfig()
Dim comm, Data, def As String
Dim OK As Integer
Dim tel As Integer
tel = Len(App.Path + "\")
Rem If Left(Text1.Text, tel) = App.Path + "\" Then Text1.Text = Mid(Text1.Text, tel + 1)

On Error GoTo geenconfig
Open App.Path + "\" + "freesyscal.cfg" For Output As #1
Write #1, "[lang]", Lname, ""
For o = 0 To max
If defaultID = o Then def = "*"
Write #1, Combo1.List(o), filestring(o), def
def = ""
Next o
Rem Lname = Text1.Text
  
  Close #1
Exit Sub
geenconfig:
  
End Sub
Private Sub cute_Click()

End Sub
Sub SUBCOMMAND(VFROM As String)
temp = addconfig(VFROM)
flags = 1
If temp = "-1" Then Exit Sub
If temp = "-2" Then Exit Sub
Combo1.ListIndex = max
Form1.Show
End Sub
Private Sub Command1_Click()
Form3.Show
End Sub

Private Sub Command2_Click()
Call cal_Click
End Sub

Private Sub Delete_Click()
If Option1.Value = True Then
    If Text1.SelLength > 0 Then
        n = Text1.SelStart
        Text1.Text = Left(Text1.Text, Text1.SelStart) & Mid(Text1.Text, Text1.SelStart + Text1.SelLength + 1)
        Text1.SelStart = n
    End If
End If
If Option2.Value = True Then
    If Text2.SelLength > 0 Then
        n = Text2.SelStart
        Text2.Text = Left(Text2.Text, Text2.SelStart) & Mid(Text2.Text, Text2.SelStart + Text2.SelLength + 1)
        Text2.SelStart = n
    End If
End If
End Sub

Private Sub ecopy_Click()
If Option1.Value = True And Text1.SelLength > 0 Then Clipboard.Clear: Clipboard.SetText Mid(Text1.Text, Text1.SelStart + 1, Text1.SelLength)
If Option2.Value = True And Text2.SelLength > 0 Then Clipboard.Clear: Clipboard.SetText Mid(Text2.Text, Text2.SelStart + 1, Text2.SelLength)
    
End Sub

Private Sub ecut_Click()
If Option1.Value = True And Text1.SelLength > 0 Then
    Clipboard.Clear
    Clipboard.SetText Mid(Text1.Text, Text1.SelStart + 1, Text1.SelLength)
    Call Delete_Click
End If
If Option2.Value = True And Text2.SelLength > 0 Then
    Clipboard.Clear
    Clipboard.SetText Mid(Text2.Text, Text2.SelStart + 1, Text2.SelLength)
    Call Delete_Click
End If
       
End Sub



Private Sub epaste_Click()
      result = ""
If Option1.Value = True Then Text1.Text = Clipboard.GetText()
If Option2.Value = True Then Text2.Text = Clipboard.GetText()
End Sub

Private Sub eselect_Click()
If Option1.Value = True Then Text1.SetFocus: Text1.SelStart = 0: Text1.SelLength = Len(Text1.Text)
If Option2.Value = True Then Text2.SetFocus: Text2.SelStart = 0: Text2.SelLength = Len(Text2.Text)
End Sub

Private Sub exit_Click()

Unload Form1
End Sub

Private Sub Form_Activate()
If Apply = True Then
                    Call form1cleanup
                    
                    If tray = True Then
                                        Tic.sTip = Form1.Caption & vbNullChar
                                        rc = Shell_NotifyIcon(NIM_MODIFY, Tic)
                                        End If
                    Apply = False
                    Exit Sub
                    End If


End Sub

Private Sub Image1_MouseDown(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  ButtonPress Image1, Me
End Sub
Private Sub Image1_MouseUp(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  ButtonRelease Image1, Me
End Sub
Private Sub Image2_MouseDown(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  ButtonPress Image2, Me
End Sub
Private Sub Image2_MouseUp(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  ButtonRelease Image2, Me
End Sub
Private Sub Image3_MouseDown(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  ButtonPress Image3, Me
End Sub
Private Sub Image3_MouseUp(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  ButtonRelease Image3, Me
End Sub
Private Sub Form_Load()
Dim test2 As String
Dim test1 As Boolean
App.Title = "Syscalculator"


test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Switch")
If test2 = "" Then test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Switch", "0")
If test2 = "0" Then switch1 = False Else switch1 = True
Call Addcombo
Call Languare(Lname, 1)
tel = InStr(1, Command, "/mod")
If tel >= 1 Then
                         onlyedname = Mid$(Command$, 5 + tel)
                         tel = Len(onlyedname)
                         If UCase(Right$(onlyedname, 3)) = "NOD" Then onlyedname = Left(onlyedname, tel - 4)
                         Form1.Hide
                         Form1.Caption = "Editor"
                         onlyeditor = 1
                         Editor.Show
                         Exit Sub
                         End If
If App.PrevInstance = True Then
                            test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Name")
                            OtherInstanceHwnd = fActivateWindowClass("ThunderRT6FormDC", test2)
                            If Not OtherInstanceHwnd = 0 Then
                            Dim cds As COPYDATASTRUCT, ThWnd As Long, buf(1 To 255) As Byte, a As String
                             ' Get the hWnd of the target application
                             ThWnd = OtherInstanceHwnd
                             a = Command
                            'Copy the string into a byte array, converting it to ASCII
                            CopyMemory buf(1), ByVal a, Len(a)
                            cds.dwData = 3
                            cds.cbData = Len(a) + 1
                            cds.lpData = VarPtr(buf(1))
                            SendMessage OtherInstanceHwnd, WM_COPYDATA, Me.Hwnd, cds
           
                            
                            Unload Me
                            Exit Sub
                            End If
                            End If
Apply = False
test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Ontop")
If test2 = "" Then test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Ontop", "0")
If test2 = "1" Then
                        TOPilse = True
                        Call WindowsAPI.AlwaysOnTop(Form1, True)
                        Altop.Checked = True
                        End If
test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Intro")
If test2 = "" Then test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Intro", "1")
If test2 = "0" Then intro = False: Indo.Checked = False Else intro = True
test2 = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "Syscal")
If test2 = "" Then startup.Checked = False
If test2 = App.Path + "\Freesyscal.exe /tray" Then startup.Checked = True Else startup.Checked = False
test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Digit")
If test2 = "" Then test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Digit", "0")
If test2 = "0" Then booldigit = False: Digitchek.Value = 0 Else booldigit = True: Digitchek.Value = 1
fORMVALUE3 = False
ActiveForm2 = False


test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Tray")
If test2 = "" Then test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Tray", "0")

If test2 = "1" Then
                        Dim rc As Long
                        Tic.cbSize = Len(Tic)
                        Tic.Hwnd = tray1.Hwnd
                        Tic.uID = vbNull
                        Tic.uFlags = NIF_DOALL
                        Tic.uCallbackMessage = WM_MOUSEMOVE
                        Tic.hIcon = Me.Icon
                        Tic.sTip = Form1.Caption & vbNullChar
                        rc = Shell_NotifyIcon(NIM_ADD, Tic)
                        tray = True
                        Mtray.Checked = True
                        tray1.Timer1.Enabled = True
                        If Command = "/tray" Then flags = 1: Form1.Hide
                        End If


If max = -1 Then Form1.Hide: Form3.Show: Exit Sub
If flags = 0 Then
                  
                  test5 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "first")
                  If test5 = "0" Then GoTo verder
                  Form1.Top = (Screen.Height * 0.85) / 2 - Form1.Height / 2
                  Form1.Left = Screen.Width / 2 - Form1.Width / 2

                  Form1.Hide
                  Form3.Show
                  If test5 = "1" Then test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "first", "0")
                  Exit Sub
                  End If
verder:
Dim temp As String
test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "command")
If test2 = "" Then Kcommand = Command Else flags = 1: Kcommand = test2: test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "command", "")

If flags = 0 And Not Kcommand = "" Then
                         temp = addconfig(Command)
                         flags = 1
                         If temp = "-1" Then Exit Sub
                         If temp = "-2" Then Exit Sub
                         Combo1.ListIndex = max
                         Exit Sub
                         End If
flags = 1
If Not Hooked Then Hook
End Sub

Private Sub Form_QueryUnload(Cancel As Integer, UnloadMode As Integer)
If tray = True Then
                    Form1.Hide
                    Load tray1
                    
Else:
If Not Hooked Then Unhook
Form_Unload (Cancel)

                    End If


End Sub

Private Sub Form_Terminate()
Form_Unload (0)
If tray = False Then Unload tray1
End Sub

Private Sub Form_Unload(Cancel As Integer)

If ActiveForm2 = True Then Unload WizardExpress
If ActiveCal = True Then Unload standard
Unload Me
If tray = False Then Unload tray1
End Sub

Private Sub help1_Click()
Dim iRet As Long
iRet = ShellExecute(Me.Hwnd, vbNullString, Configur2 + " " & App.Major & "." & App.Minor & "." & App.Revision, vbNullString, "c:\", SW_SHOWNORMAL)

End Sub

Private Sub Help2_Click()
Dim iRet As Long
iRet = ShellExecute(Me.Hwnd, vbNullString, Configur3, vbNullString, "c:\", SW_SHOWNORMAL)

End Sub

Private Sub Image1_Click()
Call Wizard_Click
End Sub
Private Sub Image1_MouseMove(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  If bOverForm Then
    HighlightBorder Image1, Me
    bOverForm = False
  End If
End Sub
Private Sub Image2_MouseMove(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  If bOverForm Then
    HighlightBorder Image2, Me
    bOverForm = False
  End If
End Sub
Private Sub Image3_MouseMove(Button As Integer, Shift As Integer, _
  X As Single, Y As Single)
  If bOverForm Then
    HighlightBorder Image3, Me
    bOverForm = False
  End If
End Sub
Private Sub Image2_Click()
Call cal_Click
End Sub

Private Sub Image3_Click()
' oude code Form3.Show
Dim numb As Integer
Dim control As String
Dim newfile As String
Dim comm, Data, def As String
Dim OK As Integer
Dim tel As Integer
If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, False)
test = fncGetFileNametoOpen(, "nod files|*.nod", "*.nod")
If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, True)
numb = Asc(Left(test, 1))
If Not (numb = 0) Then newfile = test Else Exit Sub
control = addconfig(newfile)
If control = "-1" Then Exit Sub
Combo1.ListIndex = max
End Sub
Function addconfig(newfile As String) As String
Dim tel As Integer
Dim temp As String
temp = Chr(0)
tel = InStr(newfile, temp)
If Not tel = 0 Then newfile = Left(newfile, tel - 1)
control = AddName(newfile)
If control = "-1" Then GoTo geenconfig
If control = "" Then control = newfile
For o = 0 To max
If UCase$(Combo1.List(o)) = UCase$(control) Then Combo1.Text = Combo1.List(o): Combo1.ListIndex = o: addconfig = "-2": Exit Function
Next o
tel = Len(App.Path + "\")
over:
max = max + 1
Combo1.AddItem control, max
 tel = Len(App.Path + "\")
 If Left(newfile, tel) = App.Path + "\" Then newfile = Mid(newfile, tel + 1)
 tel = Len(newfile)
 If UCase(Right$(newfile, 3)) = "NOD" Then newfile = Left(newfile, tel - 4)
 filestring(max) = newfile
 On Error GoTo geenconfig
Open App.Path + "\" + "freesyscal.cfg" For Output As #1
Write #1, "[lang]", Lname, ""
For o = 0 To max
If defaultID = o Then def = "*"
Write #1, Combo1.List(o), filestring(o), def
def = ""
Next o
Close #1
Exit Function

geenconfig:
addconfig = "-1"
End Function
Private Sub Indo_Click()
Dim test1 As Boolean
If Indo.Checked = True Then
                            intro = False
                            test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Intro", "0")
                            Indo.Checked = False

Else
                            intro = True
                            test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Intro", "1")
                            Indo.Checked = True
 
                            
                            End If
                            
End Sub

Private Sub Picture1_Click()
Rem This click can open FileDialog.
End Sub

Private Sub Label7_MouseMove(Button As Integer, Shift As Integer, X As Single, Y As Single)
If Not bOverForm Then
    Cls
    bOverForm = True
  End If
End Sub

Private Sub Mtray_Click()
Dim test1 As Boolean
If Mtray.Checked = True Then
                            tray = False
                            test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Tray", "0")
                            Mtray.Checked = False
                            tray1.Timer1.Enabled = False
                            Shell_NotifyIcon NIM_DELETE, Tic
                            
                            
Else
                            tray = True
                            test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Tray", "1")
                            Mtray.Checked = True
                            
                            Tic.cbSize = Len(Tic)
                            Tic.Hwnd = tray1.Hwnd
                            Tic.uID = vbNull
                            Tic.uFlags = NIF_DOALL
                            Tic.uCallbackMessage = WM_MOUSEMOVE
                            Tic.hIcon = tray1.Icon
                            Tic.sTip = Form1.Caption & vbNullChar
                            Shell_NotifyIcon NIM_ADD, Tic
                            tray1.Timer1.Enabled = True
                          
                            
                            End If
End Sub

Private Sub openconv_Click()
Call Image3_Click
End Sub

Private Sub Opties_Click()
Form3.Show
End Sub

Private Sub startup_Click()
If startup.Checked = True Then
                            test1 = bDeleteRegValue(HKEY_CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "Syscal")
                            startup.Checked = False
                            
Else
                            tray = True
                            test2 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "Syscal", App.Path + "\Freesyscal.exe /tray")
                            startup.Checked = True
                            
                            End If
End Sub

Private Sub Switchclick_Click()
If switch1 = True Then
      test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Switch", "0")
      switch1 = False
Else
      test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Switch", "1")
      switch1 = True
      End If
If switch1 = False Then Label1.Caption = Getsym1() Else Label1.Caption = Getsym2()
If switch1 = False Then Label2.Caption = Getsym2() Else Label2.Caption = Getsym1()
If switch1 = False Then Label5.Caption = Getsym3() Else Label5.Caption = Getsym4()
If switch1 = False Then Label6.Caption = Getsym4() Else Label6.Caption = Getsym3()
If switch1 = False Then Label3.Caption = Getask1() Else Label3.Caption = Getask2()
If switch1 = False Then Label4.Caption = Getask2() Else Label4.Caption = Getask1()
End Sub

Private Sub text1_Change()
If Form1.Text1.Text = "" And Form1.Text2.Text = "" Then Form1.delall.Enabled = False Else Form1.delall.Enabled = True
End Sub

Private Sub Text1_Click()
If Clipboard.GetText = "" Then epaste.Enabled = False Else epaste.Enabled = True
If Text1.SelLength = 0 Then Delete.Enabled = False: ecopy.Enabled = False: ecut.Enabled = False Else ecopy.Enabled = True: ecut.Enabled = True: Delete.Enabled = True

End Sub

Private Sub Text1_GotFocus()
Option1.Value = 1
Option2.Value = 0
End Sub

Private Sub Text1_KeyPress(KeyAscii As Integer)
Dim stringer As String
Option1.Value = 1
Option2.Value = 0
If ActiveCal = True Then tikactive = 1
If KeyAscii = 13 Then
                        digit = False
                        If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, False)
                        If switch1 = False Then stringer = Getans(Text1.Text, True) Else stringer = Getans(Text1.Text, False)
                        
                          
                          If Left$(stringer, 1) = " " Then Text2.Text = Mid$(stringer, 2) Else Text2.Text = stringer
                                                      
                          If Not (Sform = "") And Not (stringer = "") Then Text1.Text = nulnul(Text1.Text)
                          If digit = True Then digit = False
                        If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, True)

                          
                       End If


End Sub


Private Sub Text2_Change()
If Form1.Text1.Text = "" And Form1.Text2.Text = "" Then Form1.delall.Enabled = False Else Form1.delall.Enabled = True

End Sub

Private Sub Text2_Click()
If Clipboard.GetText = "" Then epaste.Enabled = False Else epaste.Enabled = True
If Text2.SelLength = 0 Then Delete.Enabled = False: ecopy.Enabled = False: ecut.Enabled = False Else ecopy.Enabled = True: ecut.Enabled = True: Delete.Enabled = True

End Sub

Private Sub Text2_GotFocus()
Option1.Value = 0
Option2.Value = 1
End Sub

Private Sub Text2_KeyPress(KeyAscii As Integer)

Dim stringer As String
If ActiveCal = True Then tikactive = 1
Option1.Value = 0
Option2.Value = 1
If KeyAscii = 13 Then
                        If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, False)

                        digit = False
                        If switch1 = False Then stringer = Getans(Text2.Text, False) Else stringer = Getans(Text2.Text, True)
                        If Left$(stringer, 1) = " " Then Text1.Text = Mid$(stringer, 2) Else Text1.Text = stringer
                        If Not (Sform = "") And Not (stringer = "") Then Text2.Text = nulnul(Text2.Text)
                        If digit = True Then digit = False
                        If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, True)

                        End If

End Sub

Private Sub urllaunch_Click()
Dim iRet As Long
iRet = ShellExecute(Me.Hwnd, vbNullString, Configurl, vbNullString, "c:\", SW_SHOWNORMAL)
End Sub



Private Sub Wizard_Click()
WizardExpress.Show (0)
End Sub

Private Sub Form_MouseMove(Button As Integer, Shift As Integer, X As Single, Y As Single)

If Not bOverForm Then
    Cls
    bOverForm = True
  End If
End Sub


