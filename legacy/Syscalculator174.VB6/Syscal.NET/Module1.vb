Option Strict Off
Option Explicit On
Module Hong32
	'DLL hong-technology 1998-2001
	'new architecture
	
	Public Const HWND_TOPMOST As Short = -1
	Public Const HWND_NOTOPMOST As Short = -2
	Public foutmelding As String
	Public symbool1 As String
	Public symbool2 As String
	Public symbool3 As String
	Public symbool4 As String
	Public symbola1 As Short
	Public symbola2 As Short
	Public symbola3 As Short
	Public symbola4 As Short
	'UPGRADE_NOTE: Switch was upgraded to Switch_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	Public Switch_Renamed As Boolean
	Public vraag1 As String
	Public vraag2 As String
	Public Appsnaam As String
	Public Indrotekst As String
	Public Smath(20) As String
	Public chgo(25565) As String
	Public chgn(25565) As String
	Public transo(25565) As String
	Public transn(25565) As String
	
	Public Sform As String
	Public booldigit As Boolean
	Public TSform As String
	Public lndro As String
	Public lndrn As String
	Public digit As Boolean
	Public expdate As String
	Public phcode As Short
	Public decil As Short
	Public decils As Boolean
	
	
	Public Function SortArray(ByVal lengte As Short, ByRef strArray() As String) As Object
		Dim intOut, intIn As Short
		Dim strTemp As String
		
		'loop through array
		For intOut = LBound(strArray) To lengte
			For intIn = intOut + 1 To lengte
				'check if the inner loop's current dimension is
				'higher precendence, then the outer. If so, swap
				'them.
				If FirstInAlphabeticalOrder(strArray(intOut), strArray(intIn)) = 2 Then
					strTemp = strArray(intIn)
					strArray(intIn) = strArray(intOut)
					strArray(intOut) = strTemp
				End If
			Next intIn
		Next intOut
		
	End Function
	
	Function FirstInAlphabeticalOrder(ByRef strOne As String, ByRef strTwo As String) As Integer
		
		
		
		Dim intChar, intLen As Short
		Dim strChar1, strChar2 As String
		
		'Check to see which string has more length
		'assign intLen% the length of that string.
		If Len(strOne) > Len(strTwo) Then
			intLen = Len(strOne)
		ElseIf Len(strTwo) > Len(strOne) Then 
			intLen = Len(strTwo)
		Else
			intLen = Len(strOne)
		End If
		
		
		For intChar = 1 To intLen
			strChar1 = UCase(Mid(strOne, intChar, 1))
			strChar2 = UCase(Mid(strTwo, intChar, 1))
			
			'if no more character's are left on a string
			'then that string automatically takes precedence.
			'So exit the function.
			If Len(strChar1) = 0 Then
				FirstInAlphabeticalOrder = 1
				Exit Function
			ElseIf Len(strChar2) = 0 Then 
				FirstInAlphabeticalOrder = 2
				Exit Function
			End If
			
			'if character ascii value is between the ascii
			'value of 'A' and 'Z', and the other character's
			'ascii value is not. Precednce goes to the first
			'string. If that and vice-versa is false. Check
			'which ascii value is lower than the other ascii value.
			'If one if it is lower that string takes precedence. If
			'their equal, continue to the next character.
			
			If Asc(strChar1) >= Asc("A") And Asc(strChar1) <= Asc("Z") And Asc(strChar2) <= Asc("A") And Asc(strChar2) >= Asc("Z") Then
				FirstInAlphabeticalOrder = 1
				Exit Function
			ElseIf Asc(strChar2) >= Asc("A") And Asc(strChar2) <= Asc("Z") And Asc(strChar1) <= Asc("A") And Asc(strChar1) >= Asc("Z") Then 
				FirstInAlphabeticalOrder = 2
				Exit Function
			ElseIf Asc(strChar1) < Asc(strChar2) Then 
				FirstInAlphabeticalOrder = 1
				Exit Function
			ElseIf Asc(strChar2) < Asc(strChar1) Then 
				FirstInAlphabeticalOrder = 2
				Exit Function
			End If
			
		Next intChar
		
	End Function
	Function setDec(ByRef i As Short) As Object
		decil = i
		If decil = -1 Then decils = False : Exit Function
		decils = True
	End Function
	Function resort() As Object
		Dim o As Object
		Dim lengte As Short
		lengte = Form3.Combo1.Items.Count
		Dim MyArray(255) As String
		'declare array that we're gona sort
		Dim NewString(255) As String
		Dim intBuffer As Short
		Dim i As Short
		For intBuffer = 0 To lengte
			If Not VB6.GetItemString(Form3.Combo1, intBuffer) = "" Then MyArray(intBuffer) = VB6.GetItemString(Form3.Combo1, intBuffer) Else Exit For
		Next intBuffer
		
		Call SortArray(lengte - 1, MyArray)
		
		For i = 0 To lengte
			For o = 0 To lengte
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Not VB6.GetItemString(Form3.Combo1, o) = "" Then
					'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If VB6.GetItemString(Form3.Combo1, o) = MyArray(i) Then
						'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						NewString(i) = filestring(o)
						'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						If defaultID = o Then defaultID = i
						Exit For
					End If
				End If
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If VB6.GetItemString(Form3.Combo1, o) = "" Then Exit For
			Next o
			
		Next i
		
		Form3.Combo1.Items.Clear()
		
		For intBuffer = 0 To lengte
			If Not MyArray(intBuffer) = "" Then
				Form3.Combo1.Items.Insert(intBuffer, MyArray(intBuffer))
				If defaultID = intBuffer Then Form3.Combo1.Text = MyArray(intBuffer)
				filestring(intBuffer) = NewString(intBuffer)
			End If
		Next intBuffer
		
	End Function
	
	
	Function AddName(ByRef invoer As String) As String
		Dim tes As Object
		Dim insr As Object
		Dim inst As Object
		Dim Commando As Object
		Dim ename As Object
		On Error GoTo 0
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ename = filenod(invoer)
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		FileOpen(1, ename, OpenMode.Input)
		Do Until EOF(1)
			Commando = LineInput(1)
			'UPGRADE_WARNING: Couldn't resolve default property of object Commando. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If UCase(Left(Commando, 4)) = "URLN" Then
				If AddName = "" Then
					'UPGRADE_WARNING: Couldn't resolve default property of object Commando. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					AddName = Mid(Commando, 6) : FileClose(1) : Exit Function
				End If
			End If
		Loop 
		FileClose(1)
		'UPGRADE_WARNING: Couldn't resolve default property of object inst. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		inst = InStr(1, invoer, ".nod")
		Dim temp2 As String
		'UPGRADE_WARNING: Couldn't resolve default property of object inst. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		temp2 = Left(invoer, inst - 1)
		'UPGRADE_WARNING: Couldn't resolve default property of object insr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		insr = 1
		'UPGRADE_WARNING: Couldn't resolve default property of object insr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Do While Not insr = 0
			'UPGRADE_WARNING: Couldn't resolve default property of object insr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			insr = InStr(1, temp2, "\")
			'UPGRADE_WARNING: Couldn't resolve default property of object insr. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			temp2 = Mid(temp2, insr + 1)
			AddName = temp2
		Loop 
		
		Exit Function
0: 
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object tes. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		tes = MsgBox(ename + isNotFound, MsgBoxStyle.OKOnly)
		AddName = "-1"
	End Function
	
	
	
	
	Function OpenNOD(ByRef name As String, ByRef Status As Short) As Boolean
		' Statussen:
		' 1  Labels veranderen
		' 2  uitvoeren (execute Nod files)
		' 3  lezen, foutencontrole
		Indrotekst = ""
		symbool1 = ""
		symbool2 = ""
		symbool3 = ""
		symbool4 = ""
		Dim a As Object
		Dim n As Short
		If Not (Status = 3) Then
			For n = 0 To transtel
				transo(n) = ""
				transn(n) = ""
			Next n
			transtel = 0
			
			For n = 0 To ichg
				chgn(n) = ""
				chgo(n) = ""
			Next n
			ichg = 0
			For n = 0 To MaxMath
				Smath(n) = ""
			Next n
			Sform = ""
			
			MaxMath = 0
		End If
		
		Dim Commando As String
		Dim ename As String
		Dim tel As Short
		On Error GoTo 61
		ename = filenod(name)
		FileOpen(1, ename, OpenMode.Input)
		Do Until EOF(1)
			Commando = LineInput(1)
			If Left(Commando, 1) = "'" Then
				'GoTo overstap 'rem only
			ElseIf UCase(Left(Commando, 3)) = "CHG" Then 
				If Not Status = 3 Then Call chg(Mid(Commando, 5), ichg) : ichg = ichg + 1 'reserved, Universal version
			ElseIf UCase(Left(Commando, 4)) = "NAME" Then 
				If Not Status = 3 Then Call LetName(Mid(Commando, 6))
			ElseIf UCase(Left(Commando, 4)) = "URLN" Then 
				' mo overstap
				
			ElseIf UCase(Left(Commando, 3)) = "END" Then 
				'GoTo overstap
			ElseIf UCase(Left(Commando, 4)) = "LDNR" Then 
				If Not Status = 3 Then Call lndnr(Mid(Commando, 6)) ' reserved , universal
			ElseIf UCase(Left(Commando, 4)) = "DATE" Then 
				If Not Status = 3 Then expdate = Mid(Commando, 6) 'reserved, universal
			ElseIf UCase(Left(Commando, 5)) = "TRANS" Then 
				If Not Status = 3 Then Call trans(Mid(Commando, 7), transtel) : transtel = transtel + 1
			ElseIf UCase(Left(Commando, 5)) = "SYMB1" Then 
				If Not Status = 3 Then symbool1 = Mid(Commando, 7)
			ElseIf UCase(Left(Commando, 6)) = "SYMBA1" And symbool1 = "" Then 
				If Not Status = 3 Then symbola1 = CShort(Mid(Commando, 8)) : symbool1 = Chr(symbola1)
			ElseIf UCase(Left(Commando, 6)) = "SYMBA2" And symbool2 = "" Then 
				If Not Status = 3 Then symbola2 = CShort(Mid(Commando, 8)) : symbool2 = Chr(symbola2)
			ElseIf UCase(Left(Commando, 6)) = "SYMBA3" And symbool3 = "" Then 
				If Not Status = 3 Then symbola3 = CShort(Mid(Commando, 8)) : symbool3 = Chr(symbola3)
			ElseIf UCase(Left(Commando, 6)) = "SYMBA4" And symbool4 = "" Then 
				If Not Status = 3 Then symbola4 = CShort(Mid(Commando, 8)) : symbool4 = Chr(symbola4)
			ElseIf UCase(Left(Commando, 5)) = "SYMB2" Then 
				If Not Status = 3 Then symbool2 = Mid(Commando, 7)
			ElseIf UCase(Left(Commando, 5)) = "SYMB3" Then 
				If Not Status = 3 Then symbool3 = Mid(Commando, 7)
			ElseIf UCase(Left(Commando, 5)) = "SYMB4" Then 
				If Not Status = 3 Then symbool4 = Mid(Commando, 7)
			ElseIf UCase(Left(Commando, 6)) = "INPUT1" Then 
				If Not Status = 3 Then vraag1 = Mid(Commando, 8)
			ElseIf UCase(Left(Commando, 6)) = "INPUT2" Then 
				If Not Status = 3 Then vraag2 = Mid(Commando, 8)
			ElseIf UCase(Left(Commando, 4)) = "MATH" Then 
				Call math(Mid(Commando, 6), MaxMath) : MaxMath = MaxMath + 1
			ElseIf UCase(Left(Commando, 6)) = "RESULT" Then 
				'GoTo overstap  'reseved
			ElseIf UCase(Left(Commando, 6)) = "RESFOU" Then 
				'GoTo overstap  'reseved
			ElseIf UCase(Left(Commando, 6)) = "ERRRES" Then 
				If Status = 3 Then foutmelding = Mid(Commando, 8)
			ElseIf UCase(Left(Commando, 6)) = "PHCODE" Then 
				If Not Status = 3 Then phcode = Val(Mid(Commando, 8)) 'reseved
			ElseIf UCase(Left(Commando, 9)) = "INDOPRINT" Then 
				If Not Status = 3 Then Call indroprint(Mid(Commando, 11))
			ElseIf UCase(Left(Commando, 7)) = "INDOEND" Then 
				If Not Status = 3 Then Call indroprint(Mid(Commando, 11))
			ElseIf UCase(Left(Commando, 6)) = "FORMAT" Then 
				If Not Status = 3 Then Call Vformat(Mid(Commando, 8))
			ElseIf UCase(Left(Commando, 7)) = "TFORMAT" Then 
				If Not Status = 3 Then Call VTformat(Mid(Commando, 8))
			ElseIf Not Commando = "" And Status = 3 Then 
				Status = MsgBox(Commando & ControlFout, MsgBoxStyle.Critical, errorf)
				OpenNOD = False
				FileClose(1)
				Exit Function
			End If
			
overstap: 
		Loop 
		FileClose(1)
		OpenNOD = True
		Exit Function
61: 
		FileClose(1)
		Status = MsgBox(ename & isNotFound, MsgBoxStyle.Critical, errorf)
		OpenNOD = False
	End Function
	Function Eichg() As Boolean
		Dim chgi As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object chgi. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If chgi > 0 Then ichg = True Else ichg = False
	End Function
	Sub indroprint(ByRef Indrotext As String)
		If Indrotekst = "" Then Indrotekst = Indrotext Else Indrotekst = Indrotekst & Chr(13) & Indrotext
	End Sub
	Sub Vformat(ByRef Vform As String)
		Sform = Vform
	End Sub
	Function Getformat() As Short
		Dim S As Object
		Dim e As Short
		'UPGRADE_WARNING: Couldn't resolve default property of object S. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		S = InStr(1, Sform, ".")
		'UPGRADE_WARNING: Couldn't resolve default property of object S. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If S = 0 Then S = InStr(1, Sform, ",")
		e = InStr(1, Sform, "0")
		If e = 0 Then Getformat = 0 : Exit Function
		'UPGRADE_WARNING: Couldn't resolve default property of object S. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Getformat = Len(Mid(Sform, S + 1))
	End Function
	Sub LetName(ByRef Vform As String)
		Dim test1 As Object
		Appsnaam = Vform
		'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Name", Appsnaam)
	End Sub
	Sub trans(ByRef Vform As String, ByRef max As Short)
		Dim i, o As Object
		Dim n As Short
		'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		i = InStr(Vform, ",")
		'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		transo(max) = Left(Vform, i - 1)
		'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		transn(max) = Mid(Vform, i + 1)
		'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		o = Len(transo(max))
		n = Len(transn(max))
		'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		transo(max) = Left(Mid(transo(max), 2), o - 2)
		transn(max) = Left(Mid(transn(max), 2), n - 2)
		
	End Sub
	Public Function filenod(ByRef inv As String) As String
		Dim tel As Short
		tel = Len(My.Application.Info.DirectoryPath)
		If Not UCase(Right(inv, 4)) = ".NOD" Then inv = inv & ".nod"
		If Not (Left(inv, tel) = My.Application.Info.DirectoryPath) Then
			tel = InStr(1, inv, ":\")
			If tel = 0 Then filenod = My.Application.Info.DirectoryPath & "\" & inv : Exit Function
		End If
		filenod = inv
	End Function
	Sub chg(ByRef Vform As String, ByRef max As Short)
		Dim i As Short
		i = InStr(Vform, ",")
		chgo(max) = Left(Vform, i - 1)
		chgn(max) = Mid(Vform, i + 1)
	End Sub
	Sub lndnr(ByRef Vform As String)
		Dim i As Short
		i = InStr(Vform, ",")
		lndro = Left(Vform, i - 1)
		lndrn = Mid(Vform, i + 1)
	End Sub
	Sub VTformat(ByRef Vform As String)
		TSform = Vform
	End Sub
	Sub math(ByRef Vmath As String, ByRef max As Short)
		Smath(max) = Vmath
	End Sub
	Public Function Getask1() As String
		Getask1 = vraag1
	End Function
	Public Function Getappname() As String
		Getappname = Appsnaam
	End Function
	Public Function Getsym1() As String
		Getsym1 = symbool1
	End Function
	Public Function Getsym2() As String
		Getsym2 = symbool2
	End Function
	Public Function Getsym3() As String
		Getsym3 = symbool3
	End Function
	Public Function Getsym4() As String
		Getsym4 = symbool4
	End Function
	Public Function Getask2() As String
		Getask2 = vraag2
	End Function
	Public Function Geterror() As String
		Geterror = foutmelding
	End Function
	Public Function Getindro() As String
		Getindro = Indrotekst
	End Function
	Public Function Getans(ByRef ask As String, ByRef inv As Boolean) As String
		If Not (Smath(0) = "") Then
			Getans = MathExecute(ask, inv)
			If Not Sform = "" And Not (Getans = "") Then Getans = nulnul(Getans)
			Exit Function
		End If
		If Not (chgo(0) = "") Then
			Getans = ChgMath(ask, inv)
			Exit Function
		End If
		If Not (transo(0) = "") Then
			Getans = transMath(ask, inv)
			Exit Function
		End If
	End Function
	Public Function ChgMath(ByRef ask As String, ByRef inv As Boolean) As String
		Dim a, b As Object
		Dim o As Short
		Dim xsr As Boolean
		'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		a = Len(lndro)
		'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If lndro = Left(ask, a) Then
			'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			ask = lndrn & Mid(ask, a + 1) : xsr = True
		End If
		If inv = True Then
			For o = 0 To ichg - 1
				'UPGRADE_WARNING: Couldn't resolve default property of object b. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				b = Len(chgo(o))
				'UPGRADE_WARNING: Couldn't resolve default property of object b. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If chgo(o) = Left(ask, b) Then
					'UPGRADE_WARNING: Couldn't resolve default property of object b. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ask = chgn(o) & Mid(ask, b)
					If xsr = True Then
						'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						a = Len(lndrn)
						'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						If lndrn = Left(ask, a) Then ChgMath = lndro & Mid(ask, a + 1)
					End If
					Exit Function
				End If
			Next o
		End If
		
		If inv = False Then
			For o = 0 To ichg - 1
				'UPGRADE_WARNING: Couldn't resolve default property of object b. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				b = Len(chgn(o))
				'UPGRADE_WARNING: Couldn't resolve default property of object b. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If chgn(o) = Left(ask, b) Then
					'UPGRADE_WARNING: Couldn't resolve default property of object b. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ask = chgo(o) & Mid(ask, b + 2)
					If xsr = True Then
						'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						a = Len(lndrn)
						'UPGRADE_WARNING: Couldn't resolve default property of object a. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						If lndrn = Left(ask, a) Then ChgMath = lndro & Mid(ask, a + 1)
					End If
					Exit Function
				End If
				
			Next o
		End If
		Call errorbox()
	End Function
	Public Function transMath(ByRef ask As String, ByRef inv As Boolean) As String
		Dim a, b As Object
		Dim o As Short
		If inv = True Then
			For o = 0 To transtel - 1
				If UCase(transo(o)) = UCase(ask) Then
					transMath = transn(o)
					Exit Function
				End If
			Next o
		Else
			For o = 0 To transtel - 1
				
				If UCase(transn(o)) = UCase(ask) Then
					transMath = transo(o)
					Exit Function
				End If
				
			Next o
		End If
		Call errorbox()
	End Function
	
	Public Function MathExecute(ByRef ask As String, ByRef inv As Boolean) As String
		Dim cijfer As Object
		Dim o As Object
		Dim cijfers As Object
		Dim invoer As Object
		Dim ans As Object
		Dim Stringans As String
		Dim askOnw As String
		Dim telaf As Short
		Dim neg As String
		On Error GoTo errorfout
		
		ask = ConvertPunt(ask)
		If Not Sform = "" Then ask = Convertmilioenen(ask)
		'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		invoer = CDec(kommaPunt(ask))
		If inv = True Then
			For o = 0 To (MaxMath - 1)
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				askOnw = CStr(CDec(kommaPunt(Mid(Smath(o), 7))))
				
				'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				cijfer = CDec(askOnw)
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Mid(Smath(o), 5, 1) = "/" Then
					'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ans = CDec(invoer / cijfer)
					'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				ElseIf Mid(Smath(o), 5, 1) = "*" Then 
					'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ans = CDec(invoer * cijfer)
					'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				ElseIf Mid(Smath(o), 5, 1) = "+" Then 
					'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ans = CDec(invoer + cijfer)
					'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				ElseIf Mid(Smath(o), 5, 1) = "-" Then 
					'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ans = CDec(invoer - cijfer)
				End If
				'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Not (TSform) = "" Then
					invoer = VB6.Format(CDec(ans), TSform)
				Else
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					invoer = CDec(ans)
				End If
			Next o
		Else
			telaf = MaxMath - 1
			For o = 0 To (MaxMath - 1)
				askOnw = CStr(CDec(kommaPunt(Mid(Smath(telaf), 7))))
				'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				cijfer = CDec(askOnw)
				If Mid(Smath(telaf), 5, 1) = "/" Then
					'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ans = CDec(invoer * cijfer)
				ElseIf Mid(Smath(telaf), 5, 1) = "*" Then 
					'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ans = CDec(invoer / cijfer)
				ElseIf Mid(Smath(telaf), 5, 1) = "+" Then 
					'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ans = CDec(invoer - cijfer)
				ElseIf Mid(Smath(telaf), 5, 1) = "-" Then 
					'UPGRADE_WARNING: Couldn't resolve default property of object cijfer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ans = CDec(invoer + cijfer)
				End If
				telaf = telaf - 1
				'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Not (TSform) = "" Then
					invoer = VB6.Format(CDec(ans), TSform)
				Else
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					'UPGRADE_WARNING: Couldn't resolve default property of object invoer. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					invoer = CDec(ans)
				End If
			Next o
		End If
		
		'UPGRADE_WARNING: Couldn't resolve default property of object Getalafrond(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ans = Getalafrond(ans)
		'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Left(ans, 1) = "-" Then neg = "-"
		'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If ans < 1 And ans > -1 Then
			
			'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			MathExecute = neg & "0" + ans
			
			'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ElseIf Not (ans < 1 And ans > -1) Then 
			'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If neg = "" Then
				MathExecute = ans
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				MathExecute = neg & Mid(ans, 2)
			End If
		End If
		
		If Switch_Renamed = False Then MathExecute = Convertkomma(MathExecute) : Exit Function
		'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Left(Right(ans, 2), 1) = "," Then ans = CDec(ans + "0")
		Exit Function
errorfout: 
		Call errorbox()
	End Function
	Public Function Getalafrond(ByRef ans As Object) As Object
		Dim e As String
		Dim f As Short
		If decils Then
			e = "##." & New String("0", decil)
			If decil = 0 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object afrond(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Getalafrond = afrond(ans)
				Exit Function
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Getalafrond = VB6.Format(CDec(ans), e)
				Exit Function
			End If
		Else
			If Not Sform = "" Then
				f = InStr(1, Sform, "0")
				If f = 0 Then
					'UPGRADE_WARNING: Couldn't resolve default property of object afrond(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					Getalafrond = afrond(ans)
					Exit Function
				Else
					'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					Getalafrond = VB6.Format(CDec(ans), Sform)
					Exit Function
				End If
			End If
		End If
	End Function
	Public Function afrond(ByRef ans As Object) As Object
		Dim temp As Object
		Dim temp1 As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		temp = Fix(ans)
		'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object ans. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object temp1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		temp1 = CDec(ans) - CDec(temp)
		'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object afrond. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If temp1 > 0.5 Then
			afrond = temp + 1
		Else
			'UPGRADE_WARNING: Couldn't resolve default property of object temp. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object afrond. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			afrond = temp
		End If
	End Function
	Public Function Convertkomma(ByRef ans As String) As String
		Dim i As Object
		Dim OK As Object
		Dim maximum As Short
		Dim res As Short
		Dim negl As Short
		Dim digit As String
		Dim milionen As String
		'UPGRADE_WARNING: Couldn't resolve default property of object setting(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object OK. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		OK = setting(digit, milionen)
		maximum = Len(ans)
		If Left(ans, 1) = "-" Then maximum = maximum - 1 : negl = 1
		If Left(Right(ans, 2), 1) = "." Then Convertkomma = Left(ans, maximum - 2 + negl) & digit & Right(ans, 1) & "0" : Exit Function
		For i = 1 To Len(ans)
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Mid(ans, i, 1) = "." Then
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				res = Len(Mid(ans, i + 1))
				Convertkomma = Left(ans, maximum - res - 1 + negl) & digit & Mid(ans, maximum - res + 1 + negl) : Exit Function
			End If
		Next i
		Convertkomma = ans
	End Function
	
	Public Function kommaPunt(ByRef Smath As String) As String
		Dim i As Object
		Dim OK As Object
		Dim digit As String
		Dim milionen As String
		'UPGRADE_WARNING: Couldn't resolve default property of object setting(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object OK. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		OK = setting(digit, milionen)
		If digit = "," Then
			For i = 1 To Len(Smath)
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Mid(Smath, i, 1) = "." Then
					'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					kommaPunt = Left(Smath, i - 1) & "," & Mid(Smath, i + 1) : Exit Function
				End If
				
				
			Next i
		Else
			For i = 1 To Len(Smath)
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Mid(Smath, i, 1) = "," Then
					'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					kommaPunt = Left(Smath, i - 1) & "." & Mid(Smath, i + 1) : Exit Function
				End If
				
				
			Next i
			
		End If
		kommaPunt = Smath
	End Function
	
	Public Function ConvertPunt(ByRef Smath As String) As String
		Dim ijs As Object
		Dim i As Object
		Dim OK As Object
		Dim neg As Object
		If Right(Smath, 1) = "-" Then
			Smath = Left(Smath, Len(Smath) - 1)
			'UPGRADE_WARNING: Couldn't resolve default property of object neg. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			neg = "-"
		End If
		If Len(Smath) = 0 Then Exit Function
		Dim digitet As String
		Dim milionen As String
		'UPGRADE_WARNING: Couldn't resolve default property of object setting(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object OK. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		OK = setting(digitet, milionen)
		If digitet = "," Then Switch_Renamed = True Else Switch_Renamed = False
		Dim telnul As Short
		For i = 1 To Len(Smath)
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Mid(Smath, i, 1) = "." Then
				telnul = 0
				For ijs = 3 To 1
					'UPGRADE_WARNING: Couldn't resolve default property of object ijs. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If Left(Right(Smath, ijs), 1) = "." Then
						'UPGRADE_WARNING: Couldn't resolve default property of object neg. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						ConvertPunt = Left(Smath, i - 1) & "," & Mid(Smath, i + 1) & New String("0", telnul) + neg
						Switch_Renamed = True
						Exit Function
					End If
					telnul = telnul + 1
				Next ijs
				
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Mid(Smath, i, 1) = "," Then
				If Left(Right(Smath, 3), 1) = "," Then
					'UPGRADE_WARNING: Couldn't resolve default property of object neg. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ConvertPunt = Smath + neg
					Switch_Renamed = False
					Exit Function
				End If
			End If
		Next i
		
		Dim find As Short
		If digitet = "." Then
			
			
			Switch_Renamed = True
			find = InStr(Smath, ",")
			If find > 0 Then
				If Left(Right(Smath, 3), 1) = "," Then
					'UPGRADE_WARNING: Couldn't resolve default property of object neg. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					ConvertPunt = Smath + neg
					Switch_Renamed = False
					Exit Function
				End If
			End If
			find = InStr(Smath, ".")
			If find > 0 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object neg. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				ConvertPunt = Smath + neg : Switch_Renamed = True : Exit Function
			End If
			
			'UPGRADE_WARNING: Couldn't resolve default property of object neg. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If digitet = "," Then
				ConvertPunt = Smath & ",00" + neg
			Else
				'UPGRADE_WARNING: Couldn't resolve default property of object neg. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				ConvertPunt = Smath & ".00" + neg
			End If
			Exit Function
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object neg. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		ConvertPunt = Smath + neg
		Switch_Renamed = False
	End Function
	Public Function Convertmilioenen(ByRef Smath As String) As String
		Dim i As Object
		Dim voorlopig As String
		Dim komma As String
		Dim neg As String
		Dim tel As Short
		tel = 1
		If Right(Smath, 1) = "-" Then Smath = Left(Smath, Len(Smath) - 1) : neg = "-"
		If Switch_Renamed = True Then komma = "," Else komma = "."
		For i = 1 To Len(Smath)
			'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Mid(Smath, i, 1) = komma Then
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Len(Mid(Smath, i + 1)) >= 3 Then
					If voorlopig = "" Then
						'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						voorlopig = Left(Smath, i - tel) & Mid(Smath, i + 1) & neg : tel = tel + 1
					Else
						'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						voorlopig = Left(voorlopig, i - tel) & Mid(Smath, i + 1) & neg : tel = tel + 1
					End If
					digit = True
				End If
			End If
		Next i
		If voorlopig = "" Then Convertmilioenen = Smath & neg Else Convertmilioenen = voorlopig
	End Function
	Public Function puntDigit(ByRef Smath As String) As String
		Dim test As Object
		Dim o As Object
		Dim komma As String
		Dim init, lengte As Object
		Dim e As Short
		Dim af As Short
		Dim negl As Short
		Dim negt As String
		Dim negs As Short
		Dim Bereken As Object
		Dim som As Short
		Dim changedinit As String
		If Switch_Renamed = True Then
			e = InStr(Smath, ",")
			If e > 0 Then puntDigit = Smath : Exit Function
			komma = "." : changedinit = ","
		Else : komma = "," : changedinit = "."
		End If
		
		If Left(Smath, 1) = " " Then Smath = Mid(Smath, 2)
		'UPGRADE_WARNING: Couldn't resolve default property of object init. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		init = InStr(1, Smath, komma)
		'UPGRADE_WARNING: Couldn't resolve default property of object lengte. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		lengte = Len(Smath)
		If Left(Smath, 1) = "-" Then
			'UPGRADE_WARNING: Couldn't resolve default property of object lengte. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			lengte = lengte - 1 : negl = 1
			'UPGRADE_WARNING: Couldn't resolve default property of object init. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If init = 0 Then negs = 1
			negt = "-"
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object init. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		init = init - 1
		'UPGRADE_WARNING: Couldn't resolve default property of object init. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object lengte. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		af = lengte - init
		'UPGRADE_WARNING: Couldn't resolve default property of object init. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If init = -1 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object lengte. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object init. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			init = lengte : af = 0
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object init. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object Bereken. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Bereken = (init / 3)
		If Bereken > 1.1 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object Bereken. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			For o = 1 To Bereken
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				som = (3 * o) + af
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object lengte. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				som = lengte - som - (o - 1) + negl
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				'UPGRADE_WARNING: Couldn't resolve default property of object test. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				test = som - negl + o - 1 + negs
				'UPGRADE_WARNING: Couldn't resolve default property of object o. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				Smath = Left(Smath, som - negl + o - 1 + negs) & changedinit & Mid(Smath, som + 1 + (o - 1 - negl + negs))
			Next o
		End If
		If Left(Smath, 1 + negl) = negt & changedinit Then Smath = negt & Mid(Smath, 2 + negl)
		If Left(Smath, negl + negl) = changedinit & negt Then Smath = negt & Mid(Smath, 2 + negl)
		
		puntDigit = Smath
		If Left(puntDigit, 1) = " " Then puntDigit = Mid(puntDigit, 2)
	End Function
	Sub errorbox()
		Dim Status As Boolean
		Status = MsgBox(foutmelding, MsgBoxStyle.Critical, errorf)
	End Sub
	
	Function nulnul(ByRef invoer As String) As String
		Dim oi As Object
		Dim i As Object
		Dim o As Object
		
		Dim a As Short
		Dim c As Short
		Dim verin As Object
		Dim neg As String
		Dim AST As String
		Dim f As Short
		If decils Then
			a = decil
			
		Else
			
			c = InStr(Sform, ".")
			If c = 0 Then c = InStr(Sform, ",")
			f = InStr(1, Sform, "0")
			If f = 0 Then a = 0 Else a = Len(Mid(Sform, c + 1))
		End If
		
		If invoer = "" Then nulnul = "" : Exit Function
		If digit = True Or booldigit = True Then invoer = Convertmilioenen(invoer)
		'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		verin = CDec(kommaPunt(invoer))
		Dim ao As Short
		Dim tmpoy As Short
		If Switch_Renamed = False Then
			If a = 0 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object afrond(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				invoer = afrond(invoer)
				If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
				nulnul = invoer : Exit Function
			End If
			
			
			'UPGRADE_WARNING: Couldn't resolve default property of object Getalafrond(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			verin = Getalafrond(invoer)
			'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			invoer = verin
			
			'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Left(verin, 1) = "-" Then neg = "-"
			'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If verin < 1 And verin > -1 Then
				
				'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				invoer = neg & "0" + verin
				
				'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			ElseIf Not (verin < 1 And verin > -1) Then 
				'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If neg = "" Then
					invoer = verin
				Else
					'UPGRADE_WARNING: Couldn't resolve default property of object verin. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					invoer = neg & Mid(verin, 2)
				End If
				If Left(invoer, 1) = " " Then invoer = Mid(invoer, 2)
			End If
			invoer = Convertkomma(invoer)
			If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
			ao = a
			
			For o = 1 To a
				AST = AST & "0"
				If Left(Right(invoer, ao), 1) = "," Then
					
					invoer = invoer & AST
					If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) & "-" Else nulnul = invoer
					Exit Function
				End If
				
				ao = ao - 1
				
			Next o
			For i = 1 To Len(invoer)
				'UPGRADE_WARNING: Couldn't resolve default property of object i. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Mid(invoer, i, 1) = "," Then
					
					If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) & "-" Else nulnul = invoer
					Exit Function
				End If
			Next i
			If Not invoer = "" Then
				
				If Left(invoer, 1) = "-" Or Right(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) & "," & AST & "-" Else If Mid(invoer, 1) = " " Then nulnul = Mid(invoer, 2) & "," & AST Else nulnul = invoer & "," & AST
				
			End If
		Else
			
			
			
			
			If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
			If a = 0 Then
				'UPGRADE_WARNING: Couldn't resolve default property of object afrond(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				invoer = afrond(invoer)
				If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
				nulnul = invoer : Exit Function
			End If
			
			'UPGRADE_WARNING: Couldn't resolve default property of object Getalafrond(). Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			invoer = Getalafrond(invoer)
			If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
			AST = New String("0", a)
			If Right(invoer, 1) = "-" Then invoer = Left(invoer, Len(invoer) - 1) : neg = "-"
			
			For oi = 1 To a
				'UPGRADE_WARNING: Couldn't resolve default property of object oi. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Left(Right(invoer, oi), 1) = "." Then
					invoer = invoer & AST
					If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) & "-" Else nulnul = invoer & neg
					
					Exit Function
				End If
				AST = Mid(AST, 2)
			Next oi
			
			If Left(Right(invoer, a + 1), 1) = "." Then
				If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) & "-" Else nulnul = invoer & neg
				
				Exit Function
			End If
			tmpoy = InStr(invoer, ".")
			If tmpoy > 0 Then invoer = invoer & neg Else invoer = invoer & ".00" & neg
			If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) & "-" Else nulnul = invoer
		End If
	End Function
End Module