VERSION 5.00
Begin VB.Form WizardExpress 
   BorderStyle     =   1  'Fixed Single
   Caption         =   "Wizard"
   ClientHeight    =   2265
   ClientLeft      =   3915
   ClientTop       =   2805
   ClientWidth     =   1980
   Icon            =   "WizardExpress.frx":0000
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   2265
   ScaleWidth      =   1980
   ShowInTaskbar   =   0   'False
   Begin VB.Frame Status 
      Caption         =   "Status"
      Height          =   615
      Left            =   120
      TabIndex        =   3
      Top             =   1320
      Width           =   1695
      Begin VB.Label Label1 
         Alignment       =   2  'Center
         Appearance      =   0  'Flat
         BackColor       =   &H000000FF&
         Caption         =   "Start"
         BeginProperty Font 
            Name            =   "MS Sans Serif"
            Size            =   8.25
            Charset         =   0
            Weight          =   700
            Underline       =   0   'False
            Italic          =   0   'False
            Strikethrough   =   0   'False
         EndProperty
         ForeColor       =   &H80000008&
         Height          =   255
         Left            =   120
         TabIndex        =   4
         Top             =   240
         Width           =   1455
      End
   End
   Begin VB.Timer Timer1 
      Interval        =   500
      Left            =   120
      Top             =   2040
   End
   Begin VB.CommandButton OK 
      Caption         =   "OK"
      Height          =   375
      Left            =   600
      TabIndex        =   2
      Top             =   840
      Width           =   855
   End
   Begin VB.OptionButton Option2 
      Caption         =   "2"
      Height          =   375
      Left            =   120
      TabIndex        =   1
      Top             =   360
      Width           =   1335
   End
   Begin VB.OptionButton Option1 
      Caption         =   "1"
      Height          =   315
      Left            =   120
      TabIndex        =   0
      Top             =   0
      Value           =   -1  'True
      Width           =   1455
   End
   Begin VB.Menu file 
      Caption         =   "File"
      Begin VB.Menu exit 
         Caption         =   "&Exit"
      End
   End
   Begin VB.Menu tool 
      Caption         =   "Tool"
      Begin VB.Menu symbool 
         Caption         =   "Symbool"
      End
   End
End
Attribute VB_Name = "WizardExpress"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Public unconvert As String
Public exconvert As String
Public OptionAlg As String
Public ready As String
Public optalg As String
Public Curc As String



Private Sub exit_Click()
Unload WizardExpress
End Sub



Private Sub Form_Load()
Dim test2 As String
Dim test1 As Boolean
test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "symbool")
If test2 = "" Then test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "symbool", "0")
If test2 = "1" Then
                        symbool.Checked = True
                        End If

ActiveForm2 = True
Call Languare(Lname, 4)
Call algemeen
OptionAlg = True
optalg = True
If Not (Getsym1() = "") Then Curc = Getsym1()
If Not (Getsym3() = "") Then Curc = Getsym3()
If Getsym1() = "" And Getsym3() = "" Then
            Curc = Getask1()
            End If
End Sub



Private Sub Form_Terminate()
Unload Me
End Sub

Private Sub Form_Unload(Cancel As Integer)
ActiveForm2 = False
End Sub

Private Sub OK_Click()
Dim i, o, ins As Integer
Dim uio As Boolean
Dim rij As Integer
Dim test4 As Integer
uio = False
rij = 0
Convert = ""
unconvert = Clipboard.GetText
If unconvert = "" Then Exit Sub
If Not Asc(Right$(unconvert, 1)) = 10 Then unconvert = unconvert + Chr(13) + Chr(10)
' 9 = new kolom
' 10 + 13 = new rij
' Eerste rij ..is gewoon omschrijving of letters .. dus Check eerst of meer dan drie letters. minder dan 3 letters is symbool.

o = 1

For i = 1 To Len(unconvert)
If Asc(Mid$(unconvert, i, 1)) = 9 Then
                                        exconvert = Mid$(unconvert, o, i - 1 - (o - 1))
                                                                
                                       If Not (Smath(0) = "") Then
                                                                    If ins = 2 Then GoTo over
                                                                    ins = 0
                                                                    exconvert = Incontrol(exconvert, ins, rij)
                                                                    If ins = 0 Then GoTo overslaan6
                                                                    If ins = 2 Then GoTo over
                                                                     End If
                                        uio = True
                                                                  
                                       If OptionAlg = True Then exconvert = Getans(exconvert, True) Else: exconvert = Getans(exconvert, False)
                                       digit = False
                                       If Left(exconvert, 1) = " " Then exconvert = Mid$(exconvert, 2)
                                                             
                                       Call symbenb
                                                                         
overslaan6:
                                    
                                    Convert = Convert + exconvert + Chr(9)
                                     
                                    o = i + 1
                                    GoTo over
                                    End If
                                    
                                        
If Asc(Mid$(unconvert, i, 1)) = 10 Then
                                            ins = 0
                                            uio = True
                                            exp1 = Mid$(unconvert, o + i - 2 - (o - 1), 2)
                                                     
                                            exconvert = Mid$(unconvert, o, i - 2 - (o - 1))
                                                     
                                        If Not (Smath(0) = "") Then
                                                                    exconvert = Incontrol(exconvert, ins, rij)
                                                                    rij = rij + 1
                                                                    If ins = 0 Then GoTo overslaan7
                                                                    If ins = 2 Then o = i + 1: ins = 0: GoTo over
                                                                    End If
                                        If OptionAlg = True Then exconvert = Getans(exconvert, True) Else exconvert = Getans(exconvert, False)
                                        If Left(exconvert, 1) = " " Then exconvert = Mid$(exconvert, 2)
                                        digit = False
                                        Call symbenb
overslaan7:
                                        
                                        Convert = Convert + exconvert + exp1
                                        o = i + 1

                                        End If
over:
Next i
If uio = False Then If OptionAlg = True Then Convert = Mid$(Getans(unconvert, True), 2) Else Convert = Mid$(Getans(unconvert, False), 2): Call symbenb
 Clipboard.Clear
    'Sets the Text from rtfText onto the Clipboard
 Clipboard.SetText Convert
 ready = Convert
 Label1.BackColor = &HFF00&
 Label1.Caption = eready
End Sub

Function Incontrol(Convert As String, Status As Integer, rij As Integer) As String
  Dim e, i, o, ascii, L As Integer
  Dim uio As Boolean
  uio = False
  ' This Experminet is not tested
  ' First three charahters has been i
  ' Status 1: maximum 3
  ' Status 2: meer dan 3 en rij=0
  ' Status 0: meer dan 3 en rij=1 of meer (invalid)
 Dim toegestaan As Integer
 Dim PrevASCI As Integer
  For i = 1 To Len(Convert)
                     For ascii = 48 To 57 'cijfers
                    If Asc(Mid$(Convert, i, 1)) = ascii Then
                                                            If toegestaan = 1 Then
                                                                                   Incontrol = Mid$(Convert, i - 1)
                                                                                   Status = 1
                                                                                   Exit Function
                                                                                   End If
                                                            L = L + 1
                                                             
                                                             
                                                             
                                                            End If
                '    If ascii = 32 Then
                 '
                    Next ascii
                If Asc(Mid$(Convert, i, 1)) = 32 Then
                                                      If i = 1 Then o = i + 1: GoTo verder2
                                                      If i = o Then o = o + 1: GoTo verder2
                                                      toegestaan = toegestaan + 1
                                                      End If
                If Asc(Mid$(Convert, i, 1)) = 46 Then If Not L = 0 And toegestaan = 0 Then L = L + 1
                 If Asc(Mid$(Convert, i, 1)) = 44 Then L = L + 1
                 If Asc(Mid$(Convert, i, 1)) = Asc("-") Then L = L + 1
                c = c + 1
verder2:
  PrevASCI = Asc(Mid$(Convert, i, 1))
  Next i
'geen cijfers gevonden.
i = i - 1
e = Len(Convert) - L
If e > 0 Then
             If toegestaan >= 1 Then Status = 1: Incontrol = Left$(Convert, L): Exit Function
             
             End If
If e = 0 Then
             If Not Len(Convert) = 0 Then Status = 1: Incontrol = Convert: Exit Function
             If Len(Convert) = L Then Incontrol = Convert: Status = 0: Exit Function
             End If
rest:
  If rij = 0 Then Status = 2 Else Status = 0
  Incontrol = Convert
End Function

Private Sub Option1_Click()
OptionAlg = True
End Sub

Private Sub Option2_Click()
OptionAlg = False
End Sub
Private Sub algemeen()
If TOPilse = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, True) Else Call WindowsAPI.AlwaysOnTop(WizardExpress, False)
If Not (Getsym1() = "") Then Option1.Caption = Getsym1(): symbool.Enabled = True
If Not (Getsym3() = "") Then Option1.Caption = Getsym3(): symbool.Enabled = True
If Not (Getsym2() = "") Then Option2.Caption = Getsym2(): symbool.Enabled = True
If Not (Getsym4() = "") Then Option2.Caption = Getsym4(): symbool.Enabled = True
If Getsym1() = "" And Getsym3() = "" Then
            Option1.Caption = Getask1()
            Option2.Caption = Getask2()
            Curc = Getask1()
            symbool.Enabled = False
            End If
End Sub


Private Sub symbool_Click()
Dim test1 As Boolean
If symbool.Checked = True Then
                                 test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "symbool", "0")
                                 symbool.Checked = False
Else
                                 test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "symbool", "1")
                                 symbool.Checked = True
                                 End If
End Sub

Private Sub Timer1_Timer()
Dim raad As String
On Error GoTo er1

raad = Clipboard.GetText
If Not (ready = raad) Then Label1.BackColor = &HFF&: Label1.Caption = estart
If Not (optalg = OptionAlg) Then Label1.BackColor = &HFF&: Label1.Caption = estart: optalg = OptionAlg
If Not (Getsym1() = "") Then
                             If Not (Curc = Getsym1()) Then Call algemeen: Label1.BackColor = &HFF&: Label1.Caption = estart: Curc = Getsym1()
                             End If
If Not (Getsym3() = "") Then
                             If Not (Curc = Getsym3()) Then Call algemeen: Label1.BackColor = &HFF&: Label1.Caption = estart: Curc = Getsym3()
                             End If
If Getsym1() = "" And Getsym3() = "" Then
            If Not (Curc = Getask1()) Then Call algemeen: Label1.BackColor = &HFF&: Label1.Caption = estart: Curc = Getask1()
                             End If
er1:
End Sub

Private Sub symbenb()
        If symbool.Enabled And symbool.Checked Then
                                                                  If OptionAlg = True Then
                                                                                            If Not (Getsym2() = "") Then exconvert = Getsym2() + " " + exconvert
                                                                                            If Not (Getsym4() = "") Then exconvert = exconvert + " " + Getsym4()
                                                                  Else
                                                                                            If Not (Getsym1() = "") Then exconvert = Getsym1() + " " + exconvert
                                                                                            If Not (Getsym3() = "") Then exconvert = exconvert + " " + Getsym3()
                                                                                            End If
                                                                  End If
       
                              
End Sub
