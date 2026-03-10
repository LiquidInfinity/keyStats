[![Awesome](https://awesome.re/mentioned-badge.svg )](https://github.com/jaywcjlove/awesome-mac#menu-bar-tools)

English | [简体中文](./README_ZH.md)

<img width="128" height="128" alt="ICON-iOS-Default-256x256@2x" src="https://github.com/user-attachments/assets/842780ed-c7a1-4c1b-a901-1f1d8babe51a" />


# KeyStats - macOS/Windows Keyboard & Mouse Statistics Menu Bar App

KeyStats is a lightweight native menu bar application for macOS and Windows that tracks daily keyboard keystrokes, mouse clicks, mouse movement distance, and scroll distance.

<img width="305" height="632" alt="image" src="https://github.com/user-attachments/assets/85c0b483-ad4a-458c-8bf4-c4f054b951bb" />

<img width="320" height="581" alt="image" src="https://github.com/user-attachments/assets/b363093e-9aad-4d8a-8b12-1918ef843b3f" />


## Installation & Usage

### macOS

#### Option 1: Install via Homebrew

```bash
# Tap the repository
brew tap debugtheworldbot/keystats

# Install the app
brew install keystats
```

Update the app:
```bash
brew upgrade keystats
```

#### Option 2: [Download from GitHub Releases](https://github.com/debugtheworldbot/keyStats/releases)

### Windows

[Download from GitHub Releases](https://github.com/debugtheworldbot/keyStats/releases) the Windows installer

> **No dependencies required**: The Windows version uses .NET Framework 4.8, which is pre-installed on Windows 10 (1903+) and Windows 11 - ready to use out of the box. If your Windows 10 version is older (before 1903), you can upgrade your system or [manually install .NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48).

## Features

- **Keyboard Keystroke Statistics**: Real-time tracking of daily key presses
- **Mouse Click Statistics**: Separate tracking of left and right clicks
- **Mouse Movement Distance**: Track total distance of mouse movement
- **Scroll Distance Statistics**: Record cumulative page scroll distance
- **Menu Bar Display**: Core data displayed directly in macOS menu bar
- **Detailed Panel**: Click menu bar icon to view complete statistics
- **Daily Auto-Reset**: Statistics automatically reset at midnight
- **Data Persistence**: Data persists after application restart

## System Requirements

### macOS
- macOS 13.0 (Ventura) or higher

### Windows
- **Windows 10 (1903+) or Windows 11**
- **No dependencies required**: Uses .NET Framework 4.8 (pre-installed on Windows 10/11, ready to use out of the box)
- **App size**: ~5-10 MB (lightweight, no additional runtime needed)

> **Note**: If your Windows 10 version is older (before 1903), you can:
> 1. Upgrade to Windows 10 1903 or higher (recommended)
> 2. Or manually install .NET Framework 4.8: [Download link](https://dotnet.microsoft.com/download/dotnet-framework/net48)


## First Run Permission Setup

### macOS

KeyStats requires **Accessibility permissions** to monitor keyboard and mouse events. On first run:

1. The app will prompt a permission request dialog
2. Click "Open System Settings"
3. Find KeyStats in "Privacy & Security" > "Accessibility"
4. Enable the permission toggle for KeyStats
5. Once authorized, the app will automatically start tracking

> **Note**: Without granting permissions, the app will not be able to track any data.
>
> **Reinstall/upgrade tip**: Because the app is not signed, macOS will not automatically update Accessibility authorization after each reinstall. Remove the existing KeyStats entry in "Privacy & Security" > "Accessibility", then return to the app and click the "Get Permission" button to request access again.
>
> **Auto-update tip (unsigned builds, macOS Ventura+)**: The first Sparkle update may fail. Enable KeyStats in "Privacy & Security" > "App Management" first. If it already failed, click the system authorization notification, turn on the toggle in Settings, and click "Update Now" again.

### Windows

The Windows version **requires no additional permission setup**. The app will automatically start tracking once launched.

> **Note**: On first launch, Windows may show a security warning. Click "Run anyway" to proceed.

## Usage Instructions

### Menu Bar Display

Once the app is running, the menu bar will show:

```
123
45
```

- The upper number represents total keyboard presses today
- The lower number represents total mouse clicks today (including left and right buttons)

Large numbers are automatically formatted:
- 1,000+ displayed as `1.0K`
- 1,000,000+ displayed as `1.0M`

### Detailed Panel

Clicking the menu bar icon opens the detailed statistics panel, showing:

| Statistic | Description |
|-----------|-------------|
| Keyboard Strokes | Total key presses today |
| Left Clicks | Mouse left button clicks |
| Right Clicks | Mouse right button clicks |
| Mouse Movement | Total distance of mouse movement |
| Scroll Distance | Cumulative page scroll distance |

### Action Buttons

- **Reset Statistics**: Manually clear all statistics for today
- **Quit Application**: Close KeyStats

### Import & Export (Multi-device Sync)

You can sync statistics across devices using JSON export/import.

1. On device A, open Settings, click `Export Data`, and save the `.json` file.
2. On device B, open Settings, click `Import Data`, and choose that file.
3. Choose import mode:
   - `Overwrite Existing Data`: Replace local statistics with imported data.
   - `Merge and Add`: Merge imported data with local data and add totals by date.
4. To combine both devices into B: export from A, then import on B with `Merge and Add`.
5. Then export again from B, go back to A, and import with `Overwrite Existing Data` to make A and B consistent.

Tips:
- Export local data first as a backup before using overwrite.
- Importing the same file with merge multiple times will add duplicate totals.

## Project Structure

### macOS

```
KeyStats/
├── KeyStats.xcodeproj/     # Xcode project files
├── KeyStats/
│   ├── AppDelegate.swift           # Application entry point, permission management
│   ├── InputMonitor.swift          # Input event monitor
│   ├── StatsManager.swift          # Statistics data manager
│   ├── MenuBarController.swift     # Menu bar controller
│   ├── StatsPopoverViewController.swift  # Detailed panel view
│   ├── Info.plist                  # Application configuration
│   ├── KeyStats.entitlements       # Permission configuration
│   ├── Main.storyboard             # Main interface
│   └── Assets.xcassets/            # Resource files
└── README.md
```

### Windows

```
KeyStats.Windows/
├── KeyStats.sln                    # Visual Studio solution file
├── KeyStats/
│   ├── App.xaml                    # Application entry definition
│   ├── App.xaml.cs                 # Application entry logic
│   ├── Services/
│   │   ├── InputMonitorService.cs  # Input event monitor service
│   │   ├── StatsManager.cs         # Statistics data manager
│   │   ├── NotificationService.cs  # Notification service
│   │   └── StartupManager.cs       # Startup management
│   ├── ViewModels/
│   │   ├── TrayIconViewModel.cs    # Tray icon view model
│   │   └── StatsPopupViewModel.cs  # Stats popup view model
│   ├── Views/
│   │   └── StatsPopupWindow.xaml   # Stats popup window
│   ├── Models/                     # Data models
│   ├── Helpers/                    # Utility classes
│   └── Resources/                  # Resource files
└── build.ps1                       # Build script
```

## Technical Implementation

### macOS

- **Language**: Swift 5.0
- **Frameworks**: AppKit, CoreGraphics
- **Event Monitoring**: Global event listener using `CGEvent.tapCreate`
- **Data Storage**: Local persistence using `UserDefaults`
- **UI Mode**: Pure menu bar application (LSUIElement = true)

### Windows

- **Language**: C# 10
- **Framework**: WPF (.NET Framework 4.8)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Event Monitoring**: Windows low-level keyboard/mouse hooks (SetWindowsHookEx)
- **Data Storage**: Local persistence using JSON files
- **UI Mode**: System tray application
- **Advantages**: No runtime installation required, ready to use on Windows 10/11 out of the box, small app size (5-10 MB)


## Testing (AppStats)

To make `KeyStats/AppStats.swift` regression-testable, the repository includes XCTest-based tests in `KeyStatsTests/AppStatsTests.swift`, executed with `swift test`.

Covered test cases:

1. Default initialization values.
2. Counter accumulation for key/click recording methods.
3. Absolute-value behavior of `addScrollDistance(_:)`.
4. Empty-name guard in `updateDisplayName("")`.
5. Normal display name update.
6. Codable round-trip consistency.
7. Legacy `otherClicks` compatibility fallback.
8. New side-click fields taking precedence over legacy field.
9. Missing-field decode fallback to defaults.

Run:

```bash
swift test
```

## Privacy Statement

KeyStats only tracks the **count** of keystrokes and clicks, and **does NOT record**:
- Which specific keys were pressed
- Text content that was typed
- Specific click locations or applications

All data is stored locally only and is never uploaded to any server.

## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=debugtheworldbot/keyStats&type=date&legend=top-left)](https://www.star-history.com/#debugtheworldbot/keyStats&type=date&legend=top-left)

## License

MIT License
