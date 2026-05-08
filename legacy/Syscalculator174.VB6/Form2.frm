VERSION 5.00
Begin VB.Form Form2 
   BorderStyle     =   3  'Fixed Dialog
   Caption         =   "Form2"
   ClientHeight    =   3045
   ClientLeft      =   2280
   ClientTop       =   1950
   ClientWidth     =   4710
   Icon            =   "Form2.frx":0000
   LinkTopic       =   "Form2"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   3045
   ScaleWidth      =   4710
   ShowInTaskbar   =   0   'False
   Begin VB.CheckBox Check1 
      Caption         =   "Enable Introducation Option"
      Height          =   375
      Left            =   480
      TabIndex        =   2
      Top             =   1680
      Value           =   1  'Checked
      Width           =   4095
   End
   Begin VB.CommandButton Command1 
      Caption         =   "OK"
      Height          =   375
      Left            =   1680
      TabIndex        =   0
      Top             =   2400
      Width           =   1095
   End
   Begin VB.Label text1 
      Height          =   1095
      Left            =   360
      TabIndex        =   1
      Top             =   360
      Width           =   3975
   End
End
Attribute VB_Name = "Form2"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Private Sub Check1_Click()
If Check1.Value = 0 Then
                        Check1.Value = 0
                        Form1.Indo.Checked = False
                        intro = False
                        test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Intro", "0")
Else
                            intro = True
                            Form1.Indo.Checked = True
                            test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Intro", "1")
                           
                           End If
                          
                        
End Sub

Private Sub Command1_Click()
Unload Me
End Sub

Private Sub Form_Load()


If TOPilse = True Then
                        Call WindowsAPI.AlwaysOnTop(Form1, False)
                        If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, False): WizardExpress.Enabled = False
                        If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, False): standard.Enabled = False

                        End If
                
Call Languare(Lname, 2)
Caption = Caption & Getappname()
 Form1.Enabled = False
If eidActiveForm2 = True Then WizardExpress.Enabled = False
Text1 = Getindro()
Form2.Show
End Sub

Private Sub Form_Terminate()
Unload Me
End Sub

Private Sub RichTextBox1_Change()

End Sub

Private Sub Form_Unload(Cancel As Integer)
Call algemeenform2
Unload Me
End Sub
Private Sub algemeenform2()
Form1.Enabled = True
If ActiveForm2 = True Then WizardExpress.Enabled = True
If TOPilse = True Then
                        Call WindowsAPI.AlwaysOnTop(Form1, True)
                        If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, True): WizardExpress.Enabled = True
                        If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, True): standard.Enabled = True

                        End If

End Sub

