VERSION 5.00
Begin VB.Form frmMenuXP 
   AutoRedraw      =   -1  'True
   BorderStyle     =   1  'Fixed Single
   Caption         =   " Test MenuXP ..."
   ClientHeight    =   3555
   ClientLeft      =   2550
   ClientTop       =   1590
   ClientWidth     =   6540
   BeginProperty Font 
      Name            =   "Tahoma"
      Size            =   8.25
      Charset         =   238
      Weight          =   400
      Underline       =   0   'False
      Italic          =   0   'False
      Strikethrough   =   0   'False
   EndProperty
   Icon            =   "frmMenuXP.frx":0000
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   ScaleHeight     =   3555
   ScaleWidth      =   6540
   Begin MenuXP.u_MenuXP u_MenuXP1 
      Align           =   1  'Align Top
      Height          =   360
      Left            =   0
      TabIndex        =   1
      Top             =   0
      Width           =   6540
      _ExtentX        =   11536
      _ExtentY        =   635
   End
   Begin VB.Label Label2 
      AutoSize        =   -1  'True
      BackStyle       =   0  'Transparent
      Caption         =   "Created by SOFTPAE, (c) 2002, http://www.softpae.sk"
      Enabled         =   0   'False
      BeginProperty Font 
         Name            =   "Tahoma"
         Size            =   6.75
         Charset         =   238
         Weight          =   400
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   165
      Left            =   3000
      TabIndex        =   2
      Top             =   435
      Width           =   3510
   End
   Begin VB.Label Label1 
      Appearance      =   0  'Flat
      BackColor       =   &H80000005&
      BackStyle       =   0  'Transparent
      BorderStyle     =   1  'Fixed Single
      Caption         =   " Please, right click on the form area to show menu ..."
      ForeColor       =   &H80000008&
      Height          =   255
      Left            =   15
      TabIndex        =   0
      Top             =   3300
      Width           =   6510
   End
End
Attribute VB_Name = "frmMenuXP"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False

Private Sub Form_Load()

    'Call lProcWnd(Me.hWnd, True)
    
    'Call CInitMenu
    'Call CSetupMenu(Me.hWnd)

    Me.Show

End Sub

Private Sub Form_MouseDown(Button As Integer, Shift As Integer, x As Single, y As Single)

Exit Sub

  Dim pt As POINTAPI
    
    If Button = vbRightButton Then
     
        pt.x = Me.ScaleX(x, vbTwips, vbPixels)
        pt.y = Me.ScaleY(y, vbTwips, vbPixels)
        ClientToScreen Me.hWnd, pt
        
        'Me.Print Caps(1, 1), Caps(1, 5)
        TrackPopupMenuEx Caps(1, 5), TPM_LEFTALIGN Or TPM_TOPALIGN Or TPM_LEFTBUTTON, pt.x, pt.y, Me.hWnd, ByVal 0&
    
    End If

End Sub

Private Sub Form_Resize()
    Label1.Top = Me.Height - 645: Label1.Width = Me.ScaleWidth - 30
End Sub

Private Sub Form_Unload(Cancel As Integer)
    'Call lProcWnd(Me.hWnd, False)
End Sub

