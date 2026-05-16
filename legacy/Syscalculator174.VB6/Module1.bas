Attribute VB_Name = "Hong32"
'DLL hong-technology 1998-2001
'new architecture

Public Const HWND_TOPMOST = -1
Public Const HWND_NOTOPMOST = -2
Public foutmelding As String
Public symbool1 As String
Public symbool2 As String
Public symbool3 As String
Public symbool4 As String
Public symbola1 As Integer
Public symbola2 As Integer
Public symbola3 As Integer
Public symbola4 As Integer
Public Switch As Boolean
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
Public phcode As Integer
Public decil As Integer
Public decils As Boolean


Public Function SortArray(ByVal lengte As Integer, ByRef strArray() As String)
  Dim intOut As Integer, intIn As Integer
  Dim strTemp As String
  
    'loop through array
    For intOut = LBound(strArray()) To lengte
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

Function FirstInAlphabeticalOrder(strOne As String, strTwo As String) As Long
   
   
   
   Dim intChar As Integer, intLen As Integer
   Dim strChar1 As String, strChar2 As String
   
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
Function setDec(i As Integer)
decil = i
If decil = -1 Then decils = False: Exit Function
decils = True
End Function
Function resort()
   Dim lengte As Integer
   lengte = Form3.Combo1.ListCount
   Dim MyArray(255) As String
   'declare array that we're gona sort
   Dim NewString(255) As String
   Dim intBuffer As Integer
   Dim i As Integer
     For intBuffer = 0 To lengte
        If Not Form3.Combo1.List(intBuffer) = "" Then MyArray(intBuffer) = Form3.Combo1.List(intBuffer) Else Exit For
     Next intBuffer

   Call SortArray(lengte - 1, MyArray())
   
    For i = 0 To lengte
     For o = 0 To lengte
      If Not Form3.Combo1.List(o) = "" Then
            If Form3.Combo1.List(o) = MyArray(i) Then
                    NewString(i) = filestring(o)
                    If defaultID = o Then defaultID = i
                        Exit For
                    End If
            End If
        If Form3.Combo1.List(o) = "" Then Exit For
     Next o

    Next i

    Form3.Combo1.Clear
    
    For intBuffer = 0 To lengte
       If Not MyArray(intBuffer) = "" Then
            Form3.Combo1.AddItem MyArray(intBuffer), intBuffer
            If defaultID = intBuffer Then Form3.Combo1.Text = MyArray(intBuffer)
            filestring(intBuffer) = NewString(intBuffer)
            End If
    Next intBuffer
   
End Function


Function AddName(invoer As String) As String
On Error GoTo 0
ename = filenod(invoer)
Open ename For Input As #1
 Do Until EOF(1)
 Line Input #1, Commando
 If UCase(Left(Commando, 4)) = "URLN" Then If AddName = "" Then AddName = Mid(Commando, 6): Close #1: Exit Function
 Loop
Close #1
inst = InStr(1, invoer, ".nod")
Dim temp2 As String
temp2 = Left(invoer, inst - 1)
insr = 1
Do While Not insr = 0
insr = InStr(1, temp2, "\")
temp2 = Mid(temp2, insr + 1)
AddName = temp2
Loop

Exit Function
0:
tes = MsgBox(ename + isNotFound, vbOKOnly)
AddName = "-1"
End Function




Function OpenNOD(name As String, Status As Integer) As Boolean
' Statussen:
' 1  Labels veranderen
' 2  uitvoeren (execute Nod files)
' 3  lezen, foutencontrole
Indrotekst = ""
symbool1 = ""
symbool2 = ""
symbool3 = ""
symbool4 = ""
Dim a, n As Integer
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
Dim tel As Integer
On Error GoTo 61
ename = filenod(name)
Open ename For Input As #1
 Do Until EOF(1)
 Line Input #1, Commando
 If Left(Commando, 1) = "'" Then
 'GoTo overstap 'rem only
 ElseIf UCase(Left(Commando, 3)) = "CHG" Then
  If Not Status = 3 Then Call chg(Mid(Commando, 5), ichg): ichg = ichg + 1   'reserved, Universal version
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
    If Not Status = 3 Then Call trans(Mid(Commando, 7), transtel): transtel = transtel + 1
    ElseIf UCase(Left(Commando, 5)) = "SYMB1" Then
   If Not Status = 3 Then symbool1 = Mid(Commando, 7)
   ElseIf UCase(Left(Commando, 6)) = "SYMBA1" And symbool1 = "" Then
    If Not Status = 3 Then symbola1 = Mid(Commando, 8): symbool1 = Chr(symbola1)
    ElseIf UCase(Left(Commando, 6)) = "SYMBA2" And symbool2 = "" Then
    If Not Status = 3 Then symbola2 = Mid(Commando, 8): symbool2 = Chr(symbola2)
   ElseIf UCase(Left(Commando, 6)) = "SYMBA3" And symbool3 = "" Then
    If Not Status = 3 Then symbola3 = Mid(Commando, 8): symbool3 = Chr(symbola3)
    ElseIf UCase(Left(Commando, 6)) = "SYMBA4" And symbool4 = "" Then
    If Not Status = 3 Then symbola4 = Mid(Commando, 8): symbool4 = Chr(symbola4)
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
    Call math(Mid(Commando, 6), MaxMath): MaxMath = MaxMath + 1
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
     Status = MsgBox(Commando + ControlFout, vbCritical, errorf)
     OpenNOD = False
     Close #1
     Exit Function
 End If
 
overstap:
  Loop
Close #1
OpenNOD = True
Exit Function
61:
Close #1
Status = MsgBox(ename + isNotFound, vbCritical, errorf)
OpenNOD = False
End Function
Function Eichg() As Boolean
If chgi > 0 Then ichg = True Else ichg = False
End Function
Sub indroprint(Indrotext As String)
If Indrotekst = "" Then Indrotekst = Indrotext Else Indrotekst = Indrotekst & Chr(13) & Indrotext
End Sub
Sub Vformat(Vform As String)
Sform = Vform
End Sub
Function Getformat() As Integer
Dim S, e As Integer
S = InStr(1, Sform, ".")
If S = 0 Then S = InStr(1, Sform, ",")
e = InStr(1, Sform, "0")
If e = 0 Then Getformat = 0: Exit Function
Getformat = Len(Mid(Sform, S + 1))
End Function
Sub LetName(Vform As String)
Appsnaam = Vform
test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\Tiedragon\Syscalculator Euro Edition\", "Name", Appsnaam)
End Sub
Sub trans(Vform As String, max As Integer)
Dim i, o, n As Integer
i = InStr(Vform, ",")
transo(max) = Left(Vform, i - 1)
transn(max) = Mid(Vform, i + 1)
o = Len(transo(max))
n = Len(transn(max))
transo(max) = Left(Mid(transo(max), 2), o - 2)
transn(max) = Left(Mid(transn(max), 2), n - 2)

End Sub
Public Function filenod(inv As String) As String
Dim tel As Integer
tel = Len(App.Path)
If Not UCase(Right(inv, 4)) = ".NOD" Then inv = inv + ".nod"
If Not (Left(inv, tel) = App.Path) Then
                                        tel = InStr(1, inv, ":\")
                                        If tel = 0 Then filenod = App.Path + "\" + inv: Exit Function
                                        End If
filenod = inv
End Function
Sub chg(Vform As String, max As Integer)
Dim i As Integer
i = InStr(Vform, ",")
chgo(max) = Left(Vform, i - 1)
chgn(max) = Mid(Vform, i + 1)
End Sub
Sub lndnr(Vform As String)
Dim i As Integer
i = InStr(Vform, ",")
lndro = Left(Vform, i - 1)
lndrn = Mid(Vform, i + 1)
End Sub
Sub VTformat(Vform As String)
TSform = Vform
End Sub
Sub math(Vmath As String, max As Integer)
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
Public Function Getans(ask As String, inv As Boolean) As String
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
Public Function ChgMath(ask As String, inv As Boolean) As String
Dim a, b, o As Integer
Dim xsr As Boolean
a = Len(lndro)
If lndro = Left(ask, a) Then ask = lndrn + Mid(ask, a + 1): xsr = True
If inv = True Then
    For o = 0 To ichg - 1
    b = Len(chgo(o))
    If chgo(o) = Left(ask, b) Then
        ask = chgn(o) + Mid(ask, b + 1)
        If xsr = True Then
            a = Len(lndrn)
            If lndrn = Left(ask, a) Then ChgMath = lndro + Mid(ask, a + 1)
            End If
        Exit Function
        End If
    Next o
    End If
    
If inv = False Then
    For o = 0 To ichg - 1
    b = Len(chgn(o))
    If chgn(o) = Left(ask, b) Then
        ask = chgo(o) + Mid(ask, b + 1)
        If xsr = True Then
            a = Len(lndrn)
            If lndrn = Left(ask, a) Then ChgMath = lndro + Mid(ask, a + 1)
            End If
        Exit Function
        End If

    Next o
    End If
Call errorbox
End Function
Public Function transMath(ask As String, inv As Boolean) As String
Dim a, b, o As Integer
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
Call errorbox
End Function

Public Function MathExecute(ask As String, inv As Boolean) As String
 Dim cijfers As Variant
 Dim invoer As Variant
 Dim ans As Variant
 Dim Stringans As String
 Dim askOnw As String
  Dim telaf As Integer
  Dim neg As String
On Error GoTo errorfout
 
   ask = ConvertPunt(ask)
 If Not Sform = "" Then ask = Convertmilioenen(ask)
   invoer = CDec(kommaPunt(ask))
   If inv = True Then
                    For o = 0 To (MaxMath - 1)
                     askOnw = CDec(kommaPunt(Mid(Smath(o), 7)))
                    
                    cijfer = CDec(askOnw)
                    If Mid(Smath(o), 5, 1) = "/" Then
                        ans = CDec(invoer / cijfer)
                    ElseIf Mid(Smath(o), 5, 1) = "*" Then
                        ans = CDec(invoer * cijfer)
                    ElseIf Mid(Smath(o), 5, 1) = "+" Then
                        ans = CDec(invoer + cijfer)
                    ElseIf Mid(Smath(o), 5, 1) = "-" Then
                        ans = CDec(invoer - cijfer)
                    End If
                       If Not (TSform) = "" Then invoer = Format(CDec(ans), TSform) Else invoer = CDec(ans)
                    Next o
                    Else
                          telaf = MaxMath - 1
                          For o = 0 To (MaxMath - 1)
                          askOnw = CDec(kommaPunt(Mid(Smath(telaf), 7)))
                          cijfer = CDec(askOnw)
                          If Mid(Smath(telaf), 5, 1) = "/" Then
                           ans = CDec(invoer * cijfer)
                          ElseIf Mid(Smath(telaf), 5, 1) = "*" Then
                           ans = CDec(invoer / cijfer)
                          ElseIf Mid(Smath(telaf), 5, 1) = "+" Then
                           ans = CDec(invoer - cijfer)
                          ElseIf Mid(Smath(telaf), 5, 1) = "-" Then
                           ans = CDec(invoer + cijfer)
                          End If
                          telaf = telaf - 1
                          If Not (TSform) = "" Then invoer = Format(CDec(ans), TSform) Else invoer = CDec(ans)
                          Next o
                          End If

ans = Getalafrond(ans)
If Left(ans, 1) = "-" Then neg = "-"
If ans < 1 And ans > -1 Then
                             
                             MathExecute = neg + "0" + ans
                             
                             ElseIf Not (ans < 1 And ans > -1) Then
                                                                    If neg = "" Then MathExecute = ans Else MathExecute = neg + Mid(ans, 2)
                                                                    End If
                             
If Switch = False Then MathExecute = Convertkomma(MathExecute): Exit Function
If Left(Right(ans, 2), 1) = "," Then ans = CDec(ans + "0")
Exit Function
errorfout:
Call errorbox
End Function
Public Function Getalafrond(ans As Variant) As Variant
If decils Then
    Dim e As String
    e = "##." + String(decil, "0")
    If decil = 0 Then
        Getalafrond = afrond(ans)
        Exit Function
        Else
        Getalafrond = Format(CDec(ans), e)
        Exit Function
        End If
    Else
    If Not Sform = "" Then
        Dim f As Integer
        f = InStr(1, Sform, "0")
        If f = 0 Then
            Getalafrond = afrond(ans)
            Exit Function
            Else
            Getalafrond = Format(CDec(ans), Sform)
            Exit Function
            End If
        End If
    End If
End Function
Public Function afrond(ans As Variant) As Variant
Dim temp As Variant
Dim temp1 As Variant
temp = Fix(ans)
temp1 = CDec(ans) - CDec(temp)
If temp1 > 0.5 Then afrond = temp + 1 Else afrond = temp
End Function
Public Function Convertkomma(ans As String) As String
Dim maximum As Integer
Dim res As Integer
Dim negl As Integer
Dim digit As String
Dim milionen As String
OK = setting(digit, milionen)
maximum = Len(ans)
If Left(ans, 1) = "-" Then maximum = maximum - 1: negl = 1
If Left(Right(ans, 2), 1) = "." Then Convertkomma = Left(ans, maximum - 2 + negl) + digit + Right(ans, 1) + "0": Exit Function
For i = 1 To Len(ans)
If Mid(ans, i, 1) = "." Then
    res = Len(Mid(ans, i + 1))
    Convertkomma = Left(ans, maximum - res - 1 + negl) + digit + Mid(ans, maximum - res + 1 + negl): Exit Function
    End If
Next i
Convertkomma = ans
End Function

Public Function kommaPunt(Smath As String) As String
Dim digit As String
Dim milionen As String
OK = setting(digit, milionen)
If digit = "," Then
For i = 1 To Len(Smath)
If Mid(Smath, i, 1) = "." Then kommaPunt = Left(Smath, i - 1) + "," + Mid(Smath, i + 1): Exit Function
                                                                    
                                
Next i
Else
For i = 1 To Len(Smath)
If Mid(Smath, i, 1) = "," Then kommaPunt = Left(Smath, i - 1) + "." + Mid(Smath, i + 1): Exit Function
                                                                    
                                
Next i

End If
kommaPunt = Smath
End Function

Public Function ConvertPunt(Smath As String) As String
If Right(Smath, 1) = "-" Then Smath = Left(Smath, Len(Smath) - 1): neg = "-"
If Len(Smath) = 0 Then Exit Function
Dim digitet As String
Dim milionen As String
OK = setting(digitet, milionen)
If digitet = "," Then Switch = True Else Switch = False
For i = 1 To Len(Smath)
If Mid(Smath, i, 1) = "." Then
                                Dim telnul As Integer
                                telnul = 0
                                For ijs = 3 To 1
                                If Left(Right(Smath, ijs), 1) = "." Then
                                                                    ConvertPunt = Left(Smath, i - 1) + "," + Mid(Smath, i + 1) + String(telnul, "0") + neg
                                                                    Switch = True
                                                                    Exit Function
                                                                    End If
                                telnul = telnul + 1
                                Next ijs
                                                                 
                                End If
If Mid(Smath, i, 1) = "," Then
                                If Left(Right(Smath, 3), 1) = "," Then
                                                                    ConvertPunt = Smath + neg
                                                                    Switch = False
                                                                    Exit Function
                                                                    End If
                                End If
Next i

If digitet = "." Then
                        
                        
                        Switch = True
                        Dim find As Integer
                        find = InStr(Smath, ",")
                        If find > 0 Then
                                            If Left(Right(Smath, 3), 1) = "," Then
                                                                    ConvertPunt = Smath + neg
                                                                    Switch = False
                                                                    Exit Function
                                                                    End If
                                            End If
                        find = InStr(Smath, ".")
                        If find > 0 Then ConvertPunt = Smath + neg: Switch = True: Exit Function
                        
                        If digitet = "," Then ConvertPunt = Smath + ",00" + neg Else ConvertPunt = Smath + ".00" + neg
                        Exit Function
                        End If
ConvertPunt = Smath + neg
Switch = False
End Function
Public Function Convertmilioenen(Smath As String) As String
Dim voorlopig As String
Dim komma As String
Dim neg As String
Dim tel As Integer
tel = 1
If Right(Smath, 1) = "-" Then Smath = Left(Smath, Len(Smath) - 1): neg = "-"
If Switch = True Then komma = "," Else komma = "."
For i = 1 To Len(Smath)
If Mid(Smath, i, 1) = komma Then
                                If Len(Mid(Smath, i + 1)) >= 3 Then
                                                                    If voorlopig = "" Then voorlopig = Left(Smath, i - tel) + Mid(Smath, i + 1) + neg: tel = tel + 1 Else voorlopig = Left(voorlopig, i - tel) + Mid(Smath, i + 1) + neg: tel = tel + 1
                                                                    digit = True
                                                                    End If
                                End If
Next i
If voorlopig = "" Then Convertmilioenen = Smath + neg Else Convertmilioenen = voorlopig
End Function
Public Function puntDigit(Smath As String) As String
Dim komma As String
Dim init, lengte, e As Integer
Dim af As Integer
Dim negl As Integer
Dim negt As String
Dim negs As Integer
Dim Bereken As Variant
Dim som As Integer
Dim changedinit As String
If Switch = True Then
                      e = InStr(Smath, ",")
                      If e > 0 Then puntDigit = Smath: Exit Function
                      komma = ".": changedinit = ","
                      Else: komma = ",": changedinit = "."
                      End If

If Left(Smath, 1) = " " Then Smath = Mid(Smath, 2)
init = InStr(1, Smath, komma)
lengte = Len(Smath)
If Left(Smath, 1) = "-" Then
                             lengte = lengte - 1: negl = 1
                             If init = 0 Then negs = 1
                             negt = "-"
                             End If
init = init - 1
af = lengte - init
If init = -1 Then init = lengte: af = 0
Bereken = (init / 3)
If Bereken > 1.1 Then
                For o = 1 To Bereken
                som = (3 * o) + af
                som = lengte - som - (o - 1) + negl
                test = som - negl + o - 1 + negs
                Smath = Left(Smath, som - negl + o - 1 + negs) + changedinit + Mid(Smath, (som + 1 + (o - 1 - negl + negs)))
                Next o
                End If
If Left(Smath, 1 + negl) = negt + changedinit Then Smath = negt + Mid(Smath, 2 + negl)
If Left(Smath, negl + negl) = changedinit + negt Then Smath = negt + Mid(Smath, 2 + negl)

puntDigit = Smath
If Left(puntDigit, 1) = " " Then puntDigit = Mid(puntDigit, 2)
End Function
Sub errorbox()
Dim Status As Boolean
Status = MsgBox(foutmelding, vbCritical, errorf)
End Sub

Function nulnul(invoer As String) As String
                  
                        Dim a As Integer
                        Dim c As Integer
                        Dim verin As Variant
                        Dim neg As String
                        Dim AST As String
                          If decils Then
                            a = decil
                                      
                             Else
                        
                            c = InStr(Sform, ".")
                            If c = 0 Then c = InStr(Sform, ",")
                            Dim f As Integer
                                f = InStr(1, Sform, "0")
                                    If f = 0 Then a = 0 Else a = Len(Mid(Sform, c + 1))
                                End If
                                                      
                       If invoer = "" Then nulnul = "": Exit Function
                       If digit = True Or booldigit = True Then invoer = Convertmilioenen(invoer)
                                                       verin = CDec(kommaPunt(invoer))
                       If Switch = False Then
                                                       If a = 0 Then
                                                                    invoer = afrond(invoer)
                                                                    If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
                                                                    nulnul = invoer: Exit Function
                                                                    End If
                                                       
                                                       
                                                       verin = Getalafrond(invoer)
                                                       invoer = verin
                                                       
                                                       If Left(verin, 1) = "-" Then neg = "-"
                                                       If verin < 1 And verin > -1 Then
                             
                                                                                invoer = neg + "0" + verin
                             
                                                                                ElseIf Not (verin < 1 And verin > -1) Then
                                                                                                          If neg = "" Then invoer = verin Else invoer = neg + Mid(verin, 2)
                                                                                                          If Left(invoer, 1) = " " Then invoer = Mid(invoer, 2)
                                                                                                          End If
                                                       invoer = Convertkomma(invoer)
                                                       If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
                                                       Dim ao  As Integer
                                                  ao = a
                                                  
                                                  For o = 1 To a
                                                    AST = AST + "0"
                                                  If Left(Right(invoer, ao), 1) = "," Then
                                                                                                                                             
                                                                     invoer = invoer + AST
                                                                     If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) + "-" Else nulnul = invoer
                                                                     Exit Function
                                                                     End If
                                               
                                                 ao = ao - 1
                                                 
                                                 Next o
                                                 For i = 1 To Len(invoer)
                                                 If Mid(invoer, i, 1) = "," Then
                
                                                                                If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) + "-" Else nulnul = invoer
                                                                                Exit Function
                                                                                End If
                                                 Next i
                                                 If Not invoer = "" Then
                                                                             
                                                                        If Left(invoer, 1) = "-" Or Right(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) + "," + AST + "-" Else If Mid(invoer, 1) = " " Then nulnul = Mid(invoer, 2) + "," + AST Else nulnul = invoer + "," + AST
                                                                        
                                                                        End If
                          Else
                          
                         
                         
                          
                          If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
                          If a = 0 Then
                                            invoer = afrond(invoer)
                                            If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
                                            nulnul = invoer: Exit Function
                                            End If
                          
                          invoer = Getalafrond(invoer)
                          If digit = True Or booldigit = True Then invoer = puntDigit(invoer)
                          AST = String(a, "0")
                          If Right(invoer, 1) = "-" Then invoer = Left(invoer, Len(invoer) - 1): neg = "-"

                          For oi = 1 To a
                          If Left(Right(invoer, oi), 1) = "." Then
                                                                     invoer = invoer + AST
                                                                     If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) + "-" Else nulnul = invoer + neg

                                                                     Exit Function
                                                                     End If
                          AST = Mid(AST, 2)
                          Next oi
                          
                          If Left(Right(invoer, a + 1), 1) = "." Then
                                                                     If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) + "-" Else nulnul = invoer + neg

                                                                     Exit Function
                                                                     End If
                          Dim tmpoy As Integer
                          tmpoy = InStr(invoer, ".")
                          If tmpoy > 0 Then invoer = invoer + neg Else invoer = invoer + ".00" + neg
                          If Left(invoer, 1) = "-" Then nulnul = Mid(invoer, 2) + "-" Else nulnul = invoer
                          End If
End Function
