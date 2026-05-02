<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> Partial Class standard
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
	Public WithEvents eexit As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents edit As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents MainMenu1 As System.Windows.Forms.MenuStrip
	Public WithEvents Memorystatus As System.Windows.Forms.TextBox
	Public WithEvents _Command2_8 As System.Windows.Forms.Button
	Public WithEvents _Command2_7 As System.Windows.Forms.Button
	Public WithEvents _Command2_6 As System.Windows.Forms.Button
	Public WithEvents _Command7_1 As System.Windows.Forms.Button
	Public WithEvents Convert As System.Windows.Forms.Button
	Public WithEvents _Command2_5 As System.Windows.Forms.Button
	Public WithEvents _Command7_0 As System.Windows.Forms.Button
	Public WithEvents _Command7_5 As System.Windows.Forms.Button
	Public WithEvents _Command7_4 As System.Windows.Forms.Button
	Public WithEvents _Command2_3 As System.Windows.Forms.Button
	Public WithEvents _Command2_1 As System.Windows.Forms.Button
	Public WithEvents Command4 As System.Windows.Forms.Button
	Public WithEvents _Command2_4 As System.Windows.Forms.Button
	Public WithEvents _Command2_0 As System.Windows.Forms.Button
	Public WithEvents _Command1_10 As System.Windows.Forms.Button
	Public WithEvents _Command1_0 As System.Windows.Forms.Button
	Public WithEvents _Command2_2 As System.Windows.Forms.Button
	Public WithEvents Command3 As System.Windows.Forms.Button
	Public WithEvents _Command1_9 As System.Windows.Forms.Button
	Public WithEvents _Command1_8 As System.Windows.Forms.Button
	Public WithEvents _Command1_7 As System.Windows.Forms.Button
	Public WithEvents _Command1_6 As System.Windows.Forms.Button
	Public WithEvents _Command1_5 As System.Windows.Forms.Button
	Public WithEvents _Command1_4 As System.Windows.Forms.Button
	Public WithEvents _Command1_3 As System.Windows.Forms.Button
	Public WithEvents _Command1_2 As System.Windows.Forms.Button
	Public WithEvents _Command1_1 As System.Windows.Forms.Button
	Public WithEvents Memory As System.Windows.Forms.Label
	Public WithEvents Frame1 As System.Windows.Forms.GroupBox
	Public WithEvents Command1 As Microsoft.VisualBasic.Compatibility.VB6.ButtonArray
	Public WithEvents Command2 As Microsoft.VisualBasic.Compatibility.VB6.ButtonArray
	Public WithEvents Command7 As Microsoft.VisualBasic.Compatibility.VB6.ButtonArray
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
		Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(standard))
		Me.components = New System.ComponentModel.Container()
		Me.ToolTip1 = New System.Windows.Forms.ToolTip(components)
		Me.MainMenu1 = New System.Windows.Forms.MenuStrip
		Me.edit = New System.Windows.Forms.ToolStripMenuItem
		Me.eexit = New System.Windows.Forms.ToolStripMenuItem
		Me.Frame1 = New System.Windows.Forms.GroupBox
		Me.Memorystatus = New System.Windows.Forms.TextBox
		Me._Command2_8 = New System.Windows.Forms.Button
		Me._Command2_7 = New System.Windows.Forms.Button
		Me._Command2_6 = New System.Windows.Forms.Button
		Me._Command7_1 = New System.Windows.Forms.Button
		Me.Convert = New System.Windows.Forms.Button
		Me._Command2_5 = New System.Windows.Forms.Button
		Me._Command7_0 = New System.Windows.Forms.Button
		Me._Command7_5 = New System.Windows.Forms.Button
		Me._Command7_4 = New System.Windows.Forms.Button
		Me._Command2_3 = New System.Windows.Forms.Button
		Me._Command2_1 = New System.Windows.Forms.Button
		Me.Command4 = New System.Windows.Forms.Button
		Me._Command2_4 = New System.Windows.Forms.Button
		Me._Command2_0 = New System.Windows.Forms.Button
		Me._Command1_10 = New System.Windows.Forms.Button
		Me._Command1_0 = New System.Windows.Forms.Button
		Me._Command2_2 = New System.Windows.Forms.Button
		Me.Command3 = New System.Windows.Forms.Button
		Me._Command1_9 = New System.Windows.Forms.Button
		Me._Command1_8 = New System.Windows.Forms.Button
		Me._Command1_7 = New System.Windows.Forms.Button
		Me._Command1_6 = New System.Windows.Forms.Button
		Me._Command1_5 = New System.Windows.Forms.Button
		Me._Command1_4 = New System.Windows.Forms.Button
		Me._Command1_3 = New System.Windows.Forms.Button
		Me._Command1_2 = New System.Windows.Forms.Button
		Me._Command1_1 = New System.Windows.Forms.Button
		Me.Memory = New System.Windows.Forms.Label
		Me.Command1 = New Microsoft.VisualBasic.Compatibility.VB6.ButtonArray(components)
		Me.Command2 = New Microsoft.VisualBasic.Compatibility.VB6.ButtonArray(components)
		Me.Command7 = New Microsoft.VisualBasic.Compatibility.VB6.ButtonArray(components)
		Me.MainMenu1.SuspendLayout()
		Me.Frame1.SuspendLayout()
		Me.SuspendLayout()
		Me.ToolTip1.Active = True
		CType(Me.Command1, System.ComponentModel.ISupportInitialize).BeginInit()
		CType(Me.Command2, System.ComponentModel.ISupportInitialize).BeginInit()
		CType(Me.Command7, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
		Me.Text = "Calculcator"
		Me.ClientSize = New System.Drawing.Size(303, 239)
		Me.Location = New System.Drawing.Point(115, 131)
		Me.ForeColor = System.Drawing.Color.Black
		Me.Icon = CType(resources.GetObject("standard.Icon"), System.Drawing.Icon)
		Me.KeyPreview = True
		Me.MaximizeBox = False
		Me.MinimizeBox = False
		Me.ShowInTaskbar = False
		Me.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.BackColor = System.Drawing.SystemColors.Control
		Me.ControlBox = True
		Me.Enabled = True
		Me.Cursor = System.Windows.Forms.Cursors.Default
		Me.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.HelpButton = False
		Me.WindowState = System.Windows.Forms.FormWindowState.Normal
		Me.Name = "standard"
		Me.edit.Name = "edit"
		Me.edit.Text = "&Edit"
		Me.edit.Checked = False
		Me.edit.Enabled = True
		Me.edit.Visible = True
		Me.eexit.Name = "eexit"
		Me.eexit.Text = "&Exit"
		Me.eexit.Checked = False
		Me.eexit.Enabled = True
		Me.eexit.Visible = True
		Me.Frame1.Size = New System.Drawing.Size(297, 209)
		Me.Frame1.Location = New System.Drawing.Point(0, 24)
		Me.Frame1.TabIndex = 0
		Me.Frame1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Frame1.BackColor = System.Drawing.SystemColors.Control
		Me.Frame1.Enabled = True
		Me.Frame1.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Frame1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Frame1.Visible = True
		Me.Frame1.Padding = New System.Windows.Forms.Padding(0)
		Me.Frame1.Name = "Frame1"
		Me.Memorystatus.AutoSize = False
		Me.Memorystatus.BackColor = System.Drawing.SystemColors.Control
		Me.Memorystatus.Size = New System.Drawing.Size(81, 19)
		Me.Memorystatus.Location = New System.Drawing.Point(200, 184)
		Me.Memorystatus.ReadOnly = True
		Me.Memorystatus.TabIndex = 29
		Me.Memorystatus.Text = "0"
		Me.Memorystatus.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Memorystatus.AcceptsReturn = True
		Me.Memorystatus.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
		Me.Memorystatus.CausesValidation = True
		Me.Memorystatus.Enabled = True
		Me.Memorystatus.ForeColor = System.Drawing.SystemColors.WindowText
		Me.Memorystatus.HideSelection = True
		Me.Memorystatus.Maxlength = 0
		Me.Memorystatus.Cursor = System.Windows.Forms.Cursors.IBeam
		Me.Memorystatus.MultiLine = False
		Me.Memorystatus.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Memorystatus.ScrollBars = System.Windows.Forms.ScrollBars.None
		Me.Memorystatus.TabStop = True
		Me.Memorystatus.Visible = True
		Me.Memorystatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.Memorystatus.Name = "Memorystatus"
		Me._Command2_8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_8.Text = "sqrt"
		Me._Command2_8.Size = New System.Drawing.Size(33, 41)
		Me._Command2_8.Location = New System.Drawing.Point(248, 136)
		Me._Command2_8.TabIndex = 27
		Me._Command2_8.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_8.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_8.CausesValidation = True
		Me._Command2_8.Enabled = True
		Me._Command2_8.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_8.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_8.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_8.TabStop = True
		Me._Command2_8.Name = "_Command2_8"
		Me._Command2_7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_7.Text = "1/x"
		Me._Command2_7.Size = New System.Drawing.Size(33, 41)
		Me._Command2_7.Location = New System.Drawing.Point(248, 96)
		Me._Command2_7.TabIndex = 26
		Me._Command2_7.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_7.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_7.CausesValidation = True
		Me._Command2_7.Enabled = True
		Me._Command2_7.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_7.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_7.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_7.TabStop = True
		Me._Command2_7.Name = "_Command2_7"
		Me._Command2_6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_6.Text = "-/+"
		Me._Command2_6.Size = New System.Drawing.Size(33, 41)
		Me._Command2_6.Location = New System.Drawing.Point(248, 56)
		Me._Command2_6.TabIndex = 25
		Me._Command2_6.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_6.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_6.CausesValidation = True
		Me._Command2_6.Enabled = True
		Me._Command2_6.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_6.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_6.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_6.TabStop = True
		Me._Command2_6.Name = "_Command2_6"
		Me._Command7_1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command7_1.Text = "MS"
		Me._Command7_1.Size = New System.Drawing.Size(41, 41)
		Me._Command7_1.Location = New System.Drawing.Point(208, 136)
		Me._Command7_1.TabIndex = 24
		Me._Command7_1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command7_1.BackColor = System.Drawing.SystemColors.Control
		Me._Command7_1.CausesValidation = True
		Me._Command7_1.Enabled = True
		Me._Command7_1.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command7_1.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command7_1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command7_1.TabStop = True
		Me._Command7_1.Name = "_Command7_1"
		Me.Convert.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me.Convert.Text = "Convert"
		Me.Convert.Size = New System.Drawing.Size(81, 41)
		Me.Convert.Location = New System.Drawing.Point(128, 136)
		Me.Convert.TabIndex = 23
		Me.Convert.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Convert.BackColor = System.Drawing.SystemColors.Control
		Me.Convert.CausesValidation = True
		Me.Convert.Enabled = True
		Me.Convert.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Convert.Cursor = System.Windows.Forms.Cursors.Default
		Me.Convert.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Convert.TabStop = True
		Me.Convert.Name = "Convert"
		Me._Command2_5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_5.Text = "%"
		Me._Command2_5.Size = New System.Drawing.Size(33, 41)
		Me._Command2_5.Location = New System.Drawing.Point(248, 16)
		Me._Command2_5.TabIndex = 22
		Me._Command2_5.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_5.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_5.CausesValidation = True
		Me._Command2_5.Enabled = True
		Me._Command2_5.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_5.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_5.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_5.TabStop = True
		Me._Command2_5.Name = "_Command2_5"
		Me._Command7_0.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command7_0.Text = "M +"
		Me._Command7_0.Size = New System.Drawing.Size(41, 41)
		Me._Command7_0.Location = New System.Drawing.Point(208, 96)
		Me._Command7_0.TabIndex = 21
		Me._Command7_0.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command7_0.BackColor = System.Drawing.SystemColors.Control
		Me._Command7_0.CausesValidation = True
		Me._Command7_0.Enabled = True
		Me._Command7_0.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command7_0.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command7_0.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command7_0.TabStop = True
		Me._Command7_0.Name = "_Command7_0"
		Me._Command7_5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command7_5.Text = "MC"
		Me._Command7_5.Size = New System.Drawing.Size(41, 41)
		Me._Command7_5.Location = New System.Drawing.Point(208, 56)
		Me._Command7_5.TabIndex = 20
		Me._Command7_5.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command7_5.BackColor = System.Drawing.SystemColors.Control
		Me._Command7_5.CausesValidation = True
		Me._Command7_5.Enabled = True
		Me._Command7_5.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command7_5.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command7_5.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command7_5.TabStop = True
		Me._Command7_5.Name = "_Command7_5"
		Me._Command7_4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command7_4.Text = "MR"
		Me._Command7_4.Size = New System.Drawing.Size(41, 41)
		Me._Command7_4.Location = New System.Drawing.Point(208, 16)
		Me._Command7_4.TabIndex = 19
		Me._Command7_4.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command7_4.BackColor = System.Drawing.SystemColors.Control
		Me._Command7_4.CausesValidation = True
		Me._Command7_4.Enabled = True
		Me._Command7_4.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command7_4.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command7_4.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command7_4.TabStop = True
		Me._Command7_4.Name = "_Command7_4"
		Me._Command2_3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_3.Text = "*"
		Me._Command2_3.Size = New System.Drawing.Size(41, 41)
		Me._Command2_3.Location = New System.Drawing.Point(168, 96)
		Me._Command2_3.TabIndex = 18
		Me._Command2_3.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_3.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_3.CausesValidation = True
		Me._Command2_3.Enabled = True
		Me._Command2_3.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_3.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_3.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_3.TabStop = True
		Me._Command2_3.Name = "_Command2_3"
		Me._Command2_1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_1.Text = "--"
		Me._Command2_1.Size = New System.Drawing.Size(41, 41)
		Me._Command2_1.Location = New System.Drawing.Point(168, 56)
		Me._Command2_1.TabIndex = 17
		Me._Command2_1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_1.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_1.CausesValidation = True
		Me._Command2_1.Enabled = True
		Me._Command2_1.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_1.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_1.TabStop = True
		Me._Command2_1.Name = "_Command2_1"
		Me.Command4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me.Command4.Text = "CE"
		Me.Command4.Size = New System.Drawing.Size(41, 41)
		Me.Command4.Location = New System.Drawing.Point(168, 16)
		Me.Command4.TabIndex = 16
		Me.Command4.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Command4.BackColor = System.Drawing.SystemColors.Control
		Me.Command4.CausesValidation = True
		Me.Command4.Enabled = True
		Me.Command4.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Command4.Cursor = System.Windows.Forms.Cursors.Default
		Me.Command4.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Command4.TabStop = True
		Me.Command4.Name = "Command4"
		Me._Command2_4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_4.Text = "="
		Me._Command2_4.Size = New System.Drawing.Size(41, 41)
		Me._Command2_4.Location = New System.Drawing.Point(88, 136)
		Me._Command2_4.TabIndex = 15
		Me._Command2_4.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_4.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_4.CausesValidation = True
		Me._Command2_4.Enabled = True
		Me._Command2_4.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_4.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_4.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_4.TabStop = True
		Me._Command2_4.Name = "_Command2_4"
		Me._Command2_0.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_0.Text = "+"
		Me._Command2_0.Size = New System.Drawing.Size(41, 41)
		Me._Command2_0.Location = New System.Drawing.Point(128, 96)
		Me._Command2_0.TabIndex = 14
		Me._Command2_0.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_0.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_0.CausesValidation = True
		Me._Command2_0.Enabled = True
		Me._Command2_0.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_0.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_0.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_0.TabStop = True
		Me._Command2_0.Name = "_Command2_0"
		Me._Command1_10.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_10.Text = ","
		Me._Command1_10.Size = New System.Drawing.Size(41, 41)
		Me._Command1_10.Location = New System.Drawing.Point(48, 136)
		Me._Command1_10.TabIndex = 13
		Me._Command1_10.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_10.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_10.CausesValidation = True
		Me._Command1_10.Enabled = True
		Me._Command1_10.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_10.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_10.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_10.TabStop = True
		Me._Command1_10.Name = "_Command1_10"
		Me._Command1_0.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_0.Text = "0"
		Me._Command1_0.Size = New System.Drawing.Size(41, 41)
		Me._Command1_0.Location = New System.Drawing.Point(8, 136)
		Me._Command1_0.TabIndex = 12
		Me._Command1_0.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_0.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_0.CausesValidation = True
		Me._Command1_0.Enabled = True
		Me._Command1_0.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_0.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_0.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_0.TabStop = True
		Me._Command1_0.Name = "_Command1_0"
		Me._Command2_2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command2_2.Text = "/"
		Me._Command2_2.Size = New System.Drawing.Size(41, 41)
		Me._Command2_2.Location = New System.Drawing.Point(128, 56)
		Me._Command2_2.TabIndex = 11
		Me._Command2_2.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command2_2.BackColor = System.Drawing.SystemColors.Control
		Me._Command2_2.CausesValidation = True
		Me._Command2_2.Enabled = True
		Me._Command2_2.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command2_2.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command2_2.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command2_2.TabStop = True
		Me._Command2_2.Name = "_Command2_2"
		Me.Command3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me.Command3.Text = "C"
		Me.Command3.Size = New System.Drawing.Size(41, 41)
		Me.Command3.Location = New System.Drawing.Point(128, 16)
		Me.Command3.TabIndex = 10
		Me.Command3.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Command3.BackColor = System.Drawing.SystemColors.Control
		Me.Command3.CausesValidation = True
		Me.Command3.Enabled = True
		Me.Command3.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Command3.Cursor = System.Windows.Forms.Cursors.Default
		Me.Command3.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Command3.TabStop = True
		Me.Command3.Name = "Command3"
		Me._Command1_9.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_9.Text = "9"
		Me._Command1_9.Size = New System.Drawing.Size(41, 41)
		Me._Command1_9.Location = New System.Drawing.Point(88, 96)
		Me._Command1_9.TabIndex = 9
		Me._Command1_9.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_9.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_9.CausesValidation = True
		Me._Command1_9.Enabled = True
		Me._Command1_9.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_9.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_9.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_9.TabStop = True
		Me._Command1_9.Name = "_Command1_9"
		Me._Command1_8.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_8.Text = "8"
		Me._Command1_8.Size = New System.Drawing.Size(41, 41)
		Me._Command1_8.Location = New System.Drawing.Point(48, 96)
		Me._Command1_8.TabIndex = 8
		Me._Command1_8.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_8.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_8.CausesValidation = True
		Me._Command1_8.Enabled = True
		Me._Command1_8.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_8.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_8.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_8.TabStop = True
		Me._Command1_8.Name = "_Command1_8"
		Me._Command1_7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_7.Text = "7"
		Me._Command1_7.Size = New System.Drawing.Size(41, 41)
		Me._Command1_7.Location = New System.Drawing.Point(8, 96)
		Me._Command1_7.TabIndex = 7
		Me._Command1_7.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_7.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_7.CausesValidation = True
		Me._Command1_7.Enabled = True
		Me._Command1_7.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_7.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_7.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_7.TabStop = True
		Me._Command1_7.Name = "_Command1_7"
		Me._Command1_6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_6.Text = "6"
		Me._Command1_6.Size = New System.Drawing.Size(41, 41)
		Me._Command1_6.Location = New System.Drawing.Point(88, 56)
		Me._Command1_6.TabIndex = 6
		Me._Command1_6.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_6.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_6.CausesValidation = True
		Me._Command1_6.Enabled = True
		Me._Command1_6.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_6.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_6.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_6.TabStop = True
		Me._Command1_6.Name = "_Command1_6"
		Me._Command1_5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_5.Text = "5"
		Me._Command1_5.Size = New System.Drawing.Size(41, 41)
		Me._Command1_5.Location = New System.Drawing.Point(48, 56)
		Me._Command1_5.TabIndex = 5
		Me._Command1_5.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_5.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_5.CausesValidation = True
		Me._Command1_5.Enabled = True
		Me._Command1_5.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_5.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_5.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_5.TabStop = True
		Me._Command1_5.Name = "_Command1_5"
		Me._Command1_4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_4.Text = "4"
		Me._Command1_4.Size = New System.Drawing.Size(41, 41)
		Me._Command1_4.Location = New System.Drawing.Point(8, 56)
		Me._Command1_4.TabIndex = 4
		Me._Command1_4.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_4.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_4.CausesValidation = True
		Me._Command1_4.Enabled = True
		Me._Command1_4.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_4.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_4.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_4.TabStop = True
		Me._Command1_4.Name = "_Command1_4"
		Me._Command1_3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_3.Text = "3"
		Me._Command1_3.Size = New System.Drawing.Size(41, 41)
		Me._Command1_3.Location = New System.Drawing.Point(88, 16)
		Me._Command1_3.TabIndex = 3
		Me._Command1_3.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_3.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_3.CausesValidation = True
		Me._Command1_3.Enabled = True
		Me._Command1_3.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_3.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_3.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_3.TabStop = True
		Me._Command1_3.Name = "_Command1_3"
		Me._Command1_2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_2.Text = "2"
		Me._Command1_2.Size = New System.Drawing.Size(41, 41)
		Me._Command1_2.Location = New System.Drawing.Point(48, 16)
		Me._Command1_2.TabIndex = 2
		Me._Command1_2.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_2.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_2.CausesValidation = True
		Me._Command1_2.Enabled = True
		Me._Command1_2.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_2.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_2.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_2.TabStop = True
		Me._Command1_2.Name = "_Command1_2"
		Me._Command1_1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
		Me._Command1_1.Text = "1"
		Me._Command1_1.Size = New System.Drawing.Size(41, 41)
		Me._Command1_1.Location = New System.Drawing.Point(8, 16)
		Me._Command1_1.TabIndex = 1
		Me._Command1_1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me._Command1_1.BackColor = System.Drawing.SystemColors.Control
		Me._Command1_1.CausesValidation = True
		Me._Command1_1.Enabled = True
		Me._Command1_1.ForeColor = System.Drawing.SystemColors.ControlText
		Me._Command1_1.Cursor = System.Windows.Forms.Cursors.Default
		Me._Command1_1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me._Command1_1.TabStop = True
		Me._Command1_1.Name = "_Command1_1"
		Me.Memory.Text = "Mem:"
		Me.Memory.Size = New System.Drawing.Size(41, 17)
		Me.Memory.Location = New System.Drawing.Point(168, 184)
		Me.Memory.TabIndex = 28
		Me.Memory.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Memory.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.Memory.BackColor = System.Drawing.SystemColors.Control
		Me.Memory.Enabled = True
		Me.Memory.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Memory.Cursor = System.Windows.Forms.Cursors.Default
		Me.Memory.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Memory.UseMnemonic = True
		Me.Memory.Visible = True
		Me.Memory.AutoSize = False
		Me.Memory.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Memory.Name = "Memory"
		Me.Controls.Add(Frame1)
		Me.Frame1.Controls.Add(Memorystatus)
		Me.Frame1.Controls.Add(_Command2_8)
		Me.Frame1.Controls.Add(_Command2_7)
		Me.Frame1.Controls.Add(_Command2_6)
		Me.Frame1.Controls.Add(_Command7_1)
		Me.Frame1.Controls.Add(Convert)
		Me.Frame1.Controls.Add(_Command2_5)
		Me.Frame1.Controls.Add(_Command7_0)
		Me.Frame1.Controls.Add(_Command7_5)
		Me.Frame1.Controls.Add(_Command7_4)
		Me.Frame1.Controls.Add(_Command2_3)
		Me.Frame1.Controls.Add(_Command2_1)
		Me.Frame1.Controls.Add(Command4)
		Me.Frame1.Controls.Add(_Command2_4)
		Me.Frame1.Controls.Add(_Command2_0)
		Me.Frame1.Controls.Add(_Command1_10)
		Me.Frame1.Controls.Add(_Command1_0)
		Me.Frame1.Controls.Add(_Command2_2)
		Me.Frame1.Controls.Add(Command3)
		Me.Frame1.Controls.Add(_Command1_9)
		Me.Frame1.Controls.Add(_Command1_8)
		Me.Frame1.Controls.Add(_Command1_7)
		Me.Frame1.Controls.Add(_Command1_6)
		Me.Frame1.Controls.Add(_Command1_5)
		Me.Frame1.Controls.Add(_Command1_4)
		Me.Frame1.Controls.Add(_Command1_3)
		Me.Frame1.Controls.Add(_Command1_2)
		Me.Frame1.Controls.Add(_Command1_1)
		Me.Frame1.Controls.Add(Memory)
		Me.Command1.SetIndex(_Command1_10, CType(10, Short))
		Me.Command1.SetIndex(_Command1_0, CType(0, Short))
		Me.Command1.SetIndex(_Command1_9, CType(9, Short))
		Me.Command1.SetIndex(_Command1_8, CType(8, Short))
		Me.Command1.SetIndex(_Command1_7, CType(7, Short))
		Me.Command1.SetIndex(_Command1_6, CType(6, Short))
		Me.Command1.SetIndex(_Command1_5, CType(5, Short))
		Me.Command1.SetIndex(_Command1_4, CType(4, Short))
		Me.Command1.SetIndex(_Command1_3, CType(3, Short))
		Me.Command1.SetIndex(_Command1_2, CType(2, Short))
		Me.Command1.SetIndex(_Command1_1, CType(1, Short))
		Me.Command2.SetIndex(_Command2_8, CType(8, Short))
		Me.Command2.SetIndex(_Command2_7, CType(7, Short))
		Me.Command2.SetIndex(_Command2_6, CType(6, Short))
		Me.Command2.SetIndex(_Command2_5, CType(5, Short))
		Me.Command2.SetIndex(_Command2_3, CType(3, Short))
		Me.Command2.SetIndex(_Command2_1, CType(1, Short))
		Me.Command2.SetIndex(_Command2_4, CType(4, Short))
		Me.Command2.SetIndex(_Command2_0, CType(0, Short))
		Me.Command2.SetIndex(_Command2_2, CType(2, Short))
		Me.Command7.SetIndex(_Command7_1, CType(1, Short))
		Me.Command7.SetIndex(_Command7_0, CType(0, Short))
		Me.Command7.SetIndex(_Command7_5, CType(5, Short))
		Me.Command7.SetIndex(_Command7_4, CType(4, Short))
		CType(Me.Command7, System.ComponentModel.ISupportInitialize).EndInit()
		CType(Me.Command2, System.ComponentModel.ISupportInitialize).EndInit()
		CType(Me.Command1, System.ComponentModel.ISupportInitialize).EndInit()
		MainMenu1.Items.AddRange(New System.Windows.Forms.ToolStripItem(){Me.edit})
		edit.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.eexit})
		Me.Controls.Add(MainMenu1)
		Me.MainMenu1.ResumeLayout(False)
		Me.Frame1.ResumeLayout(False)
		Me.ResumeLayout(False)
		Me.PerformLayout()
	End Sub
#End Region 
End Class