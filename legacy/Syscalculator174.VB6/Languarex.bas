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
ename = ResolveProgramFile(name)

On Error GoTo resfout
Open ename For Input As #1
Do Until EOF(1)
teller = teller + 1
Input #1, ncode, comm, Data, url
If teller = 1 Then ncode = Mid(ncode, 3)
If Left$(url, 5) = "[App]" Then url = ResolveProgramFile(url)
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
Case 3:  Call From3(ncode, comm, Data, url)
Case 8:  Call From8(ncode, comm, Data, url)
End Select
End If
af = 0
Loop
Close #1
Exit Sub
resfout: Status = MsgBox(ename + " is not found", vbCritical, "ERROR !!!")
Unload Editor
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
                                     If Data = "begin" Then stringline = url
                                     If Data = "line" Then stringline = stringline & Chr(13) & url
                                     If Data = "end" Then frmAbout.lblDisclaimer.Caption = stringline & Chr(13) & url: Exit Sub
                                     Input #1, ncode, comm, Data, url
                                     Next n
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
                   Editor.open.Caption = Data
                  Case 103:
                   Editor.save.Caption = Data
                  Case 104:
                   Editor.SaveAs.Caption = Data
                  Case 105:
                   Editor.close.Caption = Data
                  Case 201:
                   Editor.paste.Caption = Data
                  Case 202:
                   Editor.cut.Caption = Data
                  Case 203:
                   Editor.copy.Caption = Data
                  Case 204:
                   Editor.eselect.Caption = Data
                  Case 205:
                   Editor.mnufind.Caption = Data
                  Case 206:
                   Editor.mnureplace.Caption = Data
                  Case 207:
                    Editor.Delete.Caption = Data
                  Case 208:
                    Editor.Undoclick.Caption = Data
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


