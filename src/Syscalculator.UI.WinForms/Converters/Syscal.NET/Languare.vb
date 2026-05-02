Option Strict Off
Option Explicit On
Module Module2
	Sub Languare(ByRef name As String, ByRef code As Short)
		Dim Status As Object
		Dim lngtype As Object
		Dim zondertitel As Object
		Dim af As Object
		' code : 1 " Form1 "
		'        2 " Form2 "
		'        3 " frmAbout"
		'        4 " WizardExpress"
		Dim ncode As String
		Dim comm As Short
		Dim Data As String
		Dim url As String
		Dim teller As Short
		Dim ename As String
		Dim tel As Short
		teller = 0
		textChanged = ""
		tel = Len(My.Application.Info.DirectoryPath)
		If Not (Left(name, tel) = My.Application.Info.DirectoryPath) Then
			If Not Left(Mid(name, 2), 2) = ":\" Then ename = My.Application.Info.DirectoryPath & "\" & name Else ename = name
		Else
			ename = name
		End If
		FileOpen(1, ename, OpenMode.Input)
		On Error GoTo resfout
		Do Until EOF(1)
			teller = teller + 1
			Input(1, ncode)
			Input(1, comm)
			Input(1, Data)
			Input(1, url)
			If teller = 1 Then ncode = Mid(ncode, 3)
			If Left(url, 5) = "[App]" Then url = My.Application.Info.DirectoryPath & Mid(url, 6)
			If ncode = "end" Then Exit Do
			If ncode = "0" Then
				Select Case comm
					Case 100
						errorf = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 101
						isNotFound = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 102
						withoutcfg = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 103
						Sure = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 104
						Exists = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 105
						recordempty = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 106
						Alreadyerror = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 107
						ControlFout = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 108
						'UPGRADE_WARNING: Couldn't resolve default property of object zondertitel. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						zondertitel = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 109
						If textChanged = "" Then textChanged = Data Else textChanged = textChanged & vbCrLf & Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 110
						errorzero = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 201
						nomatches = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 202
						findrepl = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 203
						textsereach = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 204
						replacement = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
					Case 300
						'UPGRADE_WARNING: Couldn't resolve default property of object lngtype. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						lngtype = Data
						'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
						af = 1
				End Select
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			If Not af = 1 Then
				Select Case code
					Case 1
						Call From1(ncode, comm, Data, url)
					Case 2
						Call From2(ncode, comm, Data, url)
					Case 3
						Call From3(ncode, comm, Data, url)
					Case 4
						Call From4(ncode, comm, Data, url)
					Case 5
						Call From5(ncode, comm, Data, url)
					Case 6
						Call From6(ncode, comm, Data, url)
					Case 7
						Call From7(ncode, comm, Data, url)
					Case 8
						Call From8(ncode, comm, Data, url)
				End Select
			End If
			'UPGRADE_WARNING: Couldn't resolve default property of object af. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			af = 0
		Loop 
		FileClose(1)
		Exit Sub
resfout: 'UPGRADE_WARNING: Couldn't resolve default property of object Status. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		Status = MsgBox(name & " is not found", MsgBoxStyle.Critical, "ERROR !!!")
		Form1.Close()
	End Sub
	
	Sub From1(ByRef F1 As String, ByRef comm As Short, ByRef Data As String, ByRef url As String)
		If F1 = "1" Then
			Select Case comm
				Case 100
					Form1.file.Text = Data
				Case 101
					Form1.Exit_Renamed.Text = Data
				Case 102
					Form1.Label7.Text = Data
				Case 103
					Form1.Digitchek.Text = Data
				Case 104
					Form1.openconv.Text = Data
				Case 105
					Form1.Check1.Text = Data
				Case 200
					Form1.Config.Text = Data
				Case 201
					Form1.Altop.Text = Data
				Case 202
					Form1.Indo.Text = Data
				Case 203
					Form1.Mtray.Text = Data
				Case 204
					Form1.startup.Text = Data
				Case 205
					Form1.Switchclick.Text = Data
				Case 300
					Form1.tool.Text = Data
				Case 301
					Form1.Wizard.Text = Data
					Form1.ToolTip1.SetToolTip(Form1.Image1, Data)
				Case 302
					Form1.cal.Text = Data
					Form1.ToolTip1.SetToolTip(Form1.Image2, Data)
				Case 303
					Form1.Opties.Text = Data
					Form1.ToolTip1.SetToolTip(Form1.Image3, Data)
				Case 400
					Form1.help.Text = Data
				Case 401
					Form1.help1.Text = Data
					Configur2 = url
				Case 402
					Form1.urllaunch.Text = Data
					Configurl = url
				Case 403
					Form1.help2.Text = Data
					Configur3 = url
				Case 405
					Form1.Donate.Text = Data
					donateurl = CShort(url)
				Case 404
					Form1.about.Text = Data
				Case 499
					Form1.help.Text = Data
				Case 500
					Form1.edit.Text = Data
				Case 501
					Form1.ecopy.Text = Data
				Case 502
					Form1.epaste.Text = Data
				Case 503
					Form1.ecut.Text = Data
				Case 504
					Form1.Delete.Text = Data
				Case 505
					Form1.delall.Text = Data
				Case 601
					tray1.mnuShow.Text = Data
				Case 602
					tray1.mnuHide.Text = Data
				Case 603
					tray1.mnuExit.Text = Data
			End Select
		End If
		
	End Sub
	Sub From2(ByRef F1 As String, ByRef comm As Short, ByRef Data As String, ByRef url As String)
		
		
		
		If F1 = "2" Then
			Select Case comm
				Case 100
					Form2.Text = Data & " "
				Case 101
					Form2.Command1.Text = Data
				Case 200
					Form2.Check1.Text = Data
			End Select
		End If
		
	End Sub
	Sub From3(ByRef F1 As String, ByRef comm As Short, ByRef Data As String, ByRef url As String)
		Dim ncode As Object
		Dim n As Object
		Dim stringline As String
		
		
		If F1 = "3" Then
			Select Case comm
				Case 100
					frmAbout.Text = Data & " "
				Case 101
					frmAbout.lblVersion.Text = Data
				Case 102
					frmAbout.lblDescription.Text = Data
				Case 103
					For n = 1 To 10
						Select Case Data
							Case "begin"
								stringline = url
							Case "line"
								stringline = stringline & Chr(13) & url
							Case "end"
								frmAbout.lblDisclaimer.Text = stringline & Chr(13) & url
								Exit Sub
						End Select
						Input(1, ncode)
						Input(1, comm)
						Input(1, Data)
						Input(1, url)
					Next n
			End Select
		End If
		
	End Sub
	Sub From4(ByRef F1 As String, ByRef comm As Short, ByRef Data As String, ByRef url As String)
		
		
		
		If F1 = "4" Then
			Select Case comm
				Case 100
					WizardExpress.Text = Data
				Case 101
					WizardExpress.OK.Text = Data
				Case 200
					WizardExpress.exit_Renamed.Text = Data
				Case 201
					WizardExpress.file.Text = Data
				Case 300
					WizardExpress.tool.Text = Data
				Case 301
					WizardExpress.symbool.Text = Data
				Case 500
					WizardExpress.Status.Text = Data
				Case 501
					estart = Data
				Case 502
					eready = Data
			End Select
		End If
		
	End Sub
	
	Sub From5(ByRef F1 As String, ByRef comm As Short, ByRef Data As String, ByRef url As String)
		
		
		
		If F1 = "5" Then
			Select Case comm
				Case 100
					standard.Text = Data
				Case 101
					standard.edit.Text = Data
				Case 102
					standard.eexit.Text = Data
			End Select
		End If
		
	End Sub
	Sub From6(ByRef F1 As String, ByRef comm As Short, ByRef Data As String, ByRef url As String)
		
		
		
		If F1 = "6" Then
			Select Case comm
				Case 100
					Form3.Text = Data
				Case 101
					Form3.Label1.Text = Data
				Case 102
					Form3.Label2.Text = Data
				Case 200
					Form3.Command1.Text = Data
				Case 201
					Form3.Command2.Text = Data
				Case 202
					Form3.Command3.Text = Data
				Case 203
					Form3.Command4.Text = Data
				Case 204
					Form3.Command5.Text = Data
				Case 205
					Form3.Command6.Text = Data
				Case 206
					Form3.Command7.Text = Data
					
				Case 300
					Form3.Frame1.Text = Data
			End Select
		End If
		
	End Sub
	Sub From7(ByRef F1 As String, ByRef comm As Short, ByRef Data As String, ByRef url As String)
		
		
		
		If F1 = "7" Then
			Select Case comm
				Case 100
					form4.Text = Data
				Case 101
					form4.Label1.Text = Data
				Case 102
					form4.Label2.Text = Data
				Case 200
					form4.Command1.Text = Data
				Case 201
					form4.Command2.Text = Data
				Case 202
					form4.Command3.Text = Data
				Case 203
					form4.Command4.Text = Data
				Case 204
					form4.Check1.Text = Data
				Case 205
					form4.Command5.Text = Data
				Case 300
					form4.Frame1.Text = Data
			End Select
		End If
		
	End Sub
	Sub From8(ByRef F1 As String, ByRef comm As Short, ByRef Data As String, ByRef url As String)
		
		
		
		If F1 = "8" Then
			Select Case comm
				Case 100
					Editor.Text = Data : editornaam = Data
				Case 101
					Editor.new_Renamed.Text = Data
				Case 102
					Editor.Open.Text = Data
				Case 103
					Editor.save.Text = Data
				Case 104
					Editor.SaveAs.Text = Data
				Case 105
					Editor.close_Renamed.Text = Data
				Case 201
					Editor.Paste.Text = Data
				Case 202
					Editor.cut.Text = Data
				Case 203
					Editor.Copy.Text = Data
				Case 204
					Editor.eselect.Text = Data
				Case 205
					Editor.mnufind.Text = Data
				Case 206
					Editor.mnureplace.Text = Data
				Case 207
					Editor.Delete.Text = Data
				Case 208
					Editor.UndoClick.Text = Data
				Case 209
					Editor.Redoclick.Text = Data
				Case 300
					Editor.file.Text = Data
				Case 200
					Editor.edit.Text = Data
				Case 400
					Editor.help.Text = Data
				Case 401
					
					Editor.help1.Text = Data
					Configur2 = url
				Case 402
					Editor.help0.Text = Data
					Configurl = url
				Case 403
					Editor.help2.Text = Data
					Configur3 = url
				Case 404
					Editor.about.Text = Data
				Case 499
					Editor.help.Text = Data
			End Select
			
		End If
		
	End Sub
End Module