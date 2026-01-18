; SharperMD Installer Script for Inno Setup
; Download Inno Setup from: https://jrsoftware.org/isinfo.php

#define MyAppName "SharperMD"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sean Fellowes"
#define MyAppURL "https://github.com/SeanFellowes/SharperMD"
#define MyAppExeName "SharperMD.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{B5E8F9A2-3C4D-4E5F-6A7B-8C9D0E1F2A3B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output settings
OutputDir=..\installer\output
OutputBaseFilename=SharperMD-Setup-{#MyAppVersion}
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
; Modern look
WizardStyle=modern
; Require admin for file associations (or use PrivilegesRequiredOverridesAllowed)
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
; Minimum Windows version (Windows 10)
MinVersion=10.0
; Uninstall settings
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
; Setup icon (optional - uncomment if you have an icon)
; SetupIconFile=..\src\SharperMD\Resources\Icons\app.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "associatemd"; Description: "Associate .md files with SharperMD"; GroupDescription: "File Associations:"; Flags: checkedonce
Name: "associatemarkdown"; Description: "Associate .markdown files with SharperMD"; GroupDescription: "File Associations:"; Flags: unchecked

[Files]
; Main application files (from publish output)
Source: "..\src\SharperMD\bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; File association for .md files
Root: HKCR; Subkey: ".md"; ValueType: string; ValueName: ""; ValueData: "SharperMD.MarkdownFile"; Flags: uninsdeletevalue; Tasks: associatemd
Root: HKCR; Subkey: "SharperMD.MarkdownFile"; ValueType: string; ValueName: ""; ValueData: "Markdown File"; Flags: uninsdeletekey; Tasks: associatemd
Root: HKCR; Subkey: "SharperMD.MarkdownFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: associatemd
Root: HKCR; Subkey: "SharperMD.MarkdownFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: associatemd

; File association for .markdown files
Root: HKCR; Subkey: ".markdown"; ValueType: string; ValueName: ""; ValueData: "SharperMD.MarkdownFile"; Flags: uninsdeletevalue; Tasks: associatemarkdown
Root: HKCR; Subkey: "SharperMD.MarkdownFile"; ValueType: string; ValueName: ""; ValueData: "Markdown File"; Flags: uninsdeletekey; Tasks: associatemarkdown
Root: HKCR; Subkey: "SharperMD.MarkdownFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: associatemarkdown
Root: HKCR; Subkey: "SharperMD.MarkdownFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: associatemarkdown

; Add to "Open with" menu for .md files
Root: HKCR; Subkey: ".md\OpenWithProgids"; ValueType: string; ValueName: "SharperMD.MarkdownFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatemd
Root: HKCR; Subkey: ".markdown\OpenWithProgids"; ValueType: string; ValueName: "SharperMD.MarkdownFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatemarkdown

; Register application capabilities for Windows "Default Apps" settings
Root: HKLM; Subkey: "SOFTWARE\SharperMD"; ValueType: string; ValueName: ""; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\SharperMD\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "A beautiful markdown viewer and editor"
Root: HKLM; Subkey: "SOFTWARE\SharperMD\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#MyAppName}"
Root: HKLM; Subkey: "SOFTWARE\SharperMD\Capabilities\FileAssociations"; ValueType: string; ValueName: ".md"; ValueData: "SharperMD.MarkdownFile"
Root: HKLM; Subkey: "SOFTWARE\SharperMD\Capabilities\FileAssociations"; ValueType: string; ValueName: ".markdown"; ValueData: "SharperMD.MarkdownFile"
Root: HKLM; Subkey: "SOFTWARE\RegisteredApplications"; ValueType: string; ValueName: "SharperMD"; ValueData: "SOFTWARE\SharperMD\Capabilities"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Notify Windows that file associations have changed
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Refresh shell icons
    RegWriteStringValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Explorer', 'Refresh', 'Yes');
  end;
end;
