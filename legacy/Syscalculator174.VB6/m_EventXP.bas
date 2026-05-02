Attribute VB_Name = "m_EventXP"

Sub DoMenuItemOverAction(lIndex As String)

    frmMenuXP.Label1.Caption = " " & lIndex: DoEvents
    'raiseevent OverMenuItem (lIndex)
  
End Sub

Sub DoMenuItemClickAction(lIndex As String)

    MsgBox "Je vybrané menu =: " & lIndex
    'raiseevent OverMenuItem (lIndex)

End Sub
