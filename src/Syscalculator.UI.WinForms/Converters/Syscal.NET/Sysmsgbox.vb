Option Strict Off
Option Explicit On
Friend Class Sysmsgbox
	Inherits System.Windows.Forms.Form
	Private Sub Command1_Click(ByVal eventSender As System.Object, ByVal eventArgs As System.EventArgs) Handles Command1.Click
		Me.Close()
	End Sub
	
	'UPGRADE_NOTE: Form was upgraded to Form_Renamed. Click for more: 'ms-help://MS.VSCC.v90/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
	Function Form_Renamed(ByRef strlabel As String) As Short
		Me.Text = Form1.Text
		Label1.Text = strlabel
		Me.Show()
		Form_Renamed = 1
	End Function
	
	Private Sub Sysmsgbox_FormClosed(ByVal eventSender As System.Object, ByVal eventArgs As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
		Me.Close()
	End Sub
End Class