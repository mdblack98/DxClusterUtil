; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "DXCluster"
#define MyAppVersion "250111"
#define MyAppPublisher "W9MDB"
#define MyAppURL "https://github.com/mdblack98/DxClusterUtil"
#define MyAppExeName "DXClusterUtil.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".myp"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{6D15C7B8-6252-4C77-ADC2-A4A706998AE1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
; "ArchitecturesAllowed=x64compatible" specifies that Setup cannot run
; on anything but x64 and Windows 11 on Arm.
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" requests that the
; install be done in "64-bit mode" on x64 or Windows 11 on Arm,
; meaning it should use the native 64-bit Program Files directory and
; the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=C:\Users\mdbla\Dropbox\Projects\DXClusterUtil
OutputBaseFilename=DXCluster{#MyAppVersion}
SetupIconFile=C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\filter3.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\DXClusterUtil.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\DXClusterUtil.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\DXClusterUtil.dll.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\DXClusterUtil.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\DXClusterUtil.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\DXClusterUtil.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\Microsoft.Bcl.AsyncInterfaces.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\Microsoft.Windows.SDK.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ComponentModel.Composition.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ComponentModel.Composition.Registration.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Data.Odbc.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Data.OleDb.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Data.SqlClient.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.DirectoryServices.AccountManagement.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.DirectoryServices.Protocols.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.IO.Ports.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Private.ServiceModel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Reflection.Context.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Runtime.Caching.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ServiceModel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ServiceModel.Duplex.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ServiceModel.Http.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ServiceModel.NetTcp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ServiceModel.Primitives.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ServiceModel.Security.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ServiceModel.Syndication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.ServiceProcess.ServiceController.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Speech.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\System.Web.Services.Description.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\WinRT.Runtime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\Xamarin.Essentials.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\cs\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\Users\mdbla\Dropbox\Projects\DXClusterUtil\bin\Release\net9.0-windows10.0.17763.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".myp"; ValueData: ""

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

