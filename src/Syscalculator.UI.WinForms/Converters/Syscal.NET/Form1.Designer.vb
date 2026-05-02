<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> Partial Class Form1
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
	Public WithEvents openconv As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Exit_Renamed As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents File As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents ecut As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents ecopy As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents epaste As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Delete As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents delall As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents edit As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Switchclick As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Altop As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Indo As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Mtray As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents startup As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Config As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Wizard As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents cal As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Opties As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Tool As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Help2 As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents help1 As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents urllaunch As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Donate As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents About As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents Help As System.Windows.Forms.ToolStripMenuItem
	Public WithEvents MainMenu1 As System.Windows.Forms.MenuStrip
	Public WithEvents Check1 As System.Windows.Forms.CheckBox
	Public WithEvents Combo2 As System.Windows.Forms.ComboBox
	Public WithEvents Digitchek As System.Windows.Forms.CheckBox
	Public WithEvents Combo1 As System.Windows.Forms.ComboBox
	Public WithEvents Option2 As System.Windows.Forms.RadioButton
	Public WithEvents Option1 As System.Windows.Forms.RadioButton
	Public WithEvents Text2 As System.Windows.Forms.TextBox
	Public WithEvents Text1 As System.Windows.Forms.TextBox
    Public WithEvents Line11 As Microsoft.VisualBasic.PowerPacks.LineShape
    Public WithEvents Line10 As Microsoft.VisualBasic.PowerPacks.LineShape
    Public WithEvents Line9 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents Line8 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents Image3 As System.Windows.Forms.PictureBox
	Public WithEvents Image2 As System.Windows.Forms.PictureBox
	Public WithEvents Line7 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents Line6 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents Label7 As System.Windows.Forms.Label
	Public WithEvents Line5 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents Line4 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents Line3 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents _Line1_1 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents Line2 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents _Line1_0 As Microsoft.VisualBasic.PowerPacks.LineShape
	Public WithEvents Image1 As System.Windows.Forms.PictureBox
	Public WithEvents Label6 As System.Windows.Forms.Label
	Public WithEvents Label5 As System.Windows.Forms.Label
	Public WithEvents Label4 As System.Windows.Forms.Label
	Public WithEvents Label3 As System.Windows.Forms.Label
	Public WithEvents Label2 As System.Windows.Forms.Label
	Public WithEvents Label1 As System.Windows.Forms.Label
	Public WithEvents Line1 As LineShapeArray
	Public WithEvents ShapeContainer1 As Microsoft.VisualBasic.PowerPacks.ShapeContainer
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
		Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(Form1))
		Me.components = New System.ComponentModel.Container()
		Me.ToolTip1 = New System.Windows.Forms.ToolTip(components)
		Me.ShapeContainer1 = New Microsoft.VisualBasic.PowerPacks.ShapeContainer
		Me.MainMenu1 = New System.Windows.Forms.MenuStrip
		Me.File = New System.Windows.Forms.ToolStripMenuItem
		Me.openconv = New System.Windows.Forms.ToolStripMenuItem
		Me.Exit_Renamed = New System.Windows.Forms.ToolStripMenuItem
		Me.edit = New System.Windows.Forms.ToolStripMenuItem
		Me.ecut = New System.Windows.Forms.ToolStripMenuItem
		Me.ecopy = New System.Windows.Forms.ToolStripMenuItem
		Me.epaste = New System.Windows.Forms.ToolStripMenuItem
		Me.Delete = New System.Windows.Forms.ToolStripMenuItem
		Me.delall = New System.Windows.Forms.ToolStripMenuItem
		Me.Config = New System.Windows.Forms.ToolStripMenuItem
		Me.Switchclick = New System.Windows.Forms.ToolStripMenuItem
		Me.Altop = New System.Windows.Forms.ToolStripMenuItem
		Me.Indo = New System.Windows.Forms.ToolStripMenuItem
		Me.Mtray = New System.Windows.Forms.ToolStripMenuItem
		Me.startup = New System.Windows.Forms.ToolStripMenuItem
		Me.Tool = New System.Windows.Forms.ToolStripMenuItem
		Me.Wizard = New System.Windows.Forms.ToolStripMenuItem
		Me.cal = New System.Windows.Forms.ToolStripMenuItem
		Me.Opties = New System.Windows.Forms.ToolStripMenuItem
		Me.Help = New System.Windows.Forms.ToolStripMenuItem
		Me.Help2 = New System.Windows.Forms.ToolStripMenuItem
		Me.help1 = New System.Windows.Forms.ToolStripMenuItem
		Me.urllaunch = New System.Windows.Forms.ToolStripMenuItem
		Me.Donate = New System.Windows.Forms.ToolStripMenuItem
		Me.About = New System.Windows.Forms.ToolStripMenuItem
		Me.Check1 = New System.Windows.Forms.CheckBox
		Me.Combo2 = New System.Windows.Forms.ComboBox
		Me.Digitchek = New System.Windows.Forms.CheckBox
		Me.Combo1 = New System.Windows.Forms.ComboBox
		Me.Option2 = New System.Windows.Forms.RadioButton
		Me.Option1 = New System.Windows.Forms.RadioButton
		Me.Text2 = New System.Windows.Forms.TextBox
		Me.Text1 = New System.Windows.Forms.TextBox
		Me.Line11 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Line10 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Line9 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Line8 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Image3 = New System.Windows.Forms.PictureBox
		Me.Image2 = New System.Windows.Forms.PictureBox
		Me.Line7 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Line6 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Label7 = New System.Windows.Forms.Label
		Me.Line5 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Line4 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Line3 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me._Line1_1 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Line2 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me._Line1_0 = New Microsoft.VisualBasic.PowerPacks.LineShape
		Me.Image1 = New System.Windows.Forms.PictureBox
		Me.Label6 = New System.Windows.Forms.Label
		Me.Label5 = New System.Windows.Forms.Label
		Me.Label4 = New System.Windows.Forms.Label
		Me.Label3 = New System.Windows.Forms.Label
		Me.Label2 = New System.Windows.Forms.Label
		Me.Label1 = New System.Windows.Forms.Label
		Me.Line1 = New LineShapeArray(components)
		Me.MainMenu1.SuspendLayout()
		Me.SuspendLayout()
		Me.ToolTip1.Active = True
		CType(Me.Line1, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
		Me.Text = "Syscalculator Euro Edition"
		Me.ClientSize = New System.Drawing.Size(344, 222)
		Me.Location = New System.Drawing.Point(10, 56)
		Me.Icon = CType(resources.GetObject("Form1.Icon"), System.Drawing.Icon)
		Me.MaximizeBox = False
		Me.MinimizeBox = False
		Me.ShowInTaskbar = False
		Me.StartPosition = System.Windows.Forms.FormStartPosition.WindowsDefaultLocation
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
		Me.Name = "Form1"
		Me.File.Name = "File"
		Me.File.Text = "&File"
		Me.File.Checked = False
		Me.File.Enabled = True
		Me.File.Visible = True
		Me.openconv.Name = "openconv"
		Me.openconv.Text = "Open &Conversion"
		Me.openconv.Checked = False
		Me.openconv.Enabled = True
		Me.openconv.Visible = True
		Me.Exit_Renamed.Name = "Exit"
		Me.Exit_Renamed.Text = "&Exit"
		Me.Exit_Renamed.Checked = False
		Me.Exit_Renamed.Enabled = True
		Me.Exit_Renamed.Visible = True
		Me.edit.Name = "edit"
		Me.edit.Text = "&Edit"
		Me.edit.Checked = False
		Me.edit.Enabled = True
		Me.edit.Visible = True
		Me.ecut.Name = "ecut"
		Me.ecut.Text = "cut"
		Me.ecut.ShortcutKeys = CType(System.Windows.Forms.Keys.Control or System.Windows.Forms.Keys.X, System.Windows.Forms.Keys)
		Me.ecut.Checked = False
		Me.ecut.Enabled = True
		Me.ecut.Visible = True
		Me.ecopy.Name = "ecopy"
		Me.ecopy.Text = "copy"
		Me.ecopy.ShortcutKeys = CType(System.Windows.Forms.Keys.Control or System.Windows.Forms.Keys.C, System.Windows.Forms.Keys)
		Me.ecopy.Checked = False
		Me.ecopy.Enabled = True
		Me.ecopy.Visible = True
		Me.epaste.Name = "epaste"
		Me.epaste.Text = "paste"
		Me.epaste.ShortcutKeys = CType(System.Windows.Forms.Keys.Control or System.Windows.Forms.Keys.V, System.Windows.Forms.Keys)
		Me.epaste.Checked = False
		Me.epaste.Enabled = True
		Me.epaste.Visible = True
		Me.Delete.Name = "Delete"
		Me.Delete.Text = "Delete"
		Me.Delete.ShortcutKeys = CType(System.Windows.Forms.Keys.Delete, System.Windows.Forms.Keys)
		Me.Delete.Checked = False
		Me.Delete.Enabled = True
		Me.Delete.Visible = True
		Me.delall.Name = "delall"
		Me.delall.Text = "Delete All "
		Me.delall.Enabled = False
		Me.delall.Checked = False
		Me.delall.Visible = True
		Me.Config.Name = "Config"
		Me.Config.Text = "Config"
		Me.Config.Checked = False
		Me.Config.Enabled = True
		Me.Config.Visible = True
		Me.Switchclick.Name = "Switchclick"
		Me.Switchclick.Text = "Switch"
		Me.Switchclick.Checked = False
		Me.Switchclick.Enabled = True
		Me.Switchclick.Visible = True
		Me.Altop.Name = "Altop"
		Me.Altop.Text = "Always on Top"
		Me.Altop.Checked = False
		Me.Altop.Enabled = True
		Me.Altop.Visible = True
		Me.Indo.Name = "Indo"
		Me.Indo.Text = "Introducation Option"
		Me.Indo.Checked = True
		Me.Indo.Enabled = True
		Me.Indo.Visible = True
		Me.Mtray.Name = "Mtray"
		Me.Mtray.Text = "Tray"
		Me.Mtray.Checked = False
		Me.Mtray.Enabled = True
		Me.Mtray.Visible = True
		Me.startup.Name = "startup"
		Me.startup.Text = "Startup"
		Me.startup.Checked = False
		Me.startup.Enabled = True
		Me.startup.Visible = True
		Me.Tool.Name = "Tool"
		Me.Tool.Text = "&Tool"
		Me.Tool.Checked = False
		Me.Tool.Enabled = True
		Me.Tool.Visible = True
		Me.Wizard.Name = "Wizard"
		Me.Wizard.Text = "WizardExpress"
		Me.Wizard.Checked = False
		Me.Wizard.Enabled = True
		Me.Wizard.Visible = True
		Me.cal.Name = "cal"
		Me.cal.Text = "Calculcator"
		Me.cal.Checked = False
		Me.cal.Enabled = True
		Me.cal.Visible = True
		Me.Opties.Name = "Opties"
		Me.Opties.Text = "Opties"
		Me.Opties.Checked = False
		Me.Opties.Enabled = True
		Me.Opties.Visible = True
		Me.Help.Name = "Help"
		Me.Help.Text = "&Help"
		Me.Help.Checked = False
		Me.Help.Enabled = True
		Me.Help.Visible = True
		Me.Help2.Name = "Help2"
		Me.Help2.Text = "&Help"
		Me.Help2.ShortcutKeys = CType(System.Windows.Forms.Keys.F1, System.Windows.Forms.Keys)
		Me.Help2.Checked = False
		Me.Help2.Enabled = True
		Me.Help2.Visible = True
		Me.help1.Name = "help1"
		Me.help1.Text = "&Bugreports"
		Me.help1.Checked = False
		Me.help1.Enabled = True
		Me.help1.Visible = True
		Me.urllaunch.Name = "urllaunch"
		Me.urllaunch.Text = "Tcsoftware.com"
		Me.urllaunch.Checked = False
		Me.urllaunch.Enabled = True
		Me.urllaunch.Visible = True
		Me.Donate.Name = "Donate"
		Me.Donate.Text = "Donate us"
		Me.Donate.Checked = False
		Me.Donate.Enabled = True
		Me.Donate.Visible = True
		Me.About.Name = "About"
		Me.About.Text = "&About"
		Me.About.Checked = False
		Me.About.Enabled = True
		Me.About.Visible = True
		Me.Check1.Text = "Check1"
		Me.Check1.Size = New System.Drawing.Size(105, 25)
		Me.Check1.Location = New System.Drawing.Point(176, 184)
		Me.Check1.TabIndex = 14
		Me.Check1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Check1.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.Check1.FlatStyle = System.Windows.Forms.FlatStyle.Standard
		Me.Check1.BackColor = System.Drawing.SystemColors.Control
		Me.Check1.CausesValidation = True
		Me.Check1.Enabled = True
		Me.Check1.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Check1.Cursor = System.Windows.Forms.Cursors.Default
		Me.Check1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Check1.Appearance = System.Windows.Forms.Appearance.Normal
		Me.Check1.TabStop = True
		Me.Check1.CheckState = System.Windows.Forms.CheckState.Unchecked
		Me.Check1.Visible = True
		Me.Check1.Name = "Check1"
		Me.Combo2.Size = New System.Drawing.Size(41, 21)
		Me.Combo2.Location = New System.Drawing.Point(280, 184)
		Me.Combo2.Items.AddRange(New Object(){"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11"})
		Me.Combo2.TabIndex = 13
		Me.Combo2.Text = "1"
		Me.Combo2.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Combo2.BackColor = System.Drawing.SystemColors.Window
		Me.Combo2.CausesValidation = True
		Me.Combo2.Enabled = True
		Me.Combo2.ForeColor = System.Drawing.SystemColors.WindowText
		Me.Combo2.IntegralHeight = True
		Me.Combo2.Cursor = System.Windows.Forms.Cursors.Default
		Me.Combo2.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Combo2.Sorted = False
		Me.Combo2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown
		Me.Combo2.TabStop = True
		Me.Combo2.Visible = True
		Me.Combo2.Name = "Combo2"
		Me.Digitchek.Text = "Digit"
		Me.Digitchek.Size = New System.Drawing.Size(129, 25)
		Me.Digitchek.Location = New System.Drawing.Point(8, 184)
		Me.Digitchek.TabIndex = 12
		Me.Digitchek.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Digitchek.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.Digitchek.FlatStyle = System.Windows.Forms.FlatStyle.Standard
		Me.Digitchek.BackColor = System.Drawing.SystemColors.Control
		Me.Digitchek.CausesValidation = True
		Me.Digitchek.Enabled = True
		Me.Digitchek.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Digitchek.Cursor = System.Windows.Forms.Cursors.Default
		Me.Digitchek.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Digitchek.Appearance = System.Windows.Forms.Appearance.Normal
		Me.Digitchek.TabStop = True
		Me.Digitchek.CheckState = System.Windows.Forms.CheckState.Unchecked
		Me.Digitchek.Visible = True
		Me.Digitchek.Name = "Digitchek"
		Me.Combo1.Size = New System.Drawing.Size(161, 21)
		Me.Combo1.Location = New System.Drawing.Point(168, 32)
		Me.Combo1.Sorted = True
		Me.Combo1.TabIndex = 11
		Me.Combo1.Text = "Combo1"
		Me.Combo1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Combo1.BackColor = System.Drawing.SystemColors.Window
		Me.Combo1.CausesValidation = True
		Me.Combo1.Enabled = True
		Me.Combo1.ForeColor = System.Drawing.SystemColors.WindowText
		Me.Combo1.IntegralHeight = True
		Me.Combo1.Cursor = System.Windows.Forms.Cursors.Default
		Me.Combo1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Combo1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown
		Me.Combo1.TabStop = True
		Me.Combo1.Visible = True
		Me.Combo1.Name = "Combo1"
		Me.Option2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.Option2.Size = New System.Drawing.Size(17, 17)
		Me.Option2.Location = New System.Drawing.Point(8, 152)
		Me.Option2.TabIndex = 9
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
		Me.Option1.Size = New System.Drawing.Size(17, 17)
		Me.Option1.Location = New System.Drawing.Point(8, 96)
		Me.Option1.TabIndex = 8
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
		Me.Text2.AutoSize = False
		Me.Text2.Size = New System.Drawing.Size(193, 19)
		Me.Text2.Location = New System.Drawing.Point(72, 152)
		Me.Text2.TabIndex = 1
		Me.Text2.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Text2.AcceptsReturn = True
		Me.Text2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
		Me.Text2.BackColor = System.Drawing.SystemColors.Window
		Me.Text2.CausesValidation = True
		Me.Text2.Enabled = True
		Me.Text2.ForeColor = System.Drawing.SystemColors.WindowText
		Me.Text2.HideSelection = True
		Me.Text2.ReadOnly = False
		Me.Text2.Maxlength = 0
		Me.Text2.Cursor = System.Windows.Forms.Cursors.IBeam
		Me.Text2.MultiLine = False
		Me.Text2.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Text2.ScrollBars = System.Windows.Forms.ScrollBars.None
		Me.Text2.TabStop = True
		Me.Text2.Visible = True
		Me.Text2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.Text2.Name = "Text2"
		Me.Text1.AutoSize = False
		Me.Text1.Size = New System.Drawing.Size(193, 19)
		Me.Text1.Location = New System.Drawing.Point(72, 96)
		Me.Text1.TabIndex = 0
		Me.Text1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Text1.AcceptsReturn = True
		Me.Text1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
		Me.Text1.BackColor = System.Drawing.SystemColors.Window
		Me.Text1.CausesValidation = True
		Me.Text1.Enabled = True
		Me.Text1.ForeColor = System.Drawing.SystemColors.WindowText
		Me.Text1.HideSelection = True
		Me.Text1.ReadOnly = False
		Me.Text1.Maxlength = 0
		Me.Text1.Cursor = System.Windows.Forms.Cursors.IBeam
		Me.Text1.MultiLine = False
		Me.Text1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Text1.ScrollBars = System.Windows.Forms.ScrollBars.None
		Me.Text1.TabStop = True
		Me.Text1.Visible = True
		Me.Text1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Text1.Name = "Text1"
        REM Me.Line11.BorderColor = System.Drawing.SystemColors.ControlLight
		Me.Line11.X1 = 6
		Me.Line11.X2 = 8
		Me.Line11.Y1 = 8
		Me.Line11.Y2 = 8
		Me.Line11.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line11.BorderWidth = 1
		Me.Line11.Visible = True
		Me.Line11.Name = "Line11"
		Me.Line10.BorderColor = System.Drawing.SystemColors.ControlDark
		Me.Line10.X1 = 6
		Me.Line10.X2 = 8
		Me.Line10.Y1 = 35
		Me.Line10.Y2 = 35
		Me.Line10.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line10.BorderWidth = 1
		Me.Line10.Visible = True
		Me.Line10.Name = "Line10"
		Me.Line9.BorderColor = System.Drawing.SystemColors.ControlDark
		Me.Line9.X1 = 7
		Me.Line9.X2 = 7
		Me.Line9.Y1 = 35
		Me.Line9.Y2 = 6
		Me.Line9.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line9.BorderWidth = 1
		Me.Line9.Visible = True
		Me.Line9.Name = "Line9"
		Me.Line8.BorderColor = System.Drawing.SystemColors.ControlLight
		Me.Line8.X1 = 6
		Me.Line8.X2 = 6
		Me.Line8.Y1 = 35
		Me.Line8.Y2 = 7
		Me.Line8.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line8.BorderWidth = 1
		Me.Line8.Visible = True
		Me.Line8.Name = "Line8"
		Me.Image3.Size = New System.Drawing.Size(27, 27)
		Me.Image3.Location = New System.Drawing.Point(80, 32)
		Me.Image3.Image = CType(resources.GetObject("Image3.Image"), System.Drawing.Image)
		Me.Image3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
		Me.Image3.Enabled = True
		Me.Image3.Cursor = System.Windows.Forms.Cursors.Default
		Me.Image3.Visible = True
		Me.Image3.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Image3.Name = "Image3"
		Me.Image2.Size = New System.Drawing.Size(27, 27)
		Me.Image2.Location = New System.Drawing.Point(48, 32)
		Me.Image2.Image = CType(resources.GetObject("Image2.Image"), System.Drawing.Image)
		Me.Image2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
		Me.Image2.Enabled = True
		Me.Image2.Cursor = System.Windows.Forms.Cursors.Default
		Me.Image2.Visible = True
		Me.Image2.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Image2.Name = "Image2"
		Me.Line7.BorderColor = System.Drawing.SystemColors.ControlLight
		Me.Line7.X1 = 337
		Me.Line7.X2 = 337
		Me.Line7.Y1 = 1
		Me.Line7.Y2 = 41
		Me.Line7.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line7.BorderWidth = 1
		Me.Line7.Visible = True
		Me.Line7.Name = "Line7"
		Me.Line6.BorderColor = System.Drawing.SystemColors.ControlLight
		Me.Line6.X1 = 0
		Me.Line6.X2 = 338
		Me.Line6.Y1 = 41
		Me.Line6.Y2 = 41
		Me.Line6.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line6.BorderWidth = 1
		Me.Line6.Visible = True
		Me.Line6.Name = "Line6"
		Me.Label7.Text = "Location:"
		Me.Label7.Size = New System.Drawing.Size(73, 17)
		Me.Label7.Location = New System.Drawing.Point(112, 34)
		Me.Label7.TabIndex = 10
		Me.Label7.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Label7.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.Label7.BackColor = System.Drawing.SystemColors.Control
		Me.Label7.Enabled = True
		Me.Label7.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Label7.Cursor = System.Windows.Forms.Cursors.Default
		Me.Label7.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Label7.UseMnemonic = True
		Me.Label7.Visible = True
		Me.Label7.AutoSize = False
		Me.Label7.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Label7.Name = "Label7"
		Me.Line5.BorderColor = System.Drawing.SystemColors.ControlLight
		Me.Line5.X1 = 1
		Me.Line5.X2 = 1
		Me.Line5.Y1 = 1
		Me.Line5.Y2 = 40
		Me.Line5.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line5.BorderWidth = 1
		Me.Line5.Visible = True
		Me.Line5.Name = "Line5"
		Me.Line4.BorderColor = System.Drawing.SystemColors.ControlDark
		Me.Line4.X1 = 336
		Me.Line4.X2 = 336
		Me.Line4.Y1 = 0
		Me.Line4.Y2 = 40
		Me.Line4.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line4.BorderWidth = 1
		Me.Line4.Visible = True
		Me.Line4.Name = "Line4"
		Me.Line3.BorderColor = System.Drawing.SystemColors.ControlDark
		Me.Line3.X1 = 0
		Me.Line3.X2 = 0
		Me.Line3.Y1 = 0
		Me.Line3.Y2 = 40
		Me.Line3.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line3.BorderWidth = 1
		Me.Line3.Visible = True
		Me.Line3.Name = "Line3"
		Me._Line1_1.BorderColor = System.Drawing.SystemColors.ControlDark
		Me._Line1_1.X1 = 0
		Me._Line1_1.X2 = 336
		Me._Line1_1.Y1 = 40
		Me._Line1_1.Y2 = 40
		Me._Line1_1.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me._Line1_1.BorderWidth = 1
		Me._Line1_1.Visible = True
		Me._Line1_1.Name = "_Line1_1"
		Me.Line2.BorderColor = System.Drawing.SystemColors.ControlLight
		Me.Line2.X1 = 1
		Me.Line2.X2 = 336
		Me.Line2.Y1 = 1
		Me.Line2.Y2 = 1
		Me.Line2.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me.Line2.BorderWidth = 1
		Me.Line2.Visible = True
		Me.Line2.Name = "Line2"
		Me._Line1_0.BorderColor = System.Drawing.SystemColors.ControlDark
		Me._Line1_0.X1 = 0
		Me._Line1_0.X2 = 336
		Me._Line1_0.Y1 = 0
		Me._Line1_0.Y2 = 0
		Me._Line1_0.BorderStyle = System.Drawing.Drawing2D.DashStyle.Solid
		Me._Line1_0.BorderWidth = 1
		Me._Line1_0.Visible = True
		Me._Line1_0.Name = "_Line1_0"
		Me.Image1.Size = New System.Drawing.Size(27, 27)
		Me.Image1.Location = New System.Drawing.Point(16, 32)
		Me.Image1.Image = CType(resources.GetObject("Image1.Image"), System.Drawing.Image)
		Me.Image1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
		Me.Image1.Tag = "56"
		Me.Image1.Enabled = True
		Me.Image1.Cursor = System.Windows.Forms.Cursors.Default
		Me.Image1.Visible = True
		Me.Image1.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Image1.Name = "Image1"
		Me.Label6.Size = New System.Drawing.Size(49, 17)
		Me.Label6.Location = New System.Drawing.Point(272, 152)
		Me.Label6.TabIndex = 7
		Me.Label6.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Label6.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.Label6.BackColor = System.Drawing.SystemColors.Control
		Me.Label6.Enabled = True
		Me.Label6.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Label6.Cursor = System.Windows.Forms.Cursors.Default
		Me.Label6.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Label6.UseMnemonic = True
		Me.Label6.Visible = True
		Me.Label6.AutoSize = False
		Me.Label6.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Label6.Name = "Label6"
		Me.Label5.Size = New System.Drawing.Size(49, 17)
		Me.Label5.Location = New System.Drawing.Point(272, 96)
		Me.Label5.TabIndex = 6
		Me.Label5.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Label5.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.Label5.BackColor = System.Drawing.SystemColors.Control
		Me.Label5.Enabled = True
		Me.Label5.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Label5.Cursor = System.Windows.Forms.Cursors.Default
		Me.Label5.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Label5.UseMnemonic = True
		Me.Label5.Visible = True
		Me.Label5.AutoSize = False
		Me.Label5.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Label5.Name = "Label5"
		Me.Label4.Text = "Label4"
		Me.Label4.Size = New System.Drawing.Size(193, 17)
		Me.Label4.Location = New System.Drawing.Point(72, 128)
		Me.Label4.TabIndex = 5
		Me.Label4.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Label4.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.Label4.BackColor = System.Drawing.SystemColors.Control
		Me.Label4.Enabled = True
		Me.Label4.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Label4.Cursor = System.Windows.Forms.Cursors.Default
		Me.Label4.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Label4.UseMnemonic = True
		Me.Label4.Visible = True
		Me.Label4.AutoSize = False
		Me.Label4.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Label4.Name = "Label4"
		Me.Label3.Text = "Label3"
		Me.Label3.Size = New System.Drawing.Size(193, 17)
		Me.Label3.Location = New System.Drawing.Point(72, 72)
		Me.Label3.TabIndex = 4
		Me.Label3.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Label3.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.Label3.BackColor = System.Drawing.SystemColors.Control
		Me.Label3.Enabled = True
		Me.Label3.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Label3.Cursor = System.Windows.Forms.Cursors.Default
		Me.Label3.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Label3.UseMnemonic = True
		Me.Label3.Visible = True
		Me.Label3.AutoSize = False
		Me.Label3.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Label3.Name = "Label3"
		Me.Label2.Size = New System.Drawing.Size(41, 17)
		Me.Label2.Location = New System.Drawing.Point(32, 152)
		Me.Label2.TabIndex = 3
		Me.Label2.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Label2.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.Label2.BackColor = System.Drawing.SystemColors.Control
		Me.Label2.Enabled = True
		Me.Label2.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Label2.Cursor = System.Windows.Forms.Cursors.Default
		Me.Label2.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Label2.UseMnemonic = True
		Me.Label2.Visible = True
		Me.Label2.AutoSize = False
		Me.Label2.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Label2.Name = "Label2"
		Me.Label1.Size = New System.Drawing.Size(33, 17)
		Me.Label1.Location = New System.Drawing.Point(32, 96)
		Me.Label1.TabIndex = 2
		Me.Label1.Font = New System.Drawing.Font("Arial", 8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
		Me.Label1.TextAlign = System.Drawing.ContentAlignment.TopLeft
		Me.Label1.BackColor = System.Drawing.SystemColors.Control
		Me.Label1.Enabled = True
		Me.Label1.ForeColor = System.Drawing.SystemColors.ControlText
		Me.Label1.Cursor = System.Windows.Forms.Cursors.Default
		Me.Label1.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Label1.UseMnemonic = True
		Me.Label1.Visible = True
		Me.Label1.AutoSize = False
		Me.Label1.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.Label1.Name = "Label1"
		Me.Controls.Add(Check1)
		Me.Controls.Add(Combo2)
		Me.Controls.Add(Digitchek)
		Me.Controls.Add(Combo1)
		Me.Controls.Add(Option2)
		Me.Controls.Add(Option1)
		Me.Controls.Add(Text2)
		Me.Controls.Add(Text1)
		Me.ShapeContainer1.Shapes.Add(Line11)
		Me.ShapeContainer1.Shapes.Add(Line10)
		Me.ShapeContainer1.Shapes.Add(Line9)
		Me.ShapeContainer1.Shapes.Add(Line8)
		Me.Controls.Add(Image3)
		Me.Controls.Add(Image2)
		Me.ShapeContainer1.Shapes.Add(Line7)
		Me.ShapeContainer1.Shapes.Add(Line6)
		Me.Controls.Add(Label7)
		Me.ShapeContainer1.Shapes.Add(Line5)
		Me.ShapeContainer1.Shapes.Add(Line4)
		Me.ShapeContainer1.Shapes.Add(Line3)
		Me.ShapeContainer1.Shapes.Add(_Line1_1)
		Me.ShapeContainer1.Shapes.Add(Line2)
		Me.ShapeContainer1.Shapes.Add(_Line1_0)
		Me.Controls.Add(Image1)
		Me.Controls.Add(Label6)
		Me.Controls.Add(Label5)
		Me.Controls.Add(Label4)
		Me.Controls.Add(Label3)
		Me.Controls.Add(Label2)
		Me.Controls.Add(Label1)
		Me.Controls.Add(ShapeContainer1)
		Me.Line1.SetIndex(_Line1_1, CType(1, Short))
		Me.Line1.SetIndex(_Line1_0, CType(0, Short))
		CType(Me.Line1, System.ComponentModel.ISupportInitialize).EndInit()
		MainMenu1.Items.AddRange(New System.Windows.Forms.ToolStripItem(){Me.File, Me.edit, Me.Config, Me.Tool, Me.Help})
		File.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.openconv, Me.Exit_Renamed})
		edit.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.ecut, Me.ecopy, Me.epaste, Me.Delete, Me.delall})
		Config.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.Switchclick, Me.Altop, Me.Indo, Me.Mtray, Me.startup})
		Tool.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.Wizard, Me.cal, Me.Opties})
		Help.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem(){Me.Help2, Me.help1, Me.urllaunch, Me.Donate, Me.About})
		Me.Controls.Add(MainMenu1)
		Me.MainMenu1.ResumeLayout(False)
		Me.ResumeLayout(False)
		Me.PerformLayout()
	End Sub
#End Region 
End Class