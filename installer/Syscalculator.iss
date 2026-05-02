; Syscalculator 2.0 installer.
; Build this file with BUILD_INSTALLER.bat after the WinForms app is published.

#define MyAppName "Syscalculator"
#define MyAppPublisher "Tiedragon"
#define MyAppURL "https://www.tiedragon.com"
#define MyAppExeName "Syscalculator.exe"

#define MyAppVersion GetEnv("SYSCALC_INSTALL_VERSION")
#if MyAppVersion == ""
  #define MyAppVersion "2.0.0"
#endif

#define MyBuildChannel GetEnv("SYSCALC_INSTALL_CHANNEL")
#if MyBuildChannel == ""
  #define MyBuildChannel "daily"
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
SetupIconFile=..\src\Syscalculator.UI.WinForms\Resources\Syscalculator.ico
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
Name: "{group}\Syscalculator"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\NOD Editor"; Filename: "{app}\{#MyAppExeName}"; Parameters: "-tool"
Name: "{group}\NOD Template Wizard"; Filename: "{app}\{#MyAppExeName}"; Parameters: "-wizardtool"
Name: "{group}\{cm:UninstallProgram,Syscalculator}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Syscalculator"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Classes\.nod"; ValueType: string; ValueName: ""; ValueData: "Syscalculator.Nod"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\Syscalculator.Nod"; ValueType: string; ValueName: ""; ValueData: "NOD document"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Syscalculator.Nod\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Classes\Syscalculator.Nod\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\Syscalculator.Nod\shell\edit\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" -tool ""%1"""

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Syscalculator}"; Flags: nowait postinstall skipifsilent
