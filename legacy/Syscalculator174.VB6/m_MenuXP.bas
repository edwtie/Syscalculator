Attribute VB_Name = "m_MenuXP"
Private m_hDC As Long                   'handle to Device Context
Private lItemIndex As Long              'number of Item Index
Private lOldProc As Long                'pointer to Old Procedure
Private ax As Long

Public Caps(1 To 100, 1 To 10) As String 'save information for menuitems

'1. dimenze uklada text pre MenuItem, viz 6.
'2. dimenze uklada jmeno ikonky, ukazujici do ImageListu imgMain
'3. dimenze obsahuje cislo Parenta (cislo Menu pod ktere toto menu patri. 0 = main menu
'4. dimenze obsahuje zda toto je ci neni Parent obsahuje hodnoty N/A (zmeniù na ËÌslo parenta)
'5. dimenzia obsahuje skutoËnÈ hMenuId
'6. dimenzia obsahuje meno na MenuItem
'7. dimenzia obsahuje text pre status riadok
'8. dimenzia obsahuje stav menuitem (enabled-disabled-checked-unchecked)
'9. dimenzia pre shortcut

'1. dimension - string to MenuItem (string)
'2. dimension - pointer to icon in imagelist (long)
'3. dimension - pointer to ParentMenu, 0 = Main menu (long)
'4. dimension - parent or not parent (string / "A/N") !! will by preprocess
'5. dimension - pointer to true ID Menu / hwnd (long)
'6. dimension - name to menuitem, for DoCommand (string)
'7. dimension - string for statusbar or help (string)
'8. dimension - status of menuitem / enabled-disabled-checked-unchecked (long) !! not processed in this version
'9. dimension - string to shortcut (string) !! not processed in this version


Public lArr As Long                     'total records in Caps
Public hMainMenu As Long                'handle to PopupMenu
Public bIde As Boolean                  'is running in IDE?
Public ImlHandle As Long                'handle to ImageList
Public bMenuWidth As Long               'width of top menuitem
Public bTopMenu As Boolean              'is menu top or popup
Public hMenuBefore As Long              'needed to redraw shadows

Private Const lRightOffset = 3          'right offset in menu
Private Const lPicWidth = 21            'menu image height
Private Const lMenuWidth = 100          'menuitem width
Private Const lMenuHeight = 20          'menuitem height

Public Function lProcWnd(mHwnd As Long, bHook As Boolean) As Long

  Dim m_ThreadID As Long
  Static m_HookID As Long

    lProcWnd = 0

    If bHook = True Then

        m_ThreadID = GetWindowThreadProcessId(mHwnd, 0)
        m_HookID = SetWindowsHookEx(WH_CALLWNDPROC, AddressOf lProcHook, 0, m_ThreadID)

        lOldProc = SetWindowLong(mHwnd, GWL_WNDPROC, AddressOf lProcMenu)
      Else

        SetWindowLong mHwnd, GWL_WNDPROC, lOldProc
        UnhookWindowsHookEx m_HookID

    End If

    lProcWnd = lOldProc

End Function

Private Function lProcMenu(ByVal hWnd As Long, ByVal nMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long

  Dim sCommand As String
  Dim lResult As Long
  Dim lIndex As Long, lRet As Long

    lRet = 0
    If ImlHandle = 0 Then ImlHandle = CreateImlEx(App.hInstance, "img_mxp", 16, 16, RGB(255, 0, 255))

    Select Case nMsg

      Case WM_COMMAND

        If lParam = 0 Then

            lIndex = (wParam And &HFFFF&)

            For ax = 1 To lArr
                If lIndex = Caps(ax, 5) Then sCommand = Caps(ax, 6)
            Next ax
            Call DoMenuItemClickAction(sCommand)
            ' zmeniù caps - pridaù jednu dimenziu na n·zov menu pre raiseevent

        End If

      Case WM_EXITMENULOOP

        lResult = DestroyMenu(hMainMenu)

      Case WM_MENUSELECT

        lIndex = (wParam And &HFFFF&)
        For ax = 1 To lArr
            If lIndex = Caps(ax, 5) Then sCommand = Caps(ax, 7)
        Next ax
        If Caps(ax, 3) <> "0" Then bTopMenu = False
        DoMenuItemOverAction sCommand
        
        'mozno je odtial volana fukcia pre submenu

      Case WM_DRAWITEM

        If CItemDrawXP(hWnd, nMsg, wParam, lParam) Then
            lProcMenu = True: Exit Function
        End If

      Case WM_MEASUREITEM

        If CItemMeasure(hWnd, nMsg, wParam, lParam) Then
            lProcMenu = True: Exit Function
        End If

    End Select

    lProcMenu = CallWindowProc(lOldProc, hWnd, nMsg, wParam, lParam)

End Function

Private Function CItemDrawXP(ByVal hWnd As Long, ByVal nMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Boolean

  Dim lItemDraw As Long
  Dim tmpRect As RECT
  Dim bDummy As Boolean
  Dim lResult As Long
  Dim iRectOffset As Integer
  Dim MeasureInfo As MEASUREITEMSTRUCT
  Dim DrawInfo As DRAWITEMSTRUCT
  Dim hBr As Long, hOldBr As Long
  Dim hPen As Long, hOldPen As Long
  Dim hPenSep As Long, hOldPenSep As Long
  Dim lTextColor As Long, dwRet As Long
  Dim hBitmap As Long, hBrMenu As Long
  Dim hDC As Long, Ret As Long
  Dim lIndex As Long, hOldMenu As Long
  Dim sItem As String
  Dim dcTmp As Long
  Dim dm As POINTAPI
  Dim tmpRectS As RECT
  Dim lItem As RECT
  Dim rItem As RECT
  Dim rHwnd As Long

    CItemDrawXP = False

    Call CopyMem(DrawInfo, ByVal lParam, LenB(DrawInfo))
    'DrawInfo.rcItem.Top = DrawInfo.rcItem.Top + 2
    'DrawInfo.rcItem.Left = DrawInfo.rcItem.Left + 2
    'DrawInfo.rcItem.Right = DrawInfo.rcItem.Right + 2
    'DrawInfo.rcItem.Bottom = DrawInfo.rcItem.Bottom + 2

    If DrawInfo.CtlType = ODT_MENU Then

        m_hDC = DrawInfo.hDC
        iRectOffset = lPicWidth + 5 'offset pre obr·zok menu

        'zmena fontu v menu items
        'OldFont = SelectObject(DrawInfo.hdc, MyFont)
        'MyFont = SendMessage(hwnd, WM_GETFONT, 0&, 0&)

        'MyFont = CreateFont(14, 0, 0, 0, 100, 0, 0, 0, 0, 0, 0, 0, 0, "Courier")
        'Call SelectObject(DrawInfo.hdc, MyFont)

        ' nakreslenie pozadia menu ötandartne
        hBrRect = CreateSolidBrush(RGB(231, 227, 219))
        hOldBrRect = SelectObject(DrawInfo.hDC, hBrRect)

        tmpRectS = DrawInfo.rcItem
        tmpRectS.Right = tmpRectS.Left + lPicWidth + 5

        FillRect m_hDC, tmpRectS, hBrRect

        Call SelectObject(DrawInfo.hDC, hOldBrRect)
        Call DeleteObject(hBrRect)

        'kreslenie Item - selected/unselected
        If (DrawInfo.itemState And ODS_SELECTED) = ODS_SELECTED Then
            hBr = CreateSolidBrush(RGB(182, 190, 215))  'farba v˝beru
            hPen = CreatePen(0, 1, RGB(8, 36, 105))     'farba okraja
            lTextColor = RGB(0, 0, 0)                   'farba pÌsma
          Else
            hBr = CreateSolidBrush(RGB(246, 246, 246))  'farba v˝beru
            hPen = CreatePen(0, 1, RGB(246, 246, 246))  'farba okraja
            lTextColor = RGB(0, 0, 0)                   'farba pÌsma
        End If

        'uloûÌme info o starom pere a ötetci
        hOldBr = SelectObject(DrawInfo.hDC, hBr)
        hOldPen = SelectObject(DrawInfo.hDC, hPen)

        With DrawInfo.rcItem

            'pozadie menu pod textom menu
            tmpRect = DrawInfo.rcItem
            tmpRect.Left = lPicWidth + 5
            FillRect m_hDC, tmpRect, hBr

            lResult = GetMenuState(hMainMenu, DrawInfo.itemID, MF_BYCOMMAND)

            'zistenie inform·ciÌ o MenuItem
            For ax = 1 To lArr
                If DrawInfo.itemID = Caps(ax, 5) Then lItemDraw = ax
            Next ax

            If Caps(lItemDraw, 1) <> "-" Then

                If (DrawInfo.itemState And ODS_SELECTED) = ODS_SELECTED Then
                    Rectangle m_hDC, .Left, .Top, .Right, .Bottom
                  Else
                    Rectangle m_hDC, .Left + iRectOffset, .Top, .Right, .Bottom
                End If
                CItemText .Left + lPicWidth + 10, .Top + 3, Caps(lItemDraw, 1), lTextColor, .Right, .Bottom

            End If

        End With

        'nastavenie pÙvodnÈho pera a ötetca
        Call SelectObject(DrawInfo.hDC, hOldBr)
        Call SelectObject(DrawInfo.hDC, hOldPen)

        'zmazanie nami vytvorenÈho brush a pen
        Call DeleteObject(hBr)
        Call DeleteObject(hPen)

        'vykresæovanie obr·zku do MenuItem
        With DrawInfo

            hBrMenu = CreateSolidBrush(RGB(128, 128, 128))
            hOldMenu = SelectObject(.hDC, hBrMenu)
           'vykreslenie obyËajnej poloûky
            If (Caps(lItemDraw, 2) <> "") Then
            
                If (DrawInfo.itemState And ODS_SELECTED) = ODS_SELECTED Then

                    dwRet = DrawIconEx(ImlHandle, CInt(Caps(lItemDraw, 2)), .hDC, hBrMenu, 6, .rcItem.Top + 3, DST_ICON Or DSS_MONO)
                    dwRet = DrawIconEx(ImlHandle, CInt(Caps(lItemDraw, 2)), .hDC, hBrMenu, 4, .rcItem.Top + 1, DST_ICON)

                    ' If DrawInfo.itemState = ODS_SELECTED And (Caps(DrawInfo.itemID, 2) = "Checked") Then
                    ' vykreslenie checked boxu !!!
                    '     Call BitBlt(.hDC, 2, .rcItem.Top + 2, 16, 16, GetImageDCFromRepository("Checked2", "16x16"), 0, 0, vbSrcCopy)
                    ' End If
                
                  Else
                
                    dwRet = DrawIconEx(ImlHandle, CInt(Caps(lItemDraw, 2)), .hDC, hBrMenu, 5, .rcItem.Top + 2, DST_ICON)
                    
                End If

            End If
            Call SelectObject(.hDC, hOldMenu)
            Call DeleteObject(hBrMenu)

            'vykreslenie separatora
            If InStr(1, Caps(lItemDraw, 1), "-") > 0 Then

                hPenSep = CreatePen(0, 1, RGB(166, 166, 166))
                hOldPenSep = SelectObject(m_hDC, hPenSep)

                MoveToEx m_hDC, .rcItem.Left + lPicWidth + 10, .rcItem.Top + 1, dm
                LineTo m_hDC, .rcItem.Right, .rcItem.Top + 1

                SelectObject m_hDC, hOldPenSep
                DeleteObject hPenSep

            End If

        End With
        
        If Caps(lItemDraw, 4) = "A" Then
            rItem = DrawInfo.rcItem
            rItem.Right = rItem.Right - 2
            Ret = TPrintSymbol(m_hDC, "4", (RGB(0, 0, 0)), rItem)
        End If

    End If

    lItem = DrawInfo.rcItem
    'Call ExcludeClipRect(m_hDC, lItem.Left, lItem.Top, lItem.Right, lItem.Bottom)
    
    CItemDrawXP = True

End Function

Private Function CItemMeasure(ByVal hWnd As Long, ByVal nMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Boolean

  Static lPrevId As Long
  Static lItemWidth As Long
  Dim sMenuText As String
  Dim lTextSize As POINTAPI
  Dim nDC As Long, lItemIndex As Long
  Dim bDummy As Boolean
  Dim lResult As Long
  Dim MeasureInfo As MEASUREITEMSTRUCT

    CItemMeasure = False
    nDC = GetWindowDC(hWnd)

    Call CopyMem(MeasureInfo, ByVal lParam, Len(MeasureInfo))

    MeasureInfo.itemWidth = lMenuWidth 'pre nemeranÈ poloûky !!!!

    For ax = 1 To lArr
        If MeasureInfo.itemID = Caps(ax, 5) Then lItemIndex = ax
    Next ax

    If lItemIndex <= lArr Then

        sMenuText = IIf(Caps(lItemIndex, 1) = "-", "A", Caps(lItemIndex, 1))
        Call GetTextExtentPoint32(nDC, sMenuText, Len(sMenuText), lTextSize)

        If Caps(lItemIndex, 3) <> lPrevId Then lItemWidth = 0
        If lItemWidth < lTextSize.x Then lItemWidth = lTextSize.x + lPicWidth + 5 + lRightOffset
        If lPrevId = 0 Then lPrevId = Caps(lItemIndex, 3)

        If (lTextSize.x + lPicWidth + 5 + lRightOffset) >= lItemWidth And Caps(lItemIndex, 3) = lPrevId Then
            lItemWidth = lPicWidth + 5 + lTextSize.x + lRightOffset
        End If

        MeasureInfo.itemWidth = lItemWidth
        lPrevId = Caps(lItemIndex, 3)

    End If

    lResult = GetMenuState(hMainMenu, MeasureInfo.itemID, 0)
    If (lResult And MF_POPUP) <> MF_POPUP Then
        MeasureInfo.itemHeight = IIf(Caps(MeasureInfo.itemID, 1) = "-", 3, lMenuHeight)
      Else
        MeasureInfo.itemHeight = lMenuHeight
    End If

    'If lItemIndex = 2 Then MeasureInfo.itemHeight = MeasureInfo.itemHeight + 2

    Call CopyMem(ByVal lParam, MeasureInfo, Len(MeasureInfo))

    CItemMeasure = True

End Function

Public Function lProcHook(ByVal ncode As Long, ByVal wParam As Long, ByVal lParam As Long) As Long

  Dim CWP As CWPSTRUCT
  Dim lRet As Long

    If ncode = HC_ACTION Then

        CopyMemory CWP, ByVal lParam, Len(CWP)

        Select Case CWP.message

          Case WM_CREATE

            If TClassName(CWP.hWnd) = "#32768" Then

                lFlag = wParam \ &H10000
                If ((lFlag And MF_SYSMENU) <> MF_SYSMENU) Then

                    lRet = SetWindowLong(CWP.hWnd, GWL_WNDPROC, AddressOf lProcShadow)

                    SetProp CWP.hWnd, "OldWndProc", lRet

                End If

            End If

        End Select

    End If

    lHooklProc = CallNextHookEx(WH_CALLWNDPROC, ncode, wParam, lParam)

End Function

Public Function lProcShadow(ByVal hWnd As Long, ByVal uMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long

  Dim lTmp As Long
  Dim lRet As Long
  Dim Ret As Long, rRng As Long
  Dim Rec As RECT, nRec As RECT
  Static xOrg As Long, yOrg As Long
  Static wOrg As Long, hOrg As Long
  Dim m_DC As Long, Rng As Long
  Dim m_Bmp As Long, hColorFill As Long
  Dim lpwp As WINDOWPOS
  Dim ncsp As NCCALCSIZE_PARAMS
  Dim ncwp As WINDOWPOS
  Dim hDcDsk As Long
  
  Static wx As Long, wy As Long, ww As Long, wh As Long, ncDC As Long, pncDC As Long

    lRet = GetProp(hWnd, "OldWndProc")

    Select Case uMsg
    
      Case WM_PRINT
      
        ncDC = wParam
        Call TFillRectangle(hWnd, wParam, bTopMenu)
        Call CShadowDraw(hWnd, wParam, xOrg, yOrg)
      
      Case WM_NCCALCSIZE
        
        If wParam <> 0 Then
        
        CopyMemory ncsp, ByVal lParam, Len(ncsp)
        CopyMemory ncwp, ByVal ncsp.lppos, Len(ncwp)
        
        ncsp.rgrc(0).Left = ncwp.x + 2
        ncsp.rgrc(0).Top = ncwp.y + 2
        ncsp.rgrc(0).Right = ncwp.x + ncwp.cx - 4
        ncsp.rgrc(0).Bottom = ncwp.y + ncwp.cy - 4
        
        wx = ncwp.x: wy = ncwp.y: ww = ncwp.cx: wh = ncwp.cy
        
        CopyMemory ByVal lParam, ncsp, Len(ncsp)
        
        lProcShadow = WVR_VALIDRECTS
        
        End If
        
      Case WM_WINDOWPOSCHANGING

        CopyMemory lpwp, ByVal lParam, Len(lpwp)
        If bTopMenu = False Then
            lpwp.x = lpwp.x + 3: 'lpwp.y = lpwp.y + 1
        End If
        If lpwp.x > 0 Then xOrg = lpwp.x
        If lpwp.y > 0 Then yOrg = lpwp.y
        If lpwp.cx > 1 Then wOrg = lpwp.cx
        If lpwp.cy > 1 Then hOrg = lpwp.cy
        lpwp.cx = lpwp.cx + 2: lpwp.cy = lpwp.cy + 2
        CopyMemory ByVal lParam, lpwp, Len(lpwp)
        
      Case WM_ERASEBKGND

        'Call TFillRectangle(hwnd, wParam)
        'Call CShadowDraw(hwnd, wParam, xOrg, yOrg)
        Call SendMessage(hWnd, WM_PRINT, 0, lParam)

        lProcShadow = True
        Exit Function

      Case WM_CREATE

        lTmp = GetWindowLong(hWnd, GWL_STYLE)
        lTmp = lTmp And Not WS_BORDER

        SetWindowLong hWnd, GWL_STYLE, lTmp

        lTmp = GetWindowLong(hWnd, GWL_EXSTYLE)
        lTmp = lTmp And Not WS_EX_WINDOWEDGE
        lTmp = lTmp And Not WS_EX_DLGMODALFRAME

        SetWindowLong hWnd, GWL_EXSTYLE, lTmp

      Case WM_DESTROY

        RemoveProp hWnd, "OldWndProc"
        SetWindowLong hWnd, GWL_WNDPROC, lRet

    End Select

    lProcShadow = CallWindowProc(lRet, hWnd, uMsg, wParam, lParam)

End Function

Private Function TPrintSymbol(hDC As Long, Symbol As String, Color As Long, rt As RECT) As Long
Dim tLF As LOGFONT
Dim hOldFont As Long
Static m_MarlettFont As Long

'glyph pre öÌpku je 3 alebo 4

  If m_MarlettFont = 0& Then
    
    tLF.lfFaceName = "Marlett" + Chr(0)
    tLF.lfCharSet = 1
    tLF.lfHeight = 13

    m_MarlettFont = CreateFontIndirect(tLF)
    
  End If

  Call SetBkMode(hDC, TRANSPARENT)
    
  hOldFont = SelectObject(hDC, m_MarlettFont)
  
  Call SetTextColor(hDC, Color)
  
  Call DrawText(hDC, Symbol, 1, rt, DT_RIGHT Or DT_VCENTER Or DT_SINGLELINE)
  'Call DrawText(hdc, Symbol, 1, rt, wFormat)
  
  Call SelectObject(hDC, hOldFont)
  'Call DeleteObject(m_MarlettFont)

End Function

Public Function TClassName(ByVal hWnd As Long) As String

  Dim sClass As String
  Dim nLen As Long

    sClass = String$(128, Chr$(0))
    nLen = GetClassName(hWnd, sClass, 128)

    If nLen = 0 Then
        sClass = ""
      Else
        sClass = Left$(sClass, nLen)
    End If

    TClassName = sClass

End Function

Private Sub CItemPicture(ByVal hDcTo As Long, ByRef m_Picture As StdPicture, ByVal x As Long, ByVal y As Long, ByVal bShadow As Boolean)

  Dim lFlags As Long
  Dim hBrush As Long

    Select Case m_Picture.Type
      Case vbPicTypeBitmap
        lFlags = DST_BITMAP
      Case vbPicTypeIcon
        lFlags = DST_ICON
      Case Else
        lFlags = DST_COMPLEX
    End Select

    If bShadow Then
        hBrush = CreateSolidBrush(RGB(128, 128, 128))
    End If

    DrawState hDcTo, IIf(bShadow, hBrush, 0), 0, m_Picture.Handle, 0, x, y, m_Picture.Width, m_Picture.Height, lFlags Or IIf(bShadow, DSS_MONO, DSS_NORMAL)

    If bShadow Then
        DeleteObject hBrush
    End If

End Sub

Public Function CItemText(ByVal x As Long, ByVal y As Long, ByVal hStr As String, ByVal Clr As Long, ByVal X2 As Long, ByVal Y2 As Long) As Long

  Dim OT As Long
  Dim hRect As RECT

    If m_hDC = 0 Then Exit Function

    SetBkMode m_hDC, NEWTRANSPARENT 'FontTransparent = True

    OT = GetTextColor(m_hDC)
    SetTextColor m_hDC, Clr

    With hRect
        .Left = x
        .Right = X2
        .Top = y
        .Bottom = Y2
    End With

    hPrint = DrawText(m_hDC, hStr, Len(hStr), hRect, DT_LEFT)

    SetTextColor m_hDC, OT 'nastavenie pÙvodnej farvy textu

End Function

Public Function TFillRectangle(hWnd As Long, m_DC As Long, bTop As Boolean) As Boolean

    Dim Rec As RECT, nRec As RECT

            GetWindowRect hWnd, Rec
            
            nRec = Rec
            nRec.Right = nRec.Right - nRec.Left
            nRec.Bottom = nRec.Bottom - nRec.Top
            nRec.Left = 0: nRec.Top = 0
            
            'nastavenie per a ötetca pre kreslenie obdlûnika
            hBrFill = CreateSolidBrush(RGB(246, 246, 246))     'farba v˝beru
            hPenFill = CreatePen(0, 1, RGB(102, 102, 102))     'farba okraja
            hPenMove = CreatePen(0, 1, RGB(231, 227, 219))
            
            'uloûÌme info o starom pere a ötetci
            hOldBrFill = SelectObject(m_DC, hBrFill)
            hOldPenFill = SelectObject(m_DC, hPenFill)
            
            Rectangle m_DC, 0, 0, nRec.Right - nRec.Left - 4, nRec.Bottom - nRec.Top - 4
            
            If bTop = True Then
                hOldPenMove = SelectObject(m_DC, hPenMove)
                
                Ret = MoveToEx(m_DC, 1, 0, 0)
                Ret = LineTo(m_DC, bMenuWidth, 0)
            End If
            
            'nastavenie pÙvodnÈho pera a ötetca
            Call SelectObject(m_DC, hOldBrFill)
            Call SelectObject(m_DC, hOldPenFill)
            Call SelectObject(m_DC, hOldPenMove)
    
            'zmazanie nami vytvorenÈho brush a pen
            Call DeleteObject(hBrFill)
            Call DeleteObject(hPenFill)
            
End Function

Public Sub CShadowDraw(ByVal hWnd As Long, ByVal hDC As Long, ByVal xOrg As Long, ByVal yOrg As Long)

  Dim hDcDsk As Long
  Dim Rec As RECT
  Dim winW As Long, winH As Long
  Dim x As Long, y As Long, c As Long

    GetWindowRect hWnd, Rec
    winW = Rec.Right - Rec.Left
    winH = Rec.Bottom - Rec.Top

    hDcDsk = GetWindowDC(GetDesktopWindow)

    For x = 1 To 4
        For y = 0 To 3
            c = GetPixel(hDcDsk, xOrg + winW - x, yOrg + y)
            SetPixel hDC, winW - x, y, c
        Next y
        For y = 4 To 7
            c = GetPixel(hDcDsk, xOrg + winW - x, yOrg + y)
            SetPixel hDC, winW - x, y, CShadowMask(3 * x * (y - 3), c)
        Next y
        For y = 8 To winH - 5
            c = GetPixel(hDcDsk, xOrg + winW - x, yOrg + y)
            SetPixel hDC, winW - x, y, CShadowMask(15 * x, c)
        Next y
        For y = winH - 4 To winH - 1
            c = GetPixel(hDcDsk, xOrg + winW - x, yOrg + y)
            SetPixel hDC, winW - x, y, CShadowMask(3 * x * -(y - winH), c)
        Next y
    Next x

    For y = 1 To 4
        For x = 0 To 3
            c = GetPixel(hDcDsk, xOrg + x, yOrg + winH - y)
            SetPixel hDC, x, winH - y, c
        Next x
        For x = 4 To 7
            c = GetPixel(hDcDsk, xOrg + x, yOrg + winH - y)
            SetPixel hDC, x, winH - y, CShadowMask(3 * (x - 3) * y, c)
        Next x
        For x = 8 To winW - 5
            c = GetPixel(hDcDsk, xOrg + x, yOrg + winH - y)
            SetPixel hDC, x, winH - y, CShadowMask(15 * y, c)
        Next x
    Next y

    ReleaseDC GetDesktopWindow, hDcDsk

End Sub

Private Function CShadowMask(ByVal lScale As Long, ByVal lColor As Long) As Long

  Dim R As Long
  Dim G As Long
  Dim B As Long

    CShadowRGB lColor, R, G, B

    R = CShadowColor(lScale, R)
    G = CShadowColor(lScale, G)
    B = CShadowColor(lScale, B)

    CShadowMask = RGB(R, G, B)

End Function

Private Function CShadowColor(ByVal lScale As Long, ByVal lColor As Long) As Long

    CShadowColor = lColor - Int(lColor * lScale / 255)

End Function

Private Sub CShadowRGB(lColor, rColor, gColor, bColor)

    a$ = Hex$(lColor)
    c$ = String$(6 - (Len(a$)), "0")
    a$ = c$ & a$
    rColor = Val("&H" & Mid$(a$, 5, 2))
    gColor = Val("&H" & Mid$(a$, 3, 2))
    bColor = Val("&H" & Mid$(a$, 1, 2))

End Sub
