Option Strict Off
Option Explicit On
Imports VB = Microsoft.VisualBasic
Friend Class Form4
	Inherits System.Windows.Forms.Form
	Private old, ide As Object
	Private tup As Short
	
	'UPGRADE_WARNING: Event Check1.CheckStateChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Check1_CheckStateChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Check1.CheckStateChanged
		If Check1.CheckState = 1 Then
			Check1.CheckState = System.Windows.Forms.CheckState.Checked
			Check1.Enabled = False
			defaultID = nodid
		End If
		
	End Sub
	
	Private Sub Command1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command1.Click
		Dim test As Object
		Dim numb As Short
		Dim temp As String
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test = fncGetFileNametoOpen( , "nod files|*.nod", "*.nod")
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		numb = Asc(VB.Left(test, 1))
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Not (numb = 0) Then Text1.Text = test Else Exit Sub
		If Text2.Text = "" Then
			temp = AddName((Text1.Text))
			If Not temp = "-1" Then Text2.Text = temp
		End If
		If Not temp = "-1" Then Command5.Enabled = True
	End Sub
	
	Private Sub Command2_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command2.Click
		Dim tel As Object
		Dim o As Object
		Dim ename As Object
		'UPGRADE_NOTE: name was upgraded to name_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
		Dim name_Renamed As String
		If Text2.Text = "" Then Sysmsgbox.Form_Renamed(recordempty) : Exit Sub
		Form3.Command4.Enabled = True
		On Error GoTo 61
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ename = filenod((Text1.Text))
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		FileOpen(1, ename, OpenMode.Input)
		FileClose(1)
		If max = -1 Then nodid = 0 : Form3.Combo1.Text = Text2.Text : Form3.Command6.Enabled = True
		
		If Add = True Then
			For o = 0 To max
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If UCase(VB6.GetItemString(Form3.Combo1, o)) = UCase(Text2.Text) Then Sysmsgbox.Form_Renamed(Exists) : Exit Sub
			Next o
			max = max + 1
			Form3.Combo1.Items.Insert(max, Text2.Text)
			'UPGRADE_WARNING: Couldn't resolve default property of object tel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			tel = Len(Apppaths & "\")
			'UPGRADE_WARNING: Couldn't resolve default property of object tel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If VB.Left(Text1.Text, tel) = Apppaths & "\" Then Text1.Text = Mid(Text1.Text, tel + 1)
			'UPGRADE_WARNING: Couldn't resolve default property of object tel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			tel = Len(Text1.Text)
			'UPGRADE_WARNING: Couldn't resolve default property of object tel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If UCase(VB.Right(Text1.Text, 3)) = "NOD" Then Text1.Text = VB.Left(Text1.Text, tel - 4)
			filestring(max) = Text1.Text
			Add = False
			Form3.Command4.Enabled = True
			Me.Close()
			Exit Sub
		End If
		For o = 0 To max
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If o = nodid Then GoTo over
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If UCase(VB6.GetItemString(Form3.Combo1, o)) = UCase(Text2.Text) Then Sysmsgbox.Form_Renamed(Exists) : Exit Sub
over: 
		Next o
		VB6.SetItemString(Form3.Combo1, nodid, Text2.Text)
		'UPGRADE_WARNING: Couldn't resolve default property of object tel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		tel = Len(Apppaths & "\")
		'UPGRADE_WARNING: Couldn't resolve default property of object tel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If VB.Left(Text1.Text, tel) = Apppaths & "\" Then Text1.Text = Mid(Text1.Text, tel + 1)
		'UPGRADE_WARNING: Couldn't resolve default property of object tel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		tel = Len(Text1.Text)
		'UPGRADE_WARNING: Couldn't resolve default property of object tel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If UCase(VB.Right(Text1.Text, 3)) = "NOD" Then Text1.Text = VB.Left(Text1.Text, tel - 4)
		filestring(nodid) = Text1.Text
		Me.Command4.Enabled = True
		Form3.Combo1.SelectedIndex = nodid
		Form3.Command6.Enabled = True
		Me.Close()
		Exit Sub
61: 
		Sysmsgbox.Form_Renamed(Text1.Text & isNotFound)
	End Sub
	
	Private Sub Command3_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command3.Click
		Add = False
		'UPGRADE_WARNING: Couldn't resolve default property of object old. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		defaultID = old
		Me.Close()
	End Sub
	
	Private Sub Command4_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command4.Click
		Dim o As Object
		Dim fis As Short
		fis = MsgBox(Sure, MsgBoxStyle.YesNo + MsgBoxStyle.Question)
		'delete
		If fis = 7 Then Exit Sub
		Form3.Command4.Enabled = True
		If nodid < defaultID Then defaultID = defaultID - 1
		If nodid = defaultID Then If nodid = 0 Then defaultID = 0 Else defaultID = nodid - 1
		For o = nodid To max
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If o = max Then Form3.Combo1.Items.RemoveAt((max)) : filestring(max) = "" : max = max - 1 : If max > -1 Then Form3.Combo1.SelectedIndex = 0 : Me.Close() : Form3.Show() : Exit Sub Else Me.Close() : Form3.Command6.Enabled = False : Form3.Show() : Exit Sub
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			VB6.SetItemString(Form3.Combo1, o, VB6.GetItemString(Form3.Combo1, o + 1))
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			filestring(o) = filestring(o + 1)
		Next o
		
	End Sub
	
	Private Sub Command5_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command5.Click
		Editor.Show()
	End Sub
	
	Private Sub Form4_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
		'UPGRADE_WARNING: Couldn't resolve default property of object old. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		old = defaultID
		If max = -1 Then defaultID = nodid : Check1.CheckState = System.Windows.Forms.CheckState.Checked : Check1.Enabled = False
		Call Languare(Lname, 7)
		Form3.Enabled = False
		If Add = False Then Text1.Text = filestring(nodid)
		If Add = True Then Command5.Enabled = False : Command4.Enabled = False : Text2.Text = "" : Text1.Text = "" : Exit Sub Else Text2.Text = VB6.GetItemString(Form3.Combo1, nodid)
		If nodid = defaultID Then Check1.CheckState = System.Windows.Forms.CheckState.Checked : Check1.Enabled = False
		
	End Sub
	
	
	'UPGRADE_NOTE: Form_Terminate was upgraded to Form_Terminate_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	'UPGRADE_WARNING: Form4 event Form.Terminate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub Form_Terminate_Renamed()
		Form3.Enabled = True
		Me.Close()
	End Sub
	
	Private Sub Form4_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		Form3.Enabled = True
		Me.Close()
	End Sub
	
	
	
	'UPGRADE_WARNING: Event text1.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub text1_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles text1.TextChanged
		Dim KeyAscii As Object
		Dim test2 As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object KeyAscii. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object test2.Text. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If KeyAscii = 13 Then If Text2.Text = "" Then test2.Text = AddName((Text1.Text))
	End Sub
End Class