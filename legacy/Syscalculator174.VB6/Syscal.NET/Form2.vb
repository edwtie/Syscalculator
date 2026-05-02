Option Strict Off
Option Explicit On
Friend Class Form2
	Inherits System.Windows.Forms.Form
	'UPGRADE_WARNING: Event Check1.CheckStateChanged may fire when form is initialized. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="88B12AE1-6DE0-48A0-86F1-60C0686C026A"'
	Private Sub Check1_CheckStateChanged(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Check1.CheckStateChanged
		Dim test1 As Object
		If Check1.CheckState = 0 Then
			Check1.CheckState = System.Windows.Forms.CheckState.Unchecked
			Form1.Indo.Checked = False
			intro = False
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Intro", "0")
		Else
			intro = True
			Form1.Indo.Checked = True
			'UPGRADE_WARNING: Couldn't resolve default property of object test1. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
			test1 = bSetRegValue(HKEY_CURRENT_USER, "SOFTWARE\TCsoftware\Syscalculcator Euro Edition\", "Intro", "1")
			
		End If
		
		
	End Sub
	
	Private Sub Command1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command1.Click
		Me.Close()
	End Sub
	
	Private Sub Form2_Load(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles MyBase.Load
		Dim eidActiveForm2 As Object
		
		
		If TOPilse = True Then
			Call WindowsAPI.AlwaysOnTop(Form1, False)
			If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, False) : WizardExpress.Enabled = False
			If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, False) : standard.Enabled = False
			
		End If
		
		Call Languare(Lname, 2)
		Text = Text & Getappname()
		Form1.Enabled = False
		'UPGRADE_WARNING: Couldn't resolve default property of object eidActiveForm2. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6A50421D-15FE-4896-8A1B-2EC21E9037B2"'
		If eidActiveForm2 = True Then WizardExpress.Enabled = False
		Text1.Text = Getindro()
		Me.Show()
	End Sub
	
	'UPGRADE_NOTE: Form_Terminate was upgraded to Form_Terminate_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	'UPGRADE_WARNING: Form2 event Form.Terminate has a new behavior. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="6BA9B8D2-2A32-4B6E-8D36-44949974A5B4"'
	Private Sub Form_Terminate_Renamed()
		Me.Close()
	End Sub
	
	Private Sub RichTextBox1_Change()
		
	End Sub
	
	Private Sub Form2_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		Call algemeenform2()
		Me.Close()
	End Sub
	Private Sub algemeenform2()
		Form1.Enabled = True
		If ActiveForm2 = True Then WizardExpress.Enabled = True
		If TOPilse = True Then
			Call WindowsAPI.AlwaysOnTop(Form1, True)
			If ActiveForm2 = True Then Call WindowsAPI.AlwaysOnTop(WizardExpress, True) : WizardExpress.Enabled = True
			If ActiveCal = True Then Call WindowsAPI.AlwaysOnTop(standard, True) : standard.Enabled = True
			
		End If
		
	End Sub
End Class