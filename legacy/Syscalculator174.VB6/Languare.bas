Attribute VB_Name = "Module2"
Sub Languare(name As String, code As Integer)
' code : 1 " Form1 "
'        2 " Form2 "
'        3 " frmAbout"
'        4 " WizardExpress"
Dim ncode As String
Dim comm As Integer
Dim Data As String
Dim url As String
Dim teller As Integer
Dim ename As String
Dim tel As Integer
teller = 0
textChanged = ""
tel = Len(App.Path)
If Not (LCase$(Left$(name, tel)) = LCase$(App.Path)) Then
                                        If Mid$(name, 2, 2) <> ":\" And Left$(name, 2) <> "\\" Then ename = App.Path + "\" + name Else ename = name
                                        Else
                                        ename = name
                                        End If
If Dir$(ename) = "" And Mid$(name, 2, 2) <> ":\" And Left$(name, 2) <> "\\" Then
                                        If Apppaths <> "" Then
                                                                If Dir$(Apppaths + "\" + name) <> "" Then ename = Apppaths + "\" + name
                                                                End If
                                        End If
If Dir$(ename) = "" And Mid$(name, 2, 2) <> ":\" And Left$(name, 2) <> "\\" Then ename = App.Path + "\eng.lng"
On Error GoTo resfout
Open ename For Input As #1
Do Until EOF(1)
teller = teller + 1
Input #1, ncode, comm, Data, url
If teller = 1 Then ncode = Mid(ncode, 3)
If Left$(url, 5) = "[App]" Then url = App.Path + Mid$(url, 6)
If ncode = "end" Then Exit Do
If ncode = "0" Then
                    Select Case comm
                    Case 100:
                     errorf = Data
                     af = 1
                    Case 101:
                     isNotFound = Data
                     af = 1
                    Case 102:
                      withoutcfg = Data
                     af = 1
                    Case 103:
                      Sure = Data
                      af = 1
                    Case 104:
                     Exists = Data
                      af = 1
                    Case 105:
                      recordempty = Data
                      af = 1
                    Case 106:
                      Alreadyerror = Data
                      af = 1
                    Case 107:
                     ControlFout = Data
                      af = 1
                    Case 108:
                     zondertitel = Data
                     af = 1
                    Case 109:
                                       If textChanged = "" Then textChanged = Data Else textChanged = textChanged & vbCrLf & Data
                     af = 1
                    Case 110:
                      errorzero = Data
                      af = 1
                    Case 201:
                      nomatches = Data
                      af = 1
                     Case 202:
                      findrepl = Data
                      af = 1
                    Case 203:
                      textsereach = Data
                      af = 1
                    Case 204:
                     replacement = Data
                      af = 1
                    Case 300:
                      lngtype = Data
                      af = 1
                    End Select
                    End If
If Not af = 1 Then
            Select Case code
                Case 1:
                 Call From1(ncode, comm, Data, url)
                Case 2:
                 Call From2(ncode, comm, Data, url)
                Case 3:
                  Call From3(ncode, comm, Data, url)
                Case 4:
                 Call From4(ncode, comm, Data, url)
                Case 5:
                 Call From5(ncode, comm, Data, url)
                Case 6:
                 Call From6(ncode, comm, Data, url)
                Case 7:
                 Call From7(ncode, comm, Data, url)
                Case 8:
                 Call From8(ncode, comm, Data, url)
            End Select
            End If
af = 0
Loop
Close #1
Exit Sub
resfout: Status = MsgBox(name + " is not found", vbCritical, "ERROR !!!")
Unload Form1
End Sub

Sub From1(F1 As String, comm As Integer, Data As String, url As String)
 If F1 = "1" Then
        Select Case comm
            Case 100:
            Form1.file.Caption = Data
            Case 101:
            Form1.Exit.Caption = Data
            Case 102:
            Form1.Label7.Caption = Data
            Case 103:
            Form1.Digitchek.Caption = Data
            Case 104:
            Form1.openconv.Caption = Data
            Case 105:
            Form1.Check1.Caption = Data
            Case 200:
            Form1.Config.Caption = Data
            Case 201:
            Form1.Altop.Caption = Data
            Case 202:
            Form1.Indo.Caption = Data
            Case 203:
            Form1.Mtray.Caption = Data
            Case 204:
            Form1.startup.Caption = Data
            Case 205:
            Form1.Switchclick.Caption = Data
            Case 300:
            Form1.tool.Caption = Data
            Case 301:
            Form1.Wizard.Caption = Data
            Form1.Image1.ToolTipText = Data
            Case 302:
            Form1.cal.Caption = Data
            Form1.Image2.ToolTipText = Data
            Case 303:
            Form1.Opties.Caption = Data
            Form1.Image3.ToolTipText = Data
            Case 400:
            Form1.help.Caption = Data
            Case 401:
            Form1.help1.Caption = Data
            Configur2 = url
            Case 402:
            Form1.urllaunch.Caption = Data
            Configurl = url
            Case 403:
            Form1.help2.Caption = Data
            Configur3 = url
            Case 405:
            Form1.Donate.Caption = Data
            donateurl = url
            Case 404:
            Form1.about.Caption = Data
            Case 499:
            Form1.help.Caption = Data
            Case 500:
            Form1.edit.Caption = Data
            Case 501:
            Form1.ecopy.Caption = Data
            Case 502:
            Form1.epaste.Caption = Data
            Case 503:
            Form1.ecut.Caption = Data
            Case 504:
            Form1.Delete.Caption = Data
            Case 505:
            Form1.delall.Caption = Data
            Case 506:
            Form1.eselect.Caption = Data
            Case 601:
            tray1.mnuShow.Caption = Data
            Case 602:
            tray1.mnuHide.Caption = Data
            Case 603:
            tray1.mnuExit.Caption = Data
        End Select
    End If
                  
End Sub
Sub From2(F1 As String, comm As Integer, Data As String, url As String)



If F1 = "2" Then
        Select Case comm
        Case 100:
          Form2.Caption = Data + " "
        Case 101:
          Form2.Command1.Caption = Data
        Case 200:
          Form2.Check1.Caption = Data
        End Select
        End If
 
End Sub
Sub From3(F1 As String, comm As Integer, Data As String, url As String)
Dim stringline As String


If F1 = "3" Then
            Select Case comm
                Case 100:
                frmAbout.Caption = Data + " "
                Case 101:
                frmAbout.lblVersion.Caption = Data
                Case 102:
                frmAbout.lblDescription.Caption = Data
                Case 103:
                For n = 1 To 10
                Select Case Data
                Case "begin":
                stringline = url
                Case "line":
                stringline = stringline & Chr(13) & url
                Case "end":
                frmAbout.lblDisclaimer.Caption = stringline & Chr(13) & url
                Exit Sub
                End Select
                Input #1, ncode, comm, Data, url
                Next n
                End Select
            End If
 
End Sub
Sub From4(F1 As String, comm As Integer, Data As String, url As String)



If F1 = "4" Then
            Select Case comm
                Case 100:
                WizardExpress.Caption = Data
                Case 101:
                WizardExpress.OK.Caption = Data
                Case 200:
                WizardExpress.Exit.Caption = Data
                Case 201:
                WizardExpress.file.Caption = Data
                Case 300:
                WizardExpress.tool.Caption = Data
                Case 301:
                WizardExpress.symbool.Caption = Data
                Case 500:
                WizardExpress.Status.Caption = Data
                Case 501:
                estart = Data
                Case 502:
                eready = Data
            End Select
            End If
 
End Sub

Sub From5(F1 As String, comm As Integer, Data As String, url As String)



If F1 = "5" Then
                  Select Case comm
                  Case 100:
                   standard.Caption = Data
                  Case 101:
                   standard.edit.Caption = Data
                  Case 102:
                   standard.eexit.Caption = Data
                  End Select
                  End If
 
End Sub
Sub From6(F1 As String, comm As Integer, Data As String, url As String)



If F1 = "6" Then
                  Select Case comm
                  Case 100:
                   Form3.Caption = Data
                  Case 101:
                   Form3.Label1.Caption = Data
                  Case 102:
                   Form3.Label2.Caption = Data
                  Case 200:
                    Form3.Command1.Caption = Data
                  Case 201:
                   Form3.Command2.Caption = Data
                  Case 202:
                   Form3.Command3.Caption = Data
                  Case 203:
                   Form3.Command4.Caption = Data
                  Case 204:
                   Form3.Command5.Caption = Data
                  Case 205:
                   Form3.Command6.Caption = Data
                  Case 206:
                   Form3.Command7.Caption = Data
                 
                  Case 300:
                   Form3.Frame1.Caption = Data
                  End Select
                  End If
 
End Sub
Sub From7(F1 As String, comm As Integer, Data As String, url As String)



If F1 = "7" Then
                  Select Case comm
                  Case 100:
                   form4.Caption = Data
                  Case 101:
                   form4.Label1.Caption = Data
                  Case 102:
                   form4.Label2.Caption = Data
                  Case 200:
                   form4.Command1.Caption = Data
                  Case 201:
                   form4.Command2.Caption = Data
                  Case 202:
                  form4.Command3.Caption = Data
                  Case 203:
                   form4.Command4.Caption = Data
                  Case 204:
                   form4.Check1.Caption = Data
                  Case 205:
                   form4.Command5.Caption = Data
                  Case 300:
                   form4.Frame1.Caption = Data
                  End Select
                  End If
 
End Sub
Sub From8(F1 As String, comm As Integer, Data As String, url As String)



If F1 = "8" Then
                  Select Case comm
                  Case 100:
                   Editor.Caption = Data: editornaam = Data
                  Case 101:
                   Editor.new.Caption = Data
                  Case 102:
                   Editor.Open.Caption = Data
                  Case 103:
                   Editor.save.Caption = Data
                  Case 104:
                   Editor.SaveAs.Caption = Data
                  Case 105:
                   Editor.Close.Caption = Data
                  Case 201:
                   Editor.Paste.Caption = Data
                  Case 202:
                   Editor.cut.Caption = Data
                  Case 203:
                   Editor.Copy.Caption = Data
                  Case 204:
                   Editor.eselect.Caption = Data
                  Case 205:
                   Editor.mnufind.Caption = Data
                  Case 206:
                   Editor.mnureplace.Caption = Data
                  Case 207:
                    Editor.Delete.Caption = Data
                  Case 208:
                    Editor.UndoClick.Caption = Data
                  Case 209:
                    Editor.Redoclick.Caption = Data
                  Case 300:
                   Editor.file.Caption = Data
                  Case 200:
                   Editor.edit.Caption = Data
                  Case 400:
                   Editor.help.Caption = Data
                  Case 401:
                  
                                    Editor.help1.Caption = Data
                                    Configur2 = url
                 Case 402:
                                    Editor.help0.Caption = Data
                                    Configurl = url
                 Case 403:
                                    Editor.help2.Caption = Data
                                    Configur3 = url
                 Case 404:
                  Editor.about.Caption = Data
                 Case 499:
                  Editor.help.Caption = Data
                End Select
                  
                  End If
 
End Sub

