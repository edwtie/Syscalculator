Option Strict Off
Option Explicit On
Imports VB = Microsoft.VisualBasic
Friend Class standard
	Inherits System.Windows.Forms.Form
	Dim dflag(2) As Short
	Dim i(2) As Short
	Dim opnre(2) As Short
	Dim prev(2) As Object
	Dim oflag(2) As Short
	Dim ind(2) As Short
	Dim result(2) As String
	Dim memo(2) As Object
	Dim settings(2) As Short
	Dim Csetting(2) As Short
	Dim schaal As Short
	Dim digitSwitch As String
	
	
	
	Private Sub Command1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command1.Click
		Dim Index As Short = Command1.GetIndex(eventSender)
		If Form1.Option2.Checked = True Then schaal = 1 Else schaal = 0
		If tikactive = True Then Call System2() : oflag(schaal) = 1 : tikactive = False
		
		If ind(schaal) = 4 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			prev(schaal) = 0
			ind(schaal) = 0
			result(schaal) = ""
			Call System1()
		End If
		opnre(schaal) = 0
		If oflag(schaal) = 0 Then
			result(schaal) = ""
			Call System1()
		End If
		oflag(schaal) = 1
		If Command1(Index).Text <> digitSwitch Then
			
			If result(schaal) <> " 0" Then
				result(schaal) = result(schaal) & Command1(Index).Text
			Else
				result(schaal) = " " & Command1(Index).Text
			End If
		Else
			If dflag(schaal) = 0 Then
				If InStr(result(schaal), digitSwitch) = 0 Then
					result(schaal) = result(schaal) & digitSwitch
				End If
				dflag(schaal) = 1
			Else
				
			End If
		End If
		
		Call System1()
	End Sub
	
	Private Sub Command2_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command2.Click
		Dim Index As Short = Command2.GetIndex(eventSender)
		If Form1.Option2.Checked = True Then schaal = 1 Else schaal = 0
		If result(schaal) = "0" Then Call System2()
		result(schaal) = ConvertPunt(result(schaal))
		If Index = 8 Then
			result(schaal) = ConvertPunt(result(schaal))
			If Not result(schaal) = "" Then result(schaal) = CStr(System.Math.Sqrt(CDbl(result(schaal))))
			Index = ind(schaal)
			result(schaal) = Convertkomma(result(schaal))
			Call System1()
			Exit Sub
		End If
		If Index = 7 Then
			result(schaal) = ConvertPunt(result(schaal))
			If Not result(schaal) = "" Then
				If CDec(result(schaal)) = 0 Then
					MsgBox(errorzero)
					Exit Sub
				End If
				result(schaal) = CStr(1 / CDbl(result(schaal)))
			End If
			Index = ind(schaal)
			result(schaal) = Convertkomma(result(schaal))
			Call System1()
			Exit Sub
		End If
		
		If Index = 6 Then
			result(schaal) = ConvertPunt(result(schaal))
			If Not result(schaal) = "" Then result(schaal) = CStr(CDbl(result(schaal)) * -1)
			Index = ind(schaal)
			result(schaal) = Convertkomma(result(schaal))
			Call System1()
			Exit Sub
		End If
		If Index = 5 Then
			result(schaal) = ConvertPunt(result(schaal))
			If Not result(schaal) = "" Then
				result(schaal) = CStr(CDec(CDbl(result(schaal)) / 100))
				'UPGRADE_WARNING: Couldn't resolve default property of object prev(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				result(schaal) = CStr(CDec(prev(schaal) * CDbl(result(schaal))))
			End If
			Index = ind(schaal)
			result(schaal) = Convertkomma(result(schaal))
			Call System1()
			Exit Sub
		End If
		If opnre(schaal) = 0 Or Index = 4 Then
			If ind(schaal) = 0 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object prev(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				prev(schaal) = CDec(prev(schaal) + result(schaal))
			ElseIf ind(schaal) = 1 Then 
				'UPGRADE_WARNING: Couldn't resolve default property of object prev(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				prev(schaal) = CDec(prev(schaal) - result(schaal))
			ElseIf ind(schaal) = 2 Then 
				If CDec(result(schaal)) = 0 Then
					MsgBox(errorzero)
					Exit Sub
				Else
					'UPGRADE_WARNING: Couldn't resolve default property of object prev(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					prev(schaal) = CDec(prev(schaal) / CDbl(result(schaal)))
				End If
			ElseIf ind(schaal) = 3 Then 
				'UPGRADE_WARNING: Couldn't resolve default property of object prev(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				prev(schaal) = CDec(prev(schaal) * CDbl(result(schaal)))
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			result(schaal) = prev(schaal)
			oflag(schaal) = 0
		End If
		opnre(schaal) = 1
		ind(schaal) = Index
		dflag(schaal) = 0
		
		'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object prev(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If prev(schaal) > 0 And prev(schaal) < 1 Then
			result(schaal) = "0" & Mid(prev(schaal), 2)
		Else
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If prev(schaal) > 0 And VB.Left(prev(schaal), 1) = " " Then result(schaal) = Mid(prev(schaal), 2)
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If prev(schaal) < 0 Then result(schaal) = prev(schaal)
		result(schaal) = Convertkomma(result(schaal))
		Call System1()
	End Sub
	
	Private Sub Command3_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command3.Click
		If Form1.Option2.Checked = True Then schaal = 1 Else schaal = 0
		dflag(schaal) = 0
		result(schaal) = ""
		Call System1()
	End Sub
	
	Private Sub Command4_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command4.Click
		If Form1.Option2.Checked = True Then schaal = 1 Else schaal = 0
		
		If settings(schaal) = 1 Then
			Csetting(schaal) = 1
		Else
			'UPGRADE_WARNING: Couldn't resolve default property of object memo(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			memo(schaal) = 0
			'UPGRADE_WARNING: Couldn't resolve default property of object memo(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			Memorystatus.Text = Mid(Convertkomma(Str(memo(schaal))), 2)
		End If
		dflag(schaal) = 0
		'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		prev(schaal) = 0
		oflag(schaal) = 0
		ind(schaal) = 0
		opnre(schaal) = 0
		result(schaal) = ""
		Call System1()
	End Sub
	
	Private Sub Command5_Click()
		Me.Close()
		
	End Sub
	
	Private Sub Command7_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command7.Click
		Dim Index As Short = Command7.GetIndex(eventSender)
		If Form1.Option2.Checked = True Then schaal = 1 Else schaal = 0
		If result(schaal) = "" Then result(schaal) = CStr(0)
		Select Case Index
			Case 0
				Call System2()
				
				result(schaal) = Puntkomma(result(schaal))
				'UPGRADE_WARNING: Couldn't resolve default property of object memo(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object memo(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Not result(schaal) = "" Then memo(schaal) = memo(schaal) + CDec(result(schaal))
			Case 1
				settings(schaal) = 1
				'UPGRADE_WARNING: Couldn't resolve default property of object memo(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Not result(schaal) = "" Then
					memo(schaal) = CDec(result(schaal))
				Else
					'UPGRADE_WARNING: Couldn't resolve default property of object memo(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					memo(schaal) = ""
				End If
				If Csetting(schaal) = 1 Then
					'UPGRADE_WARNING: Couldn't resolve default property of object memo(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					memo(schaal) = 0 : settings(schaal) = 0 : Csetting(schaal) = 0
				End If
				oflag(schaal) = 0
			Case 4
				Csetting(schaal) = 0
				'UPGRADE_WARNING: Couldn't resolve default property of object memo(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				result(schaal) = memo(schaal)
				If settings(schaal) = 1 Then
					opnre(schaal) = 0
				Else
					'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If Not result(schaal) = "" Then
						prev(schaal) = CDec(result(schaal))
					Else
						'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						prev(schaal) = ""
					End If
				End If
				result(schaal) = Convertkomma(result(schaal))
				If VB.Left(result(schaal), 1) = " " Then result(schaal) = Mid(result(schaal), 2)
				Call System1()
			Case 5
				'UPGRADE_WARNING: Couldn't resolve default property of object memo(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				memo(schaal) = 0
				settings(schaal) = 0
				Csetting(schaal) = 0
		End Select
		'UPGRADE_WARNING: Couldn't resolve default property of object memo(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Memorystatus.Text = Mid(Convertkomma(Str(memo(schaal))), 2)
	End Sub
	
	Private Sub Convert_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Convert.Click
		Dim stringer As Object
		If Form1.Option1.Checked = True Then
			'UPGRADE_WARNING: Couldn't resolve default property of object stringer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If switch1 = False Then
				stringer = Getans((Form1.Text1.Text), True)
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object stringer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				stringer = Getans((Form1.Text1.Text), False)
			End If
			
			'UPGRADE_WARNING: Couldn't resolve default property of object stringer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If VB.Left(stringer, 1) = " " Then
				Form1.Text2.Text = Mid(stringer, 2)
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object stringer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Form1.Text2.Text = stringer
			End If
			
			If Not (Sform = "") Then Form1.Text1.Text = nulnul((Form1.Text1.Text))
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(1). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			prev(1) = 0
			ind(1) = 0
			dflag(1) = 0
			oflag(1) = 0
			ind(1) = 0
			opnre(1) = 0
		End If
		If Form1.Option2.Checked = True Then
			'UPGRADE_WARNING: Couldn't resolve default property of object stringer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If switch1 = False Then
				stringer = Getans((Form1.Text2.Text), False)
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object stringer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				stringer = Getans((Form1.Text2.Text), True)
			End If
			
			'UPGRADE_WARNING: Couldn't resolve default property of object stringer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If VB.Left(stringer, 1) = " " Then
				Form1.Text1.Text = Mid(stringer, 2)
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object stringer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Form1.Text1.Text = stringer
			End If
			
			
			If Not (Sform = "") Then Form1.Text2.Text = nulnul((Form1.Text2.Text))
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(0). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
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
		Dim Index As Object
		
		Call System2()
		Call System1()
		opnre(schaal) = 0
		'UPGRADE_WARNING: Couldn't resolve default property of object Index. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ind(schaal) = Index
		dflag(schaal) = 0
		oflag(schaal) = 0
	End Sub
	
	
	
	Public Sub eexit_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles eexit.Click
		Me.Close()
	End Sub
	
	'UPGRADE_WARNING: Form event standard.Activate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub standard_Activated(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Activated
		Call Languare(Lname, 5)
		If TOPilse = True Then Call WindowsAPI.AlwaysOnTop(Me, True) Else Call WindowsAPI.AlwaysOnTop(Me, False)
		
	End Sub
	
	Private Sub standard_GotFocus(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.GotFocus
		Exit Sub
	End Sub
	
	Private Sub standard_KeyPress(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.KeyPressEventArgs) Handles MyBase.KeyPress
		Dim KeyAscii As Short = Asc(eventArgs.KeyChar)
		Dim ioo As Short
		If KeyAscii = Asc(".") Then
			ioo = 10
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("0") Then 
			ioo = 0
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("1") Then 
			ioo = 1
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("2") Then 
			ioo = 2
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("3") Then 
			ioo = 3
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("4") Then 
			ioo = 4
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("5") Then 
			ioo = 5
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("6") Then 
			ioo = 6
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("7") Then 
			ioo = 7
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("8") Then 
			ioo = 8
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("9") Then 
			ioo = 9
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("0") Then 
			ioo = 0
			Command1_Click(Command1.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("+") Then 
			ioo = 0
			Command2_Click(Command2.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("+") Then 
			ioo = 0
			Command2_Click(Command2.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("-") Then 
			ioo = 1
			Command2_Click(Command2.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("/") Then 
			ioo = 2
			Command2_Click(Command2.Item(ioo), New System.EventArgs())
			Beep()
			
		ElseIf KeyAscii = Asc("*") Then 
			ioo = 3
			Command2_Click(Command2.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("=") Then 
			ioo = 4
			Command2_Click(Command2.Item(ioo), New System.EventArgs())
			Beep()
		ElseIf KeyAscii = Asc("c") Or KeyAscii = Asc("C") Then 
			dflag(schaal) = 0
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(schaal). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			prev(schaal) = 0
			oflag(schaal) = 0
			ind(schaal) = 0
			opnre(schaal) = 0
			result(schaal) = " 0"
			Beep()
			Beep()
		ElseIf KeyAscii = Asc("d") Or KeyAscii = Asc("D") Then 
			result(schaal) = " 0"
			Beep()
		End If
		eventArgs.KeyChar = Chr(KeyAscii)
		If KeyAscii = 0 Then
			eventArgs.Handled = True
		End If
	End Sub
	
	
	Private Sub standard_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
		Dim OK As Object
		Call Languare(Lname, 5)
		ActiveCal = True
		Dim mil As String
		
		'UPGRADE_WARNING: Couldn't resolve default property of object setting(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object OK. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		OK = setting(digitSwitch, mil)
		Command1(10).Text = digitSwitch
		
		If TOPilse = True Then Call WindowsAPI.AlwaysOnTop(Me, True) Else Call WindowsAPI.AlwaysOnTop(Me, False)
		
		'  standard.Height = 4090
		'  standard.Width = 3430
		Dim i As Short
		For i = 0 To 1
			dflag(i) = 0
			'UPGRADE_WARNING: Couldn't resolve default property of object prev(i). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			prev(i) = 0
			oflag(i) = 0
			ind(i) = 0
			opnre(i) = 0
		Next i
		My.Computer.Clipboard.Clear()
		Call System2()
		
	End Sub
	
	
	
	
	
	Private Sub System1()
		If Form1.Option1.Checked = True Then Form1.Text1.Text = result(0) Else Form1.Text2.Text = result(1)
		
	End Sub
	Private Sub System2()
		If oflag(schaal) = 0 Then
			If Form1.Option1.Checked = True Then
				If Not (Form1.Text1.Text = "") Then
					If Form1.Text1.Text = "0" Then Exit Sub
					result(0) = Form1.Text1.Text
					oflag(0) = 1
					Exit Sub
				End If
			End If
			If Form1.Option2.Checked = True Then
				If Not (Form1.Text2.Text = "") Then
					If Form1.Text2.Text = "0" Then Exit Sub
					result(1) = Form1.Text2.Text
					oflag(1) = 1
					Exit Sub
				End If
			End If
		End If
		
		
		If Form1.Option1.Checked = True Then result(0) = Form1.Text1.Text Else result(1) = Form1.Text2.Text
		
	End Sub
	Public Function Puntkomma(ByRef ans As String) As String
		Dim maximum As Short
		Dim res As Short
		Dim iee As Short
		maximum = Len(ans)
		If VB.Left(VB.Right(ans, 2), 1) = "," Then Puntkomma = VB.Left(ans, maximum - 2) & "." & VB.Right(ans, 1) & "0" : Exit Function
		For iee = 1 To Len(ans)
			If Mid(ans, iee, 1) = "," Then
				res = Len(Mid(ans, iee + 1))
				Puntkomma = VB.Left(ans, maximum - res - 1) & "." & VB.Right(ans, res) : Exit Function
			End If
		Next iee
		Puntkomma = ans
	End Function
	
	'UPGRADE_NOTE: Form_Terminate was upgraded to Form_Terminate_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	'UPGRADE_WARNING: standard event Form.Terminate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub Form_Terminate_Renamed()
		Me.Close()
	End Sub
	
	Private Sub standard_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		ActiveCal = False
		Me.Close()
	End Sub
End Class