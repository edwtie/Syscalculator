VERSION 5.00
Begin VB.Form Sysmsgbox 
   BorderStyle     =   3  'Fixed Dialog
   Caption         =   "Form5"
   ClientHeight    =   1065
   ClientLeft      =   3570
   ClientTop       =   3285
   ClientWidth     =   2940
   LinkTopic       =   "Form5"
   MaxButton       =   0   'False
   MinButton       =   0   'False
   ScaleHeight     =   1065
   ScaleWidth      =   2940
   ShowInTaskbar   =   0   'False
   Begin VB.CommandButton Command1 
      Caption         =   "OK"
      Height          =   375
      Left            =   840
      TabIndex        =   1
      Top             =   600
      Width           =   1095
   End
   Begin VB.Label Label1 
      Caption         =   "Label1"
      Height          =   375
      Left            =   360
      TabIndex        =   0
      Top             =   120
      Width           =   2295
   End
End
Attribute VB_Name = "Sysmsgbox"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Private Sub Command1_Click()
Unload Sysmsgbox
End Sub

Function Form(strlabel As String) As Integer
Sysmsgbox.Caption = Form1.Caption
Label1.Caption = strlabel
Sysmsgbox.Show
Form = 1
End Function

Private Sub Form_Unload(Cancel As Integer)
Unload Sysmsgbox
End Sub

