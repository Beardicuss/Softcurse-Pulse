[Setup]
AppId={{5E5D8F0B-BAF1-4F43-A33A-25FF25D899B8}
AppName=Softcurse Pulse
AppVersion=3.0.0
AppPublisher=Softcurse Systems
AppPublisherURL=https://softcurse-website.pages.dev/
AppSupportURL=https://github.com/Dant3B/Pulse/issues
AppUpdatesURL=https://github.com/Dant3B/Pulse/releases
DefaultDirName={autopf}\SoftcursePulse
DefaultGroupName=Softcurse Systems
DisableProgramGroupPage=yes
OutputDir=.\Installer
OutputBaseFilename=SoftcursePulse_Setup_v3
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=Publish\pulse.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "Publish\Pulse.App.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Publish\pulse.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Softcurse Pulse"; Filename: "{app}\Pulse.App.exe"; IconFilename: "{app}\pulse.ico"
Name: "{autodesktop}\Softcurse Pulse"; Filename: "{app}\Pulse.App.exe"; Tasks: desktopicon; IconFilename: "{app}\pulse.ico"

[Run]
Filename: "{app}\Pulse.App.exe"; Description: "{cm:LaunchProgram,Softcurse Pulse}"; Flags: nowait postinstall skipifsilent
