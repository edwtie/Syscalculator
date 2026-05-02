Option Strict Off
Option Explicit On
Module Module1
	Public Tic As NOTIFYICONDATA
	Public lasttime As Object
	Public fORMVALUE3 As Boolean
	Public Apppaths As String
	Public formchg As Boolean
	Public Decimals As Boolean
	Public flags As Short
	Public filestring(255) As String
	Public tray As Boolean
	Public MaxMath As Short
	Public donateurl As Short
	Public defaultID As Short
	Public Alreadyerror As String
	Public nodid As Short
	Public Exists As String
	Public onlyeditor As Short
	Public onlyedname As String
	Public recordempty As String
	Public Sure As String
	Public Add As Boolean
	Public max As Short
	Public maxchg As Short
	Public Ichg As Short
	Public TOPilse As Boolean
	Public Apply As Boolean
	Public tikactive As Boolean
	Public Configurl As String
	Public Configur2 As String
	Public Configur3 As String
	Public eready As String
	Public estart As String
	Public isNotFound As String
	Public errorf As String
	Public withoutcfg As String
	Public Lname As String
	Public ActiveForm2 As Boolean
	Public ActiveCal As Boolean
	Public intro As Boolean
	Public Edname As String
	Public ControlFout As String
	Public zondername As String
	Public textChanged As String
	Public editornaam As String
	Public transtel As Short
	Public nomatches As String
	Public findrepl As String
	Public replacement As String
	Public textsereach As String
	Public errorzero As String
	Public switch1 As Boolean
	Public iX, iY As Short
	
	
	
	Sub form1cleanup()
		Dim test1 As Object
		Dim uitnegatief As Object
		Dim a As Object
		Dim ind As Object
		
		'UPGRADE_WARNING: Couldn't resolve default property of object ind. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ind = Addcombo(1)
		'UPGRADE_WARNING: Couldn't resolve default property of object ind. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If ind = -1 Then Exit Sub
		'UPGRADE_WARNING: Couldn't resolve default property of object ind. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		a = OpenNOD(filestring(ind) & ".nod", 1)
		'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If a = False Then Form1.Close() : Exit Sub
		'UPGRADE_WARNING: Couldn't resolve default property of object uitnegatief. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		uitnegatief = False
		
		Form1.Text = Getappname()
		'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Name", Form1.Text)
		
		If switch1 = False Then Form1.Label1.Text = Getsym1() Else Form1.Label1.Text = Getsym2()
		If switch1 = False Then Form1.Label2.Text = Getsym2() Else Form1.Label2.Text = Getsym1()
		If switch1 = False Then Form1.Label5.Text = Getsym3() Else Form1.Label5.Text = Getsym4()
		If switch1 = False Then Form1.Label6.Text = Getsym4() Else Form1.Label6.Text = Getsym3()
		If switch1 = False Then Form1.Label3.Text = Getask1() Else Form1.Label3.Text = Getask2()
		If switch1 = False Then Form1.Label4.Text = Getask2() Else Form1.Label4.Text = Getask1()
		
		
		
	End Sub
	
	Function Addcombo(Optional ByRef clean As Short = 0) As Short
		Dim test5 As Object
		Dim oldname As Object
		Dim old As Object
		Dim i As Object
		Dim Data, comm, def As Object
		Dim reserve As String
		Dim OK As Short
		'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		i = 0
		If clean = 1 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object old. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			old = Form1.Combo1.SelectedIndex
			'UPGRADE_WARNING: Couldn't resolve default property of object oldname. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			oldname = Form1.Combo1.Text
		End If
		
		Form1.Combo1.Items.Clear()
		On Error GoTo geenconfig
		If clean = 2 Then
			FileOpen(1, My.Application.Info.DirectoryPath & "\" & "freesyscal.cfg", OpenMode.Input)
		Else
			FileOpen(1, Apppaths & "\" & "freesyscal.cfg", OpenMode.Input)
		End If
		Do Until EOF(1)
			Input(1, comm)
			Input(1, Data)
			Input(1, def)
			'UPGRADE_WARNING: Couldn't resolve default property of object comm. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not Left(comm, 1) = "'" Then
				
				'UPGRADE_WARNING: Couldn't resolve default property of object comm. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If comm = "[lang]" Then
					'UPGRADE_WARNING: Couldn't resolve default property of object Data. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					Lname = Data
				Else
					'UPGRADE_WARNING: Couldn't resolve default property of object comm. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If Not (comm = "") Then Form1.Combo1.Items.Insert(i, comm)
					'UPGRADE_WARNING: Couldn't resolve default property of object Data. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If Not (Data = "") Then
						'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						'UPGRADE_WARNING: Couldn't resolve default property of object Data. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						filestring(i) = Data
					End If
					'UPGRADE_WARNING: Couldn't resolve default property of object def. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If def = "*" Then
						'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						defaultID = i
					End If
					'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					i = i + 1
				End If
			End If
		Loop 
		FileClose(1)
		'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		max = i - 1
		If max > -1 And Not clean = 1 Then Form1.Combo1.SelectedIndex = defaultID : Addcombo = defaultID
		If clean = 1 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object old. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If old < max And old < -1 Then
				' Form1.Combo1.Text = oldname
				'UPGRADE_WARNING: Couldn't resolve default property of object old. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Form1.Combo1.SelectedIndex = old
				'UPGRADE_WARNING: Couldn't resolve default property of object old. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Addcombo = old
			Else
				Addcombo = defaultID
				Form1.Combo1.SelectedIndex = defaultID
			End If
		End If
		Exit Function
geenconfig: 
		Dim Status As Boolean
		Addcombo = -1
		'UPGRADE_WARNING: Couldn't resolve default property of object test5. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test5 = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "first")
		'UPGRADE_WARNING: Couldn't resolve default property of object test5. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If test5 = 0 Then Status = MsgBox("Configuration is not found", MsgBoxStyle.Critical, errorf) : Form1.Close()
	End Function
	Sub SaveTextFile()
		Dim numb As Object
		Dim test As Object
		On Error GoTo Felhantering
		If Edname = "" Then
			'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test = fncGetFileNametoSave("nod files|*.nod", "*.nod", "Save")
			'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object numb. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			numb = Asc(Left(test, 1))
			'UPGRADE_WARNING: Couldn't resolve default property of object numb. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not (numb = 0) Then Editor.Text1.Text = test Else Exit Sub
			'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			Edname = test
		End If
		Dim tel As Short
		Dim ename As Object
		Dim name As String
		Editor.Text = editornaam & " - " & Form4.Text1.Text
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ename = filenod(Edname)
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		FileOpen(1, ename, OpenMode.Output)
		PrintLine(1, Editor.Text1.Text)
		FileClose(1)
		Exit Sub
Felhantering: 
		FileClose(1)
		MsgBox(Edname & " " & isNotFound)
	End Sub
	Sub SetForms(ByRef ints As Boolean)
		
		'UPGRADE_ISSUE: Form method Form1.Cls was not upgraded. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="CC4C7EC0-C903-48FC-ACCC-81861D12DA4A"'
		Form1.Cls()
		If TOPilse = True Then
			Call WindowsAPI.AlwaysOnTop(Form1, ints)
			If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, ints) : WizardExpress.Enabled = ints
			If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, ints) : standard.Enabled = ints
			Exit Sub
		End If
		If ActiveForm2 = True Then WizardExpress.Enabled = ints
		If ActiveCal = True Then standard.Enabled = ints
		
	End Sub
	
	Sub SaveAsTextFile()
		Dim numb As Object
		Dim test As Object
		On Error GoTo Felhantering
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test = fncGetFileNametoSave("nod files|*.nod", "*.nod", "Save")
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object numb. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		numb = Asc(Left(test, 1))
		'UPGRADE_WARNING: Couldn't resolve default property of object numb. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Not (numb = 0) Then Edname = test Else Exit Sub
		Form4.Text1.Text = Edname
		Editor.Text = editornaam & " - " & Edname
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		FileOpen(1, test, OpenMode.Output)
		PrintLine(1, Editor.Text1.Text)
		FileClose(1)
		Exit Sub
Felhantering: 
		MsgBox(Edname & " " & isNotFound)
	End Sub
	Sub OpenTextFile()
		Dim numb As Object
		Dim test As Object
		Editor.Text1.Text = ""
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test = fncGetFileNametoOpen( , "nod files|*.nod", "*.nod")
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object numb. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		numb = Asc(Left(test, 1))
		'UPGRADE_WARNING: Couldn't resolve default property of object numb. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Not (numb = 0) Then Edname = test Else Exit Sub
		If Edname = "" Then Exit Sub
		'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		FileOpen(1, test, OpenMode.Input)
		Do While Not EOF(1)
			Editor.Text1.Text = Editor.Text1.Text & InputString(1, 1)
		Loop 
		FileClose(1)
	End Sub
	Function saveconfig(ByRef naam As String, ByRef Combo1 As System.Windows.Forms.ComboBox, Optional ByRef groen As Boolean = True) As String
		Dim o As Object
		Dim comm, Data As Object
		Dim def As String
		Dim OK As Short
		Dim tel As Short
		tel = Len(Apppaths & "\")
		If groen = True Then
			If LCase(Left(naam, tel)) = LCase(Apppaths & "\") Then naam = Mid(naam, tel + 1)
		End If
		On Error GoTo geenconfig
		FileOpen(1, Apppaths & "\" & "freesyscal.cfg", OpenMode.Output)
		WriteLine(1, "[lang]", naam, "")
		For o = 0 To max
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If defaultID = o Then def = "*"
			'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			WriteLine(1, VB6.GetItemString(Combo1, o), filestring(o), def)
			def = ""
		Next o
		If groen = True Then
			Lname = naam
			saveconfig = naam
		End If
		FileClose(1)
		Exit Function
geenconfig: 
	End Function
End Module