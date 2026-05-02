VERSION 5.00
Begin VB.Form standard 
   BorderStyle     =   1  'Fixed Single
   Caption         =   "Calculcator"
   ClientHeight    =   3225
   ClientLeft      =   1725
   ClientTop       =   1965
   ClientWidth     =   4545
   ForeColor       =   &H00000000&
   Icon            =   "Anical1.frx":0000
   KeyPreview      =   -1  'True
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   PaletteMode     =   1  'UseZOrder
   ScaleHeight     =   3225
   ScaleWidth      =   4545
   ShowInTaskbar   =   0   'False
   Begin VB.Frame Frame1 
      Height          =   3135
      Left            =   0
      TabIndex        =   0
      Top             =   0
      Width           =   4455
      Begin VB.TextBox Memorystatus 
         BackColor       =   &H8000000F&
         Height          =   285
         Left            =   3000
         Locked          =   -1  'True
         TabIndex        =   29
         Text            =   "0"
         Top             =   2760
         Width           =   1215
      End
      Begin VB.CommandButton Command2 
         Caption         =   "sqrt"
         Height          =   615
         Index           =   8
         Left            =   3720
         TabIndex        =   27
         Top             =   2040
         Width           =   495
      End
      Begin VB.CommandButton Command2 
         Caption         =   "1/x"
         Height          =   615
         Index           =   7
         Left            =   3720
         TabIndex        =   26
         Top             =   1440
         Width           =   495
      End
      Begin VB.CommandButton Command2 
         Caption         =   "-/+"
         Height          =   615
         Index           =   6
         Left            =   3720
         TabIndex        =   25
         Top             =   840
         Width           =   495
      End
      Begin VB.CommandButton Command7 
         Caption         =   "MS"
         Height          =   615
         Index           =   1
         Left            =   3120
         TabIndex        =   24
         Top             =   2040
         Width           =   615
      End
      Begin VB.CommandButton Convert 
         Caption         =   "Convert"
         Height          =   615
         Left            =   1920
         TabIndex        =   23
         Top             =   2040
         Width           =   1215
      End
      Begin VB.CommandButton Command2 
         Caption         =   "%"
         Height          =   615
         Index           =   5
         Left            =   3720
         TabIndex        =   22
         Top             =   240
         Width           =   495
      End
      Begin VB.CommandButton Command7 
         Caption         =   "M +"
         Height          =   615
         Index           =   0
         Left            =   3120
         TabIndex        =   21
         Top             =   1440
         Width           =   615
      End
      Begin VB.CommandButton Command7 
         Caption         =   "MC"
         Height          =   615
         Index           =   5
         Left            =   3120
         TabIndex        =   20
         Top             =   840
         Width           =   615
      End
      Begin VB.CommandButton Command7 
         Caption         =   "MR"
         Height          =   615
         Index           =   4
         Left            =   3120
         TabIndex        =   19
         Top             =   240
         Width           =   615
      End
      Begin VB.CommandButton Command2 
         Caption         =   "*"
         Height          =   615
         Index           =   3
         Left            =   2520
         TabIndex        =   18
         Top             =   1440
         Width           =   615
      End
      Begin VB.CommandButton Command2 
         Caption         =   "--"
         Height          =   615
         Index           =   1
         Left            =   2520
         TabIndex        =   17
         Top             =   840
         Width           =   615
      End
      Begin VB.CommandButton Command4 
         Caption         =   "CE"
         Height          =   615
         Left            =   2520
         TabIndex        =   16
         Top             =   240
         Width           =   615
      End
      Begin VB.CommandButton Command2 
         Caption         =   "="
         Height          =   615
         Index           =   4
         Left            =   1320
         TabIndex        =   15
         Top             =   2040
         Width           =   615
      End
      Begin VB.CommandButton Command2 
         Caption         =   "+"
         Height          =   615
         Index           =   0
         Left            =   1920
         TabIndex        =   14
         Top             =   1440
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   ","
         Height          =   615
         Index           =   10
         Left            =   720
         TabIndex        =   13
         Top             =   2040
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "0"
         Height          =   615
         Index           =   0
         Left            =   120
         TabIndex        =   12
         Top             =   2040
         Width           =   615
      End
      Begin VB.CommandButton Command2 
         Caption         =   "/"
         Height          =   615
         Index           =   2
         Left            =   1920
         TabIndex        =   11
         Top             =   840
         Width           =   615
      End
      Begin VB.CommandButton Command3 
         Caption         =   "C"
         Height          =   615
         Left            =   1920
         TabIndex        =   10
         Top             =   240
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "9"
         Height          =   615
         Index           =   9
         Left            =   1320
         TabIndex        =   9
         Top             =   1440
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "8"
         Height          =   615
         Index           =   8
         Left            =   720
         TabIndex        =   8
         Top             =   1440
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "7"
         Height          =   615
         Index           =   7
         Left            =   120
         TabIndex        =   7
         Top             =   1440
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "6"
         Height          =   615
         Index           =   6
         Left            =   1320
         TabIndex        =   6
         Top             =   840
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "5"
         Height          =   615
         Index           =   5
         Left            =   720
         TabIndex        =   5
         Top             =   840
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "4"
         Height          =   615
         Index           =   4
         Left            =   120
         TabIndex        =   4
         Top             =   840
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "3"
         Height          =   615
         Index           =   3
         Left            =   1320
         TabIndex        =   3
         Top             =   240
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "2"
         Height          =   615
         Index           =   2
         Left            =   720
         TabIndex        =   2
         Top             =   240
         Width           =   615
      End
      Begin VB.CommandButton Command1 
         Caption         =   "1"
         Height          =   615
         Index           =   1
         Left            =   120
         TabIndex        =   1
         Top             =   240
         Width           =   615
      End
      Begin VB.Label Memory 
         Caption         =   "Mem:"
         Height          =   255
         Left            =   2520
         TabIndex        =   28
         Top             =   2760
         Width           =   615
      End
   End
   Begin VB.Menu edit 
      Caption         =   "&Edit"
      Begin VB.Menu eexit 
         Caption         =   "&Exit"
      End
   End
End
Attribute VB_Name = "standard"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Dim dflag(2) As Integer
Dim i(2) As Integer
Dim opnre(2) As Integer
Dim prev(2) As Variant
Dim oflag(2) As Integer
Dim ind(2) As Integer
Dim result(2) As String
Dim memo(2) As Variant
Dim settings(2) As Integer
Dim Csetting(2) As Integer
Dim schaal  As Integer
Dim digitSwitch As String



Private Sub Command1_Click(Index As Integer)
 If Form1.Option2.Value = True Then schaal = 1 Else schaal = 0
 If tikactive = True Then Call System2: oflag(schaal) = 1: tikactive = False
    
    If ind(schaal) = 4 Then
        prev(schaal) = 0
        ind(schaal) = 0
        result(schaal) = ""
        Call System1
    End If
    opnre(schaal) = 0
    If oflag(schaal) = 0 Then
       result(schaal) = ""
       Call System1
       End If
    oflag(schaal) = 1
    If Command1(Index).Caption <> digitSwitch Then
            
                                                If result(schaal) <> " 0" Then
                                                result(schaal) = result(schaal) & Command1(Index).Caption
                                                Else
                                                result(schaal) = " " & Command1(Index).Caption
                                                End If
           Else
            If dflag(schaal) = 0 Then
              result(schaal) = result(schaal) & digitSwitch
              dflag(schaal) = 1
            Else
                MsgBox ("ILLEGAL SAIRAM")
            End If
     End If
            
Call System1
End Sub

Private Sub Command2_Click(Index As Integer)
       If Form1.Option2.Value = True Then schaal = 1 Else schaal = 0
           If result(schaal) = "0" Then Call System2
          result(schaal) = ConvertPunt(result(schaal))
       If Index = 8 Then
                      result(schaal) = ConvertPunt(result(schaal))
                      If Not result(schaal) = "" Then result(schaal) = Sqr(result(schaal))
                      Index = ind(schaal)
                      result(schaal) = Convertkomma(result(schaal))
                      Call System1
                      Exit Sub
                      End If
       If Index = 7 Then
                      result(schaal) = ConvertPunt(result(schaal))
                      If Not result(schaal) = "" Then
                                                If CDec(result(schaal)) = 0 Then
                                                                          MsgBox (errorzero)
                                                                          Exit Sub
                                                                          End If
                                                result(schaal) = 1 / result(schaal)
                                                End If
                      Index = ind(schaal)
                      result(schaal) = Convertkomma(result(schaal))
                      Call System1
                      Exit Sub
                      End If
    
    If Index = 6 Then
                      result(schaal) = ConvertPunt(result(schaal))
                      If Not result(schaal) = "" Then result(schaal) = result(schaal) * -1
                      Index = ind(schaal)
                      result(schaal) = Convertkomma(result(schaal))
                      Call System1
                      Exit Sub
                      End If
    If Index = 5 Then
                      result(schaal) = ConvertPunt(result(schaal))
                      If Not result(schaal) = "" Then
                                                result(schaal) = CDec(result(schaal) / 100)
                                                result(schaal) = CDec(prev(schaal) * result(schaal))
                                                End If
                      Index = ind(schaal)
                      result(schaal) = Convertkomma(result(schaal))
                      Call System1
                      Exit Sub
                      End If
        If opnre(schaal) = 0 Or Index = 4 Then
            If ind(schaal) = 0 Then
                 prev(schaal) = CDec(prev(schaal) + result(schaal))
            ElseIf ind(schaal) = 1 Then
                 prev(schaal) = CDec(prev(schaal) - result(schaal))
            ElseIf ind(schaal) = 2 Then
                If CDec(result(schaal)) = 0 Then
                    MsgBox (errorzero)
                    Exit Sub
                Else
                 prev(schaal) = CDec(prev(schaal) / result(schaal))
                End If
            ElseIf ind(schaal) = 3 Then
                 prev(schaal) = CDec(prev(schaal) * result(schaal))
            End If
            result(schaal) = prev(schaal)
            oflag(schaal) = 0
        End If
        opnre(schaal) = 1
        ind(schaal) = Index
        dflag(schaal) = 0

        If prev(schaal) > 0 And prev(schaal) < 1 Then result(schaal) = "0" + Mid$(prev(schaal), 2) Else If prev(schaal) > 0 And Left$(prev(schaal), 1) = " " Then result(schaal) = Mid$(prev(schaal), 2)
        If prev(schaal) < 0 Then result(schaal) = prev(schaal)
        result(schaal) = Convertkomma(result(schaal))
        Call System1
End Sub

Private Sub Command3_Click()
 If Form1.Option2.Value = True Then schaal = 1 Else schaal = 0
        dflag(schaal) = 0
        result(schaal) = ""
Call System1
End Sub

Private Sub Command4_Click()
If Form1.Option2.Value = True Then schaal = 1 Else schaal = 0
     
If settings(schaal) = 1 Then Csetting(schaal) = 1 Else memo(schaal) = 0: Memorystatus.Text = Mid(Convertkomma(Str(memo(schaal))), 2)
    dflag(schaal) = 0
    prev(schaal) = 0
    oflag(schaal) = 0
    ind(schaal) = 0
    opnre(schaal) = 0
    result(schaal) = ""
Call System1
End Sub

Private Sub Command5_Click()
    Unload Me
    
End Sub

Private Sub Command7_Click(Index As Integer)
 If Form1.Option2.Value = True Then schaal = 1 Else schaal = 0
 If result(schaal) = "" Then result(schaal) = 0
             Select Case Index
             Case 0
                 Call System2
                                                    
                                                    result(schaal) = Puntkomma(result(schaal))
                                                    If Not result(schaal) = "" Then memo(schaal) = memo(schaal) + CDec(result(schaal))
            Case 1
              settings(schaal) = 1
              If Not result(schaal) = "" Then memo(schaal) = CDec(result(schaal)) Else memo(schaal) = ""
              If Csetting(schaal) = 1 Then memo(schaal) = 0: settings(schaal) = 0: Csetting(schaal) = 0
              oflag(schaal) = 0
            Case 4
              Csetting(schaal) = 0
                result(schaal) = memo(schaal)
             If settings(schaal) = 1 Then opnre(schaal) = 0 Else If Not result(schaal) = "" Then prev(schaal) = CDec(result(schaal)) Else prev(schaal) = ""
            result(schaal) = Convertkomma(result(schaal))
            If Left(result(schaal), 1) = " " Then result(schaal) = Mid$(result(schaal), 2)
            Call System1
            Case 5
                memo(schaal) = 0
                settings(schaal) = 0
                Csetting(schaal) = 0
        End Select
Memorystatus.Text = Mid(Convertkomma(Str(memo(schaal))), 2)
End Sub

Private Sub Convert_Click()
 If Form1.Option1.Value = True Then
                        If switch1 = False Then stringer = Getans(Form1.Text1.Text, True) Else stringer = Getans(Form1.Text1.Text, False)
                        
                       If Left$(stringer, 1) = " " Then Form1.Text2.Text = Mid$(stringer, 2) Else Form1.Text2.Text = stringer
                  
                       If Not (Sform = "") Then Form1.Text1.Text = nulnul(Form1.Text1.Text)
                       prev(1) = 0
                       ind(1) = 0
                       dflag(1) = 0
                       oflag(1) = 0
                        ind(1) = 0
                        opnre(1) = 0
                       End If
 If Form1.Option2.Value = True Then
                        If switch1 = False Then stringer = Getans(Form1.Text2.Text, False) Else stringer = Getans(Form1.Text2.Text, True)
                        
                       If Left$(stringer, 1) = " " Then Form1.Text1.Text = Mid$(stringer, 2) Else Form1.Text1.Text = stringer
                  
                   
                        If Not (Sform = "") Then Form1.Text2.Text = nulnul(Form1.Text2.Text)
                        prev(0) = 0
                        ind(0) = 0
                         dflag(0) = 0
                       oflag(0) = 0
                        ind(0) = 0
                        opnre(0) = 0
                       End If
 result(1) = Form1.Text2.Text
 result(0) = Form1.Text1.Text
End Sub
Private Sub setopf()
    
Call System2
Call System1
 opnre(schaal) = 0
     ind(schaal) = Index
    dflag(schaal) = 0
 oflag(schaal) = 0
End Sub



Private Sub eexit_Click()
Unload Me
End Sub

Private Sub Form_Activate()
Call Languare(Lname, 5)
If TOPilse = True Then Call WindowsAPI.AlwaysOnTop(standard, True) Else Call WindowsAPI.AlwaysOnTop(standard, False)

End Sub

Private Sub Form_GotFocus()
Exit Sub
End Sub

Private Sub Form_KeyPress(KeyAscii As Integer)
  Dim ioo As Integer
   If KeyAscii = Asc(".") Then
        ioo = 10
         Command1_Click (ioo)
         Beep
   ElseIf KeyAscii = Asc("0") Then
        ioo = 0
         Command1_Click (ioo)
         Beep
   ElseIf KeyAscii = Asc("1") Then
        ioo = 1
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("2") Then
        ioo = 2
          Command1_Click (ioo)
        Beep
   ElseIf KeyAscii = Asc("3") Then
        ioo = 3
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("4") Then
        ioo = 4
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("5") Then
        ioo = 5
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("6") Then
        ioo = 6
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("7") Then
        ioo = 7
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("8") Then
        ioo = 8
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("9") Then
        ioo = 9
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("0") Then
        ioo = 0
          Command1_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("+") Then
        ioo = 0
          Command2_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("+") Then
        ioo = 0
          Command2_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("-") Then
        ioo = 1
          Command2_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("/") Then
        ioo = 2
          Command2_Click (ioo)
          Beep
 
   ElseIf KeyAscii = Asc("*") Then
        ioo = 3
          Command2_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("=") Then
        ioo = 4
          Command2_Click (ioo)
          Beep
   ElseIf KeyAscii = Asc("c") Or KeyAscii = Asc("C") Then
        dflag(schaal) = 0
        prev(schaal) = 0
        oflag(schaal) = 0
        ind(schaal) = 0
        opnre(schaal) = 0
        result(schaal) = " 0"
        Beep
        Beep
   ElseIf KeyAscii = Asc("d") Or KeyAscii = Asc("D") Then
        result(schaal) = " 0"
        Beep
   End If
End Sub


Private Sub Form_Load()
Call Languare(Lname, 5)
 ActiveCal = True
 Dim mil As String

OK = setting(digitSwitch, mil)
Command1(10).Caption = digitSwitch

If TOPilse = True Then Call WindowsAPI.AlwaysOnTop(standard, True) Else Call WindowsAPI.AlwaysOnTop(standard, False)

  '  standard.Height = 4090
  '  standard.Width = 3430
    Dim i As Integer
    For i = 0 To 1
    dflag(i) = 0
    prev(i) = 0
    oflag(i) = 0
    ind(i) = 0
    opnre(i) = 0
    Next i
    Clipboard.Clear
    Call System2
    
End Sub





Private Sub System1()
If Form1.Option1.Value = True Then Form1.Text1.Text = result(0) Else Form1.Text2.Text = result(1)

End Sub
Private Sub System2()
If oflag(schaal) = 0 Then
                    If Form1.Option1.Value = True Then
                                                        If Not (Form1.Text1.Text = "") Then
                                                                                            If Form1.Text1.Text = "0" Then Exit Sub
                                                                                            result(0) = Form1.Text1.Text
                                                                                            oflag(0) = 1
                                                                                            Exit Sub
                                                                                            End If
                                                       End If
                    If Form1.Option2.Value = True Then
                                                        If Not (Form1.Text2.Text = "") Then
                                                                                            If Form1.Text2.Text = "0" Then Exit Sub
                                                                                            result(1) = Form1.Text2.Text
                                                                                            oflag(1) = 1
                                                                                            Exit Sub
                                                                                            End If
                                                        End If
                    End If
                    

If Form1.Option1.Value = True Then result(0) = Form1.Text1.Text Else result(1) = Form1.Text2.Text

End Sub
Public Function Puntkomma(ans As String) As String
Dim maximum As Integer
Dim res As Integer
Dim iee As Integer
maximum = Len(ans)
If Left$(Right$(ans, 2), 1) = "," Then Puntkomma = Left$(ans, maximum - 2) + "." + Right$(ans, 1) + "0": Exit Function
For iee = 1 To Len(ans)
If Mid(ans, iee, 1) = "," Then
                             res = Len(Mid$(ans, iee + 1))
                             Puntkomma = Left$(ans, maximum - res - 1) + "." + Right$(ans, res): Exit Function
                             End If
Next iee
Puntkomma = ans
End Function

Private Sub Form_Terminate()
Unload Me
End Sub

Private Sub Form_Unload(Cancel As Integer)
ActiveCal = False
Unload Me
End Sub

