Attribute VB_Name = "m_TempXP"
Private Function CPopupMenu(ParentID As Long, Optional hPopMenu As Long = 0) As Long
  
  Dim hMenu As Long
  Dim hMenu1 As Long
  Dim sItem As String
  Dim bDummy As Boolean
  Dim lFlags As Long
  Dim sPicture As String
  Dim lIdx As Long
  Dim Ret As Long

    hMenu = IIf(hPopMenu = 0, CreateMenu(), hPopMenu)

    For lIdx = 1 To lArr
    
        If Caps(lIdx, 3) = ParentID Then

            sItem = Caps(lIdx, 1)
            If Caps(lIdx, 4) = "A" Then

                hMenu1 = CPopupMenu(lIdx)
                bDummy = AppendMenu(hMenu, MF_POPUP + MF_OWNERDRAW + MF_STRING, hMenu1, ByVal sItem)
                Caps(lIdx, 5) = hMenu1
                
            Else
                
                lFlags = MF_OWNERDRAW + MF_STRING
                If sItem = "-" Then
                    lFlags = MF_SEPARATOR + lFlags
                End If

                bDummy = AppendMenu(hMenu, lFlags, lIdx, ByVal sItem)
                Caps(lIdx, 5) = lIdx
                
            End If
            
        End If
        
    Next lIdx

    CPopupMenu = hMenu

End Function

Public Sub CInitMenu()

  'fill information about menu

    Caps(1, 1) = "&File"
    Caps(1, 2) = ""
    Caps(1, 3) = "0"
    Caps(1, 4) = "A"
    Caps(1, 6) = "mnuFile"
    Caps(1, 7) = ""

    Caps(2, 1) = "&Open"
    Caps(2, 2) = "1"
    Caps(2, 3) = "1"
    Caps(2, 4) = "N"
    Caps(2, 6) = "mnuOpen"
    Caps(2, 7) = "Open File ..."

    Caps(3, 1) = "&Save"
    Caps(3, 2) = "2"
    Caps(3, 3) = "1"
    Caps(3, 4) = "N"
    Caps(3, 6) = "mnuSave"
    Caps(3, 7) = "Save File ..."

    Caps(4, 1) = "-"
    Caps(4, 2) = ""
    Caps(4, 3) = "1"
    Caps(4, 4) = "N"
    Caps(4, 6) = "mnuLine1"
    Caps(4, 7) = ""


    Caps(5, 1) = "&Exit"
    Caps(5, 2) = ""
    Caps(5, 3) = "1"
    Caps(5, 4) = "N"
    Caps(5, 6) = "mnuEnd"
    Caps(5, 7) = "End Program"

    Caps(6, 1) = "&Popup"
    Caps(6, 2) = ""
    Caps(6, 3) = "0"
    Caps(6, 4) = "A"
    Caps(6, 6) = "mnuPopup"
    Caps(6, 7) = ""

    Caps(7, 1) = "&Test"
    Caps(7, 2) = ""
    Caps(7, 3) = "6"
    Caps(7, 4) = "A"
    Caps(7, 6) = "mnuTest1"
    Caps(7, 7) = ""


    Caps(8, 1) = "&Test Item 1"
    Caps(8, 2) = "3"
    Caps(8, 3) = "7"
    Caps(8, 4) = "N"
    Caps(8, 6) = "mnuTest2"
    Caps(8, 7) = "Test Item 1"


    Caps(9, 1) = "T&est Item 2"
    Caps(9, 2) = "4"
    Caps(9, 3) = "7"
    Caps(9, 4) = "N"
    Caps(9, 6) = "mnuTest3"
    Caps(9, 7) = "Test Item 2"

    Caps(10, 1) = "&View Project"
    Caps(10, 2) = "1"
    Caps(10, 3) = "6"
    Caps(10, 4) = "N"
    Caps(10, 6) = "mnuView1"
    Caps(10, 7) = "View any project ..."

    Caps(11, 1) = "&Run Project"
    Caps(11, 2) = "2"
    Caps(11, 3) = "6"
    Caps(11, 4) = "N"
    Caps(11, 6) = "mnuRun1"
    Caps(11, 7) = "Run any project ..."

    lArr = 11 'have 9 menuitem

End Sub

Public Sub CSetupMenu(hWnd As Long)

  Dim hMenu As Long
  Dim lIndex As Long
  Dim bDummy As Boolean

    hMainMenu = CreatePopupMenu()

    For lIndex = 1 To lArr
        If Caps(lIndex, 3) = "0" Then
            hMenu = CPopupMenu(lIndex)
            bDummy = AppendMenu(hMainMenu, MF_POPUP + MF_STRING, hMenu, ByVal Caps(lIndex, 1))
            Caps(lIndex, 5) = hMenu
        End If
    Next lIndex

End Sub

Private Sub TemporCode()

        
        'GetWindowRect hWnd, Rec
            
        'nRec = Rec
        'nRec.Right = nRec.Right - nRec.Left
        'nRec.Bottom = nRec.Bottom - nRec.Top

        'frmMenuXP.Print nRec.Right, nRec.Bottom
        'retrng = CreateRectRgn(0, 0, nRec.Right, nRec.Bottom)
        'Call SelectClipRgn(wParam, retrng)
        'Call ExcludeClipRect(wParam, 0, nRec.Bottom - 4, nRec.Right, nRec.Bottom)
        'Call ExcludeClipRect(wParam, nRec.Right - 4, 0, nRec.Right, nRec.Bottom)

End Sub
