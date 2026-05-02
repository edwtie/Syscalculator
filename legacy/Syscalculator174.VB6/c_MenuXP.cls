VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "c_MenuXP"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = False
Attribute VB_Description = "Provides methods for painting transparent and disabled looking images."
Attribute VB_Ext_KEY = "SavedWithClassBuilder" ,"Yes"
Attribute VB_Ext_KEY = "Top_Level" ,"Yes"
Option Explicit
DefInt A-Z

Private Type RECT
    Left       As Long
    Top        As Long
    Right      As Long
    Bottom     As Long
End Type

Private Type BITMAPINFOHEADER '40 bytes
    biSize As Long
    biWidth As Long
    biHeight As Long
    biPlanes As Integer
    biBitCount As Integer
    biCompression As Long
    biSizeImage As Long
    biXPelsPerMeter As Long
    biYPelsPerMeter As Long
    biClrUsed As Long
    biClrImportant As Long
End Type

Private Type RGBQUAD
    rgbBlue As Byte
    rgbGreen As Byte
    rgbRed As Byte
    rgbReserved As Byte
End Type

Private Type BITMAPINFO
    bmiHeader As BITMAPINFOHEADER
    bmiColors(1) As RGBQUAD
End Type

Private Declare Function DrawIcon Lib "user32" (ByVal hDC As Long, ByVal X As Long, ByVal Y As Long, ByVal hIcon As Long) As Long
Private Declare Function CreateSolidBrush Lib "gdi32" (ByVal crColor As Long) As Long
Private Declare Function BitBlt Lib "gdi32" (ByVal hDestDC As Long, ByVal X As Long, ByVal Y As Long, ByVal nWidth As Long, ByVal nHeight As Long, ByVal hSrcDC As Long, ByVal xSrc As Long, ByVal ySrc As Long, ByVal dwRop As Long) As Long
Private Declare Function SetBkColor Lib "gdi32" (ByVal hDC As Long, ByVal crColor As Long) As Long
Private Declare Function CreateCompatibleDC Lib "gdi32" (ByVal hDC As Long) As Long
Private Declare Function DeleteDC Lib "gdi32" (ByVal hDC As Long) As Long
Private Declare Function CreateCompatibleBitmap Lib "gdi32" (ByVal hDC As Long, ByVal nWidth As Long, ByVal nHeight As Long) As Long
Private Declare Function SelectObject Lib "gdi32" (ByVal hDC As Long, ByVal hObject As Long) As Long
Private Declare Function DeleteObject Lib "gdi32" (ByVal hObject As Long) As Long
Private Declare Function FillRect Lib "user32" (ByVal hDC As Long, lpRect As RECT, ByVal hBrush As Long) As Long
Private Declare Function GetDC Lib "user32" (ByVal hWnd As Long) As Long
Private Declare Function SetTextColor Lib "gdi32" (ByVal hDC As Long, ByVal crColor As Long) As Long
Private Declare Function CreateBitmap Lib "gdi32" (ByVal nWidth As Long, ByVal nHeight As Long, ByVal nPlanes As Long, ByVal nBitCount As Long, lpBits As Any) As Long
Private Declare Function GetBkColor Lib "gdi32" (ByVal hDC As Long) As Long
Private Declare Function GetTextColor Lib "gdi32" (ByVal hDC As Long) As Long
Private Declare Function SelectPalette Lib "gdi32" (ByVal hDC As Long, ByVal hPalette As Long, ByVal bForceBackground As Long) As Long
Private Declare Function RealizePalette Lib "gdi32" (ByVal hDC As Long) As Long
Private Declare Function ReleaseDC Lib "user32" (ByVal hWnd As Long, ByVal hDC As Long) As Long
Private Declare Function CreateHalftonePalette Lib "gdi32" (ByVal hDC As Long) As Long
Private Declare Function OleTranslateColor Lib "oleaut32.dll" (ByVal lOleColor As Long, ByVal lHPalette As Long, lColorRef As Long) As Long
Private Declare Function CreateDIBSection Lib "gdi32" (ByVal hDC As Long, pBitmapInfo As BITMAPINFO, ByVal un As Long, ByVal lplpVoid As Long, ByVal handle As Long, ByVal dw As Long) As Long
Private Declare Function SetDIBColorTable Lib "gdi32" (ByVal hDC As Long, ByVal un1 As Long, ByVal un2 As Long, pcRGBQuad As RGBQUAD) As Long
Private Declare Function SetMapMode Lib "gdi32" (ByVal hDC As Long, ByVal nMapMode As Long) As Long
Private Declare Function GetMapMode Lib "gdi32" (ByVal hDC As Long) As Long
Private Declare Function DrawIconEx Lib "user32" (ByVal hDC As Long, ByVal xLeft As Long, ByVal yTop As Long, ByVal hIcon As Long, ByVal cxWidth As Long, ByVal cyHeight As Long, ByVal istepIfAniCur As Long, ByVal hbrFlickerFreeDraw As Long, ByVal diFlags As Long) As Long

Private Declare Function GetClientRect Lib "user32" (ByVal hWnd As Long, lpRect As RECT) As Long
Private Declare Function DrawEdge Lib "user32" (ByVal hDC As Long, qrc As RECT, ByVal edge As Long, ByVal grfFlags As Long) As Long
Private Declare Function InflateRect Lib "user32" (lpRect As RECT, ByVal X As Long, ByVal Y As Long) As Long

Private Declare Function DrawState Lib "user32" Alias "DrawStateA" (ByVal hDC As Long, ByVal hBrush As Long, ByVal lpDrawStateProc As Long, ByVal lParam As Long, ByVal wParam As Long, ByVal n1 As Long, ByVal n2 As Long, ByVal n3 As Long, ByVal n4 As Long, ByVal un As Long) As Long

'DrawIconEx Flags
Private Const DI_MASK = &H1
Private Const DI_IMAGE = &H2
Private Const DI_NORMAL = &H3
Private Const DI_COMPAT = &H4
Private Const DI_DEFAULTSIZE = &H8

'DIB Section constants
Private Const BI_RGB = 0&
Private Const DIB_RGB_COLORS = 0 '  color table in RGBs

'Raster Operation Codes
Private Const DSna = &H220326 '0x00220326

Private Const giINVALID_PICTURE As Integer = 481

Private Const DSS_DISABLED = &H20
Private Const DSS_MONO = &H80
Private Const DSS_NORMAL = &H0
Private Const DSS_RIGHT = &H8000
Private Const DSS_UNION = &H10
Private Const DST_BITMAP = &H4
Private Const DST_COMPLEX = &H0
Private Const DST_ICON = &H3
Private Const DST_PREFIXTEXT = &H2
Private Const DST_TEXT = &H1

Private m_hpalHalftone As Long  'Halftone created for default palette use

Public Sub TileBitmapToHDC(ByVal lhDCDest As Long, _
                           ByVal picSource As Picture, _
                           ByVal lLeft As Long, _
                           ByVal lTop As Long, _
                           ByVal lWidth As Long, _
                           ByVal lHeight As Long, _
                           ByVal lDestLeft As Long, _
                           ByVal lDestTop As Long, _
                           ByVal lDestWidth As Long, _
                           ByVal lDestHeight As Long, _
                           Optional ByVal lhPal As Long)

  '-------------------------------------------------------------------------
  'Purpose:   Draws a Bitmap to an HDC without transparency
  'In:
  '   [lhdcDest]
  '           HDC of the memory device context to paint the picture on
  '   [picSource]
  '           Picture to paint
  '   [lLeft]
  '           X coordinate of the upper left corner of the area that the
  '           picture is to be painted on. (in pixels)
  '   [lTop]
  '           Y coordinate of the upper left corner of the area that the
  '           picture is to be painted on. (in pixels)
  '   [lWidth]
  '           Width of picture area to paint in pixels
  '   [lHeight]
  '           Height of picture area to paint in pixels
  '   [lhPal]
  '           Must be a valid HPALETTE
  '-------------------------------------------------------------------------
  
  Dim lhdcTemp As Long
  Dim lhPalOld As Long
  Dim hbmOld As Long
  Dim hDCScreen As Long
  Dim X As Long, Y As Long
  Dim W As Long, H As Long

    hDCScreen = GetDC(0&)

    If picSource.Type <> vbPicTypeBitmap Then Error.Raise giINVALID_PICTURE

    lhdcTemp = CreateCompatibleDC(hDCScreen)
    lhPalOld = SelectPalette(lhdcTemp, lhPal, True)
    RealizePalette lhdcTemp

    hbmOld = SelectObject(lhdcTemp, picSource.handle)

    For X = lDestLeft To lDestLeft + lDestWidth Step lWidth
        For Y = lDestTop To lDestTop + lDestHeight Step lHeight

            If X + lWidth > (lDestLeft + lDestWidth) Then
                W = (lDestLeft + lDestWidth) - X
              Else
                W = lWidth
            End If
            If Y + lHeight > (lDestTop + lDestHeight) Then
                H = (lDestTop + lDestHeight) - Y
              Else
                H = lHeight
            End If
            BitBlt lhDCDest, X, Y, W, H, lhdcTemp, 0, 0, vbSrcCopy
        Next Y
    Next X

    SelectObject lhdcTemp, hbmOld
    SelectPalette lhdcTemp, lhPalOld, True
    RealizePalette (lhdcTemp)
    DeleteDC lhdcTemp
    ReleaseDC 0&, hDCScreen

End Sub

Public Sub PaintDisabledPicture(ByVal hDCDest As Long, _
                                ByVal picSource As StdPicture, _
                                ByVal xDest As Long, _
                                ByVal yDest As Long, _
                                ByVal Width As Long, _
                                ByVal Height As Long, _
                                Optional ByVal xSrc As Long = 0, _
                                Optional ByVal ySrc As Long = 0, _
                                Optional ByVal clrMask As OLE_COLOR = 16711935, _
                                Optional ByVal hPal As Long = 0)

  Dim lbmTemp As Long
  Dim lbmTempOld As Long
  Dim lhdcTemp As Long
  Dim lhPalOld As Long
  Dim udtTempRect As RECT
  Dim lhbrWhite As Long
  Dim hDCScreen As Long
  Dim hBrush As Long

    On Error Resume Next
      If picSource Is Nothing Then

        ElseIf picSource.Type = vbPicTypeNone Then

        ElseIf picSource.Type = vbPicTypeIcon Then

          hBrush = CreateSolidBrush(RGB(128, 128, 128))

          DrawState hDCDest, hBrush, 0, picSource.handle, 0, xDest, yDest, 0, 0, (DST_ICON Or DSS_MONO)

          DeleteObject hBrush

        Else
          hDCScreen = GetDC(0&)
          lhdcTemp = CreateCompatibleDC(hDCScreen)     'Create a temporary hDC compatible to the Destination hDC
          lbmTemp = CreateCompatibleBitmap(hDCScreen, Width, Height)
          lbmTempOld = SelectObject(lhdcTemp, lbmTemp)
          lhPalOld = SelectPalette(lhdcTemp, hPal, True)
          RealizePalette lhdcTemp
          With udtTempRect
              .Top = 0
              .Left = 0
              .Bottom = Height
              .Right = Width
          End With
          SetBkColor lhdcTemp, vbWhite
          lhbrWhite = CreateSolidBrush(vbWhite)
          FillRect lhdcTemp, udtTempRect, lhbrWhite
          PaintTransparentPicture lhdcTemp, picSource, 0, 0, Width, Height, xSrc, ySrc, clrMask
          SelectObject lhdcTemp, lbmTempOld
          Err.Clear

          hBrush = CreateSolidBrush(RGB(128, 128, 128))

          DrawState hDCDest, hBrush, 0, lbmTemp, 0, xDest, yDest, 0, 0, (DST_BITMAP Or DSS_MONO)

          DeleteObject hBrush

          DeleteObject lhbrWhite
          SelectPalette lhdcTemp, lhPalOld, True
          RealizePalette lhdcTemp
          DeleteObject SelectObject(lhdcTemp, lbmTempOld)
          DeleteObject lbmTempOld
          DeleteObject lbmTemp
          DeleteObject lhPalOld
          DeleteDC lhdcTemp
          ReleaseDC 0&, hDCScreen
      End If

End Sub

Public Sub PaintCheckedPattern(ByVal hDCDest As Long, ByVal hdcSrc As Long, ByVal xDest As Long, ByVal yDest As Long, _
                               ByVal wRequired As Long, ByVal hRequired As Long, _
                               ByVal clrRequired As OLE_COLOR, Optional ByVal hPal As Long = 0)

  Dim hdcMask As Long        'hDC of the created mask image
  Dim hdcColor As Long       'hDC of the created color image
  Dim hbmMask As Long        'Bitmap handle to the mask image
  Dim hbmColor As Long       'Bitmap handle to the color image
  Dim hbmColorOld As Long
  Dim hbmMaskOld As Long
  Dim hPalOld As Long
  Dim hPalBufferOld As Long
  Dim lRequiredColor As Long
  Dim X As Long, Y As Long
  Dim bltWidth As Integer, bltHeight As Integer
  Dim hDCScreen As Long

    On Error Resume Next
      hDCScreen = GetDC(0&)

      If hPal = 0 Then hPal = m_hpalHalftone

      OleTranslateColor clrRequired, hPal, lRequiredColor

      hbmColor = CreateCompatibleBitmap(hDCScreen, 8, 8)

      hbmMask = CreateBitmap(8, 8, 1, 1, ByVal 0&)

      hdcColor = CreateCompatibleDC(hDCScreen)
      hbmColorOld = SelectObject(hdcColor, hbmColor)
      hPalOld = SelectPalette(hdcColor, hPal, True)
      RealizePalette hdcColor

      SetBkColor hdcColor, GetBkColor(hdcSrc)
      SetTextColor hdcColor, GetTextColor(hdcSrc)
      BitBlt hdcColor, 0, 0, 8, 8, hdcSrc, 0, 0, vbSrcCopy

      hdcMask = CreateCompatibleDC(hDCScreen)
      hbmMaskOld = SelectObject(hdcMask, hbmMask)

      SetBkColor hdcColor, vbBlack
      SetTextColor hdcColor, vbWhite
      BitBlt hdcMask, 0, 0, 8, 8, hdcColor, 0, 0, vbSrcCopy

      SetTextColor hDCDest, lRequiredColor

      For X = xDest To xDest + wRequired Step 8
          For Y = yDest To yDest + hRequired Step 8

              If X + 8 > (xDest + wRequired) Then
                  bltWidth = (xDest + wRequired) - X
                Else
                  bltWidth = 8
              End If
              If Y + 8 > (yDest + hRequired) Then
                  bltHeight = (yDest + hRequired) - Y
                Else
                  bltHeight = 8
              End If
              BitBlt hDCDest, X, Y, bltWidth, bltHeight, hdcMask, 0, 0, vbSrcCopy
          Next Y
      Next X

      DeleteObject SelectObject(hdcColor, hbmColorOld)
      SelectPalette hdcColor, hPalOld, True
      RealizePalette hdcColor
      DeleteDC hdcColor
      DeleteObject SelectObject(hdcMask, hbmMaskOld)
      DeleteDC hdcMask
      ReleaseDC 0&, hDCScreen

End Sub

'-------------------------------------------------------------------------
'Purpose:   Creates a disabled-appearing (grayed) bitmap, given any format of
'           input bitmap.
'In:
'   [hdcDest]
'           Device context to paint the picture on
'   [xDest]
'           X coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [yDest]
'           Y coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [Width]
'           Width of picture area to paint in pixels.  Note: If this value
'           is outrageous (i.e.: you passed a forms ScaleWidth in twips
'           instead of the pictures' width in pixels), this procedure will
'           attempt to create bitmaps that require outrageous
'           amounts of memory.
'   [Height]
'           Height of picture area to paint in pixels.  Note: If this
'           value is outrageous (i.e.: you passed a forms ScaleHeight in
'           twips instead of the pictures' height in pixels), this
'           procedure will attempt to create bitmaps that require
'           outrageous amounts of memory.
'   [picSource]
'           Standard Picture object to be used as the image source
'   [xSrc]
'           X coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'           Ignored if picSource is an Icon.
'   [ySrc]
'           Y coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'           Ignored if picSource is an Icon.
'   [clrMask]
'           Color of pixels to be masked out
'   [clrHighlight]
'           Color to be used as outline highlight
'   [clrShadow]
'           Color to be used as outline shadow
'   [hPal]
'           Handle of palette to select into the memory DC's used to create
'           the painting effect.
'           If not provided, a HalfTone palette is used.
'-------------------------------------------------------------------------
Public Sub PaintDisabledPictureEx(ByVal hDCDest As Long, _
                                  ByVal xDest As Long, _
                                  ByVal yDest As Long, _
                                  ByVal Width As Long, _
                                  ByVal Height As Long, _
                                  ByVal picSource As StdPicture, _
                                  Optional ByVal xSrc As Long = 0, _
                                  Optional ByVal ySrc As Long = 0, _
                                  Optional ByVal clrMask As OLE_COLOR = vbWhite, _
                                  Optional ByVal clrHighlight As OLE_COLOR = vb3DHighlight, _
                                  Optional ByVal clrShadow As OLE_COLOR = vb3DShadow, _
                                  Optional ByVal hPal As Long = 0)
Attribute PaintDisabledPictureEx.VB_Description = "Paints a disabled appearing image (embossed) given a source picture object."

  Dim hdcSrc As Long         'hDC that the source bitmap is selected into
  Dim hbmMemSrcOld As Long
  Dim hbmMemSrc As Long
  Dim udtRect As RECT
  Dim hbrMask As Long
  Dim lMaskColor As Long
  Dim hDCScreen As Long
  Dim hPalOld As Long

    If picSource Is Nothing Then GoTo PaintDisabledDC_InvalidParam
    Select Case picSource.Type
      Case vbPicTypeBitmap

        hDCScreen = GetDC(0&)

        If hPal = 0 Then
            hPal = m_hpalHalftone
        End If
        hdcSrc = CreateCompatibleDC(hDCScreen)
        hbmMemSrcOld = SelectObject(hdcSrc, picSource.handle)
        hPalOld = SelectPalette(hdcSrc, hPal, True)
        RealizePalette hdcSrc

        PaintDisabledDC hDCDest, xDest, yDest, Width, Height, hdcSrc, xSrc, ySrc, clrMask, clrHighlight, clrShadow, hPal

        SelectObject hdcSrc, hbmMemSrcOld
        SelectPalette hdcSrc, hPalOld, True
        RealizePalette hdcSrc
        DeleteDC hdcSrc
        ReleaseDC 0&, hDCScreen
      Case vbPicTypeIcon

        hDCScreen = GetDC(0&)

        If hPal = 0 Then
            hPal = m_hpalHalftone
        End If
        On Error Resume Next
          DrawState hDCDest, 0, 0, picSource.handle, 0, xDest, yDest, 0, 0, (DST_ICON Or DSS_MONO)
          If Err Then
              hdcSrc = CreateCompatibleDC(hDCScreen)
              hbmMemSrc = CreateCompatibleBitmap(hDCScreen, Width, Height)
              hbmMemSrcOld = SelectObject(hdcSrc, hbmMemSrc)
              hPalOld = SelectPalette(hdcSrc, hPal, True)
              RealizePalette hdcSrc

              udtRect.Bottom = Height
              udtRect.Right = Width
              OleTranslateColor clrMask, 0&, lMaskColor
              SetBkColor hdcSrc, lMaskColor
              hbrMask = CreateSolidBrush(lMaskColor)
              FillRect hdcSrc, udtRect, hbrMask
              DeleteObject hbrMask
              DrawIcon hdcSrc, 0, 0, picSource.handle

              PaintDisabledDC hDCDest, xDest, yDest, Width, Height, hdcSrc, 0&, 0&, clrMask, clrHighlight, clrShadow, hPal

              SelectPalette hdcSrc, hPalOld, True
              RealizePalette hdcSrc
              DeleteObject SelectObject(hdcSrc, hbmMemSrcOld)
              DeleteDC hdcSrc
          End If
        On Error GoTo 0
        ReleaseDC 0&, hDCScreen
      Case Else
        GoTo PaintDisabledDC_InvalidParam
    End Select

    Exit Sub

PaintDisabledDC_InvalidParam:

Exit Sub

End Sub

'-------------------------------------------------------------------------
'Purpose:   Creates a disabled-appearing (grayed) bitmap, given any format of
'           input bitmap.
'In:
'   [hdcDest]
'           Device context to paint the picture on
'   [xDest]
'           X coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [yDest]
'           Y coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [Width]
'           Width of picture area to paint in pixels.  Note: If this value
'           is outrageous (i.e.: you passed a forms ScaleWidth in twips
'           instead of the pictures' width in pixels), this procedure will
'           attempt to create bitmaps that require outrageous
'           amounts of memory.
'   [Height]
'           Height of picture area to paint in pixels.  Note: If this
'           value is outrageous (i.e.: you passed a forms ScaleHeight in
'           twips instead of the pictures' height in pixels), this
'           procedure will attempt to create bitmaps that require
'           outrageous amounts of memory.
'   [hdcSrc]
'           Device context that contains the source picture
'   [xSrc]
'           X coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'   [ySrc]
'           Y coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'   [clrMask]
'           Color of pixels to be masked out
'   [clrHighlight]
'           Color to be used as outline highlight
'   [clrShadow]
'           Color to be used as outline shadow
'   [hPal]
'           Handle of palette to select into the memory DC's used to create
'           the painting effect.
'           If not provided, a HalfTone palette is used.
'-------------------------------------------------------------------------
Public Sub PaintDisabledDC(ByVal hDCDest As Long, _
                           ByVal xDest As Long, _
                           ByVal yDest As Long, _
                           ByVal Width As Long, _
                           ByVal Height As Long, _
                           ByVal hdcSrc As Long, _
                           Optional ByVal xSrc As Long = 0, _
                           Optional ByVal ySrc As Long = 0, _
                           Optional ByVal clrMask As OLE_COLOR = vbWhite, _
                           Optional ByVal clrHighlight As OLE_COLOR = vb3DHighlight, _
                           Optional ByVal clrShadow As OLE_COLOR = vb3DShadow, _
                           Optional ByVal hPal As Long = 0)
Attribute PaintDisabledDC.VB_Description = "Paints a disabled appearing image (embossed) given a source hDC."

  Dim hDCScreen As Long
  Dim hbmMonoSection As Long
  Dim hbmMonoSectionSav As Long
  Dim hdcMonoSection As Long
  Dim hdcColor As Long
  Dim hdcDisabled As Long
  Dim hbmDisabledSav As Long
  Dim lpbi As BITMAPINFO
  Dim hbmMono As Long
  Dim hdcMono As Long
  Dim hbmMonoSav As Long
  Dim lMaskColor As Long
  Dim lMaskColorCompare As Long
  Dim hdcMaskedSource As Long
  Dim hbmMasked As Long
  Dim hbmMaskedOld As Long
  Dim hpalMaskedOld As Long
  Dim hpalDisabledOld As Long
  Dim hpalMonoOld As Long
  Dim rgbBlack As RGBQUAD
  Dim rgbWhite As RGBQUAD
  Dim dwSys3dShadow As Long
  Dim dwSys3dHighlight As Long
  Dim pvBits As Long
  Dim rgbnew(1) As RGBQUAD
  Dim hbmDisabled As Long
  Dim lMonoBkGrnd As Long
  Dim lMonoBkGrndChoices(2) As Long
  Dim lIndex As Long  'For ... Next index
  Dim hbrWhite As Long
  Dim udtRect As RECT

    If hPal = 0 Then
        hPal = m_hpalHalftone
    End If

    OleTranslateColor clrShadow, hPal, dwSys3dShadow
    OleTranslateColor clrHighlight, hPal, dwSys3dHighlight

    hDCScreen = GetDC(0&)
    With rgbBlack
        .rgbBlue = 0
        .rgbGreen = 0
        .rgbRed = 0
        .rgbReserved = 0
    End With
    With rgbWhite
        .rgbBlue = 255
        .rgbGreen = 255
        .rgbRed = 255
        .rgbReserved = 255
    End With

    With lpbi.bmiHeader
        .biSize = LenB(lpbi.bmiHeader)
        .biWidth = Width
        .biHeight = -Height
        .biPlanes = 1
        .biBitCount = 1         ' monochrome
        .biCompression = BI_RGB
        .biSizeImage = 0
        .biXPelsPerMeter = 0
        .biYPelsPerMeter = 0
        .biClrUsed = 0          ' max colors used (2^1 = 2)
        .biClrImportant = 0     ' all (both :-]) colors are important
    End With
    With lpbi
        .bmiColors(0) = rgbBlack
        .bmiColors(1) = rgbWhite
    End With

    hbmMonoSection = CreateDIBSection(hDCScreen, lpbi, DIB_RGB_COLORS, pvBits, 0&, 0)

    hdcMonoSection = CreateCompatibleDC(hDCScreen)
    hbmMonoSectionSav = SelectObject(hdcMonoSection, hbmMonoSection)

    OleTranslateColor vbWhite, hPal, lMaskColorCompare
    OleTranslateColor clrMask, hPal, lMaskColor
    If lMaskColor = lMaskColorCompare Then
        BitBlt hdcMonoSection, 0, 0, Width, Height, hdcSrc, xSrc, ySrc, vbSrcCopy
      Else
        hbmMasked = CreateCompatibleBitmap(hDCScreen, Width, Height)
        hdcMaskedSource = CreateCompatibleDC(hDCScreen)
        hbmMaskedOld = SelectObject(hdcMaskedSource, hbmMasked)
        hpalMaskedOld = SelectPalette(hdcMaskedSource, hPal, True)
        RealizePalette hdcMaskedSource

        With udtRect
            .Left = 0
            .Top = 0
            .Right = Width
            .Bottom = Height
        End With
        hbrWhite = CreateSolidBrush(vbWhite)
        FillRect hdcMaskedSource, udtRect, hbrWhite
        DeleteObject hbrWhite

        PaintTransparentDC hdcMaskedSource, 0, 0, Width, Height, hdcSrc, xSrc, ySrc, lMaskColor, hPal

        BitBlt hdcMonoSection, 0, 0, Width, Height, hdcMaskedSource, 0, 0, vbSrcCopy

        SelectPalette hdcMaskedSource, hpalMaskedOld, True
        RealizePalette hdcMaskedSource
        DeleteObject SelectObject(hdcMaskedSource, hbmMaskedOld)
        DeleteDC hdcMaskedSource
    End If

    hbmDisabled = CreateCompatibleBitmap(hDCScreen, Width, Height)

    hdcDisabled = CreateCompatibleDC(hDCScreen)
    hbmDisabledSav = SelectObject(hdcDisabled, hbmDisabled)
    hpalDisabledOld = SelectPalette(hdcDisabled, hPal, True)
    RealizePalette hdcDisabled

    BitBlt hdcDisabled, 0, 0, Width, Height, hDCDest, xDest, yDest, vbSrcCopy

    OleTranslateColor vbBlack, hPal, lMonoBkGrndChoices(0)
    OleTranslateColor vbRed, hPal, lMonoBkGrndChoices(1)
    OleTranslateColor vbBlue, hPal, lMonoBkGrndChoices(2)

    For lIndex = 0 To 2
        If lMonoBkGrndChoices(lIndex) <> dwSys3dHighlight And _
           lMonoBkGrndChoices(lIndex) <> dwSys3dShadow Then

            lMonoBkGrnd = lMonoBkGrndChoices(lIndex)
            Exit For
        End If
    Next lIndex

    With rgbnew(0)
        .rgbRed = (vbWhite \ 2 ^ 16) And &HFF
        .rgbGreen = (vbWhite \ 2 ^ 8) And &HFF
        .rgbBlue = vbWhite And &HFF
    End With
    With rgbnew(1)
        .rgbRed = (vbBlack \ 2 ^ 16) And &HFF
        .rgbGreen = (vbBlack \ 2 ^ 8) And &HFF
        .rgbBlue = vbBlack And &HFF
    End With

    SetDIBColorTable hdcMonoSection, 0, 2, rgbnew(0)

    hbmMono = CreateBitmap(Width, Height, 1, 1, ByVal 0&)
    hdcMono = CreateCompatibleDC(hDCScreen)
    hbmMonoSav = SelectObject(hdcMono, hbmMono)
    SetMapMode hdcMono, GetMapMode(hdcSrc)
    SetBkColor hdcMono, dwSys3dHighlight
    SetTextColor hdcMono, lMonoBkGrnd
    hpalMonoOld = SelectPalette(hdcMono, hPal, True)
    RealizePalette hdcMono
    BitBlt hdcMono, 0, 0, Width, Height, hdcMonoSection, 0, 0, vbSrcCopy

    PaintTransparentDC hdcDisabled, 1, 1, Width, Height, hdcMono, 0, 0, lMonoBkGrnd, hPal

    SetBkColor hdcMono, dwSys3dShadow
    SetTextColor hdcMono, lMonoBkGrnd
    BitBlt hdcMono, 0, 0, Width, Height, hdcMonoSection, 0, 0, vbSrcCopy

    PaintTransparentDC hdcDisabled, 0, 0, Width, Height, hdcMono, 0, 0, lMonoBkGrnd, hPal
    BitBlt hDCDest, xDest, yDest, Width, Height, hdcDisabled, 0, 0, vbSrcCopy

    SelectPalette hdcDisabled, hpalDisabledOld, True
    RealizePalette hdcDisabled
    DeleteObject SelectObject(hdcMonoSection, hbmMonoSectionSav)
    DeleteDC hdcMonoSection
    DeleteObject SelectObject(hdcDisabled, hbmDisabledSav)
    DeleteDC hdcDisabled
    DeleteObject SelectObject(hdcMono, hbmMonoSav)
    SelectPalette hdcMono, hpalMonoOld, True
    RealizePalette hdcMono
    DeleteDC hdcMono
    ReleaseDC 0&, hDCScreen

End Sub

'-------------------------------------------------------------------------
'Purpose:   Draws a transparent bitmap to a DC.  The pixels of the passed
'           bitmap that match the passed mask color will not be painted
'           to the destination DC
'In:
'   [hdcDest]
'           Device context to paint the picture on
'   [xDest]
'           X coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [yDest]
'           Y coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [Width]
'           Width of picture area to paint in pixels.  Note: If this value
'           is outrageous (i.e.: you passed a forms ScaleWidth in twips
'           instead of the pictures' width in pixels), this procedure will
'           attempt to create bitmaps that require outrageous
'           amounts of memory.
'   [Height]
'           Height of picture area to paint in pixels.  Note: If this
'           value is outrageous (i.e.: you passed a forms ScaleHeight in
'           twips instead of the pictures' height in pixels), this
'           procedure will attempt to create bitmaps that require
'           outrageous amounts of memory.
'   [hdcSrc]
'           Device context that contains the source picture
'   [xSrc]
'           X coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'   [ySrc]
'           Y coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'   [clrMask]
'           Color of pixels to be masked out
'   [hPal]
'           Handle of palette to select into the memory DC's used to create
'           the painting effect.
'           If not provided, a HalfTone palette is used.
'-------------------------------------------------------------------------
Public Sub PaintTransparentDC(ByVal hDCDest As Long, _
                              ByVal xDest As Long, _
                              ByVal yDest As Long, _
                              ByVal Width As Long, _
                              ByVal Height As Long, _
                              ByVal hdcSrc As Long, _
                              Optional ByVal xSrc As Long = 0, _
                              Optional ByVal ySrc As Long = 0, _
                              Optional ByVal clrMask As OLE_COLOR = 16711935, _
                              Optional ByVal hPal As Long = 0)
Attribute PaintTransparentDC.VB_Description = "Paints an image with transparent pixels defined by the mask color.  Accepts an hDC as its image source."

  Dim hdcMask As Long        'hDC of the created mask image
  Dim hdcColor As Long       'hDC of the created color image
  Dim hbmMask As Long        'Bitmap handle to the mask image
  Dim hbmColor As Long       'Bitmap handle to the color image
  Dim hbmColorOld As Long
  Dim hbmMaskOld As Long
  Dim hPalOld As Long
  Dim hDCScreen As Long
  Dim hdcScnBuffer As Long         'Buffer to do all work on
  Dim hbmScnBuffer As Long
  Dim hbmScnBufferOld As Long
  Dim hPalBufferOld As Long
  Dim lMaskColor As Long

    hDCScreen = GetDC(0&)

    If hPal = 0 Then
        hPal = m_hpalHalftone
    End If
    OleTranslateColor clrMask, hPal, lMaskColor

    hbmScnBuffer = CreateCompatibleBitmap(hDCScreen, Width, Height)

    hdcScnBuffer = CreateCompatibleDC(hDCScreen)
    hbmScnBufferOld = SelectObject(hdcScnBuffer, hbmScnBuffer)
    hPalBufferOld = SelectPalette(hdcScnBuffer, hPal, True)
    RealizePalette hdcScnBuffer

    BitBlt hdcScnBuffer, 0, 0, Width, Height, hDCDest, xDest, yDest, vbSrcCopy

    hbmColor = CreateCompatibleBitmap(hDCScreen, Width, Height)

    hbmMask = CreateBitmap(Width, Height, 1, 1, ByVal 0&)

    hdcColor = CreateCompatibleDC(hDCScreen)
    hbmColorOld = SelectObject(hdcColor, hbmColor)
    hPalOld = SelectPalette(hdcColor, hPal, True)
    RealizePalette hdcColor

    SetBkColor hdcColor, GetBkColor(hdcSrc)
    SetTextColor hdcColor, GetTextColor(hdcSrc)
    BitBlt hdcColor, 0, 0, Width, Height, hdcSrc, xSrc, ySrc, vbSrcCopy

    hdcMask = CreateCompatibleDC(hDCScreen)
    hbmMaskOld = SelectObject(hdcMask, hbmMask)

    SetBkColor hdcColor, lMaskColor
    SetTextColor hdcColor, vbWhite
    BitBlt hdcMask, 0, 0, Width, Height, hdcColor, 0, 0, vbSrcCopy

    SetTextColor hdcColor, vbBlack
    SetBkColor hdcColor, vbWhite
    BitBlt hdcColor, 0, 0, Width, Height, hdcMask, 0, 0, DSna

    BitBlt hdcScnBuffer, 0, 0, Width, Height, hdcMask, 0, 0, vbSrcAnd

    BitBlt hdcScnBuffer, 0, 0, Width, Height, hdcColor, 0, 0, vbSrcPaint

    BitBlt hDCDest, xDest, yDest, Width, Height, hdcScnBuffer, 0, 0, vbSrcCopy

    DeleteObject SelectObject(hdcColor, hbmColorOld)
    SelectPalette hdcColor, hPalOld, True
    RealizePalette hdcColor
    DeleteDC hdcColor
    DeleteObject SelectObject(hdcScnBuffer, hbmScnBufferOld)
    SelectPalette hdcScnBuffer, hPalBufferOld, True
    RealizePalette hdcScnBuffer
    DeleteDC hdcScnBuffer

    DeleteObject SelectObject(hdcMask, hbmMaskOld)
    DeleteDC hdcMask
    ReleaseDC 0&, hDCScreen

End Sub

'-------------------------------------------------------------------------
'Purpose:   Draws a transparent bitmap to a DC.  The pixels of the passed
'           bitmap that match the passed mask color will not be painted
'           to the destination DC
'In:
'   [hdcDest]
'           Device context to paint the picture on
'   [xDest]
'           X coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [yDest]
'           Y coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [Width]
'           Width of picture area to paint in pixels.  Note: If this value
'           is outrageous (i.e.: you passed a forms ScaleWidth in twips
'           instead of the pictures' width in pixels), this procedure will
'           attempt to create bitmaps that require outrageous
'           amounts of memory.
'   [Height]
'           Height of picture area to paint in pixels.  Note: If this
'           value is outrageous (i.e.: you passed a forms ScaleHeight in
'           twips instead of the pictures' height in pixels), this
'           procedure will attempt to create bitmaps that require
'           outrageous amounts of memory.
'   [picSource]
'           Standard Picture object to be used as the image source
'   [xSrc]
'           X coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'           Ignored if picSource is an Icon.
'   [ySrc]
'           Y coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'           Ignored if picSource is an Icon.
'   [clrMask]
'           Color of pixels to be masked out
'   [hPal]
'           Handle of palette to select into the memory DC's used to create
'           the painting effect.
'           If not provided, a HalfTone palette is used.
'-------------------------------------------------------------------------
Public Sub PaintTransparentPicture(ByVal hDCDest As Long, _
                                   ByVal picSource As Picture, _
                                   ByVal xDest As Long, _
                                   ByVal yDest As Long, _
                                   ByVal Width As Long, _
                                   ByVal Height As Long, _
                                   Optional ByVal xSrc As Long = 0, _
                                   Optional ByVal ySrc As Long = 0, _
                                   Optional ByVal clrMask As OLE_COLOR = 16711935, _
                                   Optional ByVal hPal As Long = 0)
Attribute PaintTransparentPicture.VB_Description = "Paints an image with transparent pixels defined by the mask color.  Accepts a picture object as its image source."

  Dim hdcSrc As Long         'hDC that the source bitmap is selected into
  Dim hbmMemSrcOld As Long
  Dim hbmMemSrc As Long
  Dim udtRect As RECT
  Dim hbrMask As Long
  Dim lMaskColor As Long
  Dim hDCScreen As Long
  Dim hPalOld As Long

    If picSource Is Nothing Then GoTo PaintTransparentStdPic_InvalidParam

    Select Case picSource.Type
      Case vbPicTypeBitmap
        hDCScreen = GetDC(0&)

        If hPal = 0 Then
            hPal = m_hpalHalftone
        End If

        hdcSrc = CreateCompatibleDC(hDCScreen)
        hbmMemSrcOld = SelectObject(hdcSrc, picSource.handle)
        hPalOld = SelectPalette(hdcSrc, hPal, True)
        RealizePalette hdcSrc

        PaintTransparentDC hDCDest, xDest, yDest, Width, Height, hdcSrc, xSrc, ySrc, clrMask, hPal

        SelectObject hdcSrc, hbmMemSrcOld
        SelectPalette hdcSrc, hPalOld, True
        RealizePalette hdcSrc
        DeleteDC hdcSrc
        ReleaseDC 0&, hDCScreen
      Case vbPicTypeIcon

        hDCScreen = GetDC(0&)

        If hPal = 0 Then
            hPal = m_hpalHalftone
        End If
        hdcSrc = CreateCompatibleDC(hDCScreen)
        hbmMemSrc = CreateCompatibleBitmap(hDCScreen, Width, Height)
        hbmMemSrcOld = SelectObject(hdcSrc, hbmMemSrc)
        hPalOld = SelectPalette(hdcSrc, hPal, True)
        RealizePalette hdcSrc

        udtRect.Bottom = Height
        udtRect.Right = Width
        OleTranslateColor clrMask, 0&, lMaskColor
        hbrMask = CreateSolidBrush(lMaskColor)
        FillRect hdcSrc, udtRect, hbrMask
        DeleteObject hbrMask
        DrawIcon hdcSrc, 0, 0, picSource.handle

        PaintTransparentDC hDCDest, xDest, yDest, Width, Height, hdcSrc, xSrc, ySrc, lMaskColor, hPal

        DeleteObject SelectObject(hdcSrc, hbmMemSrcOld)
        SelectPalette hdcSrc, hPalOld, True
        RealizePalette hdcSrc
        DeleteDC hdcSrc
        ReleaseDC 0&, hDCScreen
      Case Else
        GoTo PaintTransparentStdPic_InvalidParam
    End Select

    Exit Sub

PaintTransparentStdPic_InvalidParam:

Exit Sub

End Sub

'-------------------------------------------------------------------------
'Purpose:   Draws a standard picture object to a DC
'In:
'   [hdcDest]
'           Handle of the device context to paint the picture on
'   [xDest]
'           X coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [yDest]
'           Y coordinate of the upper left corner of the area that the
'           picture is to be painted on. (in pixels)
'   [Width]
'           Width of picture area to paint in pixels.  Note: If this value
'           is outrageous (i.e.: you passed a forms ScaleWidth in twips
'           instead of the pictures' width in pixels), this procedure will
'           attempt to create bitmaps that require outrageous
'           amounts of memory.
'   [Height]
'           Height of picture area to paint in pixels.  Note: If this
'           value is outrageous (i.e.: you passed a forms ScaleHeight in
'           twips instead of the pictures' height in pixels), this
'           procedure will attempt to create bitmaps that require
'           outrageous amounts of memory.
'   [picSource]
'           Standard Picture object to be used as the image source
'   [xSrc]
'           X coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'           Ignored if picSource is an Icon.
'   [ySrc]
'           Y coordinate of the upper left corner of the area in the picture
'           to use as the source. (in pixels)
'           Ignored if picSource is an Icon.
'   [hPal]
'           Handle of palette to select into the memory DC's used to create
'           the painting effect.
'           If not provided, a HalfTone palette is used.
'-------------------------------------------------------------------------
Public Sub PaintStandardPicture(ByVal hDCDest As Long, _
                                ByVal picSource As Picture, _
                                ByVal xDest As Long, _
                                ByVal yDest As Long, _
                                ByVal Width As Long, _
                                ByVal Height As Long, _
                                Optional ByVal xSrc As Long = 0, _
                                Optional ByVal ySrc As Long = 0, _
                                Optional ByVal hPal As Long = 0)
Attribute PaintStandardPicture.VB_Description = "Paints an image provided by a picture object to an hDC with no effects."

  Dim hdcTemp As Long
  Dim hPalOld As Long
  Dim hbmMemSrcOld As Long
  Dim hDCScreen As Long
  Dim hbmMemSrc As Long

    If picSource Is Nothing Then GoTo PaintNormalStdPic_InvalidParam
    Select Case picSource.Type
      Case vbPicTypeBitmap
        If hPal = 0 Then
            hPal = m_hpalHalftone
        End If
        hDCScreen = GetDC(0&)

        hdcTemp = CreateCompatibleDC(hDCScreen)
        hPalOld = SelectPalette(hdcTemp, hPal, True)
        RealizePalette hdcTemp

        hbmMemSrcOld = SelectObject(hdcTemp, picSource.handle)

        BitBlt hDCDest, xDest, yDest, Width, Height, hdcTemp, xSrc, ySrc, vbSrcCopy

        SelectObject hdcTemp, hbmMemSrcOld
        SelectPalette hdcTemp, hPalOld, True
        RealizePalette hdcTemp
        DeleteDC hdcTemp
        ReleaseDC 0&, hDCScreen
      Case vbPicTypeIcon
        DrawIconEx hDCDest, xDest, yDest, picSource.handle, Width, Height, 0&, 0&, DI_NORMAL
      Case Else
        GoTo PaintNormalStdPic_InvalidParam
    End Select

    Exit Sub

PaintNormalStdPic_InvalidParam:

End Sub

Private Sub Class_Initialize()

  Dim hDCScreen As Long

    hDCScreen = GetDC(0&)
    m_hpalHalftone = CreateHalftonePalette(hDCScreen)
    ReleaseDC 0&, hDCScreen

End Sub

Private Sub Class_Terminate()

    DeleteObject m_hpalHalftone

End Sub

Public Sub PaintPictureToDC(ByVal hDCDest As Long, ByVal picSource As StdPicture, ByVal xDest As Long, ByVal yDest As Long, ByVal Width As Long, ByVal Height As Long, Optional ByVal clrMask As OLE_COLOR = 16711935, Optional ByVal bShadow As Boolean)

  Dim lbmTemp As Long
  Dim lbmTempOld As Long
  Dim lhdcTemp As Long
  Dim lhPalOld As Long
  Dim udtTempRect As RECT
  Dim lhbrWhite As Long
  Dim hDCScreen As Long
  Dim xSrc As Long, ySrc, hPal

    xSrc = 0
    ySrc = 0
    hPal = 0

    On Error Resume Next
      If picSource Is Nothing Then

        ElseIf picSource.Type = vbPicTypeNone Then

        ElseIf picSource.Type = vbPicTypeIcon Then

          DrawState hDCDest, 0, 0, picSource.handle, 0, xDest, yDest, 0, 0, (DST_ICON Or IIf(bShadow, DSS_MONO, DSS_NORMAL))
          If Err Then PaintDisabledPictureEx hDCDest, xDest, yDest, Width, Height, picSource, xSrc, ySrc, clrMask, vb3DHighlight, vb3DShadow, hPal
        Else
          hDCScreen = GetDC(0&)
          lhdcTemp = CreateCompatibleDC(hDCScreen)     'Create a temporary hDC compatible to the Destination hDC
          lbmTemp = CreateCompatibleBitmap(hDCScreen, Width, Height)
          lbmTempOld = SelectObject(lhdcTemp, lbmTemp)
          lhPalOld = SelectPalette(lhdcTemp, hPal, True)
          RealizePalette lhdcTemp
          With udtTempRect
              .Top = 0
              .Left = 0
              .Bottom = Height
              .Right = Width
          End With
          SetBkColor lhdcTemp, vbWhite
          lhbrWhite = CreateSolidBrush(vbWhite)
          FillRect lhdcTemp, udtTempRect, lhbrWhite
          PaintTransparentPicture lhdcTemp, picSource, 0, 0, Width, Height, xSrc, ySrc, clrMask
          SelectObject lhdcTemp, lbmTempOld
          Err.Clear
          DrawState hDCDest, 0, 0, lbmTemp, 0, xDest, yDest, 0, 0, (DST_BITMAP Or IIf(bShadow, DSS_MONO, DSS_NORMAL))
          If Err Then PaintDisabledPictureEx hDCDest, xDest, yDest, Width, Height, picSource, xSrc, ySrc, clrMask, vb3DHighlight, vb3DShadow, hPal

          DeleteObject lhbrWhite
          SelectPalette lhdcTemp, lhPalOld, True
          RealizePalette lhdcTemp
          DeleteObject SelectObject(lhdcTemp, lbmTempOld)
          DeleteObject lbmTempOld
          DeleteObject lbmTemp
          DeleteObject lhPalOld
          DeleteDC lhdcTemp
          ReleaseDC 0&, hDCScreen
      End If

End Sub
