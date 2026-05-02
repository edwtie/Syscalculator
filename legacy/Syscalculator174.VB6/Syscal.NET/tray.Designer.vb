<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> Partial Class tray1
#Region "Windows Form Designer generated code "
	<System.Diagnostics.DebuggerNonUserCode()> Public Sub New()
		MyBase.New()
		'This call is required by the Windows Form Designer.
		InitializeComponent()
	End Sub
	'Form overrides dispose to clean up the component list.
	<System.Diagnostics.DebuggerNonUserCode()> Protected Overloads Overrides Sub Dispose(ByVal Disposing As Boolean)
		If Disposing Then
			Static fTerminateCalled As Boolean
			If Not fTerminateCalled Then
				Form_Terminate_renamed()
				fTerminateCalled = True
			End If
			If Not components Is Nothing Then
				components.Dispose()
			End If
		End If
		MyBase.Dispose(Disposing)
	End Sub
	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer
	Public ToolTip1 As System.Windows.Forms.ToolTip
	Public WithEvents mnuShow As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents mnuHide As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents spar As System.Windows.Forms.ToolStripSeparator
	Public WithEvents mnuExit As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents mnuMain As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents MainMenu1 As System.Windows.Forms.MenuStrip
	Public WithEvents Timer1 As System.Windows.Forms.Timer
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
		Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(tray1))
		Me.components = New System.ComponentModel.Container()
		Me.ToolTip1 = New System.Windows.Forms.ToolTip(components)
		Me.MainMenu1 = New System.Windows.Forms.MenuStrip
		Me.mnuMain = New System.Windows.Forms.ToolStripMenuItem
		Me.mnuShow = New System.Windows.Forms.ToolStripMenuItem
		Me.mnuHide = New System.Windows.Forms.ToolStripMenuItem
		Me.spar = New System.Windows.Forms.ToolStripSeparator
		Me.mnuExit = New System.Windows.Forms.ToolStripMenuItem
		Me.Timer1 = New System.Windows.Forms.Timer(components)
		Me.MainMenu1.SuspendLayout()
		Me.SuspendLayout()
		Me.ToolTip1.Active = True
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
		Me.Text = "Form5"
		Me.ClientSize = New System.Drawing.Size(312, 237)
		Me.Location = New System.Drawing.Point(10, 48)
		Me.Icon = CType(resources.GetObject("tray1.Icon"), System.Drawing.Icon)
		Me.MaximizeBox = False
		Me.MinimizeBox = False
		Me.StartPosition = System.Windows.Forms.FormStartPosition.WindowsDefaultLocation
		Me.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.BackColor = System.Drawing.SystemColors.Control
		Me.ControlBox = True
		Me.Enabled = True
		Me.KeyPreview = False
		Me.Cursor = System.Windows.Forms.Cursors.Default
		Me.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.ShowInTaskbar = True
		Me.HelpButton = False
		Me.WindowState = System.Windows.Forms.FormWindowState.Normal
		Me.Name = "tray1"
		Me.mnuMain.Name = "mnuMain"
		Me.mnuMain.Text = "Main"
		Me.mnuMain.Checked = False
		Me.mnuMain.Enabled = True
		Me.mnuMain.Visible = True
		Me.mnuShow.Name = "mnuShow"
		Me.mnuShow.Text = "Show"
		Me.mnuShow.Enabled = False
		Me.mnuShow.Checked = False
		Me.mnuShow.Visible = True
		Me.mnuHide.Name = "mnuHide"
		Me.mnuHide.Text = "Hide"
		Me.mnuHide.Enabled = False
		Me.mnuHide.Checked = False
		Me.mnuHide.Visible = True
		Me.spar.Enabled = True
		Me.spar.Visible = True
		Me.spar.Name = "spar"
		Me.mnuExit.Name = "mnuExit"
		Me.mnuExit.Text = "Exit"
		Me.mnuExit.Checked = False
		Me.mnuExit.Enabled = True
		Me.mnuExit.Visible = True
		Me.Timer1.Enabled = False
		Me.Timer1.Interval = 300
		MainMenu1.Items.AddRange(New System.Windows.Forms.ToolStripItem(){Me.mnuMain})
		mnuMain.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.mnuShow, Me.mnuHide, Me.spar, Me.mnuExit})
		Me.Controls.Add(MainMenu1)
		Me.MainMenu1.ResumeLayout(False)
		Me.ResumeLayout(False)
		Me.PerformLayout()
	End Sub
#End Region 
End Class