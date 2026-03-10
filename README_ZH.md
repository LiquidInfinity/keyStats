[![Awesome](https://awesome.re/mentioned-badge.svg )](https://github.com/jaywcjlove/awesome-mac#menu-bar-tools)

[English](./README_EN.md) | 简体中文


<img width="128" height="128" alt="ICON-iOS-Default-256x256@2x" src="https://github.com/user-attachments/assets/842780ed-c7a1-4c1b-a901-1f1d8babe51a" />


# KeyStats - macOS/Windows 键鼠统计菜单栏应用


KeyStats可以统计用户每日的键盘敲击次数、鼠标点击次数、鼠标移动距离和滚动距离。


  
<img width="300" height="581" alt="image" src="https://github.com/user-attachments/assets/c5146f66-0aa3-49ce-9184-2c75301bf398" />

<img width="320" height="581" alt="image" src="https://github.com/user-attachments/assets/b363093e-9aad-4d8a-8b12-1918ef843b3f" />

<img width="360" height="681" alt="image" src="https://github.com/user-attachments/assets/24a796a1-6d21-4186-9568-b223ac309078" />


## 安装与使用

### macOS

#### 方式一：使用 Homebrew 安装

```bash
# 订阅 tap
brew tap debugtheworldbot/keystats

# 安装应用
brew install keystats
```

更新应用：
```bash
brew upgrade keystats
```

#### 方式二：[从 GitHub Release 下载](https://github.com/debugtheworldbot/keyStats/releases)

### Windows

[从 GitHub Release 下载](https://github.com/debugtheworldbot/keyStats/releases) Windows 版本安装包

> **无需安装任何依赖**：Windows 版本使用 .NET Framework 4.8，Windows 10 (1903+) 和 Windows 11 已预装，开箱即用。如果你的 Windows 10 版本较旧（早于 1903），可以升级系统或[手动安装 .NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)。

## 功能特性

- **键盘敲击统计**：实时统计每日键盘按键次数
- **鼠标点击统计**：分别统计左键和右键点击次数
- **鼠标移动距离**：追踪鼠标移动的总距离
- **滚动距离统计**：记录页面滚动的累计距离
- **菜单栏显示**：核心数据直接显示在菜单栏
- **详细面板**：点击菜单栏图标查看完整统计信息
- **每日自动重置**：午夜自动重置统计数据
- **数据持久化**：应用重启后数据不丢失

## 系统要求

### macOS
- macOS 13.0 (Ventura) 或更高版本

### Windows
- **Windows 10 (1903+) 或 Windows 11**
- **无需安装任何依赖**：使用 .NET Framework 4.8（Windows 10/11 已预装，开箱即用）
- **应用大小**：约 5-10 MB（轻量级，无需额外运行时）

> **注意**：如果你的 Windows 10 版本较旧（早于 1903），可以：
> 1. 升级到 Windows 10 1903 或更高版本（推荐）
> 2. 或手动安装 .NET Framework 4.8：[下载链接](https://dotnet.microsoft.com/download/dotnet-framework/net48)


## 首次运行权限设置

### macOS

KeyStats 需要**辅助功能权限**才能监听键盘和鼠标事件。首次运行时：

1. 应用会弹出权限请求对话框
2. 点击"打开系统设置"
3. 在"隐私与安全性" > "辅助功能"中找到 KeyStats
4. 开启 KeyStats 的权限开关
5. 授权后应用将自动开始统计

> **注意**：如果没有授予权限，应用将无法统计任何数据。
>
> **重新安装/升级app提示**：由于应用未进行签名，每次重新安装后 macOS 不会自动更新辅助功能授权。请先在"隐私与安全性">"辅助功能"中移除 KeyStats 的旧授权，再回到应用点击"获取权限"按钮重新授权即可。
>
> **自动更新提示（未签名版本，macOS Ventura+）**：首次 Sparkle 自动更新可能失败。请先在“隐私与安全性”>"App 管理"中开启 KeyStats；如果已失败，可点击系统授权通知进入设置打开开关，再点“立即更新”重试。

### Windows

Windows 版本**无需额外权限设置**，应用启动后会自动开始统计。

> **注意**：首次启动时，Windows 可能会弹出安全警告，点击"仍要运行"即可。

## 使用说明

### 菜单栏显示

应用运行后，菜单栏会显示：

```
123
45
```

- 上面的数字表示今日键盘按下的总次数
- 下面的数字表示今日鼠标点击的总次数（包含左右键）

当数字较大时会自动格式化：
- 1,000+ 显示为 `1.0K`
- 1,000,000+ 显示为 `1.0M`

### 详细面板

点击菜单栏图标会弹出详细统计面板，显示：

| 统计项 | 说明 |
|--------|------|
| 键盘敲击 | 今日按键总次数 |
| 左键点击 | 鼠标左键点击次数 |
| 右键点击 | 鼠标右键点击次数 |
| 鼠标移动 | 鼠标移动的总距离 |
| 滚动距离 | 页面滚动的累计距离 |

### 功能按钮

- **重置统计**：手动清零今日所有统计数据
- **退出应用**：关闭 KeyStats

### 数据导入与导出（多设备同步）

你可以通过 JSON 导出/导入在多台设备间同步统计数据。

1. 在设备 A 打开设置，点击 `数据导出`，保存导出文件（`.json`）。
2. 在设备 B 打开设置，点击 `数据导入` 并选择该文件。
3. 导入时选择模式：
   - `覆盖现有数据`：用导入文件替换本机统计数据。
   - `合并并累加`：把导入文件与本机数据按日期合并并累加。
4. 若想把两台设备的数据汇总到 B：A 导出后，B 导入时选择 `合并并累加`。
5. 之后在 B 再次导出，回到 A 导入时选择 `覆盖现有数据`，即可让 A 与 B 保持一致。

建议：
- 在执行“覆盖”前先导出一次本机数据作为备份。
- 如果同一份文件重复“合并”多次，统计值会重复累加。

## 项目结构

### macOS

```
KeyStats/
├── KeyStats.xcodeproj/     # Xcode 项目文件
├── KeyStats/
│   ├── AppDelegate.swift           # 应用入口，权限管理
│   ├── InputMonitor.swift          # 输入事件监听器
│   ├── StatsManager.swift          # 统计数据管理
│   ├── MenuBarController.swift     # 菜单栏控制器
│   ├── StatsPopoverViewController.swift  # 详细面板视图
│   ├── Info.plist                  # 应用配置
│   ├── KeyStats.entitlements       # 权限配置
│   ├── Main.storyboard             # 主界面
│   └── Assets.xcassets/            # 资源文件
└── README.md
```

### Windows

```
KeyStats.Windows/
├── KeyStats.sln                    # Visual Studio 解决方案文件
├── KeyStats/
│   ├── App.xaml                    # 应用入口定义
│   ├── App.xaml.cs                 # 应用入口逻辑
│   ├── Services/
│   │   ├── InputMonitorService.cs  # 输入事件监听服务
│   │   ├── StatsManager.cs         # 统计数据管理
│   │   ├── NotificationService.cs  # 通知服务
│   │   └── StartupManager.cs       # 开机启动管理
│   ├── ViewModels/
│   │   ├── TrayIconViewModel.cs    # 托盘图标视图模型
│   │   └── StatsPopupViewModel.cs  # 统计面板视图模型
│   ├── Views/
│   │   └── StatsPopupWindow.xaml   # 统计面板界面
│   ├── Models/                     # 数据模型
│   ├── Helpers/                    # 工具类
│   └── Resources/                  # 资源文件
└── build.ps1                       # 构建脚本
```

## 技术实现

### macOS

- **语言**：Swift 5.0
- **框架**：AppKit, CoreGraphics
- **事件监听**：使用 `CGEvent.tapCreate` 创建全局事件监听器
- **数据存储**：使用 `UserDefaults` 进行本地持久化
- **UI 模式**：纯菜单栏应用（LSUIElement = true）

### Windows

- **语言**：C# 10
- **框架**：WPF (.NET Framework 4.8)
- **架构模式**：MVVM (Model-View-ViewModel)
- **事件监听**：使用 Windows 低级键盘/鼠标钩子 (SetWindowsHookEx)
- **数据存储**：使用 JSON 文件进行本地持久化
- **UI 模式**：系统托盘应用
- **优势**：无需安装运行时，Windows 10/11 开箱即用，应用体积小（5-10 MB）


## 测试（AppStats）

为了保证 `KeyStats/AppStats.swift` 的核心逻辑可回归验证，仓库新增了基于 `XCTest` 的测试代码（见 `KeyStatsTests/AppStatsTests.swift`），并通过 `swift test` 运行。

当前覆盖的测试用例如下：

1. 初始化默认值：验证 `init(bundleId:displayName:)` 后计数字段均为 0。
2. 按键与点击累加：验证 `recordKeyPress/recordLeftClick/recordRightClick/recordSideBackClick/recordSideForwardClick` 的累加行为。
3. 滚动距离绝对值：验证 `addScrollDistance(_:)` 对正负输入都按绝对值累加。
4. 空名称保护：验证 `updateDisplayName("")` 不会覆盖旧名称。
5. 名称更新：验证 `updateDisplayName("Updated")` 能正确更新。
6. Codable 往返：编码再解码后字段保持一致。
7. 旧字段兼容：仅存在 `otherClicks` 时回填 `sideBackClicks`。
8. 新字段优先：当 `sideBackClicks/sideForwardClicks` 存在时忽略 `otherClicks`。
9. 缺省字段容错：空 JSON 解码时回落到默认值。

运行方式：

```bash
swift test
```

## 隐私说明

KeyStats 仅统计按键和点击的**次数**，**不会记录**：
- 具体按下了哪些键
- 输入的文字内容
- 点击的具体位置或应用

所有数据仅存储在本地，不会上传到任何服务器。

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=debugtheworldbot/keyStats&type=date&legend=top-left)](https://www.star-history.com/#debugtheworldbot/keyStats&type=date&legend=top-left)

## 许可证

MIT License
