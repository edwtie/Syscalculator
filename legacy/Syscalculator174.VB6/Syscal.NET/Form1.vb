Option Strict Off
Option Explicit On
Imports VB = Microsoft.VisualBasic
Imports Microsoft.VisualBasic.PowerPacks
Friend Class Form1
	Inherits System.Windows.Forms.Form
	Dim bOverForm As Boolean
	Dim change As Short
	Private Const CB_SHOWDROPDOWN As Integer = &H14F
	
	
	
	Public Sub about_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles about.Click
		'UPGRADE_ISSUE: Load statement is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="B530EFF2-3132-48F8-B8BC-D88AF543D321"'
		Load(frmAbout)
		VB6.ShowForm(frmAbout, (0))
	End Sub
	
	Public Sub Altop_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Altop.Click
		Dim test1 As Boolean
		If Altop.Checked = True Then
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Ontop", "0")
			Call WindowsAPI.AlwaysOnTop(Me, False)
			If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, False)
			If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, False)
			Altop.Checked = False
			TOPilse = False
		Else
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Ontop", "1")
			Call WindowsAPI.AlwaysOnTop(Me, True)
			If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, True)
			If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, True)
			Altop.Checked = True
			TOPilse = True
			Exit Sub
		End If
	End Sub
	
	Public Sub cal_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles cal.Click
		standard.Show()
	End Sub
	
	'UPGRADE_WARNING: Event Check1.CheckStateChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Check1_CheckStateChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Check1.CheckStateChanged
		Dim test1 As Object
		If Check1.CheckState = 1 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "precision", "1")
			Combo2.Enabled = True
			setDec((Combo2.SelectedIndex))
			Decimals = True
			Exit Sub
		End If
		If Check1.CheckState = 0 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "precision", "0")
			Combo2.Enabled = False
			setDec(-1)
			Decimals = False
			Exit Sub
		End If
	End Sub
	
	Private Sub Combo1_Enter(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Combo1.Enter
		Combo1.SelectionStart = 0
		Combo1.SelectionLength = Len(Combo1.Text)
		
	End Sub
	
	Private Sub Combo1_KeyDown(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyEventArgs) Handles Combo1.KeyDown
		Dim KeyCode As Short = eventArgs.KeyCode
		Dim Shift As Short = eventArgs.KeyData \ &H10000
		If KeyCode = System.Windows.Forms.Keys.Delete Then
			Combo1.Text = ""
			KeyCode = 0
		End If
		
	End Sub
	
	Private Sub Combo1_KeyPress(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyPressEventArgs) Handles Combo1.KeyPress
		Dim KeyAscii As Short = Asc(eventArgs.KeyChar)
		Dim strSearchText As String
		Dim strEnteredText As String
		Dim intLength As Short
		Dim intIndex As Short
		Dim intCounter As Short
		On Error GoTo ErrorHandler
		
		
		With Combo1
			
			
			If .SelectionStart > 0 Then
				strEnteredText = VB.Left(.Text, .SelectionStart)
			End If
			Select Case KeyAscii
				Case System.Windows.Forms.Keys.Return
					
					
					If .SelectedIndex > -1 Then
						.SelectionStart = 0
						.SelectionLength = Len(VB6.GetItemString(Combo1, .SelectedIndex))
						GoTo EventExitSub
					End If
				Case System.Windows.Forms.Keys.Escape, System.Windows.Forms.Keys.Delete
					.Text = ""
					KeyAscii = 0
					GoTo EventExitSub
				Case System.Windows.Forms.Keys.Back
					
					
					If Len(strEnteredText) > 1 Then
						strSearchText = LCase(VB.Left(strEnteredText, Len(strEnteredText) - 1))
					Else
						strEnteredText = ""
						KeyAscii = 0
						.Text = ""
						GoTo EventExitSub
					End If
				Case Else
					strSearchText = LCase(strEnteredText & Chr(KeyAscii))
			End Select
			
			SendMessage(Combo1.Handle.ToInt32, CB_SHOWDROPDOWN, -1, strSearchText)
			
			
			For intCounter = 0 To .Items.Count - 1
				
				
				If LCase(VB.Left(VB6.GetItemString(Combo1, intCounter), intLength)) = strSearchText Then
					intIndex = intCounter
					Exit For
				End If
			Next intCounter
			
			
			If intIndex > -1 Then
				.SelectedIndex = intIndex
				.SelectionStart = Len(strSearchText)
				.SelectionLength = Len(VB6.GetItemString(Combo1, intIndex)) - Len(strSearchText)
			Else
				Beep()
			End If
			
		End With
		KeyAscii = 0
		GoTo EventExitSub
ErrorHandler: 
		KeyAscii = 0
		Beep()
EventExitSub: 
		eventArgs.KeyChar = Chr(KeyAscii)
		If KeyAscii = 0 Then
			eventArgs.Handled = True
		End If
	End Sub
	
	Private Sub Combo1_Leave(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Combo1.Leave
		Combo1.SelectionLength = 0
	End Sub
	
	'UPGRADE_WARNING: Event Combo2.SelectedIndexChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Combo2_SelectedIndexChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Combo2.SelectedIndexChanged
		Dim test1 As Object
		If Check1.CheckState = 1 Then
			setDec((Combo2.SelectedIndex))
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "decnum", CStr(Combo2.SelectedIndex))
		End If
	End Sub
	
	Private Sub Combo2_KeyDown(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyEventArgs) Handles Combo2.KeyDown
		Dim KeyCode As Short = eventArgs.KeyCode
		Dim Shift As Short = eventArgs.KeyData \ &H10000
		If KeyCode = System.Windows.Forms.Keys.Delete Then
			Combo2.Text = ""
			KeyCode = 0
		End If
		
	End Sub
	
	Private Sub Combo2_KeyPress(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyPressEventArgs) Handles Combo2.KeyPress
		Dim KeyAscii As Short = Asc(eventArgs.KeyChar)
		Dim strSearchText As String
		Dim strEnteredText As String
		Dim intLength As Short
		Dim intIndex As Short
		Dim intCounter As Short
		On Error GoTo ErrorHandler
		
		
		With Combo2
			
			
			If .SelectionStart > 0 Then
				strEnteredText = VB.Left(.Text, .SelectionStart)
			End If
			
			
			Select Case KeyAscii
				Case System.Windows.Forms.Keys.Return
					
					
					If .SelectedIndex > -1 Then
						.SelectionStart = 0
						.SelectionLength = Len(VB6.GetItemString(Combo2, .SelectedIndex))
						GoTo EventExitSub
					End If
				Case System.Windows.Forms.Keys.Escape, System.Windows.Forms.Keys.Delete
					.Text = ""
					KeyAscii = 0
					GoTo EventExitSub
				Case System.Windows.Forms.Keys.Back
					
					
					If Len(strEnteredText) > 1 Then
						strSearchText = LCase(VB.Left(strEnteredText, Len(strEnteredText) - 1))
					Else
						strEnteredText = ""
						KeyAscii = 0
						.Text = ""
						GoTo EventExitSub
					End If
				Case Else
					strSearchText = LCase(strEnteredText & Chr(KeyAscii))
			End Select
			intIndex = -1
			intLength = Len(strSearchText)
			
			
			For intCounter = 0 To .Items.Count - 1
				
				
				If LCase(VB.Left(VB6.GetItemString(Combo2, intCounter), intLength)) = strSearchText Then
					intIndex = intCounter
					Exit For
				End If
			Next intCounter
			
			
			If intIndex > -1 Then
				.SelectedIndex = intIndex
				.SelectionStart = Len(strSearchText)
				.SelectionLength = Len(VB6.GetItemString(Combo2, intIndex)) - Len(strSearchText)
			Else
				Beep()
			End If
		End With
		KeyAscii = 0
		GoTo EventExitSub
ErrorHandler: 
		KeyAscii = 0
		Beep()
EventExitSub: 
		eventArgs.KeyChar = Chr(KeyAscii)
		If KeyAscii = 0 Then
			eventArgs.Handled = True
		End If
	End Sub
	
	Private Sub Combo2_Leave(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Combo2.Leave
		Combo2.SelectionLength = 0
	End Sub
	
	Public Sub delall_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles delall.Click
		Me.Text1.Text = ""
		Me.Text2.Text = ""
	End Sub
	
	'UPGRADE_WARNING: Event Digitchek.CheckStateChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Digitchek_CheckStateChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Digitchek.CheckStateChanged
		Dim test1 As Object
		If Digitchek.CheckState = 1 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Digit", "1")
			booldigit = True
			
			Exit Sub
		End If
		If Digitchek.CheckState = 0 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Digit", "0")
			booldigit = False
			
			Exit Sub
		End If
	End Sub
	
	
	'UPGRADE_WARNING: Event Combo1.SelectedIndexChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Combo1_SelectedIndexChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Combo1.SelectedIndexChanged
		Dim test1 As Object
		Dim rc As Object
		Dim test100 As Object
		Dim o As Object
		Dim i As Object
		Dim e As Short
		Dim a As Boolean
		Dim fout As Short
		Dim nodid As Short
		nodid = Combo1.SelectedIndex
		a = OpenNOD(filestring(Combo1.SelectedIndex) & ".nod", 1)
		If a = False Then
			If nodid < defaultID Then defaultID = defaultID - 1
			If nodid = defaultID Then If nodid = 0 Then defaultID = 0 Else defaultID = nodid - 1
			For o = nodid To max
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If o = max Then
					Combo1.Items.RemoveAt((max))
					filestring(max) = ""
					max = max - 1
					'UPGRADE_WARNING: Couldn't resolve default property of object test100. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					test100 = saveconfig(Lname, Combo1, False)
					If max > -1 Then
						
						If Combo1.SelectedIndex <= 0 Then Combo1.SelectedIndex = 0 : Exit Sub Else Combo1.SelectedIndex = Combo1.SelectedIndex - 1
						Exit Sub
					Else
						If max = -1 Then Me.Hide() : Form3.Show() : Exit Sub
						Combo1.SelectedIndex = 0
						Exit Sub
					End If
				End If
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				VB6.SetItemString(Combo1, o, VB6.GetItemString(Combo1, o + 1))
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				filestring(o) = filestring(o + 1)
			Next o
			
		End If
		
		If intro = True Then
			If Not (Getindro() = "") Then VB6.ShowForm(Me, (0)) : VB6.ShowForm(Form2, (0))
		End If
		If ActiveForm2 = True Then WizardExpress.Show()
		If tray = True Then
			Tic.sTip = Me.Text & vbNullChar
			'UPGRADE_WARNING: Couldn't resolve default property of object rc. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			rc = Shell_NotifyIcon(NIM_MODIFY, Tic)
		End If
		Text = Getappname()
		formchg = Eichg()
		If formchg Then
			Check1.Enabled = False
			Digitchek.Enabled = False
			Combo2.Enabled = False
		Else
			Check1.Enabled = True
			Digitchek.Enabled = True
			If Decimals Then Combo2.Enabled = True Else Combo2.Enabled = False
			'Combo2.ListIndex = Getformat()
		End If
		
		'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Name", Text)
		If switch1 = False Then Label1.Text = Getsym1() Else Label1.Text = Getsym2()
		If switch1 = False Then Label2.Text = Getsym2() Else Label2.Text = Getsym1()
		If switch1 = False Then Label5.Text = Getsym3() Else Label5.Text = Getsym4()
		If switch1 = False Then Label6.Text = Getsym4() Else Label6.Text = Getsym3()
		If switch1 = False Then Label3.Text = Getask1() Else Label3.Text = Getask2()
		If switch1 = False Then Label4.Text = Getask2() Else Label4.Text = Getask1()
	End Sub
	
	Private Sub cute_Click()
		
	End Sub
	Sub SUBCOMMAND(ByRef VFROM As String)
		Dim temp As Object
		If VFROM = "" Then If tray Then Call trayshow() : Exit Sub
		'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		temp = addconfig(VFROM)
		flags = 1
		'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If temp = "-1" Then Exit Sub
		'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If temp = "-2" Then Exit Sub
		Combo1.SelectedIndex = max
		Call trayshow()
	End Sub
	Private Sub trayshow()
		
		If ActiveForm2 = True Then WizardExpress.Visible = True : WizardExpress.Timer1.Enabled = True
		If ActiveCal = True Then standard.Visible = True
		Me.Show()
	End Sub
	
	Private Sub Command1_Click()
		Form3.Show()
	End Sub
	
	Private Sub Command2_Click()
		Call cal_Click(cal, New System.EventArgs())
	End Sub
	
	Public Sub Delete_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Delete.Click
		If Option1.Checked = True Then Text1.Text = ""
		If Option2.Checked = True Then Text2.Text = ""
	End Sub
	
	Public Sub ecopy_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles ecopy.Click
		My.Computer.Clipboard.Clear()
		If Option1.Checked = True Then My.Computer.Clipboard.SetText(Text1.Text)
		If Option2.Checked = True Then My.Computer.Clipboard.SetText(Text2.Text)
		
	End Sub
	
	Public Sub ecut_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles ecut.Click
		My.Computer.Clipboard.Clear()
		If Option1.Checked = True Then My.Computer.Clipboard.SetText(Text1.Text) : Text1.Text = ""
		If Option2.Checked = True Then My.Computer.Clipboard.SetText(Text2.Text) : Text2.Text = ""
		
	End Sub
	
	
	
	Public Sub epaste_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles epaste.Click
		Dim result As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object result. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		result = ""
		'UPGRADE_ISSUE: Clipboard method Clipboard.GetText was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="076C26E5-B7A9-4E77-B69C-B4448DF39E58"'
		If Option1.Checked = True Then Text1.Text = My.Computer.Clipboard.GetText()
		'UPGRADE_ISSUE: Clipboard method Clipboard.GetText was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="076C26E5-B7A9-4E77-B69C-B4448DF39E58"'
		If Option2.Checked = True Then Text2.Text = My.Computer.Clipboard.GetText()
	End Sub
	
	Public Sub Exit_Renamed_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Exit_Renamed.Click
		
		Me.Close()
	End Sub
	
	'UPGRADE_WARNING: Form event Form1.Activate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub Form1_Activated(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Activated
		Dim rc As Object
		If flags <= 1 And Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Me, True)
		If flags = 5 Then
			Call Languare(Lname, 1)
			If ActiveForm2 = True Then Call Languare(Lname, 4)
			If ActiveCal = True Then Call Languare(Lname, 5)
			flags = 1
		End If
		If Apply = True Then
			Call form1cleanup()
			
			If tray = True Then
				Tic.sTip = Me.Text & vbNullChar
				'UPGRADE_WARNING: Couldn't resolve default property of object rc. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				rc = Shell_NotifyIcon(NIM_MODIFY, Tic)
			End If
			Apply = False
			Exit Sub
		End If
		
		
	End Sub
	
	Private Sub Form1_GotFocus(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.GotFocus
		Me.Enabled = True
	End Sub
	
	Private Sub Image1_MouseDown(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image1.MouseDown
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		ButtonPress(Image1, Me)
	End Sub
	Private Sub Image1_MouseUp(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image1.MouseUp
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		ButtonRelease(Image1, Me)
	End Sub
	Private Sub Image2_MouseDown(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image2.MouseDown
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		ButtonPress(Image2, Me)
	End Sub
	Private Sub Image2_MouseUp(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image2.MouseUp
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		ButtonRelease(Image2, Me)
	End Sub
	Private Sub Image3_MouseDown(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image3.MouseDown
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		ButtonPress(Image3, Me)
	End Sub
	Private Sub Image3_MouseUp(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image3.MouseUp
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		ButtonRelease(Image3, Me)
	End Sub
	Private Sub Form1_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
		Dim srun As Object
		Dim First As Object
		Dim Names As Object
		Dim test5 As Object
		Dim symbtest1 As Object
		Dim precision As Object
		Dim traysn As Object
		Dim decnum As Object
		Dim digitnr As Object
		Dim intros As Object
		Dim Ontop As Object
		Dim Switchs As Object
		Dim Firsttime As Object
		Dim migration As Object
		Dim test2 As String
		Dim test1 As Boolean
		'UPGRADE_ISSUE: App property App.Title was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="076C26E5-B7A9-4E77-B69C-B4448DF39E58"'
		App.Title = "Syscalculator"
		' migratie = 1 And firsttime = 0    Upgrade
		' migratie = 1 and firsttime = 1    first time for run
		' migratie = 0 and firsttime = 0    none
		
		
		
		'UPGRADE_ISSUE: App property App.PrevInstance was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="076C26E5-B7A9-4E77-B69C-B4448DF39E58"'
		'UPGRADE_WARNING: Lower bound of array buf was changed from 1 to 0. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="0F1C9BE1-AF9D-476E-83B1-17D43BECFF20"'
		Dim cds As COPYDATASTRUCT
		Dim ThWnd As Integer
		Dim buf(255) As Byte
		Dim a As String
		If App.PrevInstance Then
			test2 = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Name")
			OtherInstanceHwnd = fActivateWindowClass("ThunderRT6FormDC", test2)
			' Get the hWnd of the target application
			ThWnd = OtherInstanceHwnd
			a = VB.Command()
			'Copy the string into a byte array, converting it to ASCII
			CopyMemory(buf(1), a, Len(a))
			cds.dwData = 3
			cds.cbData = Len(a) + 1
			'UPGRADE_ISSUE: VarPtr function is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="367764E5-F3F8-4E43-AC3E-7FE0B5E074E2"'
			cds.lpData = VarPtr(buf(1))
			'UPGRADE_WARNING: Couldn't resolve default property of object cds. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			SendMessage(OtherInstanceHwnd, WM_COPYDATA, Me.Handle.ToInt32, cds)
			
			Me.Close()
			Exit Sub
		End If
		
		'UPGRADE_WARNING: Couldn't resolve default property of object GetDataFolder(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Apppaths = GetDataFolder(Me, "Syscalculator")
		'UPGRADE_WARNING: Couldn't resolve default property of object migration. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		migration = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Migration")
		'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Firsttime = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "first")
		'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Firsttime = "" Then Firsttime = "1"
		
		
		'UPGRADE_WARNING: Couldn't resolve default property of object migration. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Firsttime = "0" And migration = "1" Then
			'UPGRADE_WARNING: Couldn't resolve default property of object Switchs. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			Switchs = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Switch")
			'UPGRADE_WARNING: Couldn't resolve default property of object Ontop. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			Ontop = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Ontop")
			'UPGRADE_WARNING: Couldn't resolve default property of object intros. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			intros = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Intro")
			'UPGRADE_WARNING: Couldn't resolve default property of object digitnr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			digitnr = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Digit")
			'UPGRADE_WARNING: Couldn't resolve default property of object decnum. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			decnum = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "decnum")
			'UPGRADE_WARNING: Couldn't resolve default property of object traysn. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			traysn = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Tray")
			'UPGRADE_WARNING: Couldn't resolve default property of object precision. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			precision = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "precision")
			'UPGRADE_WARNING: Couldn't resolve default property of object symbtest1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			symbtest1 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "symbool")
			switch1 = False
			Call Addcombo(2)
			MakeDirectory((Apppaths))
			Call saveconfig(Lname, Combo1)
			Kill(My.Application.Info.DirectoryPath & "\freesyscal.cfg")
			'UPGRADE_WARNING: Couldn't resolve default property of object decnum. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not decnum = "" Then decnum = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "decnum", decnum)
			'UPGRADE_WARNING: Couldn't resolve default property of object Switchs. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not Switchs = "" Then Switchs = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Switch", Switchs)
			'UPGRADE_WARNING: Couldn't resolve default property of object Ontop. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not Ontop = "" Then Ontop = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Ontop", Ontop)
			'UPGRADE_WARNING: Couldn't resolve default property of object intros. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not intros = "" Then intros = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Intro", intros)
			'UPGRADE_WARNING: Couldn't resolve default property of object digitnr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not digitnr = "" Then
				digitnr = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Digit", CStr(digit))
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object test5. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				test5 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Digit", "2")
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object precision. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not precision = "" Then
				precision = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "precision", precision)
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object test5. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				test5 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "precision", "1")
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object traysn. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not traysn = "" Then traysn = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Tray", traysn)
			'UPGRADE_WARNING: Couldn't resolve default property of object symbtest1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not symbtest1 = "" Then symbtest1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "symbool", symbtest1)
			
			'UPGRADE_WARNING: Couldn't resolve default property of object Ontop. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not Ontop = "" Then Ontop = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Ontop")
			'UPGRADE_WARNING: Couldn't resolve default property of object intros. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not intros = "" Then intros = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Intro")
			'UPGRADE_WARNING: Couldn't resolve default property of object digitnr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not digitnr = "" Then digitnr = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Digit")
			'UPGRADE_WARNING: Couldn't resolve default property of object decnum. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not decnum = "" Then decnum = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "decnum")
			'UPGRADE_WARNING: Couldn't resolve default property of object traysn. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not traysn = "" Then traysn = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Tray")
			'UPGRADE_WARNING: Couldn't resolve default property of object precision. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not precision = "" Then precision = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "precision")
			'UPGRADE_WARNING: Couldn't resolve default property of object Names. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not Names = "" Then Names = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Name")
			'UPGRADE_WARNING: Couldn't resolve default property of object symbtest1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not symbtest1 = "" Then symbtest1 = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "symbool")
			'UPGRADE_WARNING: Couldn't resolve default property of object Switchs. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not Switchs = "" Then Switchs = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Switch")
			'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object First. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not Firsttime = "" Then First = bDeleteRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "first")
			'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Firsttime = "0" Then Firsttime = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "first", "0")
			'UPGRADE_WARNING: Couldn't resolve default property of object migration. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If migration = "1" Then Firsttime = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Migration", "0")
			
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Firsttime = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "first")
		
		'UPGRADE_WARNING: Couldn't resolve default property of object migration. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Firsttime = "1" And migration = "1" Then
			'UPGRADE_WARNING: Couldn't resolve default property of object Switchs. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			Switchs = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Switch", "0")
			'UPGRADE_WARNING: Couldn't resolve default property of object Ontop. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			Ontop = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Ontop", "1")
			'UPGRADE_WARNING: Couldn't resolve default property of object intros. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			intros = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Intro", "0")
			'UPGRADE_WARNING: Couldn't resolve default property of object digitnr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			digitnr = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Digit", "0")
			'UPGRADE_WARNING: Couldn't resolve default property of object precision. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			precision = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "precision", "0")
			'UPGRADE_WARNING: Couldn't resolve default property of object symbtest1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			symbtest1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "symbool", "0")
			'UPGRADE_WARNING: Couldn't resolve default property of object decnum. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			decnum = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "decnum", "2")
			
			startup.Checked = False
			'UPGRADE_WARNING: Couldn't resolve default property of object traysn. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			traysn = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Tray", "1")
			Me.Top = VB6.TwipsToPixelsY((VB6.PixelsToTwipsY(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height) * 0.85) / 2 - VB6.PixelsToTwipsY(Me.Height) / 2)
			Me.Left = VB6.TwipsToPixelsX(VB6.PixelsToTwipsX(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width) / 2 - VB6.PixelsToTwipsX(Me.Width) / 2)
			If Addcombo = -1 Then
				MakeDirectory((Apppaths))
				Call savefirstconfig()
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Firsttime = "1" Then Firsttime = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "first", "0")
			'UPGRADE_WARNING: Couldn't resolve default property of object migration. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If migration = "1" Then Firsttime = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Migration", "0")
		End If
		
		
		'UPGRADE_WARNING: Couldn't resolve default property of object Ontop. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Ontop = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition", "Ontop")
		'UPGRADE_WARNING: Couldn't resolve default property of object intros. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		intros = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition", "Intro")
		'UPGRADE_WARNING: Couldn't resolve default property of object srun. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		srun = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "Syscal")
		'UPGRADE_WARNING: Couldn't resolve default property of object digitnr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		digitnr = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition", "Digit")
		'UPGRADE_WARNING: Couldn't resolve default property of object decnum. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		decnum = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition", "decnum")
		'UPGRADE_WARNING: Couldn't resolve default property of object traysn. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		traysn = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition", "Tray")
		'UPGRADE_WARNING: Couldn't resolve default property of object precision. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		precision = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition", "precision")
		'UPGRADE_WARNING: Couldn't resolve default property of object Switchs. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Switchs = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition", "Switch")
		'UPGRADE_WARNING: Couldn't resolve default property of object Switchs. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Switchs = "0" Then switch1 = False Else switch1 = True
		Call Addcombo()
		
		'UPGRADE_WARNING: Couldn't resolve default property of object Firsttime. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Firsttime = "0" Then
			If LCase(VB.Left(VB.Command(), 4)) = "/lng" Then Lname = Mid(VB.Command(), 6) : Lname = saveconfig(Lname, Combo1)
		End If
		Call Languare(Lname, 1)
		
		Apply = False
		'UPGRADE_WARNING: Couldn't resolve default property of object Ontop. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Ontop = "1" Then
			TOPilse = True
			Call WindowsAPI.AlwaysOnTop(Me, True)
			Altop.Checked = True
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object intros. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If intros = "0" Then intro = False : Indo.Checked = False Else intro = True
		'UPGRADE_WARNING: Couldn't resolve default property of object srun. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If srun = "" Then startup.Checked = False
		'UPGRADE_WARNING: Couldn't resolve default property of object srun. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If srun = My.Application.Info.DirectoryPath & "\Freesyscal.exe /tray" Then startup.Checked = True Else startup.Checked = False
		'UPGRADE_WARNING: Couldn't resolve default property of object digitnr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If digitnr = "0" Then booldigit = False : Digitchek.CheckState = System.Windows.Forms.CheckState.Unchecked Else booldigit = True : Digitchek.CheckState = System.Windows.Forms.CheckState.Checked
		fORMVALUE3 = False
		ActiveForm2 = False
		'UPGRADE_WARNING: Couldn't resolve default property of object decnum. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Not decnum = "" Then Combo2.SelectedIndex = decnum Else Combo2.SelectedIndex = 2
		'UPGRADE_WARNING: Couldn't resolve default property of object precision. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If precision = "0" Then Decimals = False : Check1.CheckState = System.Windows.Forms.CheckState.Unchecked Else Decimals = True : Check1.CheckState = System.Windows.Forms.CheckState.Checked
		
		'UPGRADE_WARNING: Couldn't resolve default property of object traysn. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Dim rc As Integer
		If traysn = "1" Then
			Tic.cbSize = Len(Tic)
			Tic.Hwnd = tray1.Handle.ToInt32
			Tic.uID = VariantType.Null
			Tic.uFlags = NIF_DOALL
			Tic.uCallbackMessage = WM_MOUSEMOVE
			Tic.hIcon = CInt(CObj(Me.Icon))
			Tic.sTip = Me.Text & vbNullChar
			rc = Shell_NotifyIcon(NIM_ADD, Tic)
			tray = True
			Mtray.Checked = True
			tray1.Timer1.Enabled = True
			'UPGRADE_ISSUE: App property App.TaskVisible was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="076C26E5-B7A9-4E77-B69C-B4448DF39E58"'
			App.TaskVisible = False
			If VB.Command() = "/tray" Then flags = 1 : Me.Hide()
		End If
		
		
		If max = -1 Then Me.Hide() : Form3.Show() : Exit Sub
		
		Dim temp As String
		If flags = 0 And Not VB.Command() = "" Then
			If Not VB.Left(VB.Command(), 4) = "/lng" Then
				Call SUBCOMMAND(VB.Command())
			End If
		End If
		
		flags = 1
		If flags <= 1 And Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Me, True)
		If Not Hooked Then Hook()
	End Sub
	
	Private Sub Form1_FormClosing(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
		Dim Cancel As Boolean = eventArgs.Cancel
		Dim UnloadMode As System.Windows.Forms.CloseReason = eventArgs.CloseReason
		If UnloadMode = System.Windows.Forms.CloseReason.WindowsShutDown Then
			tray = False
			If Hooked Then Unhook()
			Form1_FormClosed(Me, New System.Windows.Forms.FormClosedEventArgs((Cancel), FormAction.Closed))
		End If
		
		
		If tray = True Then
			If ActiveForm2 = True Then WizardExpress.Timer1.Enabled = False : WizardExpress.Hide()
			If ActiveCal = True Then standard.Hide()
			Cancel = True
			Me.Hide()
			'UPGRADE_ISSUE: Load statement is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="B530EFF2-3132-48F8-B8BC-D88AF543D321"'
			Load(tray1)
			
		Else
			If Hooked Then Unhook()
			Form1_FormClosed(Me, New System.Windows.Forms.FormClosedEventArgs((Cancel), FormAction.Closed))
			
		End If
		
		
		eventArgs.Cancel = Cancel
	End Sub
	
	'UPGRADE_NOTE: Form_Terminate was upgraded to Form_Terminate_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	'UPGRADE_WARNING: Form1 event Form.Terminate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub Form_Terminate_Renamed()
		Dim i As Object
		If Hooked Then Unhook()
		For i = My.Application.OpenForms.Count - 1 To 0 Step -1
			'UPGRADE_ISSUE: Unload Forms() was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="875EBAD7-D704-4539-9969-BC7DBDAA62A2"'
			Unload(My.Application.OpenForms(i))
		Next 
		Call EndApp()
		End
	End Sub
	
	Private Sub Form1_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		Dim i As Object
		If Hooked Then Unhook()
		For i = My.Application.OpenForms.Count - 1 To 0 Step -1
			'UPGRADE_ISSUE: Unload Forms() was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="875EBAD7-D704-4539-9969-BC7DBDAA62A2"'
			Unload(My.Application.OpenForms(i))
		Next 
	End Sub
	
	Public Sub help1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles help1.Click
		Dim SW_SHOWNORMAL As Object
		Dim iRet As Integer
		'UPGRADE_WARNING: Couldn't resolve default property of object SW_SHOWNORMAL. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		iRet = ShellExecute(Me.Handle.ToInt32, vbNullString, Configur2 & " " & My.Application.Info.Version.Major & "." & My.Application.Info.Version.Minor & "." & My.Application.Info.Version.Revision, vbNullString, "c:\", SW_SHOWNORMAL)
		
	End Sub
	
	Public Sub Help2_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Help2.Click
		Dim SW_SHOWNORMAL As Object
		Dim iRet As Integer
		'UPGRADE_WARNING: Couldn't resolve default property of object SW_SHOWNORMAL. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		iRet = ShellExecute(Me.Handle.ToInt32, vbNullString, Configur3, vbNullString, "c:\", SW_SHOWNORMAL)
		
	End Sub
	
	Private Sub Image1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Image1.Click
		Call Wizard_Click(Wizard, New System.EventArgs())
	End Sub
	Private Sub Image1_MouseMove(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image1.MouseMove
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		If bOverForm Then
			HighlightBorder(Image1, Me)
			bOverForm = False
		End If
	End Sub
	Private Sub Image2_MouseMove(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image2.MouseMove
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		If bOverForm Then
			HighlightBorder(Image2, Me)
			bOverForm = False
		End If
	End Sub
	Private Sub Image3_MouseMove(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Image3.MouseMove
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		If bOverForm Then
			HighlightBorder(Image3, Me)
			bOverForm = False
		End If
	End Sub
	Private Sub Image2_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Image2.Click
		Call cal_Click(cal, New System.EventArgs())
	End Sub
	
	Private Sub Image3_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Image3.Click
		Dim test As Object
		' oude code Form3.Show
		Dim numb As Short
		Dim Control As String
		Dim newfile As String
		Dim comm, Data As Object
		Dim def As String
		Dim OK As Short
		Dim tel As Short
		If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Me, False)
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test = fncGetFileNametoOpen( , "nod files|*.nod", "*.nod")
		If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Me, True)
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		numb = Asc(VB.Left(test, 1))
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Not (numb = 0) Then newfile = test Else Exit Sub
		newfile = Knipname(newfile)
		Call SUBCOMMAND(newfile)
	End Sub
	Function Knipname(ByRef newfile As String) As String
		Dim temp As Object
		Dim tel As Short
		'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		temp = Chr(0)
		'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		tel = InStr(newfile, temp)
		If Not tel = 0 Then Knipname = VB.Left(newfile, tel - 1) : Exit Function
		Knipname = newfile
	End Function
	Function addconfig(ByRef newfile As String) As String
		Dim def As Object
		Dim o As Object
		Dim Control As Object
		Dim tel As Short
		Dim temp As String
		newfile = Knipname(newfile)
		'UPGRADE_WARNING: Couldn't resolve default property of object Control. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Control = AddName(newfile)
		'UPGRADE_WARNING: Couldn't resolve default property of object Control. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Control = "-1" Then GoTo geenconfig
		'UPGRADE_WARNING: Couldn't resolve default property of object Control. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Control = "" Then Control = newfile
		For o = 0 To max
			'UPGRADE_WARNING: Couldn't resolve default property of object Control. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If UCase(VB6.GetItemString(Combo1, o)) = UCase(Control) Then
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Combo1.Text = VB6.GetItemString(Combo1, o)
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Combo1.SelectedIndex = o : addconfig = "-2" : Exit Function
			End If
		Next o
		tel = Len(Apppaths & "\")
over: 
		max = max + 1
		'UPGRADE_WARNING: Couldn't resolve default property of object Control. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Combo1.Items.Insert(max, Control)
		tel = Len(Apppaths & "\")
		If VB.Left(newfile, tel) = Apppaths & "\" Then newfile = Mid(newfile, tel + 1)
		tel = Len(newfile)
		If UCase(VB.Right(newfile, 3)) = "NOD" Then newfile = VB.Left(newfile, tel - 4)
		filestring(max) = newfile
		On Error GoTo geenconfig
		FileOpen(1, Apppaths & "\" & "freesyscal.cfg", OpenMode.Output)
		WriteLine(1, "[lang]", Lname, "")
		For o = 0 To max
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object def. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If defaultID = o Then def = "*"
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			WriteLine(1, VB6.GetItemString(Combo1, o), filestring(o), def)
			'UPGRADE_WARNING: Couldn't resolve default property of object def. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			def = ""
		Next o
		FileClose(1)
		Exit Function
		
geenconfig: 
		addconfig = "-1"
	End Function
	Public Sub Indo_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Indo.Click
		Dim test1 As Boolean
		If Indo.Checked = True Then
			intro = False
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Intro", "0")
			Indo.Checked = False
			
		Else
			intro = True
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Intro", "1")
			Indo.Checked = True
			
			
		End If
		
	End Sub
	
	Private Sub Picture1_Click()
		' This click can open FileDialog.
	End Sub
	
	Private Sub Label7_MouseMove(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles Label7.MouseMove
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		If Not bOverForm Then
			'UPGRADE_ISSUE: Form method Form1.Cls was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="CC4C7EC0-C903-48FC-ACCC-81861D12DA4A"'
			Cls()
			bOverForm = True
		End If
	End Sub
	
	Public Sub Mtray_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Mtray.Click
		Dim test1 As Boolean
		If Mtray.Checked = True Then
			tray = False
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Tray", "0")
			Mtray.Checked = False
			tray1.Timer1.Enabled = False
			Shell_NotifyIcon(NIM_DELETE, Tic)
			'UPGRADE_ISSUE: App property App.TaskVisible was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="076C26E5-B7A9-4E77-B69C-B4448DF39E58"'
			App.TaskVisible = True
			
		Else
			tray = True
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Tray", "1")
			Mtray.Checked = True
			
			Tic.cbSize = Len(Tic)
			Tic.Hwnd = tray1.Handle.ToInt32
			Tic.uID = VariantType.Null
			Tic.uFlags = NIF_DOALL
			Tic.uCallbackMessage = WM_MOUSEMOVE
			Tic.hIcon = CInt(CObj(tray1.Icon))
			Tic.sTip = Me.Text & vbNullChar
			Shell_NotifyIcon(NIM_ADD, Tic)
			tray1.Timer1.Enabled = True
			'UPGRADE_ISSUE: App property App.TaskVisible was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="076C26E5-B7A9-4E77-B69C-B4448DF39E58"'
			App.TaskVisible = False
			
		End If
	End Sub
	
	Public Sub openconv_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles openconv.Click
		Call Image3_Click(Image3, New System.EventArgs())
	End Sub
	
	Public Sub Opties_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Opties.Click
		Form3.Show()
	End Sub
	
	Public Sub startup_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles startup.Click
		Dim test2 As Object
		Dim test1 As Object
		If startup.Checked = True Then
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bDeleteRegValue(HKEY_CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "Syscal")
			startup.Checked = False
			
		Else
			tray = True
			'UPGRADE_WARNING: Couldn't resolve default property of object test2. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test2 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "Syscal", My.Application.Info.DirectoryPath & "\Freesyscal.exe /tray")
			startup.Checked = True
			
		End If
	End Sub
	
	Public Sub Switchclick_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Switchclick.Click
		Dim test1 As Object
		If switch1 = True Then
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Switch", "0")
			switch1 = False
		Else
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Switch", "1")
			switch1 = True
		End If
		If switch1 = False Then Label1.Text = Getsym1() Else Label1.Text = Getsym2()
		If switch1 = False Then Label2.Text = Getsym2() Else Label2.Text = Getsym1()
		If switch1 = False Then Label5.Text = Getsym3() Else Label5.Text = Getsym4()
		If switch1 = False Then Label6.Text = Getsym4() Else Label6.Text = Getsym3()
		If switch1 = False Then Label3.Text = Getask1() Else Label3.Text = Getask2()
		If switch1 = False Then Label4.Text = Getask2() Else Label4.Text = Getask1()
	End Sub
	
	'UPGRADE_WARNING: Event text1.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub text1_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles text1.TextChanged
		If Me.Text1.Text = "" And Me.Text2.Text = "" Then Me.delall.Enabled = False Else Me.delall.Enabled = True
	End Sub
	
	Private Sub Text1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Text1.Click
		If My.Computer.Clipboard.GetText = "" Then epaste.Enabled = False Else epaste.Enabled = True
		If Text1.SelectionLength = 0 Then Delete.Enabled = False : ecopy.Enabled = False : ecut.Enabled = False Else ecopy.Enabled = True : ecut.Enabled = True : Delete.Enabled = True
		
	End Sub
	
	Private Sub Text1_Enter(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Text1.Enter
		Option1.Checked = 1
		Option2.Checked = 0
	End Sub
	
	Private Sub Text1_KeyPress(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyPressEventArgs) Handles Text1.KeyPress
		Dim KeyAscii As Short = Asc(eventArgs.KeyChar)
		Dim stringer As String
		Option1.Checked = 1
		Option2.Checked = 0
		If ActiveCal = True Then tikactive = 1
		If KeyAscii = 13 Then
			digit = False
			If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Me, False)
			If switch1 = False Then stringer = Getans((Text1.Text), True) Else stringer = Getans((Text1.Text), False)
			
			
			If VB.Left(stringer, 1) = " " Then Text2.Text = Mid(stringer, 2) Else Text2.Text = stringer
			
			If Not (Sform = "") And Not (stringer = "") Then Text1.Text = nulnul((Text1.Text))
			If digit = True Then digit = False
			If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Me, True)
			
			
		End If
		
		
		eventArgs.KeyChar = Chr(KeyAscii)
		If KeyAscii = 0 Then
			eventArgs.Handled = True
		End If
	End Sub
	
	
	'UPGRADE_WARNING: Event Text2.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Text2_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Text2.TextChanged
		If Me.Text1.Text = "" And Me.Text2.Text = "" Then Me.delall.Enabled = False Else Me.delall.Enabled = True
		
	End Sub
	
	Private Sub Text2_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Text2.Click
		If My.Computer.Clipboard.GetText = "" Then epaste.Enabled = False Else epaste.Enabled = True
		If Text2.SelectionLength = 0 Then Delete.Enabled = False : ecopy.Enabled = False : ecut.Enabled = False Else ecopy.Enabled = True : ecut.Enabled = True : Delete.Enabled = True
		
	End Sub
	
	Private Sub Text2_Enter(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Text2.Enter
		Option1.Checked = 0
		Option2.Checked = 1
	End Sub
	
	Private Sub Text2_KeyPress(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyPressEventArgs) Handles Text2.KeyPress
		Dim KeyAscii As Short = Asc(eventArgs.KeyChar)
		
		Dim stringer As String
		If ActiveCal = True Then tikactive = 1
		Option1.Checked = 0
		Option2.Checked = 1
		If KeyAscii = 13 Then
			If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Me, False)
			
			digit = False
			If switch1 = False Then stringer = Getans((Text2.Text), False) Else stringer = Getans((Text2.Text), True)
			If VB.Left(stringer, 1) = " " Then Text1.Text = Mid(stringer, 2) Else Text1.Text = stringer
			If Not (Sform = "") And Not (stringer = "") Then Text2.Text = nulnul((Text2.Text))
			If digit = True Then digit = False
			If Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Me, True)
			
		End If
		
		eventArgs.KeyChar = Chr(KeyAscii)
		If KeyAscii = 0 Then
			eventArgs.Handled = True
		End If
	End Sub
	
	Public Sub urllaunch_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles urllaunch.Click
		Dim SW_SHOWNORMAL As Object
		Dim iRet As Integer
		'UPGRADE_WARNING: Couldn't resolve default property of object SW_SHOWNORMAL. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		iRet = ShellExecute(Me.Handle.ToInt32, vbNullString, Configurl, vbNullString, "c:\", SW_SHOWNORMAL)
	End Sub
	Public Sub Donate_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Donate.Click
		Dim SW_SHOWNORMAL As Object
		Dim iRet As Integer
		'UPGRADE_WARNING: Couldn't resolve default property of object SW_SHOWNORMAL. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		iRet = ShellExecute(Me.Handle.ToInt32, vbNullString, CStr(donateurl), vbNullString, "c:\", SW_SHOWNORMAL)
	End Sub
	
	
	Public Sub Wizard_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Wizard.Click
		VB6.ShowForm(WizardExpress, (0))
	End Sub
	
	Private Sub Form1_MouseMove(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.MouseEventArgs) Handles MyBase.MouseMove
		Dim Button As Short = eventArgs.Button \ &H100000
		Dim Shift As Short = System.Windows.Forms.Control.ModifierKeys \ &H10000
		Dim X As Single = VB6.PixelsToTwipsX(eventArgs.X)
		Dim Y As Single = VB6.PixelsToTwipsY(eventArgs.Y)
		
		If Not bOverForm Then
			'UPGRADE_ISSUE: Form method Form1.Cls was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="CC4C7EC0-C903-48FC-ACCC-81861D12DA4A"'
			Cls()
			bOverForm = True
		End If
	End Sub
	Private Sub savefirstconfig()
		Dim comm, Data As Object
		Dim def As String
		Dim OK As Short
		Dim tel As Short
		If LCase(VB.Left(VB.Command(), 4)) = "/lng" Then Lname = Mid(VB.Command(), 6) Else Lname = "eng.lng"
		On Error GoTo geenconfig
		FileOpen(1, Apppaths & "\" & "freesyscal.cfg", OpenMode.Output)
		WriteLine(1, "[lang]", Lname, "")
		WriteLine(1, "Graden - Fahrenheit", "Temperature\graden fahrenheit", "*")
		WriteLine(1, "meters - feets", "Distance\feet meter", "*")
		WriteLine(1, "Graden - Kelvin", "Temperature\graden kelvin", "")
		WriteLine(1, "Euro-NLG", "Euro\NLG", "")
		FileClose(1)
		Exit Sub
geenconfig: 
		
	End Sub
End Class