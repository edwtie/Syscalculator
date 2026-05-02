VERSION 5.00
Begin VB.Form Form2 
   BorderStyle     =   4  'Fixed ToolWindow
   Caption         =   "Search & Replace"
   ClientHeight    =   1980
   ClientLeft      =   5325
   ClientTop       =   8805
   ClientWidth     =   6270
   LinkTopic       =   "Form2"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   1980
   ScaleWidth      =   6270
   ShowInTaskbar   =   0   'False
   Begin VB.TextBox Text2 
      BeginProperty Font 
         Name            =   "Times New Roman"
         Size            =   9
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   285
      Left            =   1560
      TabIndex        =   8
      Top             =   600
      Width           =   2100
   End
   Begin VB.TextBox Text1 
      BeginProperty Font 
         Name            =   "Times New Roman"
         Size            =   9
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   285
      Left            =   1560
      TabIndex        =   7
      Top             =   120
      Width           =   2115
   End
   Begin VB.CheckBox Check2 
      Caption         =   "Whole word only"
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   240
      Left            =   210
      TabIndex        =   6
      Top             =   1575
      Width           =   2040
   End
   Begin VB.CheckBox Check1 
      Caption         =   "Case sensitive"
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   240
      Left            =   210
      TabIndex        =   5
      Top             =   1275
      Width           =   2040
   End
   Begin VB.CommandButton Command5 
      Cancel          =   -1  'True
      Caption         =   "Cancel"
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   480
      Left            =   5040
      TabIndex        =   4
      Top             =   1320
      Width           =   1230
   End
   Begin VB.CommandButton ReplaceAllButton 
      Caption         =   "Replace All"
      Enabled         =   0   'False
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   480
      Left            =   5040
      TabIndex        =   3
      Top             =   600
      Width           =   1230
   End
   Begin VB.CommandButton ReplaceButton 
      Caption         =   "Replace"
      Enabled         =   0   'False
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   480
      Left            =   3720
      TabIndex        =   2
      Top             =   600
      Width           =   1230
   End
   Begin VB.CommandButton FindNextButton 
      Caption         =   "Find Again"
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   480
      Left            =   5040
      TabIndex        =   1
      Top             =   120
      Width           =   1230
   End
   Begin VB.CommandButton FindButton 
      Caption         =   "Find"
      Default         =   -1  'True
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   480
      Left            =   3720
      TabIndex        =   0
      Top             =   105
      Width           =   1230
   End
   Begin VB.Label Label2 
      Caption         =   "Replace with"
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   255
      Left            =   120
      TabIndex        =   10
      Top             =   600
      Width           =   1485
   End
   Begin VB.Label Label1 
      Caption         =   "Find what"
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   9.75
         Charset         =   0
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   240
      Left            =   150
      TabIndex        =   9
      Top             =   120
      Width           =   1410
   End
End
Attribute VB_Name = "Form2"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit
Dim Position As Integer

Private Sub FindButton_Click()
Dim FindFlags As Integer

    Position = 0
    FindFlags = Check1.Value * 4 + Check2.Value * 2
    Position = Form1.RTF.Find(Text1.Text, Position + 1, , FindFlags)
    If Position >= 0 Then
        ReplaceButton.Enabled = True
        ReplaceAllButton.Enabled = True
        Form1.SetFocus
    Else
        MsgBox "String not found", vbOKOnly, "Search Help"
        ReplaceButton.Enabled = False
        ReplaceAllButton.Enabled = False
    End If
    
End Sub

Private Sub FindNextButton_Click()
Dim FindFlags

FindFlags = Check1.Value * 4 + Check2.Value * 2
Position = Form1.RTF.Find(Text1.Text, Position + 1, , FindFlags)
If Position > 0 Then
    Form1.SetFocus
Else
    MsgBox "String not found", vbOKOnly, "Search Help"
    ReplaceButton.Enabled = False
    ReplaceAllButton.Enabled = False
End If

End Sub

Private Sub Command5_Click()

    Form2.Hide
    
End Sub
Private Sub Form_GotFocus()
Text1.SetFocus
End Sub
Private Sub ReplaceButton_Click()
Dim FindFlags As Integer

    Form1.RTF.SelText = Text2.Text
    FindFlags = Check1.Value * 4 + Check2.Value * 2
    Position = Form1.RTF.Find(Text1.Text, Position + 1, , FindFlags)
    If Position > 0 Then
        Form1.SetFocus
    Else
        MsgBox "String not found", vbOKOnly, "Search Help"
        ReplaceButton.Enabled = False
        ReplaceAllButton.Enabled = False
    End If
    
End Sub

Private Sub ReplaceAllButton_Click()
Dim FindFlags As Integer

    FindFlags = Check1.Value * 4 + Check2.Value * 2
    Form1.RTF.SelText = Text2.Text
    Position = Form1.RTF.Find(Text1.Text, Position + 1, , FindFlags)
    While Position > 0
        Form1.RTF.SelText = Text2.Text
        Position = Form1.RTF.Find(Text1.Text, Position + 1, , FindFlags)
    Wend
        ReplaceButton.Enabled = False
        ReplaceAllButton.Enabled = False
        MsgBox "Done replacing", vbOKOnly, "Search Help"
End Sub

