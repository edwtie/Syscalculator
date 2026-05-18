; Syscalculator 2.0 installer.
; Build this file with BUILD_INSTALLER.bat after the WinForms app is published.

#define MyAppName "Syscalculator"
#define MyAppPublisher "Tiedragon"
#define MyAppURL "https://www.tiedragon.com"
#define DotNetDesktopRuntimeURL "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"
#define MyAppExeName "Syscalculator.exe"

#define MyAppVersion GetEnv("SYSCALC_INSTALL_VERSION")
#if MyAppVersion == ""
  #define MyAppVersion "2.0.0"
#endif

#define MyBuildChannel GetEnv("SYSCALC_INSTALL_CHANNEL")
#if MyBuildChannel == ""
  #define MyBuildChannel "daily"
#endif

#define MySelfContained GetEnv("SYSCALC_SELF_CONTAINED")
#if MySelfContained == ""
  #define MySelfContained "false"
#endif

[Setup]
AppId={{2EC35B6A-1C36-4B5F-AB75-4D4A2F3F8F90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion} {#MyBuildChannel}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Syscalculator
DefaultGroupName=Syscalculator
OutputDir=..\artifacts\installer
OutputBaseFilename=Syscalculator-2.0-{#MyBuildChannel}-{#MyAppVersion}
SetupIconFile=..\src\syscalculator\Resources\Syscalculator.ico
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "nl"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "pt"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "id"; MessagesFile: "compiler:Default.isl,Languages\Indonesian.isl"
Name: "zh"; MessagesFile: "compiler:Default.isl,Languages\ChineseSimplified.isl"

[CustomMessages]
en.DotNetDesktopRuntimeMissing=Syscalculator requires Microsoft .NET 10 Desktop Runtime x64. Missing runtime: Microsoft.WindowsDesktop.App 10.x.%n%nOpen the official Microsoft download page now?
nl.DotNetDesktopRuntimeMissing=Syscalculator heeft Microsoft .NET 10 Desktop Runtime x64 nodig. Ontbrekende runtime: Microsoft.WindowsDesktop.App 10.x.%n%nOfficiele Microsoft-downloadpagina nu openen?
de.DotNetDesktopRuntimeMissing=Syscalculator benoetigt Microsoft .NET 10 Desktop Runtime x64. Fehlende Runtime: Microsoft.WindowsDesktop.App 10.x.%n%nOffizielle Microsoft-Downloadseite jetzt oeffnen?
fr.DotNetDesktopRuntimeMissing=Syscalculator necessite Microsoft .NET 10 Desktop Runtime x64. Runtime manquante: Microsoft.WindowsDesktop.App 10.x.%n%nOuvrir maintenant la page de telechargement officielle de Microsoft?
it.DotNetDesktopRuntimeMissing=Syscalculator richiede Microsoft .NET 10 Desktop Runtime x64. Runtime mancante: Microsoft.WindowsDesktop.App 10.x.%n%nAprire ora la pagina ufficiale di download Microsoft?
pt.DotNetDesktopRuntimeMissing=Syscalculator precisa do Microsoft .NET 10 Desktop Runtime x64. Runtime ausente: Microsoft.WindowsDesktop.App 10.x.%n%nAbrir agora a pagina oficial de download da Microsoft?
es.DotNetDesktopRuntimeMissing=Syscalculator necesita Microsoft .NET 10 Desktop Runtime x64. Runtime faltante: Microsoft.WindowsDesktop.App 10.x.%n%nAbrir ahora la pagina oficial de descarga de Microsoft?
id.DotNetDesktopRuntimeMissing=Syscalculator memerlukan Microsoft .NET 10 Desktop Runtime x64. Runtime yang hilang: Microsoft.WindowsDesktop.App 10.x.%n%nBuka halaman unduhan resmi Microsoft sekarang?
zh.DotNetDesktopRuntimeMissing=Syscalculator requires Microsoft .NET 10 Desktop Runtime x64. Missing runtime: Microsoft.WindowsDesktop.App 10.x.%n%nOpen the official Microsoft download page now?

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\Syscalculator\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: files; Name: "{app}\Converters\celsius_fahrenheit.nod"
Type: files; Name: "{app}\Converters\e2.nod"
Type: files; Name: "{app}\Converters\machtsregel_animatie_solver_demo.nod"
Type: files; Name: "{app}\Converters\nederlands_engels_demo.nod"
Type: files; Name: "{app}\Converters\operatie_decibel_1995_demo.nod"
Type: files; Name: "{app}\Converters\postcode_adres_demo.nod"
Type: files; Name: "{app}\Converters\sinus_graden.nod"
Type: files; Name: "{app}\Converters\snijpunt_lijnen_solver_demo.nod"
Type: filesandordirs; Name: "{app}\Converters\Output"

[Icons]
Name: "{group}\Syscalculator"; Filename: "{app}\{#MyAppExeName}"; AppUserModelID: "Tiedragon.Syscalculator"
Name: "{group}\NOD Editor"; Filename: "{app}\{#MyAppExeName}"; Parameters: "-tool"; AppUserModelID: "Tiedragon.Syscalculator.NodEditor"
Name: "{group}\NOD Template Wizard"; Filename: "{app}\{#MyAppExeName}"; Parameters: "-wizardtool"; AppUserModelID: "Tiedragon.Syscalculator.NodTemplateWizard"
Name: "{group}\{cm:UninstallProgram,Syscalculator}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Syscalculator"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; AppUserModelID: "Tiedragon.Syscalculator"

[Registry]
Root: HKCU; Subkey: "Software\Classes\.nod"; ValueType: string; ValueName: ""; ValueData: "Syscalculator.Nod"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\Syscalculator.Nod"; ValueType: string; ValueName: ""; ValueData: "NOD document"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Syscalculator.Nod\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Classes\Syscalculator.Nod\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\Syscalculator.Nod\shell\edit\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" -tool ""%1"""

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Syscalculator}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet10DesktopRuntimeVersion(Version: String): Boolean;
begin
  Result := (Version = '10') or (Pos('10.', Version) = 1);
end;

function HasDotNet10DesktopRuntimeRegistryEntry(RootKey: Integer; Subkey: String): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;

  if RegGetValueNames(RootKey, Subkey, Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if IsDotNet10DesktopRuntimeVersion(Names[I]) then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;

  if RegGetSubkeyNames(RootKey, Subkey, Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if IsDotNet10DesktopRuntimeVersion(Names[I]) then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

function HasDotNet10DesktopRuntimeFolder(BaseDir: String): Boolean;
var
  FindRec: TFindRec;
  RuntimeDir: String;
begin
  Result := False;

  if not DirExists(BaseDir) then
  begin
    Exit;
  end;

  if FindFirst(AddBackslash(BaseDir) + '10.*', FindRec) then
  begin
    try
      repeat
        RuntimeDir := AddBackslash(BaseDir) + FindRec.Name;
        if DirExists(RuntimeDir) and IsDotNet10DesktopRuntimeVersion(FindRec.Name) then
        begin
          Result := True;
          Exit;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function IsDotNet10DesktopRuntimeInstalled: Boolean;
var
  SharedFxKey: String;
begin
  SharedFxKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';

  Result :=
    HasDotNet10DesktopRuntimeRegistryEntry(HKLM64, SharedFxKey) or
    HasDotNet10DesktopRuntimeRegistryEntry(HKLM32, SharedFxKey) or
    HasDotNet10DesktopRuntimeFolder(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App')) or
    HasDotNet10DesktopRuntimeFolder(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App'));
end;

function AppLanguageFile: String;
begin
  case ActiveLanguage of
    'nl': Result := 'ned.lng';
    'de': Result := 'deu.lng';
    'fr': Result := 'fra.lng';
    'it': Result := 'ita.lng';
    'pt': Result := 'por.lng';
    'es': Result := 'spa.lng';
    'id': Result := 'ind.lng';
    'zh': Result := 'zho.lng';
  else
    Result := 'eng.lng';
  end;
end;

function InitializeSetup: Boolean;
#if MySelfContained != "true"
var
  ErrorCode: Integer;
#endif
begin
#if MySelfContained == "true"
  Result := True;
#else
  Result := IsDotNet10DesktopRuntimeInstalled;
  if not Result then
  begin
    if MsgBox(ExpandConstant('{cm:DotNetDesktopRuntimeMissing}'), mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', '{#DotNetDesktopRuntimeURL}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
  end;
#endif
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    SaveStringToFile(
      ExpandConstant('{app}\language.cfg'),
      '# Active language file.' + #13#10 +
      'language=' + AppLanguageFile + #13#10,
      False);
  end;
end;
