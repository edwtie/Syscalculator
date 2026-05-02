Attribute VB_Name = "c_MenuXP"
Private m_hIml As Long
Private IccInit As Boolean

Private Const SM_CXSMICON = 49
Private Const SM_CYSMICON = 50

Const ILC_MASK = &H1
Const ILC_COLOR = &H0
Const ILC_COLORDDB = &HFE
Const ILC_COLOR4 = &H4
Const ILC_COLOR8 = &H8
Const ILC_COLOR16 = &H10
Const ILC_COLOR24 = &H18
Const ILC_COLOR32 = &H20

Const ILD_BLEND25 = &H2
Const ILD_BLEND50 = &H4
Const ILD_MASK = &H10
Const ILD_NORMAL = &H0
Const ILD_FOCUS = ILD_BLEND25
Const ILD_SELECTED = ILD_BLEND50
Const ILD_TRANSPARENT = &H1

Const IMAGE_BITMAP = 0
Const LR_DEFAULTCOLOR = &H0
Const LR_CREATEDIBSECTION = &H2000
Const LR_LOADTRANSPARENT = &H20
Const LR_VGACOLOR = &H80

Const CLR_NONE = &HFFFFFFFF
Const CLR_DEFAULT = &HFF000000

Private Declare Sub InitCommonControls Lib "comctl32.dll" ()

Private Declare Function GetSystemMetrics Lib "user32" (ByVal nIndex As Long) As Long
Private Declare Function DrawState Lib "user32" Alias "DrawStateA" (ByVal hDC As Long, ByVal hBr As Long, ByVal lpDrawStateProc As Long, ByVal lParam As Long, ByVal wParam As Long, ByVal x As Long, ByVal y As Long, ByVal cx As Long, ByVal cy As Long, ByVal fuFlags As Long) As Long
Private Declare Function DestroyIcon Lib "user32" (ByVal hIcon As Long) As Long
Private Declare Function ImageList_Create Lib "comctl32.dll" (ByVal cx As Long, ByVal cy As Long, ByVal flags As Long, ByVal cInitial As Long, ByVal cGrow As Long) As Long
Private Declare Function ImageList_Destroy Lib "comctl32.dll" (ByVal hIml As Long) As Long
Private Declare Function ImageList_GetIcon Lib "comctl32.dll" (ByVal hIml As Long, ByVal i As Long, ByVal flags As Long) As Long
Private Declare Function ImageList_GetImageCount Lib "comctl32.dll" (ByVal hIml As Long) As Long
Private Declare Function ImageList_ReplaceIcon Lib "comctl32.dll" (ByVal hIml As Long, ByVal i As Long, ByVal hIcon As Long) As Long
Private Declare Function ImageList_AddMasked Lib "comctl32.dll" (ByVal hIml As Long, ByVal hbmImage As Long, ByVal crMask As Long) As Long
Private Declare Function ImageList_LoadImage Lib "comctl32.dll" (ByVal hi As Long, ByVal lpbmp As String, ByVal cx As Long, ByVal cGrow As Long, ByVal crMask As Long, ByVal uType As Long, ByVal uFlags As Long) As Long
Private Declare Function ImageList_Remove Lib "comctl32.dll" (ByVal hIml As Long, ByVal i As Long) As Long
Private Declare Function ImageList_Draw Lib "comctl32.dll" (ByVal hIml As Long, ByVal i As Long, ByVal hdcDst As Long, ByVal x As Long, ByVal y As Long, ByVal fStyle As Long) As Long
Private Declare Function ImageList_DrawEx Lib "comctl32.dll" (ByVal hIml As Long, ByVal i As Long, ByVal hdcDst As Long, ByVal x As Long, ByVal y As Long, ByVal dx As Long, ByVal dy As Long, ByVal rgbBk As Long, ByVal rgbFg As Long, ByVal fStyle As Long) As Long

Public Function DrawIconEx(ByVal hIml As Long, ByVal hIndex As Long, ByVal hDcDesc As Long, ByVal hBrMenu As Long, ByVal hX As Long, ByVal hY As Long, ByVal hStyle As Long) As Long
  hIcon = ImageList_GetIcon(hIml, hIndex, 0)
  DrawIconEx = DrawState(hDcDesc, hBrMenu, 0, hIcon, 0, hX, hY, 16, 16, hStyle)
  DestroyIcon hIcon
End Function

Public Function DrawIcon(ByVal hIml As Long, ByVal hIndex As Long, ByVal hDcDesc As Long, ByVal hX As Long, ByVal hY As Long, ByVal hStyle As Long) As Long
  DrawIcon = ImageList_Draw(hIml, hIndex, hDcDesc, hX, hY, hStyle)
End Function

Public Function CreateImlEx(ByVal hInst As Long, ByVal sBitmap As String, ByVal hDef As Long, ByVal hTotal As Long, ByVal cMask As Long) As Long
  m_hIml = ImageList_LoadImage(hInst, sBitmap, hDef, hTotal, cMask, IMAGE_BITMAP, LR_DEFAULTCOLOR)
  CreateImlEx = m_hIml
End Function

Public Function AddMasked(ByVal hIml As Long, ByVal hImage As Long, ByVal cMask As Long) As Long
  AddMasked = ImageList_AddMasked(hIml, hImage, cMask)
End Function

Public Function AddIcon(ByVal hIml As Long, ByVal hIcon As Long) As Long
  AddIcon = ImageList_ReplaceIcon(hIml, -1, hIcon)
End Function

Public Function Remove(ByVal hIml As Long, ByVal hIndex As Long) As Long
  Remove = ImageList_Remove(hIml, hIndex)
End Function

Public Function CreateIml(ByVal lColor As Long, ByVal InitialImages As Long, Optional ByVal TotalImages As Variant) As Long
  Dim cxSmIcon As Long, cySmIcon As Long
  
  If IccInit = False Then InitCommonControls
  
  If IsNull(TotalImages) = True Then TotalImages = 0
   
  cxSmIcon = GetSystemMetrics(SM_CXSMICON)
  cySmIcon = GetSystemMetrics(SM_CYSMICON)
  m_hIml = ImageList_Create(cxSmIcon, cySmIcon, lColor, InitialImages, TotalImages)
  CreateIml = m_hIml
  
End Function

Public Function DestroyIml() As Boolean
  Dim x As Long
  DestroyIml = ImageList_Destroy(m_hIml)
  m_hIml = 0
End Function

Public Property Get ImageCount()
  ImageCount = ImageList_GetImageCount(m_hIml)
End Property

Public Property Get hImageList()
  hImageList = m_hIml
End Property
