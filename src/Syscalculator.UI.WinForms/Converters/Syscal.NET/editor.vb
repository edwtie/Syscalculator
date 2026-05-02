Option Strict Off
Option Explicit On
Imports VB = Microsoft.VisualBasic
Friend Class Editor
	Inherits System.Windows.Forms.Form
	Dim ChangedText As Short
	Const maxUndo As Short = 50 'Maximum num of undos
	
	Dim gblnIgnoreChange As Boolean
	Dim gintIndex As Short
	Dim gstrStack(maxUndo) As String
	Dim stackBK(maxUndo) As String
	Dim i As Short
	Const max As Short = 3
	
	
	Public Sub about_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles about.Click
		If onlyeditor = 0 Then onlyeditor = 3
		'UPGRADE_ISSUE: Load statement is not supported. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="B530EFF2-3132-48F8-B8BC-D88AF543D321"'
		Load(frmAbout)
		VB6.ShowForm(frmAbout, (0))
	End Sub
	
	Public Sub help0_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles help0.Click
		Dim SW_SHOWNORMAL As Object
		Dim iRet As Integer
		'UPGRADE_WARNING: Couldn't resolve default property of object SW_SHOWNORMAL. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		iRet = ShellExecute(Me.Handle.ToInt32, vbNullString, Configurl, vbNullString, "c:\", SW_SHOWNORMAL)
		
	End Sub
	
	Public Sub Help2_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Help2.Click
		Dim SW_SHOWNORMAL As Object
		Dim iRet As Integer
		'UPGRADE_WARNING: Couldn't resolve default property of object SW_SHOWNORMAL. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		iRet = ShellExecute(Me.Handle.ToInt32, vbNullString, Configur3, vbNullString, "c:\", SW_SHOWNORMAL)
		
	End Sub
	
	Public Sub close_Renamed_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles close_Renamed.Click
		Me.Close()
	End Sub
	Public Sub copy_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles copy.Click
		My.Computer.Clipboard.Clear()
		My.Computer.Clipboard.SetText(Mid(Text1.Text, Text1.SelectionStart + 1, Text1.SelectionLength))
	End Sub
	Public Sub cut_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles cut.Click
		Dim strRight As Object
		Dim strleft As Object
		My.Computer.Clipboard.Clear()
		'UPGRADE_WARNING: Couldn't resolve default property of object strleft. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		strleft = VB.Left(Text1.Text, Text1.SelectionStart)
		'UPGRADE_WARNING: Couldn't resolve default property of object strRight. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		strRight = Mid(Text1.Text, Text1.SelectionStart + Text1.SelectionLength + 1)
		My.Computer.Clipboard.SetText(Mid(Text1.Text, Text1.SelectionStart + 1, Text1.SelectionLength))
		'UPGRADE_WARNING: Couldn't resolve default property of object strRight. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object strleft. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Text1.Text = strleft & strRight
	End Sub
	
	Public Sub Delete_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Delete.Click
		Dim strRight As Object
		Dim strleft As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object strleft. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		strleft = VB.Left(Text1.Text, Text1.SelectionStart)
		'UPGRADE_WARNING: Couldn't resolve default property of object strRight. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		strRight = Mid(Text1.Text, Text1.SelectionStart + Text1.SelectionLength + 1)
		'UPGRADE_WARNING: Couldn't resolve default property of object strRight. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object strleft. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Text1.Text = strleft & strRight
	End Sub
	
	Public Sub edit_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles edit.Click
		If My.Computer.Clipboard.GetText = "" Then paste.Enabled = False Else paste.Enabled = True
		If Text1.SelectionLength = 0 Then Delete.Enabled = False : copy.Enabled = False : cut.Enabled = False Else copy.Enabled = True : cut.Enabled = True : Delete.Enabled = True
	End Sub
	
	Public Sub eselect_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles eselect.Click
		
		Me.Text1.SelectionStart = 0
		Me.Text1.SelectionLength = Len(Me.Text1.Text)
		
		
	End Sub
	
	Private Sub Editor_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
		Dim n As Object
		Dim tel As Short
		'UPGRADE_NOTE: name was upgraded to name_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
		Dim ename As Object
		Dim name_Renamed As String
		If onlyeditor = 0 Then Form4.Enabled = False
		Call Languare(Lname, 8)
		Dim usedlast(3) As String
		For n = 0 To max - 1
			'UPGRADE_WARNING: Couldn't resolve default property of object n. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			usedlast(n) = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & (n + 1))
			'UPGRADE_WARNING: Couldn't resolve default property of object n. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not Mid(usedlast(n), 3) = "" Then
				'UPGRADE_WARNING: Couldn't resolve default property of object n. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				With lastused(n + 1)
					'UPGRADE_WARNING: Couldn't resolve default property of object n. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					.Text = "&" & (n + 1) & ". " & visiblefile(usedlast(n))
					'UPGRADE_WARNING: Couldn't resolve default property of object n. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					.Visible = True
				End With
				line1.Visible = True
			End If
		Next n
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If onlyeditor = 0 Then
			ename = filenod((Form4.Text1).Text)
		Else
			'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			ename = filenod(onlyedname)
		End If
		If onlyedname = ".nod" Then Exit Sub
		On Error GoTo 51
		Openfiles((ename))
		If onlyeditor = 0 Then Edname = Form4.Text1.Text Else Edname = onlyedname
		
		Exit Sub
51: 
		'UPGRADE_WARNING: Couldn't resolve default property of object ename. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		MsgBox(ename + " " + isNotFound)
	End Sub
	
	Private Sub Editor_FormClosing(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
		Dim Cancel As Boolean = eventArgs.Cancel
		Dim UnloadMode As System.Windows.Forms.CloseReason = eventArgs.CloseReason
		Dim Question As Object
		If onlyeditor = 3 Then onlyeditor = 0
		If Edname = "" Then
			If onlyeditor = 0 Then Form4.Enabled = True : Exit Sub
			Form1.Close()
			Editor_FormClosed(Me, New System.Windows.Forms.FormClosedEventArgs((0), FormAction.Closed))
			Exit Sub
		End If
		'UPGRADE_WARNING: Couldn't resolve default property of object Question. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If ChangedText = 1 Then Question = MsgBox(textChanged, MsgBoxStyle.YesNoCancel, editornaam)
		'UPGRADE_WARNING: Couldn't resolve default property of object Question. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Question = 2 Then Cancel = 1 : Exit Sub 'Avbryt
		'UPGRADE_WARNING: Couldn't resolve default property of object Question. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Question = 6 Then SaveTextFile() 'Ja
		'UPGRADE_WARNING: Couldn't resolve default property of object Question. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Question = 7 Then
			If onlyeditor = 0 Then Form4.Enabled = True : Exit Sub
			Form1.Close()
			Editor_FormClosed(Me, New System.Windows.Forms.FormClosedEventArgs((0), FormAction.Closed))
			Exit Sub
		End If
		Dim a As Boolean
		a = OpenNOD(Edname, 3)
		If a = False Then Cancel = 1 : Exit Sub
		
		If onlyeditor = 0 Then
			Form4.Enabled = True
			Form4.Text2.Text = AddName(Edname)
			If Form4.Text1.Text = "" Then Form4.Command5.Enabled = False
			Exit Sub
		End If
		Form1.Close()
		Editor_FormClosed(Me, New System.Windows.Forms.FormClosedEventArgs((0), FormAction.Closed))
		eventArgs.Cancel = Cancel
	End Sub
	
	'UPGRADE_WARNING: Event Editor.Resize may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Editor_Resize(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Resize
		Text1.Top = 0
		Text1.Left = 0
		Text1.Height = VB6.TwipsToPixelsY(VB6.PixelsToTwipsY(Me.Height) - 689)
		Text1.Width = VB6.TwipsToPixelsX(VB6.PixelsToTwipsX(Me.Width) - 120)
	End Sub
	
	Private Sub Editor_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		Me.Close()
	End Sub
	
	Public Sub help1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles help1.Click
		Dim SW_SHOWNORMAL As Object
		Dim iRet As Integer
		'UPGRADE_WARNING: Couldn't resolve default property of object SW_SHOWNORMAL. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		iRet = ShellExecute(Me.Handle.ToInt32, vbNullString, Configur2 & " " & My.Application.Info.Version.Major & "." & My.Application.Info.Version.Minor & "." & My.Application.Info.Version.Revision, vbNullString, "c:\", SW_SHOWNORMAL)
		
	End Sub
	
	Public Sub mnufind_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnufind.Click
		Dim S As String
		If Text1.SelectionLength > 0 Then S = Text1.SelectedText Else S = "Find Text"
		ShowFind(Me, Text1, FR_SHOWHELP, S)
	End Sub
	
	Public Sub mnureplace_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles mnureplace.Click
		Dim S As String
		If Text1.SelectionLength > 0 Then S = Text1.SelectedText Else S = "Find Text"
		ShowFind(Me, Text1, FR_SHOWHELP, S, True, "Replace Text")
	End Sub
	
	Public Sub new_Renamed_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles new_Renamed.Click
		Dim zondertitel As Object
		Dim what As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object what. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If ChangedText = 1 Then what = MsgBox(textChanged, MsgBoxStyle.YesNoCancel, editornaam)
		'UPGRADE_WARNING: Couldn't resolve default property of object what. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If what = 2 Then Exit Sub 'Avbryt
		'UPGRADE_WARNING: Couldn't resolve default property of object what. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If what = 6 Then SaveTextFile() 'Ja
		'UPGRADE_WARNING: Couldn't resolve default property of object zondertitel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Me.Text = editornaam & " - " + zondertitel
		Me.Text1.Text = ""
		Edname = ""
		Call resetundo()
	End Sub
	Public Sub open_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles open.Click
		Dim Question As Object
		On Error GoTo Felhantering
		'UPGRADE_WARNING: Couldn't resolve default property of object Question. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If ChangedText = 1 Then Question = MsgBox(textChanged, MsgBoxStyle.YesNoCancel, editornaam)
		'UPGRADE_WARNING: Couldn't resolve default property of object Question. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Question = 2 Then Exit Sub 'Avbryt
		'UPGRADE_WARNING: Couldn't resolve default property of object Question. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If Question = 6 Then SaveAsTextFile() 'Ja
		Call resetundo()
		OpenTextFile()
		If onlyeditor = 0 Then Form4.Text1.Text = Edname
		Me.Text = editornaam & " - " & Edname
		ChangedText = CShort("0")
		salastused()
		Exit Sub
Felhantering: 
		MsgBox(Edname & " " & isNotFound)
	End Sub
	Private Sub resetundo()
		For i = 0 To maxUndo
			gstrStack(i) = ""
			stackBK(i) = ""
		Next i
		gintIndex = 0
		Redoclick.Enabled = False
		UndoClick.Enabled = False
	End Sub
	Public Sub Paste_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Paste.Click
		Dim strRight As Object
		Dim strleft As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object strleft. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		strleft = VB.Left(Me.Text1.Text, Text1.SelectionStart)
		'UPGRADE_WARNING: Couldn't resolve default property of object strRight. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		strRight = Mid(Me.Text1.Text, Text1.SelectionStart + Text1.SelectionLength + 1)
		'UPGRADE_WARNING: Couldn't resolve default property of object strRight. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		'UPGRADE_WARNING: Couldn't resolve default property of object strleft. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Text1.Text = strleft & My.Computer.Clipboard.GetText & strRight
	End Sub
	
	Private Sub Pathen_Change()
		
	End Sub
	
	Public Sub Redoclick_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Redoclick.Click
		If gintIndex < maxUndo Then ' max undo level is reached, do not redo
			gblnIgnoreChange = True
			gintIndex = gintIndex + 1
			On Error Resume Next
			Text1.Text = gstrStack(gintIndex)
			gblnIgnoreChange = False
		End If
	End Sub
	
	Public Sub save_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles save.Click
		SaveTextFile()
		ChangedText = CShort("0")
		salastused()
	End Sub
	Public Sub SaveAs_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles SaveAs.Click
		
		SaveAsTextFile()
		
		ChangedText = CShort("0")
		salastused()
	End Sub
	'UPGRADE_WARNING: Event text1.TextChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub text1_TextChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles text1.TextChanged
		Dim b As Object
		Dim g As Object
		ChangedText = 1
		
		'UPGRADE_WARNING: Couldn't resolve default property of object g. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		g = maxUndo 'Initialize this to the max number of undos
		
		If Not gblnIgnoreChange Then
			gintIndex = gintIndex + 1
			
			If gintIndex >= maxUndo + 1 Then 'If > max num of undos reached
				
				For b = 0 To maxUndo 'Copy the undo info to a backup array
					'UPGRADE_WARNING: Couldn't resolve default property of object b. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					stackBK(b) = gstrStack(b)
				Next b
				
				For i = 0 To maxUndo 'Copy the backup array info back to the original, but in a different order
					'UPGRADE_WARNING: Couldn't resolve default property of object g. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					If g >= 1 Then
						'UPGRADE_WARNING: Couldn't resolve default property of object g. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						g = g - 1
						'UPGRADE_WARNING: Couldn't resolve default property of object g. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						gstrStack(g) = stackBK(g + 1) 'gstrstack(49) = stackBK(50) get it??
					End If
				Next i
				
				gintIndex = maxUndo 'Set it to the max number of undos
				
			End If
			gstrStack(gintIndex) = Text1.Text
			If gintIndex <= 1 Then
				Redoclick.Enabled = True
				UndoClick.Enabled = True
			End If
		End If
	End Sub
	Public Sub lastused_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles lastused.Click
		Dim Index As Short = lastused.GetIndex(eventSender)
		Dim usedlast As Object
		'UPGRADE_WARNING: Couldn't resolve default property of object usedlast. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		usedlast = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & Index)
		'UPGRADE_WARNING: Couldn't resolve default property of object usedlast. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Edname = usedlast
		On Error GoTo 51
		Me.Text1.Text = ""
		Call resetundo()
		Openfiles((Edname))
		Me.Text = editornaam & " - " & Edname
		ChangedText = CShort("0")
		salastused()
		Exit Sub
51: 
		MsgBox(Edname & " " & isNotFound)
	End Sub
	Sub salastused()
		Dim ns2 As Object
		Dim ong As Object
		Dim n As Object
		
		
		Dim usedlast(max) As String
		For n = 0 To max - 1
			'UPGRADE_WARNING: Couldn't resolve default property of object n. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			usedlast(n) = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & (n + 1))
		Next n
		Dim okfile As String
		okfile = visiblefile(Edname)
		For ong = max - 1 To 2 Step -1
			If Trim(Mid(lastused(ong).Text, 4)) = VB.Left(okfile, Len(okfile) - 1) Then
				'UPGRADE_WARNING: Couldn't resolve default property of object ong. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				For n = ong To 2 Step -1
					With lastused(n)
						'UPGRADE_WARNING: Couldn't resolve default property of object n. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						.Text = "&" & n & ". " & visiblefile(usedlast(n - 2))
						.Visible = True
					End With
					'UPGRADE_WARNING: Couldn't resolve default property of object n. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					Call bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & n, usedlast(n - 2))
				Next n
				Call addnr1()
				Exit Sub
			End If
		Next ong
		If Not Trim(Mid(lastused(1).Text, 4)) = VB.Left(okfile, Len(okfile) - 1) Then
			For ns2 = max To 2 Step -1
				'UPGRADE_WARNING: Couldn't resolve default property of object ns2. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
				If Not usedlast(ns2 - 2) = "" Then
					With lastused(ns2)
						'UPGRADE_WARNING: Couldn't resolve default property of object ns2. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						.Text = "&" & ns2 & ". " & visiblefile(usedlast(ns2 - 2))
						.Visible = True
					End With
					'UPGRADE_WARNING: Couldn't resolve default property of object ns2. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
					Call bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast" & ns2, usedlast(ns2 - 2))
					
				End If
			Next ns2
			
			Call addnr1()
		End If
		
		
		
	End Sub
	Sub addnr1()
		Call bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "usedlast1", Trim(Edname))
		lastused(1).Text = "&1. " & visiblefile(Trim(Edname))
		lastused(1).Visible = True
		line1.Visible = True
		
	End Sub
	Function visiblefile(ByRef ename As String) As String
		Dim getal As Object
		ename = Trim(ename)
		If Len(ename) > 20 Then
			'UPGRADE_WARNING: Couldn't resolve default property of object getal. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			getal = InStr(1, VB.Right(ename, 15), "\")
			
			visiblefile = Mid(ename, 1, 3) & "..." & Mid(VB.Right(ename, 15), InStr(1, VB.Right(ename, 15), "\"))
		End If
		visiblefile = ename
	End Function
	Sub Openfiles(ByRef test As String)
		FileOpen(1, test, OpenMode.Input)
		gblnIgnoreChange = True
		Do While Not EOF(1)
			Me.Text1.Text = Me.Text1.Text & InputString(1, 1)
		Loop 
		FileClose(1)
		gblnIgnoreChange = False
		gstrStack(0) = Text1.Text
		Me.Text = editornaam & " - " & test
	End Sub
	
	Public Sub UndoClick_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles UndoClick.Click
		'This says that if the Index is = to 0, then It shouldn't undo anymore
		If gintIndex = 0 Then Exit Sub
		
		'This is the basic undo stuff.
		gblnIgnoreChange = True
		gintIndex = gintIndex - 1
		On Error Resume Next
		Text1.Text = gstrStack(gintIndex)
		gblnIgnoreChange = False
	End Sub
End Class