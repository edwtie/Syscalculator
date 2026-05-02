VERSION 5.00
Begin VB.Form tray1 
   BorderStyle     =   1  'Fixed Single
   Caption         =   "Form5"
   ClientHeight    =   3195
   ClientLeft      =   150
   ClientTop       =   720
   ClientWidth     =   4680
   Icon            =   "tray.frx":0000
   LinkTopic       =   "Form5"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   3195
   ScaleWidth      =   4680
   StartUpPosition =   3  'Windows Default
   Begin VB.Timer Timer1 
      Enabled         =   0   'False
      Interval        =   300
      Left            =   480
      Top             =   1920
   End
   Begin VB.Menu mnuMain 
      Caption         =   "Main"
      Begin VB.Menu mnuShow 
         Caption         =   "Show"
         Enabled         =   0   'False
      End
      Begin VB.Menu mnuHide 
         Caption         =   "Hide"
         Enabled         =   0   'False
      End
      Begin VB.Menu spar 
         Caption         =   "-"
      End
      Begin VB.Menu mnuExit 
         Caption         =   "Exit"
      End
   End
End
Attribute VB_Name = "tray1"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False



Private Sub Form_Terminate()
Call EndApp
If Hooked Then Unhook
Shell_NotifyIcon NIM_DELETE, Tic
Timer1.Enabled = False
End
End Sub

Private Sub Form_Unload(Cancel As Integer)
If Hooked Then Unhook
Shell_NotifyIcon NIM_DELETE, Tic
tray = False
Timer1.Enabled = False
End Sub

Private Sub mnuExit_Click()
If Me.Enabled = False Then Exit Sub
Unload Form1
If ActiveForm2 = True Then Unload WizardExpress
If ActiveCal = True Then Unload standard
Unload Me
For i = Forms.Count - 1 To 0 Step -1
        Unload Forms(i)
    Next
Call EndApp
End Sub

Private Sub mnuHide_Click()
If Me.Enabled = False Then Exit Sub
Form1.Hide
mnuShow.Enabled = True
mnuHide.Enabled = False
If ActiveForm2 = True Then WizardExpress.Timer1.Enabled = False: WizardExpress.Hide
If ActiveCal = True Then standard.Hide
mnuMain.Visible = True
End Sub

Private Sub mnuShow_Click()
If Me.Enabled = False Then Exit Sub
mnuHide.Enabled = True
mnuShow.Enabled = False
Form1.Show
If ActiveForm2 = True Then WizardExpress.Visible = True: WizardExpress.Timer1.Enabled = True
If ActiveCal = True Then standard.Visible = True


End Sub
Private Sub Form_MouseMove(Button As Integer, Shift As Integer, X As Single, Y As Single)
flags = 1
If Form1.Enabled = False Then
                            mnuShow.Enabled = False
                            mnuHide.Enabled = False
                            mnuExit.Enabled = False
                            
                            ElseIf Form1.Enabled = True Then
                                                            mnuShow.Enabled = False
                                                            mnuHide.Enabled = True
                                                            mnuExit.Enabled = True
                                                            End If
If Form1.Visible = False Then
                            mnuShow.Enabled = True
                            mnuHide.Enabled = False
                            ElseIf Form1.Visible = True Then
                                                        mnuShow.Enabled = False
                                                        mnuHide.Enabled = True
                                                        End If
If fORMVALUE3 = False Then
        Dim Msg As Long
        Dim sFilter As String
        Msg = X / Screen.TwipsPerPixelX
        Select Case Msg
            Case WM_RBUTTONUP
                SetForegroundWindow Me.Hwnd
                PopupMenu Me.mnuMain
            Case WM_LBUTTONDBLCLK
                Call mnuShow_Click
        End Select
        End If
End Sub

Private Sub Timer1_Timer()
Rem als explorer onverwacht crasht, dan moet er toch icon verschijnen op dit bar. zonder dit methode komt logo na herstel niet meer terug.
Dim rc As Long
Tic.cbSize = Len(Tic)
Tic.Hwnd = tray1.Hwnd
Tic.uID = vbNull
Tic.uFlags = NIF_DOALL
Tic.uCallbackMessage = WM_MOUSEMOVE
Tic.hIcon = tray1.Icon
Tic.sTip = Form1.Caption & vbNullChar
rc = Shell_NotifyIcon(NIM_MODIFY, Tic)
Rem als icon niet op de bar verschijnt dan gaat hij nieuw icon laten vertonen.
If rc = 0 Then rc = Shell_NotifyIcon(NIM_ADD, Tic)
Rem timer begint tikken dus zodat hij controleer op elk seconde of icon aanwezig is. dus voor inlog en uitlog zal er timer dan alleen opsommen als uitlog dan timer maakt een timestamp in database .
Rem timer in flashcode moet je via boek vinden.

Rem If lasttime = 600 * 2 Then lasttime = 0: Call eupdate
lasttime = lasttime + 1
Rem Call eupdate
End Sub

'Private Sub eupdate()
'  Dim sourceUrl As String
'   Dim SlocalFile As String
'   Dim hfile As Long
'
'   sourceUrl = "http://www.tcsoftware.com/update/version.txt"
'   SlocalFile = Apppaths + "\check.tmp"
'
'
'   If DownloadFile(sourceUrl, SlocalFile) Then
'
'      hfile = FreeFile
'      Open SlocalFile For Input As #1
'      Do Until EOF(1)
'      Line Input #1, Commando
'      If Left$(Commando, 1) = "'" Then GoTo overstap 'rem only
'      Select Case UCase(Left$(Commando, 3))
'      Case "STS":
'        Value = Mid$(Commando, 5) 'reserved, Universal version
'      Case "URL":
'        temppath = Mid$(Commando, 5)
'      Case "END":
'        Exit Do
'      End Select
'overstap:
'      Loop
'      Close #1
'
'   Else
'   End If
'End Sub
