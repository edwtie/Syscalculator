Attribute VB_Name = "Module1"
Global Tic As NOTIFYICONDATA
Global lasttime As Variant
Global fORMVALUE3 As Boolean
Global Apppaths As String
Global formchg As Boolean
Global Decimals As Boolean
Global flags As Integer
Global filestring(255) As String
Global tray As Boolean
Global MaxMath As Integer
Global donateurl As Integer
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
Global Ichg As Integer
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
Global switch1 As Boolean
Global iX As Integer, iY As Integer



Sub form1cleanup()
                        
                        ind = Addcombo(1)
                        If ind = -1 Then Exit Sub
                            a = OpenNOD(filestring(ind) + ".nod", 1)
                            If a = False Then Unload Form1: Exit Sub
                            uitnegatief = False
                            
                            Form1.Caption = Getappname()
                            test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Name", Form1.Caption)

                          If switch1 = False Then Form1.Label1.Caption = Getsym1() Else Form1.Label1.Caption = Getsym2()
                          If switch1 = False Then Form1.Label2.Caption = Getsym2() Else Form1.Label2.Caption = Getsym1()
                          If switch1 = False Then Form1.Label5.Caption = Getsym3() Else Form1.Label5.Caption = Getsym4()
                          If switch1 = False Then Form1.Label6.Caption = Getsym4() Else Form1.Label6.Caption = Getsym3()
                          If switch1 = False Then Form1.Label3.Caption = Getask1() Else Form1.Label3.Caption = Getask2()
                          If switch1 = False Then Form1.Label4.Caption = Getask2() Else Form1.Label4.Caption = Getask1()

                            
    
End Sub

Function Addcombo(Optional clean As Integer) As Integer
Dim comm, Data, def, reserve As String
Dim OK As Integer
i = 0
If clean = 1 Then old = Form1.Combo1.ListIndex: oldname = Form1.Combo1.Text

Form1.Combo1.Clear
On Error GoTo geenconfig
Open UserDataFilePath("freesyscal.cfg") For Input As #1
Do Until EOF(1)
 Input #1, comm, Data, def
 If Not Left$(comm, 1) = "'" Then
 
    If comm = "[lang]" Then
                            Lname = Data
                            Else
    If Not (comm = "") Then Form1.Combo1.AddItem comm, i
    If Not (Data = "") Then
                    filestring(i) = Data
                    End If
    If def = "*" Then
                          defaultID = i
                    End If
                    i = i + 1
                    End If
  End If
  Loop
  Close #1
  max = i - 1
  If max > -1 And Not clean = 1 Then Form1.Combo1.ListIndex = defaultID: Addcombo = defaultID
  If clean = 1 Then
            If old < max And old < -1 Then
                            Rem Form1.Combo1.Text = oldname
                            Form1.Combo1.ListIndex = old
                            Addcombo = old
                            Else
                            Addcombo = defaultID
                            Form1.Combo1.ListIndex = defaultID
                            End If
            End If
Exit Function
geenconfig:
Dim Status As Boolean
Addcombo = -1
test5 = bGetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "first")
If test5 = 0 Then Status = MsgBox("Configuration is not found", vbCritical, errorf): Unload Form1
End Function
Sub SaveTextFile()
On Error GoTo Felhantering
If Edname = "" Then
                           test = fncGetFileNametoSave("nod files|*.nod", "*.nod", "Save")
                           numb = Asc(Left(test, 1))
                           If Not (numb = 0) Then Editor.Text1.Text = test Else Exit Sub
                           Edname = test
                           End If
Dim tel As Integer
Dim ename, name As String
Editor.Caption = editornaam + " - " + Form4.Text1
ename = filenod(Edname)
Open ename For Output As 1
Print #1, Editor.Text1.Text
Close #1
Exit Sub
Felhantering:
Close #1
MsgBox Edname + " " + isNotFound
End Sub
Sub SetForms(ints As Boolean)
 
Form1.Cls
If TOPilse = True Then
                        Call WindowsAPI.AlwaysOnTop(Form1, ints)
                        If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, ints): WizardExpress.Enabled = ints
                        If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, ints): standard.Enabled = ints
                        Exit Sub
                        End If
If ActiveForm2 = True Then WizardExpress.Enabled = ints
If ActiveCal = True Then standard.Enabled = ints

End Sub

Sub SaveAsTextFile()
On Error GoTo Felhantering
test = fncGetFileNametoSave("nod files|*.nod", "*.nod", "Save")
numb = Asc(Left(test, 1))
If Not (numb = 0) Then Edname = test Else Exit Sub
Form4.Text1.Text = Edname
Editor.Caption = editornaam + " - " + Edname
Open test For Output As 1
Print #1, Editor.Text1.Text
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
Open test For Input As #1
Do While Not EOF(1)
Editor.Text1.Text = Editor.Text1.Text & Input(1, #1)
Loop
Close #1
End Sub
Function saveconfig(naam As String, Combo1 As ComboBox, Optional groen As Boolean = True) As String
Dim comm, Data, def As String
Dim OK As Integer
Dim tel As Integer
tel = Len(Apppaths + "\")
If groen = True Then
  If LCase(Left(naam, tel)) = LCase(Apppaths + "\") Then naam = Mid(naam, tel + 1)
End If
On Error GoTo geenconfig
Open UserDataFilePath("freesyscal.cfg") For Output As #1
Write #1, "[lang]", naam, ""
For o = 0 To max
If defaultID = o Then def = "*"
Write #1, Combo1.List(o), filestring(o), def
def = ""
Next o
If groen = True Then
Lname = naam
saveconfig = naam
End If
  Close #1
Exit Function
geenconfig:
End Function
