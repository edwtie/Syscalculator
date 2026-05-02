Option Strict Off
Option Explicit On
Imports VB = Microsoft.VisualBasic
Friend Class Form3
	Inherits System.Windows.Forms.Form
	Dim old As String
	
	'UPGRADE_WARNING: Event Combo1.SelectedIndexChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Combo1_SelectedIndexChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Combo1.SelectedIndexChanged
		nodid = Combo1.SelectedIndex
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
			intIndex = -1
			intLength = Len(strSearchText)
			
			
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
	
	Private Sub Command1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command1.Click
		Dim test As Object
		
		Dim numb As Short
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test = fncGetFileNametoOpen( , "lng files|*.lng", "*.lng")
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		numb = Asc(VB.Left(test, 1))
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Not (numb = 0) Then Text1.Text = test
		Command4.Enabled = True
	End Sub
	
	Private Sub Command2_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command2.Click
		If max = -1 Then
			
			If CDbl(old) > -1 Then
				Text1.Text = saveconfig((Text1.Text), Combo1)
			End If
			If tray = True Then tray1.Close()
			Form1.Close()
			Me.Close()
			Exit Sub
		End If
		If CDbl(old) = -1 Then old = CStr(max) : Text1.Text = saveconfig((Text1.Text), Combo1) : Call form1cleanup() : Form1.Show() : Me.Close() : Exit Sub
		If Form1.Visible = False Then Call form1cleanup() : Form1.Show()
		flags = 5
		Text1.Text = saveconfig((Text1.Text), Combo1)
		Me.Close()
	End Sub
	
	Private Sub Command3_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command3.Click
		If Command4.Enabled = True Then max = CShort(old) Else old = CStr(max)
		If max = -1 Then
			If tray = True Then tray1.Close()
			Form1.Close()
			Me.Close()
			Exit Sub
		End If
		Addcombo(1)
		
		flags = 1
		Me.Close()
	End Sub
	
	Private Sub Command4_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command4.Click
		old = CStr(max)
		Text1.Text = saveconfig((Text1.Text), Combo1)
		Combo1.Items.Clear()
		If max > -1 Then Call comb()
		Command4.Enabled = False
		'nodid = defaultID
		flags = 5
		If max > -1 Then Call form1cleanup()
		Call Languare(Lname, 6)
		Call Languare(Lname, 1)
		If max > -1 Then Combo1.SelectedIndex = Form1.Combo1.SelectedIndex
		If ActiveForm2 = True Then Call Languare(Lname, 4)
		If ActiveCal = True Then Call Languare(Lname, 5)
		'If Form1.Altop.Checked = True Then Call WindowsAPI.AlwaysOnTop(Form1, False)
		flags = 1
		Me.Show()
	End Sub
	
	Private Sub Command5_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command5.Click
		If max = 0 Then defaultID = 0
		Add = True
		form4.Show()
	End Sub
	
	Private Sub Command6_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command6.Click
		
		form4.Show()
	End Sub
	
	
	Private Sub Command7_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command7.Click
		Call resort()
		
	End Sub
	
	Private Sub Form3_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
		
		fORMVALUE3 = True
		old = CStr(max)
		Call Languare(Lname, 6)
		If max = -1 Then Me.Command6.Enabled = False
		Form1.Enabled = False
		SetForms((False))
		Call comb()
		Command4.Enabled = False
		nodid = Form1.Combo1.SelectedIndex
		Combo1.SelectedIndex = nodid
	End Sub
	Private Sub comb()
		Dim i As Object
		Dim comm, Data As Object
		Dim def As String
		Dim OK As Short
		'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		i = 0
		On Error GoTo geenconfig
		FileOpen(1, Apppaths & "\" & "freesyscal.cfg", OpenMode.Input)
		Do Until EOF(1)
			Input(1, comm)
			Input(1, Data)
			Input(1, def)
			'UPGRADE_WARNING: Couldn't resolve default property of object comm. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If VB.Left(comm, 1) = "'" Then GoTo overstap 'rem only
			'UPGRADE_WARNING: Couldn't resolve default property of object comm. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If comm = "[lang]" Then
				'UPGRADE_WARNING: Couldn't resolve default property of object Data. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Lname = Data : Text1.Text = Lname : GoTo overstap
			End If
			
			'UPGRADE_WARNING: Couldn't resolve default property of object comm. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not (comm = "") Then Combo1.Items.Insert(i, comm)
			
			'UPGRADE_WARNING: Couldn't resolve default property of object Data. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object Data. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not (Data = "") Then filestring(i) = Data
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If def = "*" Then defaultID = i
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			i = i + 1
overstap: 
			
		Loop 
		FileClose(1)
		'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		max = i - 1
		Exit Sub
geenconfig: 
		Dim Status As Boolean
		Status = MsgBox(withoutcfg, MsgBoxStyle.Critical, errorf)
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
	
	'UPGRADE_NOTE: Form_Terminate was upgraded to Form_Terminate_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	'UPGRADE_WARNING: Form3 event Form.Terminate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub Form_Terminate_Renamed()
		Me.Close()
	End Sub
	
	Private Sub Form3_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		Dim flag As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object flag. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If flag = 0 And VB.Command() = "/Once" And intro = True Then Form2.Show() : flags = 1
		fORMVALUE3 = False
		If max > -1 Then Form1.Enabled = True
		SetForms((True))
		Apply = True
		Me.Close()
	End Sub
	
	'UPGRADE_WARNING: Event text1.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub text1_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles text1.TextChanged
		Command4.Enabled = True
	End Sub
End Class