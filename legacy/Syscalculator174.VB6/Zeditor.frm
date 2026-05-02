VERSION 5.00
Begin VB.Form Editor 
   Caption         =   "Editor - "
   ClientHeight    =   5370
   ClientLeft      =   5145
   ClientTop       =   5145
   ClientWidth     =   8070
   Icon            =   "Zeditor.frx":0000
   LinkTopic       =   "Form1"
   ScaleHeight     =   5370
   ScaleWidth      =   8070
   Visible         =   0   'False
   Begin VB.TextBox Text1 
      Height          =   4695
      Left            =   0
      MultiLine       =   -1  'True
      ScrollBars      =   3  'Both
      TabIndex        =   0
      Top             =   0
      Width           =   8055
   End
   Begin VB.Menu file 
      Caption         =   "File"
      Begin VB.Menu new 
         Caption         =   "New"
         Shortcut        =   ^N
      End
      Begin VB.Menu open 
         Caption         =   "Open"
         Shortcut        =   ^O
      End
      Begin VB.Menu save 
         Caption         =   "Save"
         Shortcut        =   ^S
      End
      Begin VB.Menu SaveAs 
         Caption         =   "Save As"
      End
      Begin VB.Menu line1 
         Caption         =   "-"
         Visible         =   0   'False
      End
      Begin VB.Menu lastused 
         Caption         =   "&1"
         Index           =   1
         Visible         =   0   'False
      End
      Begin VB.Menu lastused 
         Caption         =   "&2"
         Index           =   2
         Visible         =   0   'False
      End
      Begin VB.Menu lastused 
         Caption         =   "&3"
         Index           =   3
         Visible         =   0   'False
      End
      Begin VB.Menu line 
         Caption         =   "-"
      End
      Begin VB.Menu close 
         Caption         =   "Exit"
      End
   End
   Begin VB.Menu edit 
      Caption         =   "Edit"
      Begin VB.Menu Undoclick 
         Caption         =   "Undo"
         Enabled         =   0   'False
         Shortcut        =   ^Z
      End
      Begin VB.Menu Redoclick 
         Caption         =   "Redo"
         Enabled         =   0   'False
      End
      Begin VB.Menu sep61 
         Caption         =   "-"
      End
      Begin VB.Menu cut 
         Caption         =   "Cut"
         Shortcut        =   ^X
      End
      Begin VB.Menu copy 
         Caption         =   "Copy"
         Shortcut        =   ^C
      End
      Begin VB.Menu paste 
         Caption         =   "Paste"
         Enabled         =   0   'False
         Shortcut        =   ^V
      End
      Begin VB.Menu Delete 
         Caption         =   "Delete"
         Shortcut        =   {DEL}
      End
      Begin VB.Menu sep 
         Caption         =   "-"
      End
      Begin VB.Menu eselect 
         Caption         =   "Select &All"
         Shortcut        =   ^A
      End
      Begin VB.Menu se4 
         Caption         =   "-"
      End
      Begin VB.Menu mnufind 
         Caption         =   "Find"
         Shortcut        =   ^F
      End
      Begin VB.Menu findnext 
         Caption         =   "Find Next"
         Shortcut        =   {F3}
      End
      Begin VB.Menu mnureplace 
         Caption         =   "Replace"
         Shortcut        =   ^H
      End
   End
   Begin VB.Menu help 
      Caption         =   "Help"
      Begin VB.Menu help2 
         Caption         =   "Help"
         Shortcut        =   {F1}
      End
      Begin VB.Menu help1 
         Caption         =   "Bugreports"
      End
      Begin VB.Menu help0 
         Caption         =   "Tiedragon"
      End
      Begin VB.Menu about 
         Caption         =   "About"
      End
   End
End
Attribute VB_Name = "Editor"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Dim ChangedText As Integer
Const maxUndo = 50 'Maximum num of undos
Dim gblnIgnoreChange As Boolean
Dim gintIndex As Integer
Dim gstrStack(maxUndo) As String
Dim stackBK(maxUndo) As String
Dim i As Integer
Const max = 3


Private Sub about_Click()
If onlyeditor = 0 Then onlyeditor = 3
Load frmAbout
frmAbout.Show (0)
End Sub

Private Sub findnext_Click()
Dim S As String
  If Text1.SelLength > 0 Then S = Text1.SelText Else S = "Find Text"
  ShowFind Me, Text1, FR_SHOWHELP, S, True, "Find Next"

End Sub

Private Sub help0_Click()
Dim iRet As Long
iRet = ShellExecute(Me.Hwnd, vbNullString, Configurl, vbNullString, "c:\", SW_SHOWNORMAL)

End Sub

Private Sub Help2_Click()
Dim iRet As Long
iRet = ShellExecute(Me.Hwnd, vbNullString, Configur3, vbNullString, "c:\", SW_SHOWNORMAL)

End Sub

Private Sub close_Click()
Unload Me
End Sub
Private Sub copy_Click()
Clipboard.Clear
Clipboard.SetText Mid(Text1.Text, Text1.SelStart + 1, Text1.SelLength)
End Sub
Private Sub cut_Click()
Clipboard.Clear
strleft = Left(Text1.Text, Text1.SelStart)
strRight = Mid(Text1.Text, Text1.SelStart + Text1.SelLength + 1)
Clipboard.SetText Mid(Text1.Text, Text1.SelStart + 1, Text1.SelLength)
Text1 = strleft & strRight
End Sub

Private Sub Delete_Click()
strleft = Left(Text1.Text, Text1.SelStart)
strRight = Mid(Text1.Text, Text1.SelStart + Text1.SelLength + 1)
Text1 = strleft & strRight
End Sub

Private Sub edit_Click()
If Clipboard.GetText = "" Then Paste.Enabled = False Else Paste.Enabled = True
If Text1.SelLength = 0 Then Delete.Enabled = False: Copy.Enabled = False: cut.Enabled = False Else Copy.Enabled = True: cut.Enabled = True: Delete.Enabled = True
End Sub

Private Sub eselect_Click()
      
        Editor.Text1.SelStart = 0
        Editor.Text1.SelLength = Len(Editor.Text1.Text)
        
        
End Sub

Private Sub Form_Load()

Dim tel As Integer
Dim ename, name As String
Apppaths = GetDataFolder(Editor, "Syscalculator")
Call Addcombo(Editor)
Call Languare(Lname, 8)
onlyedname = Command$
tel = Len(onlyedname)
Dim usedlast(3) As String
For n = 0 To max - 1
usedlast(n) = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & (n + 1))
If Not Mid(usedlast(n), 3) = "" Then
                            With lastused(n + 1)
                             .Caption = "&" & (n + 1) & ". " & visiblefile(usedlast(n))
                             .Visible = True
                            End With
                            line1.Visible = True
                            End If
Next n
If UCase(Right$(onlyedname, 3)) = "NOD" Then onlyedname = Left(onlyedname, tel - 4)
ename = filenod(onlyedname)
If onlyedname = ".nod" Then Editor.Show: Exit Sub
On Error GoTo 51
Openfile (ename)
Close #1
Edname = onlyedname
Editor.Visible = True
Editor.Show

Exit Sub
51:
MsgBox ename + " " + isNotFound
End Sub

Private Sub Form_QueryUnload(Cancel As Integer, UnloadMode As Integer)
If Edname = "" Then
                    Form_Unload (0)
                    Exit Sub
                    End If
If ChangedText = 1 Then Question = MsgBox(textChanged, vbYesNoCancel, editornaam)
If Question = 2 Then Cancel = 1: Exit Sub        'Avbryt
If Question = 6 Then SaveTextFile    'Ja
If Question = 7 Then
                     Form_Unload (0)
                     Exit Sub
                     End If
Dim a As Boolean
a = OpenNOD(Edname, 3)
If a = False Then Cancel = 1: Exit Sub

Form_Unload (0)
End Sub

Private Sub Form_Resize()
Text1.Top = 0
Text1.Left = 0
Text1.Height = Editor.Height - 689
Text1.Width = Editor.Width - 120
End Sub

Private Sub Form_Unload(Cancel As Integer)
Unload Me
End Sub

Private Sub help1_Click()
Dim iRet As Long
iRet = ShellExecute(Me.Hwnd, vbNullString, Configur2 + " " & App.Major & "." & App.Minor & "." & App.Revision, vbNullString, "c:\", SW_SHOWNORMAL)

End Sub

Private Sub mnufind_Click()
 Dim S As String
  If Text1.SelLength > 0 Then S = Text1.SelText Else S = "Find Text"
  ShowFind Me, Editor.Text1, FR_SHOWHELP, S
End Sub

Private Sub mnureplace_Click()
 Dim S As String
  If Text1.SelLength > 0 Then S = Text1.SelText Else S = "Find Text"
  ShowFind Me, Editor.Text1, FR_SHOWHELP, S, True, "Replace Text"
End Sub

Private Sub new_Click()
If ChangedText = 1 Then what = MsgBox(textChanged, vbYesNoCancel, editornaam)
If what = 2 Then Exit Sub      'Avbryt
If what = 6 Then SaveTextFile  'Ja
Editor.Caption = editornaam + " - " + zondertitel
Editor.Text1.Text = ""
Edname = ""
Call resetundo
End Sub
Private Sub open_Click()
On Error GoTo Felhantering
If ChangedText = 1 Then Question = MsgBox(textChanged, vbYesNoCancel, editornaam)
If Question = 2 Then Exit Sub        'Avbryt
If Question = 6 Then SaveAsTextFile    'Ja
Call resetundo
gblnIgnoreChange = True
OpenTextFile
gblnIgnoreChange = False
gstrStack(0) = Text1.Text
ChangedText = "0"
salastused
Exit Sub
Felhantering:
MsgBox Edname + " " + isNotFound
End Sub
Private Sub lastused_Click(Index As Integer)
usedlast = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & Index)
Edname = usedlast
On Error GoTo 51
Editor.Text1.Text = ""
Call resetundo
gblnIgnoreChange = True
Openfile (Edname)
gblnIgnoreChange = False
gstrStack(0) = Text1.Text
Editor.Caption = editornaam + " - " + Edname
ChangedText = "0"
salastused
Exit Sub
51:
MsgBox Edname + " " + isNotFound
End Sub
Private Sub Paste_Click()
strleft = Left(Editor.Text1, Text1.SelStart)
strRight = Mid(Editor.Text1, Text1.SelStart + Text1.SelLength + 1)
Text1 = strleft & Clipboard.GetText & strRight
End Sub

Private Sub Pathen_Change()

End Sub

Private Sub save_Click()
SaveTextFile
ChangedText = "0"
salastused
End Sub
Private Sub SaveAs_Click()

SaveAsTextFile

ChangedText = "0"
salastused
End Sub
Private Sub text1_Change()
ChangedText = 1
g = maxUndo 'Initialize this to the max number of undos

    If Not gblnIgnoreChange Then
        gintIndex = gintIndex + 1
        
        If gintIndex >= maxUndo + 1 Then 'If > max num of undos reached
        
            For b = 0 To maxUndo 'Copy the undo info to a backup array
                stackBK(b) = gstrStack(b)
            Next b
            
            For i = 0 To maxUndo 'Copy the backup array info back to the original, but in a different order
                If g >= 1 Then
                g = g - 1
                gstrStack(g) = stackBK(g + 1) 'gstrstack(49) = stackBK(50) get it??
                End If
            Next i
            
            gintIndex = maxUndo 'Set it to the max number of undos
            
        End If
        gstrStack(gintIndex) = Text1.Text
        If gintIndex <= 1 Then
          Redoclick.Enabled = True
          Undoclick.Enabled = True
        End If
    End If
End Sub
Sub salastused()


Dim usedlast(max) As String
For n = 0 To max - 1
usedlast(n) = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & (n + 1))
Next n
Dim okfile As String
okfile = visiblefile(Edname)
For ong = max - 1 To 2 Step -1
If Trim(Mid(lastused(ong).Caption, 4)) = Left(okfile, Len(okfile) - 1) Then
                    For n = ong To 2 Step -1
                    With lastused(n)
                     .Caption = "&" & n & ". " & visiblefile(usedlast(n - 2))
                     .Visible = True
                    End With
                    Call bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & n, usedlast(n - 2))
                    Next n
                    Call addnr1
                    Exit Sub
                    End If
Next ong
If Not Trim(Mid(lastused(1).Caption, 4)) = Left(okfile, Len(okfile) - 1) Then
                    For ns2 = max To 2 Step -1
                    If Not usedlast(ns2 - 2) = "" Then
                                                 With lastused(ns2)
                                                    .Caption = "&" & ns2 & ". " & visiblefile(usedlast(ns2 - 2))
                                                    .Visible = True
                                                 End With
                                                 Call bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & ns2, usedlast(ns2 - 2))
                   
                                                 End If
                    Next ns2
                    
                    Call addnr1
                    End If



End Sub
Sub addnr1()
Call bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast1", Trim(Edname))
                    lastused(1).Caption = "&1. " + visiblefile(Trim(Edname))
                    lastused(1).Visible = True
                    line1.Visible = True
                   
End Sub
Function visiblefile(ename As String) As String
ename = Trim(ename)
If Len(ename) > 20 Then
       getal = InStr(1, Right(ename, 15), "\")
       
       visiblefile = Mid(ename, 1, 3) + "..." + Mid(Right(ename, 15), InStr(1, Right(ename, 15), "\"))
       End If
visiblefile = ename
End Function

Private Sub UndoClick_Click()
 'This says that if the Index is = to 0, then It shouldn't undo anymore
    If gintIndex = 0 Then Exit Sub
    
    'This is the basic undo stuff.
    gblnIgnoreChange = True
    gintIndex = gintIndex - 1
    On Error Resume Next
    Text1.Text = gstrStack(gintIndex)
    gblnIgnoreChange = False
End Sub

Private Sub resetundo()
For i = 0 To maxUndo
gstrStack(i) = ""
stackBK(i) = ""
Next i
gintIndex = 0
Redoclick.Enabled = False
Undoclick.Enabled = False
End Sub
Private Sub Redoclick_Click()
 If gintIndex < maxUndo Then ' max undo level is reached, do not redo
        gblnIgnoreChange = True
        gintIndex = gintIndex + 1
        On Error Resume Next
        Text1.Text = gstrStack(gintIndex)
        gblnIgnoreChange = False
    End If
End Sub
