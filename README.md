<h1 align="center">
  <img src="docs/logo.png" alt="MouseClicker" width="128" />
  <br>
  <a href="https://github.com/Hfugghg/MouseClicker">MouseClicker</a>
  <br>
</h1>

MouseClicker 是一款功能强大的 Windows 桌面应用程序，旨在自动执行鼠标点击任务。无论您是需要执行重复性点击任务，还是希望在指定屏幕区域内实现自动化操作，MouseClicker 都能为您提供灵活、可靠的解决方案。

## ✨ 功能特性

*   **多区域点击**：自由添加、管理多个屏幕点击区域。
*   **可视化区域选择**：通过拖拽矩形框，直观地在屏幕上选择您希望点击的范围。
*   **自定义标签**：为每个点击区域设置清晰的名称，方便管理。
*   **灵活的时间控制**：
    *   **点击间隔**：自定义每次点击之间的基础间隔时间（毫秒）。
    *   **随机抖动**：为点击间隔增加一个随机值，模拟人工操作，避免被检测。
*   **多样的点击序列**：
    *   **顺序循环**：按列表顺序依次点击，完成后从头开始。
    *   **随机点击**：在所有区域中随机选择一个进行点击。
    *   **点击一次**：按顺序点击完所有区域后自动停止。
*   **硬件模拟模式**：使用 `SendInput` API + 非零 `dwExtraInfo` 发送点击事件，模拟真实硬件输入，可绕过部分反模拟检测软件。
*   **可视化辅助**：
    *   **区域覆盖层**：在屏幕上实时显示所有已定义的点击区域边框，并可随时开启或关闭。
*   **一键启停与热键**：
    *   **主控按钮**：通过界面按钮轻松开始或停止点击任务。
    *   **ESC 热键**：在点击过程中，随时按下 `ESC` 键可立即停止任务。
*   **配置管理**：
    *   **保存配置**：将当前的点击区域、时间设置和序列模式保存为 JSON 文件。
    *   **加载配置**：随时加载之前保存的配置，快速恢复工作场景。
*   **多语言支持**：界面支持简体中文、English、日本語。
*   **多语言安装程序**：安装向导支持 10 种语言（中/英/日/韩/繁中/法/德/西/俄/葡）。
*   **内置帮助文档**：提供详细的使用说明，帮助您快速上手。

## 🚀 如何使用

1.  **添加点击区域**：
    *   点击 "**添加**" 按钮。
    *   屏幕将被一层半透明的遮罩覆盖，此时您可以按住鼠标左键并拖拽，以选择一个矩形区域。
    *   选定后，在弹出的对话框中为该区域命名。
2.  **管理区域列表**：
    *   在主界面的列表中，您可以查看所有已添加的区域。
    *   使用 "**移除**"、"**上移**"、"**下移**" 按钮来管理您的点击列表。
3.  **设置点击参数**：
    *   设置 "**间隔**" 和 "**抖动**" 时间（以毫秒为单位）。
    *   在 "**序列**" 下拉菜单中选择您想要的点击模式（循环、随机或一次）。
    *   勾选 "**硬件模拟**" 可启用更底层的 SendInput 模式。
4.  **开始任务**：
    *   点击 "**开始点击**" 按钮启动自动点击。
    *   程序将根据您的设置，在指定区域内模拟鼠标点击。
5.  **停止任务**：
    *   点击 "**停止点击**" 按钮。
    *   或者，直接按下键盘上的 `ESC` 键。

## 🛠️ 技术栈

*   **语言**：C#
*   **框架**：.NET 10 / Windows Forms
*   **测试**：xUnit（84 个单元测试）

## 📁 项目结构

```
MouseClicker/
├── src/MouseClicker/
│   ├── Forms/          # UI 窗体
│   ├── Models/         # 数据模型
│   ├── Services/       # 业务逻辑
│   ├── Resources/      # 图标等资源
│   └── Languages/      # 语言文件
├── tests/MouseClicker.Tests/
│   └── TestDoubles/    # 测试替身
├── setup.iss           # Inno Setup 安装脚本
└── .github/workflows/  # CI/CD
```

## 🔨 构建

### 依赖

*   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
*   [Inno Setup 7](https://jrsoftware.org/isinfo.php)（可选，仅用于打包安装程序）

### 构建便携版

```powershell
dotnet publish .\src\MouseClicker\MouseClicker.csproj --configuration Release --runtime win-x64 --self-contained false --output .\release-portable
```

### 构建安装版

```powershell
dotnet publish .\src\MouseClicker\MouseClicker.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=false --output .\release-for-inno
& "D:\Program Files\Inno Setup 7\ISCC.exe" setup.iss
```

### 运行测试

```powershell
dotnet test .\MouseClicker.sln --configuration Release
```

## 📄 许可证

[LICENSE](LICENSE)
