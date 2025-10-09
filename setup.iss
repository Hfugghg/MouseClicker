; Inno Setup 脚本，用于创建 MouseClicker 安装包

#define MyAppName "MouseClicker"
; 从命令行接收版本号，例如 "v1.0.0"
#define MyAppVersion "{#AppVersion}"
#define MyAppPublisher "Hfugghg"
#define MyAppURL "https://github.com/Hfugghg/MouseClicker"
#define MyAppExeName "MouseClicker.exe"
; 定义输出安装包的文件名格式
#define MyAppSetupName "MouseClicker-Setup-" + MyAppVersion

[Setup]
; 安装包元数据
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; 输出目录
OutputDir=output
; 直接使用预处理器常量来构建文件名
OutputBaseFilename=MouseClicker-Setup-{#MyAppVersion}
; 使用现代化的向导界面
WizardStyle=modern
; 配置最高级别的压缩
Compression=lzma2/ultra64
SolidCompression=yes
; 安装包所需的权限
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
; 让用户选择是否创建桌面快捷方式，默认勾选
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 将 dotnet publish 输出的所有文件打包到安装目录 {app}
Source: "release-for-inno\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; 创建开始菜单快捷方式
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
; 创建桌面快捷方式（如果用户在任务中选择了）
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; 安装完成后，让用户选择是否立即运行应用
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent