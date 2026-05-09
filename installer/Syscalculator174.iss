; Syscalculator 1.74 RC1 installer.
; Build with BUILD_SYSCALCULATOR174_INSTALLER.bat.

#define MyAppName "Syscalculator 1.74"
#define MyAppPublisher "Tiedragon"
#define MyAppURL "https://www.tiedragon.com"
#define Vb6RuntimeURL "https://support.microsoft.com/en-us/topic/description-of-the-cumulative-update-rollup-for-the-visual-basic-6-0-service-pack-6-runtime-extended-files-e02acc79-217b-fc0a-6edc-540403af2081"
#define MyAppExeName "freesyscal.exe"
#define MyAppEditorExeName "syscaleditor.exe"
#define MySourceRoot "..\legacy\Syscalculator174.VB6"

#define MyAppVersion GetEnv("SYSCALC174_INSTALL_VERSION")
#if MyAppVersion == ""
  #define MyAppVersion "1.74.0"
#endif

#define MyBuildChannel GetEnv("SYSCALC174_INSTALL_CHANNEL")
#if MyBuildChannel == ""
  #define MyBuildChannel "rc1"
#endif

[Setup]
AppId={{B71AEF2C-9E1E-4B0E-AC68-F31D1740A7F1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyBuildChannel}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\Syscalculator 1.74
DefaultGroupName=Syscalculator 1.74
OutputDir=..\artifacts\legacy\installer
OutputBaseFilename=Syscalculator-1.74-{#MyBuildChannel}-{#MyAppVersion}
SetupIconFile={#MySourceRoot}\syscal.ico
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ChangesAssociations=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "nl"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "cat"; MessagesFile: "compiler:Languages\Catalan.isl"

[CustomMessages]
en.Vb6RuntimeMissing=Syscalculator 1.74 requires the Visual Basic 6 Runtime. Missing file: msvbvm60.dll.%n%nOpen the official Microsoft download page now?
nl.Vb6RuntimeMissing=Syscalculator 1.74 heeft de Visual Basic 6 Runtime nodig. Ontbrekend bestand: msvbvm60.dll.%n%nOfficiele Microsoft-downloadpagina nu openen?
es.Vb6RuntimeMissing=Syscalculator 1.74 necesita Visual Basic 6 Runtime. Falta el archivo: msvbvm60.dll.%n%nAbrir ahora la pagina oficial de descarga de Microsoft?
cat.Vb6RuntimeMissing=Syscalculator 1.74 necessita Visual Basic 6 Runtime. Falta el fitxer: msvbvm60.dll.%n%nVoleu obrir ara la pagina oficial de descarrega de Microsoft?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MySourceRoot}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceRoot}\{#MyAppEditorExeName}"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#MySourceRoot}\*.lng"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceRoot}\*.lng"; DestDir: "{userappdata}\Syscalculator"; Flags: ignoreversion
Source: "{#MySourceRoot}\freesyscal.cfg"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceRoot}\freesyscal.cfg"; DestDir: "{userappdata}\Syscalculator"; Flags: ignoreversion
Source: "{#MySourceRoot}\freesysc.cfg"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#MySourceRoot}\freesysc.cfg"; DestDir: "{userappdata}\Syscalculator"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#MySourceRoot}\readme.rtf"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#MySourceRoot}\readme.txt"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#MySourceRoot}\help\Syscalculator174.chm"; DestDir: "{app}\help"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#MySourceRoot}\help\*"; DestDir: "{app}\help"; Excludes: "*.LOG,*.hhp,*.hhc,*.hhk,Syscalculator174.chm"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#MySourceRoot}\Distance\*"; DestDir: "{app}\Distance"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#MySourceRoot}\euro\*"; DestDir: "{app}\euro"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#MySourceRoot}\Mass\*"; DestDir: "{app}\Mass"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#MySourceRoot}\Pressure\*"; DestDir: "{app}\Pressure"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#MySourceRoot}\Temperature\*"; DestDir: "{app}\Temperature"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#MySourceRoot}\Text\*"; DestDir: "{app}\Text"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "{#MySourceRoot}\Volume\*"; DestDir: "{app}\Volume"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Icons]
Name: "{group}\Syscalculator 1.74"; Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng eng.lng"; Languages: en
Name: "{group}\Syscalculator 1.74"; Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng ned.lng"; Languages: nl
Name: "{group}\Syscalculator 1.74"; Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng esp.lng"; Languages: es
Name: "{group}\Syscalculator 1.74"; Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng cat.lng"; Languages: cat
Name: "{group}\Syscalculator 1.74 Editor"; Filename: "{app}\{#MyAppEditorExeName}"; Check: FileExists(ExpandConstant('{app}\{#MyAppEditorExeName}'))
Name: "{group}\{cm:UninstallProgram,Syscalculator 1.74}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Syscalculator 1.74"; Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng eng.lng"; Tasks: desktopicon; Languages: en
Name: "{autodesktop}\Syscalculator 1.74"; Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng ned.lng"; Tasks: desktopicon; Languages: nl
Name: "{autodesktop}\Syscalculator 1.74"; Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng esp.lng"; Tasks: desktopicon; Languages: es
Name: "{autodesktop}\Syscalculator 1.74"; Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng cat.lng"; Tasks: desktopicon; Languages: cat

[Registry]
Root: HKCU; Subkey: "Software\Tiedragon\Syscalculator Euro Edition"; ValueType: string; ValueName: "first"; ValueData: "1"; Flags: createvalueifdoesntexist uninsdeletekey
Root: HKCU; Subkey: "Software\Tiedragon\Syscalculator Euro Edition"; ValueType: string; ValueName: "Migration"; ValueData: "1"; Flags: createvalueifdoesntexist
Root: HKCU; Subkey: "Software\Classes\.nod"; ValueType: string; ValueName: ""; ValueData: "Syscalculator174.Nod"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\Syscalculator174.Nod"; ValueType: string; ValueName: ""; ValueData: "Syscalculator 1.74 NOD document"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Syscalculator174.Nod\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Classes\Syscalculator174.Nod\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\Syscalculator174.Nod\shell\edit\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppEditorExeName}"" ""%1"""; Check: FileExists(ExpandConstant('{app}\{#MyAppEditorExeName}'))

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng eng.lng"; Description: "{cm:LaunchProgram,Syscalculator 1.74}"; Flags: nowait postinstall skipifsilent; Languages: en
Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng ned.lng"; Description: "{cm:LaunchProgram,Syscalculator 1.74}"; Flags: nowait postinstall skipifsilent; Languages: nl
Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng esp.lng"; Description: "{cm:LaunchProgram,Syscalculator 1.74}"; Flags: nowait postinstall skipifsilent; Languages: es
Filename: "{app}\{#MyAppExeName}"; Parameters: "/lng cat.lng"; Description: "{cm:LaunchProgram,Syscalculator 1.74}"; Flags: nowait postinstall skipifsilent; Languages: cat

[Code]
function IsVb6RuntimeInstalled(): Boolean;
begin
  Result :=
    FileExists(ExpandConstant('{sys}\msvbvm60.dll')) or
    FileExists(ExpandConstant('{syswow64}\msvbvm60.dll'));
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := IsVb6RuntimeInstalled();
  if not Result then
  begin
    if MsgBox(ExpandConstant('{cm:Vb6RuntimeMissing}'), mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', '{#Vb6RuntimeURL}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
  end;
end;
