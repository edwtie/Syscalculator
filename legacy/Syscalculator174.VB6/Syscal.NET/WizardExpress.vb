Option Strict Off
Option Explicit On
Imports VB = Microsoft.VisualBasic
Friend Class WizardExpress
	Inherits System.Windows.Forms.Form
	Public unconvert As String
	Public exconvert As String
	Public OptionAlg As String
	Public ready As String
	Public optalg As String
	Public Curc As String
	
	
	
	Public Sub exit_Renamed_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles exit_Renamed.Click
		Me.Close()
	End Sub
	
	
	
	Private Sub WizardExpress_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
		Dim test2 As String
		Dim test1 As Boolean
		test2 = bGetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "symbool")
		If test2 = "" Then test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "symbool", "0")
		If test2 = "1" Then
			symbool.Checked = True
		End If
		
		ActiveForm2 = True
		Call Languare(Lname, 4)
		Call algemeen()
		OptionAlg = CStr(True)
		optalg = CStr(True)
		If Not (Getsym1() = "") Then Curc = Getsym1()
		If Not (Getsym3() = "") Then Curc = Getsym3()
		If Getsym1() = "" And Getsym3() = "" Then
			Curc = Getask1()
		End If
	End Sub
	
	
	
	'UPGRADE_NOTE: Form_Terminate was upgraded to Form_Terminate_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	'UPGRADE_WARNING: WizardExpress event Form.Terminate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub Form_Terminate_Renamed()
		Me.Close()
	End Sub
	
	Private Sub WizardExpress_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		ActiveForm2 = False
	End Sub
	
	Private Sub OK_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles OK.Click
		Dim exp1 As Object
		Dim Convert As Object
		Dim i, o As Object
		Dim ins As Short
		Dim uio As Boolean
		Dim rij As Short
		Dim test4 As Short
		uio = False
		rij = 0
		'UPGRADE_WARNING: Couldn't resolve default property of object Convert. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Convert = ""
		unconvert = My.Computer.Clipboard.GetText
		If unconvert = "" Then Exit Sub
		If Not Asc(VB.Right(unconvert, 1)) = 10 Then unconvert = unconvert & Chr(13) & Chr(10)
		' 9 = new kolom
		' 10 + 13 = new rij
		' Eerste rij ..is gewoon omschrijving of letters .. dus Check eerst of meer dan drie letters. minder dan 3 letters is symbool.
		
		'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		o = 1
		
		For i = 1 To Len(unconvert)
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Asc(Mid(unconvert, i, 1)) = 9 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				exconvert = Mid(unconvert, o, i - 1 - (o - 1))
				
				If Not (Smath(0) = "") Then
					If ins = 2 Then GoTo over
					ins = 0
					exconvert = Incontrol(exconvert, ins, rij)
					If ins = 0 Then GoTo overslaan6
					If ins = 2 Then GoTo over
				End If
				uio = True
				
				If CBool(OptionAlg) = True Then exconvert = Getans(exconvert, True) Else  : exconvert = Getans(exconvert, False)
				digit = False
				If VB.Left(exconvert, 1) = " " Then exconvert = Mid(exconvert, 2)
				
				Call symbenb()
				
overslaan6: 
				
				'UPGRADE_WARNING: Couldn't resolve default property of object Convert. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Convert = Convert + exconvert + Chr(9)
				
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				o = i + 1
				GoTo over
			End If
			
			
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Asc(Mid(unconvert, i, 1)) = 10 Then
				ins = 0
				uio = True
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object exp1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				exp1 = Mid(unconvert, o + i - 2 - (o - 1), 2)
				
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				exconvert = Mid(unconvert, o, i - 2 - (o - 1))
				
				If Not (Smath(0) = "") Then
					exconvert = Incontrol(exconvert, ins, rij)
					rij = rij + 1
					If ins = 0 Then GoTo overslaan7
					If ins = 2 Then
						'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						o = i + 1 : ins = 0 : GoTo over
					End If
				End If
				If CBool(OptionAlg) = True Then exconvert = Getans(exconvert, True) Else exconvert = Getans(exconvert, False)
				If VB.Left(exconvert, 1) = " " Then exconvert = Mid(exconvert, 2)
				digit = False
				Call symbenb()
overslaan7: 
				
				'UPGRADE_WARNING: Couldn't resolve default property of object exp1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object Convert. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Convert = Convert + exconvert + exp1
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				o = i + 1
				
			End If
over: 
		Next i
		'UPGRADE_WARNING: Couldn't resolve default property of object Convert. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If uio = False Then
			If CBool(OptionAlg) = True Then
				Convert = Mid(Getans(unconvert, True), 2)
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object Convert. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Convert = Mid(Getans(unconvert, False), 2) : Call symbenb()
			End If
		End If
		My.Computer.Clipboard.Clear()
		'Sets the Text from rtfText onto the Clipboard
		My.Computer.Clipboard.SetText(Convert)
		'UPGRADE_WARNING: Couldn't resolve default property of object Convert. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ready = Convert
		Label1.BackColor = System.Drawing.ColorTranslator.FromOle(&HFF00)
		Label1.Text = eready
	End Sub
	
	Function Incontrol(ByRef Convert As String, ByRef Status As Short, ByRef rij As Short) As String
		Dim c As Object
		Dim o, e, i, ascii As Object
		Dim L As Short
		Dim uio As Boolean
		uio = False
		' This Experminet is not tested
		' First three charahters has been i
		' Status 1: maximum 3
		' Status 2: meer dan 3 en rij=0
		' Status 0: meer dan 3 en rij=1 of meer (invalid)
		Dim toegestaan As Short
		Dim PrevASCI As Short
		For i = 1 To Len(Convert)
			For ascii = 48 To 57 'cijfers
				'UPGRADE_WARNING: Couldn't resolve default property of object ascii. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Asc(Mid(Convert, i, 1)) = ascii Then
					If toegestaan = 1 Then
						'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						Incontrol = Mid(Convert, i - 1)
						Status = 1
						Exit Function
					End If
					L = L + 1
					
					
					
				End If
				'    If ascii = 32 Then
				'
			Next ascii
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Asc(Mid(Convert, i, 1)) = 32 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If i = 1 Then
					'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					o = i + 1 : GoTo verder2
				End If
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If i = o Then
					'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					o = o + 1 : GoTo verder2
				End If
				toegestaan = toegestaan + 1
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Asc(Mid(Convert, i, 1)) = 46 Then If Not L = 0 And toegestaan = 0 Then L = L + 1
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Asc(Mid(Convert, i, 1)) = 44 Then L = L + 1
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Asc(Mid(Convert, i, 1)) = Asc("-") Then L = L + 1
			'UPGRADE_WARNING: Couldn't resolve default property of object c. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			c = c + 1
verder2: 
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			PrevASCI = Asc(Mid(Convert, i, 1))
		Next i
		'geen cijfers gevonden.
		'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		i = i - 1
		'UPGRADE_WARNING: Couldn't resolve default property of object e. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		e = Len(Convert) - L
		'UPGRADE_WARNING: Couldn't resolve default property of object e. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If e > 0 Then
			If toegestaan >= 1 Then Status = 1 : Incontrol = VB.Left(Convert, L) : Exit Function
			
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object e. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If e = 0 Then
			If Not Len(Convert) = 0 Then Status = 1 : Incontrol = Convert : Exit Function
			If Len(Convert) = L Then Incontrol = Convert : Status = 0 : Exit Function
		End If
rest: 
		If rij = 0 Then Status = 2 Else Status = 0
		Incontrol = Convert
	End Function
	
	'UPGRADE_WARNING: Event Option1.CheckedChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Option1_CheckedChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Option1.CheckedChanged
		If eventSender.Checked Then
			OptionAlg = CStr(True)
		End If
	End Sub
	
	'UPGRADE_WARNING: Event Option2.CheckedChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Option2_CheckedChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Option2.CheckedChanged
		If eventSender.Checked Then
			OptionAlg = CStr(False)
		End If
	End Sub
	Private Sub algemeen()
		If TOPilse = True Then Call WindowsAPI.AlwaysOnTop(Me, True) Else Call WindowsAPI.AlwaysOnTop(Me, False)
		If Not (Getsym1() = "") Then Option1.Text = Getsym1() : symbool.Enabled = True
		If Not (Getsym3() = "") Then Option1.Text = Getsym3() : symbool.Enabled = True
		If Not (Getsym2() = "") Then Option2.Text = Getsym2() : symbool.Enabled = True
		If Not (Getsym4() = "") Then Option2.Text = Getsym4() : symbool.Enabled = True
		If Getsym1() = "" And Getsym3() = "" Then
			Option1.Text = Getask1()
			Option2.Text = Getask2()
			Curc = Getask1()
			symbool.Enabled = False
		End If
	End Sub
	
	
	Public Sub symbool_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles symbool.Click
		Dim test1 As Boolean
		If symbool.Checked = True Then
			test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "symbool", "0")
			symbool.Checked = False
		Else
			test1 = bSetRegValue(HKEY_LOCAL_MACHINE, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "symbool", "1")
			symbool.Checked = True
		End If
	End Sub
	
	Private Sub Timer1_Tick(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Timer1.Tick
		Dim raad As String
		On Error GoTo er1
		
		raad = My.Computer.Clipboard.GetText
		If Not (ready = raad) Then Label1.BackColor = System.Drawing.ColorTranslator.FromOle(&HFF) : Label1.Text = estart
		If Not (optalg = OptionAlg) Then Label1.BackColor = System.Drawing.ColorTranslator.FromOle(&HFF) : Label1.Text = estart : optalg = OptionAlg
		If Not (Getsym1() = "") Then
			If Not (Curc = Getsym1()) Then Call algemeen() : Label1.BackColor = System.Drawing.ColorTranslator.FromOle(&HFF) : Label1.Text = estart : Curc = Getsym1()
		End If
		If Not (Getsym3() = "") Then
			If Not (Curc = Getsym3()) Then Call algemeen() : Label1.BackColor = System.Drawing.ColorTranslator.FromOle(&HFF) : Label1.Text = estart : Curc = Getsym3()
		End If
		If Getsym1() = "" And Getsym3() = "" Then
			If Not (Curc = Getask1()) Then Call algemeen() : Label1.BackColor = System.Drawing.ColorTranslator.FromOle(&HFF) : Label1.Text = estart : Curc = Getask1()
		End If
er1: 
	End Sub
	
	Private Sub symbenb()
		If symbool.Enabled And symbool.Checked Then
			If CBool(OptionAlg) = True Then
				If Not (Getsym2() = "") Then exconvert = Getsym2() & " " & exconvert
				If Not (Getsym4() = "") Then exconvert = exconvert & " " & Getsym4()
			Else
				If Not (Getsym1() = "") Then exconvert = Getsym1() & " " & exconvert
				If Not (Getsym3() = "") Then exconvert = exconvert & " " & Getsym3()
			End If
		End If
		
		
	End Sub
End Class