<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> Partial Class WizardExpress
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
	Public WithEvents exit_Renamed As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents file As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents symbool As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents tool As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents MainMenu1 As System.Windows.Forms.MenuStrip
	Public WithEvents Label1 As System.Windows.Forms.Label
	Public WithEvents Status As System.Windows.Forms.GroupBox
	Public WithEvents Timer1 As System.Windows.Forms.Timer
	Public WithEvents OK As System.Windows.Forms.Button
	Public WithEvents Option2 As System.Windows.Forms.RadioButton
	Public WithEvents Option1 As System.Windows.Forms.RadioButton
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
		Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(WizardExpress))
		Me.components = New System.ComponentModel.Container()
		Me.ToolTip1 = New System.Windows.Forms.ToolTip(components)
		Me.MainMenu1 = New System.Windows.Forms.MenuStrip
		Me.file = New System.Windows.Forms.ToolStripMenuItem
		Me.exit_Renamed = New System.Windows.Forms.ToolStripMenuItem
		Me.tool = New System.Windows.Forms.ToolStripMenuItem
		Me.symbool = New System.Windows.Forms.ToolStripMenuItem
		Me.Status = New System.Windows.Forms.GroupBox
		Me.Label1 = New System.Windows.Forms.Label
		Me.Timer1 = New System.Windows.Forms.Timer(components)
		Me.OK = New System.Windows.Forms.Button
		Me.Option2 = New System.Windows.Forms.RadioButton
		Me.Option1 = New System.Windows.Forms.RadioButton
		Me.MainMenu1.SuspendLayout()
		Me.Status.SuspendLayout()
		Me.SuspendLayout()
		Me.ToolTip1.Active = True
		Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
		Me.Text = "Wizard"
		Me.ClientSize = New System.Drawing.Size(125, 159)
		Me.Location = New System.Drawing.Point(261, 187)
		Me.Icon = CType(resources.GetObject("WizardExpress.Icon"), System.Drawing.Icon)
		Me.MaximizeBox = False
		Me.MinimizeBox = False
		Me.ShowInTaskbar = False
		Me.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.BackColor = System.Drawing.SystemColors.Control
		Me.ControlBox = True
		Me.Enabled = True
		Me.KeyPreview = False
		Me.Cursor = System.Windows.Forms.Cursors.Default
		Me.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.HelpButton = False
		Me.WindowState = System.Windows.Forms.FormWindowState.Normal
		Me.Name = "WizardExpress"
		Me.file.Name = "file"
		Me.file.Text = "File"
		Me.file.Checked = False
		Me.file.Enabled = True
		Me.file.Visible = True
		Me.exit_Renamed.Name = "exit"
		Me.exit_Renamed.Text = "&Exit"
		Me.exit_Renamed.Checked = False
		Me.exit_Renamed.Enabled = True
		Me.exit_Renamed.Visible = True
		Me.tool.Name = "tool"
		Me.tool.Text = "Tool"
		Me.tool.Checked = False
		Me.tool.Enabled = True
		Me.tool.Visible = True
		Me.symbool.Name = "symbool"
		Me.symbool.Text = "Symbool"
		Me.symbool.Checked = False
		Me.symbool.Enabled = True
		Me.symbool.Visible = True
		Me.Status.Text = "Status"
		Me.Status.Size = New System.Drawing.Size(113, 41)
		Me.Status.Location = New System.Drawing.Point(8, 112)
		Me.Status.TabIndex = 3
		Me.Status.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Status.BackColor = System.Drawing.SystemColors.Control
		Me.Status.Enabled = True
		Me.Status.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Status.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Status.Visible = True
		Me.Status.Padding = New System.Windows.Forms.Padding(0)
		Me.Status.Name = "Status"
		Me.Label1.TextAlign = System.Drawing.ContentAlignment.TopCenter
		Me.Label1.BackColor = System.Drawing.Color.Red
		Me.Label1.Text = "Start"
		Me.Label1.Font = New System.Drawing.Font("Arial", 8.25!, System.Drawing.FontStyle.Bold Or System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Label1.ForeColor = System.Drawing.SystemColors.WindowText
		Me.Label1.Size = New System.Drawing.Size(97, 17)
		Me.Label1.Location = New System.Drawing.Point(8, 16)
		Me.Label1.TabIndex = 4
		Me.Label1.Enabled = True
		Me.Label1.Cursor = System.Windows.Forms.Cursors.Default
		Me.Label1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Label1.UseMnemonic = True
		Me.Label1.Visible = True
		Me.Label1.AutoSize = False
		Me.Label1.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Label1.Name = "Label1"
		Me.Timer1.Interval = 500
		Me.Timer1.Enabled = True
		Me.OK.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me.OK.Text = "OK"
		Me.OK.Size = New System.Drawing.Size(57, 25)
		Me.OK.Location = New System.Drawing.Point(40, 80)
		Me.OK.TabIndex = 2
		Me.OK.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.OK.BackColor = System.Drawing.SystemColors.Control
		Me.OK.CausesValidation = True
		Me.OK.Enabled = True
		Me.OK.ForeColor = System.Drawing.SystemColors.ControlText
		Me.OK.Cursor = System.Windows.Forms.Cursors.Default
		Me.OK.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.OK.TabStop = True
		Me.OK.Name = "OK"
		Me.Option2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.Option2.Text = "2"
		Me.Option2.Size = New System.Drawing.Size(89, 25)
		Me.Option2.Location = New System.Drawing.Point(8, 48)
		Me.Option2.TabIndex = 1
		Me.Option2.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Option2.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.Option2.BackColor = System.Drawing.SystemColors.Control
		Me.Option2.CausesValidation = True
		Me.Option2.Enabled = True
		Me.Option2.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Option2.Cursor = System.Windows.Forms.Cursors.Default
		Me.Option2.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Option2.Appearance = System.Windows.Forms.Appearance.Normal
		Me.Option2.TabStop = True
		Me.Option2.Checked = False
		Me.Option2.Visible = True
		Me.Option2.Name = "Option2"
		Me.Option1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.Option1.Text = "1"
		Me.Option1.Size = New System.Drawing.Size(97, 21)
		Me.Option1.Location = New System.Drawing.Point(8, 24)
		Me.Option1.TabIndex = 0
		Me.Option1.Checked = True
		Me.Option1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Option1.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.Option1.BackColor = System.Drawing.SystemColors.Control
		Me.Option1.CausesValidation = True
		Me.Option1.Enabled = True
		Me.Option1.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Option1.Cursor = System.Windows.Forms.Cursors.Default
		Me.Option1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Option1.Appearance = System.Windows.Forms.Appearance.Normal
		Me.Option1.TabStop = True
		Me.Option1.Visible = True
		Me.Option1.Name = "Option1"
		Me.Controls.Add(Status)
		Me.Controls.Add(OK)
		Me.Controls.Add(Option2)
		Me.Controls.Add(Option1)
		Me.Status.Controls.Add(Label1)
		MainMenu1.Items.AddRange(New System.Windows.Forms.ToolStripItem(){Me.file, Me.tool})
		file.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.exit_Renamed})
		tool.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.symbool})
		Me.Controls.Add(MainMenu1)
		Me.MainMenu1.ResumeLayout(False)
		Me.Status.ResumeLayout(False)
		Me.ResumeLayout(False)
		Me.PerformLayout()
	End Sub
#End Region 
End Class