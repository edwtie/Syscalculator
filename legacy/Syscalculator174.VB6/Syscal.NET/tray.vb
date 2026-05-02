Option Strict Off
Option Explicit On
Friend Class tray1
	Inherits System.Windows.Forms.Form
	
	
	
	'UPGRADE_NOTE: Form_Terminate was upgraded to Form_Terminate_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	'UPGRADE_WARNING: tray1 event Form.Terminate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub Form_Terminate_Renamed()
		Call EndApp()
		If Hooked Then Unhook()
		Shell_NotifyIcon(NIM_DELETE, Tic)
		Timer1.Enabled = False
		End
	End Sub
	
	Private Sub tray1_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		If Hooked Then Unhook()
		Shell_NotifyIcon(NIM_DELETE, Tic)
		tray = False
		Timer1.Enabled = False
	End Sub
	
	Public Sub mnuExit_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuExit.Click
		Dim i As Object
		If Me.Enabled = False Then Exit Sub
		Form1.Close()
		If ActiveForm2 = True Then WizardExpress.Close()
		If ActiveCal = True Then standard.Close()
		Me.Close()
		For i = My.Application.OpenForms.Count - 1 To 0 Step -1
			'UPGRADE_ISSUE: Unload Forms() was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="875EBAD7-D704-4539-9969-BC7DBDAA62A2"'
			Unload(My.Application.OpenForms(i))
		Next 
		Call EndApp()
	End Sub
	
	Public Sub mnuHide_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuHide.Click
		If Me.Enabled = False Then Exit Sub
		Form1.Hide()
		mnuShow.Enabled = True
		mnuHide.Enabled = False
		If ActiveForm2 = True Then WizardExpress.Timer1.Enabled = False : WizardExpress.Hide()
		If ActiveCal = True Then standard.Hide()
		mnuMain.Visible = True
	End Sub
	
	Public Sub mnuShow_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnuShow.Click
		If Me.Enabled = False Then Exit Sub
		mnuHide.Enabled = True
		mnuShow.Enabled = False
		Form1.Show()
		If ActiveForm2 = True Then WizardExpress.Visible = True : WizardExpress.Timer1.Enabled = True
		If ActiveCal = True Then standard.Visible = True
		
		
	End Sub
	Private Sub tray1_MouseMove(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles MyBase.MouseMove
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
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
		Dim sFilter As String
		'UPGRADE_NOTE: Msg was upgraded to Msg_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
		Dim Msg_Renamed As Integer
		If fORMVALUE3 = False Then
			Msg_Renamed = X / VB6.TwipsPerPixelX
			Select Case Msg_Renamed
				Case WM_RBUTTONUP
					SetForegroundWindow(Me.Handle.ToInt32)
					'UPGRADE_ISSUE: Form method tray1.PopupMenu was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="CC4C7EC0-C903-48FC-ACCC-81861D12DA4A"'
					PopupMenu(Me.mnuMain)
				Case WM_LBUTTONDBLCLK
					Call mnuShow_Click(mnuShow, New System.EventArgs())
			End Select
		End If
	End Sub
	
	Private Sub Timer1_Tick(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Timer1.Tick
		' als explorer onverwacht crasht, dan moet er toch icon verschijnen op dit bar. zonder dit methode komt logo na herstel niet meer terug.
		Dim rc As Integer
		Tic.cbSize = Len(Tic)
		Tic.Hwnd = Me.Handle.ToInt32
		Tic.uID = VariantType.Null
		Tic.uFlags = NIF_DOALL
		Tic.uCallbackMessage = WM_MOUSEMOVE
		Tic.hIcon = CInt(CObj(Me.Icon))
		Tic.sTip = Form1.Text & vbNullChar
		rc = Shell_NotifyIcon(NIM_MODIFY, Tic)
		' als icon niet op de bar verschijnt dan gaat hij nieuw icon laten vertonen.
		If rc = 0 Then rc = Shell_NotifyIcon(NIM_ADD, Tic)
		' timer begint tikken dus zodat hij controleer op elk seconde of icon aanwezig is. dus voor inlog en uitlog zal er timer dan alleen opsommen als uitlog dan timer maakt een timestamp in database .
		' timer in flashcode moet je via boek vinden.
		
		' If lasttime = 600 * 2 Then lasttime = 0: Call eupdate
		'UPGRADE_WARNING: Couldn't resolve default property of object lasttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		lasttime = lasttime + 1
		' Call eupdate
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
End Class