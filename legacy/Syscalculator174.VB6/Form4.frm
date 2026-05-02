VERSION 5.00
Begin VB.Form Form4 
   BorderStyle     =   1  'Fixed Single
   Caption         =   "Form4"
   ClientHeight    =   2955
   ClientLeft      =   3300
   ClientTop       =   2445
   ClientWidth     =   4125
   Icon            =   "Form4.frx":0000
   LinkTopic       =   "Form4"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   2955
   ScaleWidth      =   4125
   ShowInTaskbar   =   0   'False
   Begin VB.Frame Frame1 
      Caption         =   "Frame1"
      Height          =   2175
      Left            =   120
      TabIndex        =   2
      Top             =   120
      Width           =   3975
      Begin VB.CommandButton Command5 
         Caption         =   "Command5"
         Height          =   375
         Left            =   1560
         TabIndex        =   10
         Top             =   1320
         Width           =   1095
      End
      Begin VB.CheckBox Check1 
         Caption         =   "Check1"
         Height          =   255
         Left            =   360
         TabIndex        =   9
         Top             =   1800
         Width           =   3375
      End
      Begin VB.TextBox Text1 
         Height          =   375
         Left            =   1680
         TabIndex        =   6
         Text            =   "Text1"
         Top             =   840
         Width           =   2055
      End
      Begin VB.CommandButton Command1 
         Caption         =   "Command1"
         Height          =   375
         Left            =   2760
         TabIndex        =   5
         Top             =   1320
         Width           =   975
      End
      Begin VB.TextBox Text2 
         Height          =   375
         Left            =   1680
         TabIndex        =   4
         Text            =   "Text2"
         Top             =   240
         Width           =   2055
      End
      Begin VB.CommandButton Command4 
         Caption         =   "Command4"
         Height          =   375
         Left            =   120
         TabIndex        =   3
         Top             =   1320
         Width           =   1215
      End
      Begin VB.Label Label1 
         Caption         =   "Label1"
         Height          =   375
         Left            =   240
         TabIndex        =   8
         Top             =   240
         Width           =   1095
      End
      Begin VB.Label Label2 
         Caption         =   "Label2"
         Height          =   375
         Left            =   240
         TabIndex        =   7
         Top             =   840
         Width           =   975
      End
   End
   Begin VB.CommandButton Command3 
      Caption         =   "Command3"
      Height          =   375
      Left            =   360
      TabIndex        =   1
      Top             =   2400
      Width           =   1455
   End
   Begin VB.CommandButton Command2 
      Caption         =   "Command2"
      Height          =   375
      Left            =   2520
      TabIndex        =   0
      Top             =   2400
      Width           =   1455
   End
End
Attribute VB_Name = "Form4"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Private old, ide, tup As Integer

Private Sub Check1_Click()
If Check1.Value = 1 Then
                            Check1.Value = 1
                            Check1.Enabled = False
                            defaultID = nodid
                            End If
                            
End Sub

Private Sub Command1_Click()
Dim numb As Integer
Dim temp As String
test = fncGetFileNametoOpen(, "nod files|*.nod", "*.nod")
numb = Asc(Left(test, 1))
If Not (numb = 0) Then Text1.Text = test Else Exit Sub
If Text2.Text = "" Then
                        temp = AddName(Text1.Text)
                        If Not temp = "-1" Then Text2.Text = temp
                        End If
If Not temp = "-1" Then Command5.Enabled = True
End Sub

Private Sub Command2_Click()
Dim name As String
If Text2.Text = "" Then Sysmsgbox.Form (recordempty): Exit Sub
Form3.Command4.Enabled = True
On Error GoTo 61
ename = filenod(Text1.Text)
Open ename For Input As #1
Close #1
If max = -1 Then nodid = 0: Form3.Combo1.Text = Text2.Text: Form3.Command6.Enabled = True
              
If Add = True Then
                For o = 0 To max
                If UCase$(Form3.Combo1.List(o)) = UCase$(Text2.Text) Then Sysmsgbox.Form (Exists): Exit Sub
                Next o
                max = max + 1
                Form3.Combo1.AddItem Text2.Text, max
                tel = Len(Apppaths + "\")
                If Left(Text1.Text, tel) = Apppaths + "\" Then Text1.Text = Mid(Text1.Text, tel + 1)
                tel = Len(Text1.Text)
                If UCase(Right$(Text1.Text, 3)) = "NOD" Then Text1.Text = Left(Text1.Text, tel - 4)
                filestring(max) = Text1.Text
                Add = False
                Form3.Command4.Enabled = True
                Unload Me
                Exit Sub
                End If
For o = 0 To max
If o = nodid Then GoTo over
If UCase$(Form3.Combo1.List(o)) = UCase$(Text2.Text) Then Sysmsgbox.Form (Exists): Exit Sub
over:
Next o
Form3.Combo1.List(nodid) = Text2.Text
tel = Len(Apppaths + "\")
If Left(Text1.Text, tel) = Apppaths + "\" Then Text1.Text = Mid(Text1.Text, tel + 1)
tel = Len(Text1.Text)
If UCase(Right$(Text1.Text, 3)) = "NOD" Then Text1.Text = Left(Text1.Text, tel - 4)
filestring(nodid) = Text1.Text
form4.Command4.Enabled = True
Form3.Combo1.ListIndex = nodid
Form3.Command6.Enabled = True
Unload Me
Exit Sub
61:
Sysmsgbox.Form (Text1.Text + isNotFound)
End Sub

Private Sub Command3_Click()
Add = False
defaultID = old
Unload form4
End Sub

Private Sub Command4_Click()
Dim fis As Integer
fis = MsgBox(Sure, vbYesNo + vbQuestion)
'delete
If fis = 7 Then Exit Sub
Form3.Command4.Enabled = True
If nodid < defaultID Then defaultID = defaultID - 1
If nodid = defaultID Then If nodid = 0 Then defaultID = 0 Else defaultID = nodid - 1
For o = nodid To max
If o = max Then Form3.Combo1.RemoveItem (max): filestring(max) = "": max = max - 1: If max > -1 Then Form3.Combo1.ListIndex = 0: Unload Me: Form3.Show: Exit Sub Else Unload Me: Form3.Command6.Enabled = False: Form3.Show: Exit Sub
Form3.Combo1.List(o) = Form3.Combo1.List(o + 1)
filestring(o) = filestring(o + 1)
Next o

End Sub

Private Sub Command5_Click()
Editor.Show
End Sub

Private Sub Form_Load()
old = defaultID
If max = -1 Then defaultID = nodid: Check1.Value = 1: Check1.Enabled = False
Call Languare(Lname, 7)
Form3.Enabled = False
If Add = False Then Text1.Text = filestring(nodid)
If Add = True Then Command5.Enabled = False: Command4.Enabled = False: Text2.Text = "": Text1.Text = "": Exit Sub Else Text2.Text = Form3.Combo1.List(nodid)
If nodid = defaultID Then Check1.Value = 1: Check1.Enabled = False

End Sub


Private Sub Form_Terminate()
Form3.Enabled = True
Unload form4
End Sub

Private Sub Form_Unload(Cancel As Integer)
Form3.Enabled = True
Unload form4
End Sub



Private Sub text1_Change()
If KeyAscii = 13 Then If Text2.Text = "" Then test2.Text = AddName(Text1.Text)
End Sub

