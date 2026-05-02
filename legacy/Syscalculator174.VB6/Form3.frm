VERSION 5.00
Begin VB.Form Form3 
   BorderStyle     =   1  'Fixed Single
   Caption         =   "Form3"
   ClientHeight    =   3195
   ClientLeft      =   4080
   ClientTop       =   1155
   ClientWidth     =   4680
   Icon            =   "Form3.frx":0000
   LinkTopic       =   "Form3"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   3195
   ScaleWidth      =   4680
   ShowInTaskbar   =   0   'False
   Begin VB.CommandButton Command4 
      Caption         =   "Apply"
      Enabled         =   0   'False
      Height          =   375
      Left            =   3360
      TabIndex        =   8
      Top             =   2760
      Width           =   1095
   End
   Begin VB.CommandButton Command3 
      Caption         =   "Cancel"
      Height          =   375
      Left            =   1800
      TabIndex        =   7
      Top             =   2760
      Width           =   1335
   End
   Begin VB.CommandButton Command2 
      Caption         =   "Ok"
      Height          =   375
      Left            =   480
      TabIndex        =   6
      Top             =   2760
      Width           =   1215
   End
   Begin VB.Frame Frame1 
      Caption         =   "Frame1"
      Height          =   2295
      Left            =   240
      TabIndex        =   0
      Top             =   240
      Width           =   4215
      Begin VB.CommandButton Command7 
         Caption         =   "Command7"
         Height          =   375
         Left            =   3000
         TabIndex        =   11
         Top             =   1680
         Width           =   975
      End
      Begin VB.CommandButton Command6 
         Caption         =   "Porperty"
         Height          =   375
         Left            =   1680
         TabIndex        =   10
         Top             =   1680
         Width           =   1095
      End
      Begin VB.CommandButton Command5 
         Caption         =   "Add"
         Height          =   375
         Left            =   120
         TabIndex        =   9
         Top             =   1680
         Width           =   1215
      End
      Begin VB.ComboBox Combo1 
         Height          =   315
         Left            =   2160
         TabIndex        =   5
         Top             =   1200
         Width           =   1695
      End
      Begin VB.CommandButton Command1 
         Caption         =   "Command1"
         Height          =   375
         Left            =   2280
         TabIndex        =   4
         Top             =   480
         Width           =   1335
      End
      Begin VB.TextBox Text1 
         Height          =   375
         Left            =   240
         TabIndex        =   2
         Text            =   "Text1"
         Top             =   480
         Width           =   1575
      End
      Begin VB.Label Label2 
         Caption         =   "Label2"
         Height          =   375
         Left            =   240
         TabIndex        =   3
         Top             =   1200
         Width           =   1695
      End
      Begin VB.Label Label1 
         Caption         =   "Label1"
         Height          =   375
         Left            =   240
         TabIndex        =   1
         Top             =   240
         Width           =   1455
      End
   End
End
Attribute VB_Name = "Form3"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Dim old As String

Private Sub Combo1_Click()
nodid = Combo1.ListIndex
End Sub

Private Sub Combo1_KeyDown(KeyCode As Integer, Shift As Integer)
If KeyCode = vbKeyDelete Then
        Combo1.Text = ""
        KeyCode = 0
    End If

End Sub

Private Sub Combo1_KeyPress(KeyAscii As Integer)
  Dim strSearchText As String
    Dim strEnteredText As String
    Dim intLength As Integer
    Dim intIndex As Integer
    Dim intCounter As Integer
    On Error GoTo ErrorHandler


    With Combo1


        If .SelStart > 0 Then
            strEnteredText = Left(.Text, .SelStart)
        End If


        Select Case KeyAscii
            Case vbKeyReturn


            If .ListIndex > -1 Then
                .SelStart = 0
                .SelLength = Len(.List(.ListIndex))
                Exit Sub
            End If
            Case vbKeyEscape, vbKeyDelete
            .Text = ""
            KeyAscii = 0
            Exit Sub
            Case vbKeyBack


            If Len(strEnteredText) > 1 Then
                strSearchText = LCase(Left(strEnteredText, Len(strEnteredText) - 1))
            Else
                strEnteredText = ""
                KeyAscii = 0
                .Text = ""
                Exit Sub
            End If
            Case Else
            strSearchText = LCase(strEnteredText & Chr(KeyAscii))
        End Select
    intIndex = -1
    intLength = Len(strSearchText)


    For intCounter = 0 To .ListCount - 1


        If LCase(Left(.List(intCounter), intLength)) = strSearchText Then
            intIndex = intCounter
            Exit For
        End If
    Next intCounter


    If intIndex > -1 Then
        .ListIndex = intIndex
        .SelStart = Len(strSearchText)
        .SelLength = Len(.List(intIndex)) - Len(strSearchText)
    Else
        Beep
    End If
End With
KeyAscii = 0
Exit Sub
ErrorHandler:
KeyAscii = 0
Beep

End Sub

Private Sub Combo1_LostFocus()
Combo1.SelLength = 0
End Sub

Private Sub Command1_Click()

Dim numb As Integer
test = fncGetFileNametoOpen(, "lng files|*.lng", "*.lng")
numb = Asc(Left(test, 1))
If Not (numb = 0) Then Text1.Text = test
Command4.Enabled = True
End Sub

Private Sub Command2_Click()
If max = -1 Then
                
                 If old > -1 Then
                                 Text1.Text = saveconfig(Text1.Text, Combo1)
                                 End If
                If tray = True Then Unload tray1
                Unload Form1
                Unload Me
                Exit Sub
                End If
If old = -1 Then old = max: Text1.Text = saveconfig(Text1.Text, Combo1): Call form1cleanup: Form1.Show: Unload Me: Exit Sub
If Form1.Visible = False Then Call form1cleanup: Form1.Show
flags = 5
Text1.Text = saveconfig(Text1.Text, Combo1)
Unload Me
End Sub

Private Sub Command3_Click()
If Command4.Enabled = True Then max = old Else old = max
If max = -1 Then
                If tray = True Then Unload tray1
                Unload Form1
                Unload Me
                Exit Sub
                End If
Addcombo (1)

flags = 1
Unload Me
End Sub

Private Sub Command4_Click()
old = max
Text1.Text = saveconfig(Text1.Text, Combo1)
Combo1.Clear
If max > -1 Then Call comb
Command4.Enabled = False
'nodid = defaultID
flags = 5
If max > -1 Then Call form1cleanup
Call Languare(Lname, 6)
Call Languare(Lname, 1)
If max > -1 Then Combo1.ListIndex = Form1.Combo1.ListIndex
If ActiveForm2 = True Then Call Languare(Lname, 4)
If ActiveCal = True Then Call Languare(Lname, 5)
'If Form1.Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, False)
flags = 1
Form3.Show
End Sub

Private Sub Command5_Click()
If max = 0 Then defaultID = 0
Add = True
form4.Show
End Sub

Private Sub Command6_Click()

form4.Show
End Sub


Private Sub Command7_Click()
Call resort

End Sub

Private Sub Form_Load()

fORMVALUE3 = True
old = max
 Call Languare(Lname, 6)
 If max = -1 Then Form3.Command6.Enabled = False
 Form1.Enabled = False
 SetForms (False)
Call comb
Command4.Enabled = False
nodid = Form1.Combo1.ListIndex
Combo1.ListIndex = nodid
End Sub
Private Sub comb()
Dim comm, Data, def As String
Dim OK As Integer
i = 0
On Error GoTo geenconfig
Open UserDataFilePath("freesyscal.cfg") For Input As #1
Do Until EOF(1)
 Input #1, comm, Data, def
 If Left$(comm, 1) = "'" Then GoTo overstap 'rem only
 If comm = "[lang]" Then Lname = Data: Text1.Text = Lname: GoTo overstap
 
 If Not (comm = "") Then Combo1.AddItem comm, i
                        
 If Not (Data = "") Then filestring(i) = Data
 If def = "*" Then defaultID = i
 i = i + 1
overstap:
  
  Loop
  Close #1
  max = i - 1
Exit Sub
geenconfig:
Dim Status As Boolean
Status = MsgBox(withoutcfg, vbCritical, errorf)
End Sub
''Private Sub saveconfig()
''Dim comm, Data, def As String
''Dim OK As Integer
''Dim tel As Integer
''tel = Len(Apppaths + "\")
''If Left(Text1.Text, tel) = Apppaths + "\" Then Text1.Text = Mid(Text1.Text, tel + 1)
''On Error GoTo geenconfig
''Open Apppaths + "\" + "freesyscal.cfg" For Output As #1
''Write #1, "[lang]", Text1.Text, ""
''For o = 0 To max
''If defaultID = o Then def = "*"
''Write #1, Combo1.List(o), filestring(o), def
''def = ""
''Next o
''Lname = Text1.Text
  
''  Close #1
''Exit Sub
''geenconfig:
''
''End Sub

Private Sub Form_Terminate()
Unload Me
End Sub

Private Sub Form_Unload(Cancel As Integer)
If flag = 0 And Command = "/Once" And intro = True Then Form2.Show: flags = 1
fORMVALUE3 = False
If max > -1 Then Form1.Enabled = True
   SetForms (True)
Apply = True
Unload Me
End Sub

Private Sub text1_Change()
Command4.Enabled = True
End Sub
