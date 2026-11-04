;=============================================================================
; Inno Setup Script — صندوق سرمایه‌گذاری طوبی (Tobi Fund)
; خروجی: TobiFundSetup.exe  (معادل FundManagementSetup.exe در سند طراحی)
;
; نحوه استفاده:
;   1. ابتدا پروژه را Publish کنید:
;      dotnet publish src/TobiFund.WPF/TobiFund.WPF.csproj -c Release -r win-x64 `
;        --self-contained true -p:PublishSingleFile=true -o publish
;   2. این اسکریپت را با Inno Setup Compiler (6.x) Compile کنید.
;=============================================================================

#define MyAppName "صندوق طوبی"
#define MyAppNameEn "Tobi Fund"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TobiFund"
#define MyAppExeName "TobiFund.exe"

[Setup]
AppId={{A7C2E1D4-5B6F-4A8C-9D0E-1F2A3B4C5D6E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\TobiFund
DefaultGroupName={#MyAppName}
OutputDir=..\installer-output
OutputBaseFilename=TobiFundSetup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableWelcomePage=no

[Languages]
Name: "farsi"; MessagesFile: "compiler:Languages\Farsi.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\حذف {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "ساخت میان‌بر روی Desktop"; GroupDescription: "میان‌برها:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "اجرای صندوق طوبی پس از نصب"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
