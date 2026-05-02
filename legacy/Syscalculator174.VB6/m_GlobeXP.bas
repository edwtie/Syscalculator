Attribute VB_Name = "m_GlobeXP"
'deklarac API funkci a datovych struktur pouzitych v priklade
Option Explicit

'datovy typ CTVEREC
Type RECT
    Left As Long
    Top As Long
    Right As Long
    Bottom As Long
End Type

'datovy typ SOURADNICE BODU
Type POINTAPI
    x As Long
    y As Long
End Type

'datova struktura obsahujici informace o menuitem, kdyz se vytvari menu
Type MEASUREITEMSTRUCT  'structura obsahujici informace o menuitem
    CtlType As Long
    CtlID As Long
    itemID As Long
    itemWidth As Long
    itemHeight As Long
    ItemData As Long
End Type

'datova struktura obsahujici informace o menuitem, kdyz se kresli menuitems
Type DRAWITEMSTRUCT
    CtlType As Long
    CtlID As Long
    itemID As Long
    itemAction As Long
    itemState As Long
    hwndItem As Long
    hDC As Long
    rcItem As RECT
    ItemData As Long
End Type

'datova struktura obsahujici informace o menuitem
Type MENUITEMINFO
    cbSize As Long
    fMask As Long
    fType As Long
    fState As Long
    wID As Long
    hSubMenu As Long
    hbmpChecked As Long
    hbmpUnchecked As Long
    dwItemData As Long
    dwTypeData As String
    cch As Long
    hbmpItem As Long
End Type

Declare Function GetTextExtentPoint32 Lib "gdi32" Alias "GetTextExtentPoint32A" (ByVal hDC As Long, ByVal lpsz As String, ByVal cbString As Long, lpSize As POINTAPI) As Long
Declare Function GetWindowDC Lib "user32" (ByVal hWnd As Long) As Long

'API pro SUBCLASSING
Declare Function GetWindowLong Lib "user32" Alias "GetWindowLongA" (ByVal hWnd As Long, ByVal nIndex As Long) As Long
Declare Function SetWindowLong Lib "user32" Alias "SetWindowLongA" (ByVal hWnd As Long, ByVal nIndex As Long, ByVal dwNewLong As Long) As Long
Declare Function CallWindowProc Lib "user32" Alias "CallWindowProcA" (ByVal lpPrevWndFunc As Long, ByVal hWnd As Long, ByVal Msg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long

'API pro vytvoreni MENU
Declare Function CreateMenu Lib "user32" () As Long
Declare Function CreatePopupMenu Lib "user32" () As Long
Declare Function GetMenu Lib "user32" (ByVal hWnd As Long) As Long
Declare Function DestroyMenu Lib "user32" (ByVal hMenu As Long) As Long
Declare Function SetMenu Lib "user32" (ByVal hWnd As Long, ByVal hMenu As Long) As Long
Declare Function AppendMenu Lib "user32" Alias "AppendMenuA" (ByVal hMenu As Long, ByVal wFlags As Long, ByVal wIDNewItem As Long, ByVal lpNewItem As Any) As Long
Declare Function SetMenuItemBitmaps Lib "user32" (ByVal hMenu As Long, ByVal nPosition As Long, ByVal wFlags As Long, ByVal hBitmapUnchecked As Long, ByVal hBitmapChecked As Long) As Long
Declare Function DrawMenuBar Lib "user32" (ByVal hWnd As Long) As Long
Declare Function ModifyMenu Lib "user32" Alias "ModifyMenuA" (ByVal hMenu As Long, ByVal nPosition As Long, ByVal wFlags As Long, ByVal wIDNewItem As Long, ByVal lpString As Long) As Long
Declare Function ModifyStringMenu Lib "user32" Alias "ModifyMenuA" (ByVal hMenu As Long, ByVal nPosition As Long, ByVal wFlags As Long, ByVal wIDNewItem As Long, ByVal lpString As String) As Long
Declare Function GetMenuState Lib "user32" (ByVal hMenu As Long, ByVal wID As Long, ByVal wFlags As Long) As Long
Declare Function CheckMenuItem Lib "user32" (ByVal hMenu As Long, ByVal wIDCheckItem As Long, ByVal wCheck As Long) As Long
Declare Function GetMenuString Lib "user32" Alias "GetMenuStringA" (ByVal hMenu As Long, ByVal wIDItem As Long, ByVal lpString As String, ByVal nMaxCount As Long, ByVal wFlag As Long) As Long
Declare Function GetMenuItemInfo Lib "user32" Alias "GetMenuItemInfoA" (ByVal hMenu As Long, ByVal un As Long, ByVal B As Long, lpMenuItemInfo As MENUITEMINFO) As Long
Declare Function GetMenuItemID Lib "user32" (ByVal hMenu As Long, ByVal nPos As Long) As Long
Declare Function TrackPopupMenuEx Lib "user32" (ByVal hMenu As Long, ByVal un As Long, ByVal n1 As Long, ByVal n2 As Long, ByVal hWnd As Long, lpTPMParams As Any) As Long

'API pro vykreslovanir a vypisovani MenuItems
Declare Sub CopyMem Lib "kernel32" Alias "RtlMoveMemory" (pDest As Any, pSource As Any, ByVal ByteLen As Long)
Declare Sub SetSysColors Lib "user32" (ByVal nChanges As Integer, lpSysColor As Integer, lpColorValues As Long)
Declare Function SendMessage Lib "user32" Alias "SendMessageA" (ByVal hWnd As Long, ByVal wMsg As Long, ByVal wParam As Long, lParam As Any) As Long
Declare Function SelectObject Lib "gdi32" (ByVal hDC As Long, ByVal hObject As Long) As Long
Declare Function DeleteObject Lib "gdi32" (ByVal hObject As Long) As Integer
Declare Function CreateSolidBrush Lib "gdi32" (ByVal crColor As Long) As Long
Declare Function GetSysColor Lib "user32" (ByVal nIndex As Integer) As Long
Declare Function CreatePen Lib "gdi32" (ByVal nPenStyle As Long, ByVal nWidth As Long, ByVal crColor As Long) As Long
Declare Function SetBkMode Lib "gdi32" (ByVal hDC As Long, ByVal nBkMode As Long) As Long
Declare Function GetTextColor Lib "gdi32" (ByVal hDC As Long) As Long
Declare Function SetTextColor Lib "gdi32" (ByVal hDC As Long, ByVal crColor As Long) As Long
Declare Function TextOut Lib "gdi32" Alias "TextOutA" (ByVal hDC As Long, ByVal x As Long, ByVal y As Long, ByVal lpString As String, ByVal nCount As Long) As Long
Declare Function BitBlt Lib "gdi32" (ByVal hDestDC As Long, ByVal x As Long, ByVal y As Long, ByVal nWidth As Long, ByVal nHeight As Long, ByVal hSrcDC As Long, ByVal xSrc As Long, ByVal ySrc As Long, ByVal dwRop As Long) As Long
Declare Function MoveToEx Lib "gdi32" (ByVal hDC As Long, ByVal x As Long, ByVal y As Long, lpPoint As Any) As Long 'POINTAPI) As Long
Declare Function LineTo Lib "gdi32" (ByVal hDC As Long, ByVal x As Long, ByVal y As Long) As Long
Declare Function InvertRect Lib "user32" (ByVal hDC As Long, lpRect As RECT) As Long
Declare Function Rectangle Lib "gdi32" (ByVal hDC As Long, ByVal X1 As Long, ByVal Y1 As Long, ByVal X2 As Long, ByVal Y2 As Long) As Long
Declare Function GetDC Lib "user32" (ByVal hWnd As Long) As Long
Declare Function DrawText Lib "user32" Alias "DrawTextA" (ByVal hDC As Long, ByVal lpStr As String, ByVal nCount As Long, lpRect As RECT, ByVal wFormat As Long) As Long
Declare Function CreateFont Lib "gdi32" Alias "CreateFontA" (ByVal H As Long, ByVal W As Long, ByVal E As Long, ByVal O As Long, ByVal W As Long, ByVal i As Long, ByVal u As Long, ByVal s As Long, ByVal c As Long, ByVal OP As Long, ByVal CP As Long, ByVal Q As Long, ByVal PAF As Long, ByVal F As String) As Long
Declare Function CreateCompatibleDC Lib "gdi32" (ByVal hDC As Long) As Long
Declare Function DeleteDC Lib "gdi32" (ByVal hDC As Long) As Long
Declare Function FillRect Lib "user32" (ByVal hDC As Long, lpRect As RECT, ByVal hBrush As Long) As Long

'hodnoty pro typy fontu
Public Const TRANSPARENT = 1
Public Const OPAQUE = 2
Public Const NEWTRANSPARENT = 3

'DefWndProc
Public Const GWL_WNDPROC = (-4)

' MenuItem flags => informace o jaky se vlastne jedna MenuItem
Public Const MF_CHECKED = &H8&
Public Const MF_DISABLED = &H2&
Public Const MF_ENABLED = &H0&
Public Const MF_GRAYED = &H1&
Public Const MF_POPUP = &H10&
Public Const MF_SEPARATOR = &H800&
Public Const MF_STRING = &H0&
Public Const MF_UNCHECKED = &H0&
Public Const MF_BITMAP = &H4&
Public Const MF_BYPOSITION = &H400&
Public Const MF_BYCOMMAND = &H0&
Public Const MF_HELP = &H4000&
Public Const MF_HILITE = &H80&
Public Const MF_MENUBARBREAK = &H20&
Public Const MF_MENUBREAK = &H40&
Public Const MF_OWNERDRAW = &H100&
Public Const MF_USECHECKBITMAPS = &H200&
Public Const MFS_DEFAULT = &H1000&
Public Const MIIM_ID = &H2
Public Const MIIM_SUBMENU = &H4
Public Const MIIM_TYPE = &H10
Public Const MIIM_DATA = &H20

' Windows messages => tak takovydle vsechny zpravy se budou odchytavat
Public Const WM_COMMAND = &H111
Public Const WM_MENUSELECT = &H11F
Public Const WM_SETFOCUS = &H7
Public Const WM_ACTIVATE = &H6
Public Const WM_ACTIVATEAPP = &H1C
Public Const WM_MEASUREITEM = &H2C
Public Const WM_DRAWITEM = &H2B
Public Const WM_GETFONT = &H31

' DrawItemStruct konstanty => hodnoty urcujici stav a typ chovani menuitem
Public Const ODS_CHECKED = &H8
Public Const ODS_DISABLED = &H4
Public Const ODS_FOCUS = &H10
Public Const ODS_GRAYED = &H2
Public Const ODS_SELECTED = &H1
Public Const ODS_NOFOCUSRECT = &H200
Public Const ODT_MENU = 1
Public Const ODA_DRAWENTIRE = &H1
Public Const ODA_FOCUS = &H4
Public Const ODA_SELECT = &H2

' Barvy pera
Public Const COLOR_BTNHIGHLIGHT = 20
Public Const COLOR_BTNSHADOW = 16

' DrawText
Public Const DT_LEFT = &H0

' konstanty barev
Public Enum ColConst
    ActiveBorder = 10
    ActiveCaption = 2
    ADJ_MAX = 100
    ADJ_MIN = -100
    APPWORKSPACE = 12
    Background = 1
    BTNFACE = 15
    BTNHIGHLIGHT = 20
    BTNSHADOW = 16
    BTNTEXT = 18
    CAPTIONTEXT = 9
    GRAYTEXT = 17
    HIGHLIGHT = 13
    HIGHLIGHTTEXT = 14
    INACTIVEBORDER = 11
    INACTIVECAPTION = 3
    INACTIVECAPTIONTEXT = 19
    Menu = 4
    MENUTEXT = 7
    SCROLLBAR = 0
    WINDOW = 5
    WINDOWFRAME = 6
    WINDOWTEXT = 8
End Enum

'======================================================================================

Public Type WINDOWPOS
    hWnd As Long
    hWndInsertAfter As Long
    x As Long
    y As Long
    cx As Long
    cy As Long
    flags As Long
End Type

Type NCCALCSIZE_PARAMS
    rgrc(2) As RECT
    lppos As Long ' WINDOWPOS
End Type

Public Type LOGFONT
    lfHeight As Long
    lfWidth As Long
    lfEscapement As Long
    lfOrientation As Long
    lfWeight As Long
    lfItalic As Byte      '0=false; 255=true
    lfUnderline As Byte   '0=f; 255=t
    lfStrikeOut As Byte   '0=f; 255=t
    lfCharSet As Byte
    lfOutPrecision As Byte
    lfClipPrecision As Byte
    lfQuality As Byte
    lfPitchAndFamily As Byte
    lfFaceName As String * 32
End Type

Public Type CWPSTRUCT
    lParam As Long
    wParam As Long
    message As Long
    hWnd As Long
End Type

Public Type TPMPARAMS
    cbSize As Long
    rcExclude As RECT
End Type

' SysMetrics
Private Const SM_CXBORDER = 5
Private Const SM_CXDLGFRAME = 7
Private Const SM_CXFIXEDFRAME = SM_CXDLGFRAME
Private Const SM_CXFRAME = 32
Private Const SM_CXHSCROLL = 21
Private Const SM_CXVSCROLL = 2
Private Const SM_CYCAPTION = 4
Private Const SM_CYDLGFRAME = 8
Private Const SM_CYFIXEDFRAME = SM_CYDLGFRAME
Private Const SM_CYFRAME = 33
Private Const SM_CYHSCROLL = 3
Private Const SM_CYMENU = 15
Private Const SM_CYSMSIZE = 31
Private Const SM_CXSMSIZE = 30

Public Const RDW_INVALIDATE = &H1
Public Const HC_ACTION = 0
Public Const WM_CREATE = &H1
Public Const WM_WINDOWPOSCHANGING = &H46
Public Const WM_NCCALCSIZE = &H83
Public Const WH_CALLWNDPROC = 4
Public Const GWL_STYLE = (-16)
Public Const WS_BORDER = &H800000
Public Const WS_EX_TRANSPARENT = &H20&
Public Const GWL_EXSTYLE = (-20)
Public Const WS_EX_WINDOWEDGE = &H100&
Public Const WS_EX_DLGMODALFRAME = &H1&
Public Const MF_SYSMENU = &H2000&
Public Const TPM_LEFTALIGN = &H0&
Public Const TPM_TOPALIGN = &H0&
Public Const TPM_LEFTBUTTON = &H0&
Public Const WM_EXITMENULOOP = &H212
Public Const WM_ERASEBKGND = &H14
Public Const WM_NCPAINT = &H85
Public Const WM_PAINT = &HF
Public Const WM_PRINT = &H317

Public Const WVR_ALIGNBOTTOM = &H40
Public Const WVR_ALIGNLEFT = &H20
Public Const WVR_ALIGNRIGHT = &H80
Public Const WVR_ALIGNTOP = &H10
Public Const WVR_HREDRAW = &H100
Public Const WVR_VALIDRECTS = &H400
Public Const WVR_VREDRAW = &H200
Public Const WVR_REDRAW = (WVR_HREDRAW Or WVR_VREDRAW)

Public Declare Sub CopyMemory Lib "kernel32" Alias "RtlMoveMemory" (pDst As Any, pSrc As Any, ByVal ByteLen As Long)

Public Declare Function GetDesktopWindow Lib "user32" () As Long
Public Declare Function CreateCompatibleBitmap Lib "gdi32" (ByVal hDC As Long, ByVal nWidth As Long, ByVal nHeight As Long) As Long
Public Declare Function ReleaseDC Lib "user32" (ByVal hWnd As Long, ByVal hDC As Long) As Long

Public Declare Function GetSystemMetrics Lib "user32" (ByVal nIndex As Long) As Long
Public Declare Function GetWindowThreadProcessId Lib "user32" (ByVal hWnd As Long, lpdwProcessId As Long) As Long
Public Declare Function SetWindowsHookEx Lib "user32" Alias "SetWindowsHookExA" (ByVal idHook As Long, ByVal lpfn As Long, ByVal hmod As Long, ByVal dwThreadId As Long) As Long
Public Declare Function ClientToScreen Lib "user32" (ByVal hWnd As Long, lpPoint As POINTAPI) As Long
Public Declare Function UnhookWindowsHookEx Lib "user32" (ByVal hHook As Long) As Long
Public Declare Function SetProp Lib "user32" Alias "SetPropA" (ByVal hWnd As Long, ByVal lpString As String, ByVal hData As Long) As Long
Public Declare Function CallNextHookEx Lib "user32" (ByVal hHook As Long, ByVal ncode As Long, ByVal wParam As Long, lParam As Any) As Long
Public Declare Function GetProp Lib "user32" Alias "GetPropA" (ByVal hWnd As Long, ByVal lpString As String) As Long
Public Declare Function RemoveProp Lib "user32" Alias "RemovePropA" (ByVal hWnd As Long, ByVal lpString As String) As Long
Public Declare Function GetClassName Lib "user32" Alias "GetClassNameA" (ByVal hWnd As Long, ByVal lpClassName As String, ByVal nMaxCount As Long) As Long
Public Declare Function GetWindowRect Lib "user32" (ByVal hWnd As Long, lpRect As RECT) As Long
Public Declare Function GetPixel Lib "gdi32" (ByVal hDC As Long, ByVal x As Long, ByVal y As Long) As Long
Public Declare Function SetPixel Lib "gdi32" (ByVal hDC As Long, ByVal x As Long, ByVal y As Long, ByVal crColor As Long) As Long

Public Const DST_BITMAP = &H4
Public Const DST_ICON = &H3
Public Const DST_COMPLEX = 16
Public Const DSS_DISABLED = &H20
Public Const DSS_MONO = &H80
Public Const DSS_NORMAL = &H0
Public Const RDW_FRAME = &H400

' DrawText constants
Public Const DT_BOTTOM = &H8
Public Const DT_CALCRECT = &H400
Public Const DT_CENTER = &H1
Public Const DT_EXPANDTABS = &H40
Public Const DT_EXTERNALLEADING = &H200
Public Const DT_INTERNAL = &H1000
'Public Const DT_LEFT = &H0
Public Const DT_NOCLIP = &H100
Public Const DT_NOPREFIX = &H800
Public Const DT_RIGHT = &H2
Public Const DT_SINGLELINE = &H20
Public Const DT_TABSTOP = &H80
Public Const DT_TOP = &H0
Public Const DT_VCENTER = &H4
Public Const DT_WORDBREAK = &H10

'required for font API functions
Public Const LF_FACESIZE = 32
Public Const SYMBOL_CHARSET = 2

Public Const DCX_INTERSECTRGN = &H80&
Public Const DCX_WINDOW = &H1&

Public Declare Function DrawState Lib "user32" Alias "DrawStateA" (ByVal hDC As Long, ByVal hBr As Long, ByVal lpDrawStateProc As Long, ByVal lParam As Long, ByVal wParam As Long, ByVal x As Long, ByVal y As Long, ByVal cx As Long, ByVal cy As Long, ByVal fuFlags As Long) As Long
Public Declare Function WindowFromDC Lib "user32" (ByVal hDC As Long) As Long
Public Declare Function RedrawWindow Lib "user32" (ByVal hWnd As Long, lprcUpdate As Any, ByVal hrgnUpdate As Long, ByVal fuRedraw As Long) As Long
Public Declare Function DefWindowProc Lib "user32" Alias "DefWindowProcA" (ByVal hWnd As Long, ByVal wMsg As Long, ByVal wParam As Long, ByVal lParam As Long) As Long
Public Declare Function ExcludeClipRect Lib "gdi32" (ByVal hDC As Long, ByVal X1 As Long, ByVal Y1 As Long, ByVal X2 As Long, ByVal Y2 As Long) As Long
Public Declare Function CreateFontIndirect Lib "gdi32" Alias "CreateFontIndirectA" (lpLogFont As LOGFONT) As Long
Public Declare Function GetDCEx Lib "user32" (ByVal hWnd As Long, ByVal hrgnclip As Long, ByVal fdwOptions As Long) As Long
Public Declare Function SelectClipRgn Lib "gdi32" (ByVal hDC As Long, ByVal hRgn As Long) As Long
Public Declare Function CreateRectRgn Lib "gdi32" (ByVal X1 As Long, ByVal Y1 As Long, ByVal X2 As Long, ByVal Y2 As Long) As Long
