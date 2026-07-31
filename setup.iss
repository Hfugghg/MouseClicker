; Inno Setup 脚本，用于创�?MouseClicker 安装�?

#define MyAppName "MouseClicker"
; 从命令行接收版本号，例如 "v1.0.0"
#define MyAppVersion "{#AppVersion}"
#define MyAppPublisher "Hfugghg"
#define MyAppURL "https://github.com/Hfugghg/MouseClicker"
#define MyAppExeName "MouseClicker.exe"
; 定义输出安装包的文件名格�?
#define MyAppSetupName "MouseClicker-Setup-" + MyAppVersion

[Setup]
; 安装包元数据
AppName={#MyAppName}
; [重要修改] 直接在此处使用占位符，绕过预处理�?
AppVersion=__APP_VERSION__
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; 输出目录
OutputDir=output
; 文件名直接拼接占位符
OutputBaseFilename=MouseClicker-Setup-__APP_VERSION__
; 使用现代化的向导界面
WizardStyle=modern
; 配置最高级别的压缩
Compression=lzma2/ultra64
SolidCompression=yes
; 安装包所需的权�?
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "src\MouseClicker\Languages\ChineseSimplified.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "chinesetraditional"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "release-for-inno\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

