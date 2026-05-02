Attribute VB_Name = "Module1"
Global Tic As NOTIFYICONDATA
Global fORMVALUE3 As Boolean
Global Apppaths As String
Global flags As Integer
Global filestring(255) As String
Global tray As Boolean
Global MaxMath As Integer
Global defaultID As Integer
Global Alreadyerror As String
Global nodid As Integer
Global Exists As String
Global onlyeditor As Integer
Global onlyedname As String
Global recordempty As String
Global Sure As String
Global Add As Boolean
Global max As Integer
Global maxchg As Integer
Global ichg As Integer
Global TOPilse As Boolean
Global Apply As Boolean
Global tikactive As Boolean
Global Configurl As String
Global Configur2 As String
Global Configur3 As String
Global eready As String
Global estart As String
Global isNotFound As String
Global errorf As String
Global withoutcfg As String
Global Lname As String
Global ActiveForm2 As Boolean
Global ActiveCal As Boolean
Global intro As Boolean
Global Edname As String
Global ControlFout As String
Global zondername As String
Global textChanged As String
Global editornaam As String
Global transtel As Integer
Global nomatches As String
Global findrepl As String
Global replacement As String
Global textsereach As String
Global errorzero As String
Global switch1 As String
Global iX As Integer, iY As Integer




Function Addcombo(foras As Form, Optional clean As Integer) As Integer
Dim comm, Data, def, reserve As String
Dim OK As Integer
i = 0
On Error GoTo geenconfig
Open UserDataFilePath("freesyscal.cfg") For Input As #1
Do Until EOF(1)
 Input #1, comm, Data, def
 If Left$(comm, 1) = "'" Then GoTo overstap 'rem only
 If comm = "[lang]" Then Lname = Data: Close #1: Exit Function
overstap:
  Loop
 Close #1
 Exit Function
geenconfig:
Dim Status As Boolean
Status = MsgBox(withoutcfg, vbCritical, errorf)
Unload foras
End Function
Sub SaveTextFile()
On Error GoTo Felhantering
If Edname = "" Then
                           test = fncGetFileNametoSave("nod files|*.nod", "*.nod", "Save")
                           numb = Asc(Left(test, 1))
                           If Not (numb = 0) Then Editor.Text1.text = test Else Exit Sub
                           Edname = test
                           End If
Dim tel As Integer

Dim ename, name As String
Editor.Caption = editornaam + " - " + Edname
ename = filenod(Edname)
Open ename For Output As 1
Print #1, Editor.Text1.text
Close #1
Exit Sub
Felhantering:
Close #1
MsgBox Edname + " " + isNotFound
End Sub
Sub SetForms(ints As Boolean)
 

End Sub

Sub SaveAsTextFile()
On Error GoTo Felhantering
test = fncGetFileNametoSave("nod files|*.nod", "*.nod", "Save")
numb = Asc(Left(test, 1))
If Not (numb = 0) Then Edname = test Else Exit Sub
Editor.Caption = editornaam + " - " + Edname
Open test For Output As 1
Print #1, Editor.Text1.text
Close #1
Exit Sub
Felhantering:
MsgBox Edname + " " + isNotFound
End Sub
Sub OpenTextFile()
Editor.Text1 = ""
test = fncGetFileNametoOpen(, "nod files|*.nod", "*.nod")
numb = Asc(Left(test, 1))
If Not (numb = 0) Then Edname = test Else Exit Sub
If Edname = "" Then Exit Sub
Openfile (Edname)
End Sub
Sub Openfile(test As String)
Open test For Input As #1
Do While Not EOF(1)
Editor.Text1.text = Editor.Text1.text & Input(1, #1)
Loop
Close #1
Editor.Caption = editornaam + " - " + test
End Sub
