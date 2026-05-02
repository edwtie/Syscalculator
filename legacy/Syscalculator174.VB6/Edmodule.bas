Attribute VB_Name = "Module1"
Sub Main()
Splash.Show
Form1.Show
Form1.Visible = False
End Sub
Sub SaveTextFile()
On Error GoTo Felhantering
If Form1.Pathen = "" Then Form1.Cd.ShowSave
Form1.Pathen = Form1.Cd.FileName
Open Form1.Cd.FileName For Output As 1
Print #1, Form1.Text1.Text
Close #1
Exit Sub
Felhantering:
MsgBox "Kunde inte spara filen"
End Sub
Sub SaveAsTextFile()
On Error GoTo Felhantering
Form1.Cd.ShowSave
Open Form1.Cd.FileName For Output As 1
Print #1, Form1.Text1.Text
Close #1
Exit Sub
Felhantering:
MsgBox "Kunde inte spara filen"
End Sub
Sub OpenTextFile()
Form1.Text1 = ""
Form1.Cd.ShowOpen
Form1.Pathen = Form1.Cd.FileName
If Form1.Pathen = "" Then Exit Sub
Open Form1.Cd.FileName For Input As #1
Do While Not EOF(1)
Form1.Text1.Text = Form1.Text1.Text & Input(1, #1)
Loop
Close #1
End Sub

