import Foundation
import Cocoa
import CoreGraphics

/// 输入事件监听器
class InputMonitor {
    static let shared = InputMonitor()
    
    private var eventTap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?
    private var isMonitoring = false
    private let mouseSampleInterval: TimeInterval = 1.0 / 30.0
    private var lastMouseSampleTime: TimeInterval = 0
    private let swapLeftRightButtonKey = "com.apple.mouse.swapLeftRightButton"
    
    private init() {}
    
    // MARK: - 权限检查
    
    /// 检查是否有辅助功能权限
    func checkAccessibilityPermission() -> Bool {
        let options: NSDictionary = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true]
        return AXIsProcessTrustedWithOptions(options)
    }
    
    /// 仅检查权限状态（不弹出提示）
    func hasAccessibilityPermission() -> Bool {
        return AXIsProcessTrusted()
    }
    
    // MARK: - 开始/停止监听
    
    func startMonitoring() {
        guard !isMonitoring else { return }
        
        // 检查权限
        guard checkAccessibilityPermission() else {
            print("需要辅助功能权限才能监听输入事件")
            return
        }
        
        // 创建事件掩码 - 监听键盘、鼠标点击、鼠标移动和滚动事件
        let eventMask: CGEventMask = (
            (1 << CGEventType.keyDown.rawValue) |
            (1 << CGEventType.leftMouseDown.rawValue) |
            (1 << CGEventType.rightMouseDown.rawValue) |
            (1 << CGEventType.mouseMoved.rawValue) |
            (1 << CGEventType.leftMouseDragged.rawValue) |
            (1 << CGEventType.rightMouseDragged.rawValue) |
            (1 << CGEventType.scrollWheel.rawValue)
        )
        
        // 创建事件回调
        let callback: CGEventTapCallBack = { (proxy, type, event, refcon) -> Unmanaged<CGEvent>? in
            InputMonitor.shared.handleEvent(type: type, event: event)
            return Unmanaged.passUnretained(event)
        }
        
        // 创建事件监听器
        guard let tap = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .listenOnly,
            eventsOfInterest: eventMask,
            callback: callback,
            userInfo: nil
        ) else {
            print("无法创建事件监听器")
            return
        }
        
        eventTap = tap
        
        // 创建 RunLoop 源
        runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0)
        
        // 添加到主 RunLoop
        CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource, .commonModes)
        
        // 启用事件监听器
        CGEvent.tapEnable(tap: tap, enable: true)
        
        isMonitoring = true
        print("输入监听已启动")
    }
    
    func stopMonitoring() {
        guard isMonitoring else { return }
        
        if let tap = eventTap {
            CGEvent.tapEnable(tap: tap, enable: false)
        }
        
        if let source = runLoopSource {
            CFRunLoopRemoveSource(CFRunLoopGetCurrent(), source, .commonModes)
        }
        
        eventTap = nil
        runLoopSource = nil
        isMonitoring = false
        
        print("输入监听已停止")
    }
    
    // MARK: - 事件处理
    
    private func handleEvent(type: CGEventType, event: CGEvent) {
        let statsManager = StatsManager.shared
        let appIdentityProvider: () -> AppIdentity? = {
            statsManager.appStatsEnabled ? AppActivityTracker.shared.appIdentity(for: event) : nil
        }
        
        switch type {
        case .keyDown:
            // 忽略自动重复的按键（按住不放产生的重复事件）
            let isAutoRepeat = event.getIntegerValueField(.keyboardEventAutorepeat) != 0
            if !isAutoRepeat {
                let keyName = keyName(for: event)
                let appIdentity = appIdentityProvider()
                statsManager.incrementKeyPresses(keyName: keyName, appIdentity: appIdentity)
            }
            
        case .leftMouseDown:
            if shouldSwapMouseButtons(for: event) {
                statsManager.incrementRightClicks(appIdentity: appIdentityProvider())
            } else {
                statsManager.incrementLeftClicks(appIdentity: appIdentityProvider())
            }
            
        case .rightMouseDown:
            if shouldSwapMouseButtons(for: event) {
                statsManager.incrementLeftClicks(appIdentity: appIdentityProvider())
            } else {
                statsManager.incrementRightClicks(appIdentity: appIdentityProvider())
            }
            
        case .mouseMoved, .leftMouseDragged, .rightMouseDragged:
            handleMouseMove(event: event)
            
        case .scrollWheel:
            handleScroll(event: event, appIdentity: appIdentityProvider())
            
        default:
            break
        }
    }

    private func keyName(for event: CGEvent) -> String {
        let keyCode = Int(event.getIntegerValueField(.keyboardEventKeycode))
        let baseName = baseKeyName(for: keyCode, event: event)
        let modifiers = modifierNames(for: event.flags, keyCode: keyCode)
        if modifiers.isEmpty {
            return baseName
        }
        return modifiers.joined(separator: "+") + "+" + baseName
    }

    private func isPrimaryButtonRight() -> Bool {
        let key = swapLeftRightButtonKey as CFString
        if let value = CFPreferencesCopyValue(key, kCFPreferencesAnyApplication, kCFPreferencesAnyUser, kCFPreferencesCurrentHost) as? NSNumber {
            return value.boolValue
        }
        if let value = CFPreferencesCopyValue(key, kCFPreferencesAnyApplication, kCFPreferencesCurrentUser, kCFPreferencesAnyHost) as? NSNumber {
            return value.boolValue
        }
        if let value = UserDefaults.standard.object(forKey: swapLeftRightButtonKey) as? NSNumber {
            return value.boolValue
        }
        return false
    }

    private func shouldSwapMouseButtons(for event: CGEvent) -> Bool {
        guard isPrimaryButtonRight() else { return false }
        // "Primary mouse button" applies to mice; trackpad primary click remains left.
        guard let nsEvent = NSEvent(cgEvent: event) else { return false }
        switch nsEvent.subtype {
        case .mouseEvent:
            return true
        case .tabletPoint, .tabletProximity, .touch:
            return false
        default:
            return true
        }
    }

    private func modifierNames(for flags: CGEventFlags, keyCode: Int) -> [String] {
        var names: [String] = []
        if flags.contains(.maskCommand) { names.append("Cmd") }
        if flags.contains(.maskShift) { names.append("Shift") }
        if flags.contains(.maskAlternate) { names.append("Option") }
        if flags.contains(.maskControl) { names.append("Ctrl") }
        // 方向键(123-126)、Home(115)、End(119)、PageUp(116)、PageDown(121) 等导航键忽略 Fn 标志
        // 因为在某些键盘上这些键会自动带上 Fn 标志
        let isNavigationKey = (123...126).contains(keyCode) || [115, 116, 119, 121, 117].contains(keyCode)
        if flags.contains(.maskSecondaryFn) && !isNavigationKey {
            names.append("Fn")
        }
        return names
    }

    private func baseKeyName(for keyCode: Int, event: CGEvent) -> String {
        if let mapped = Self.keyCodeMap[keyCode] {
            return mapped
        }
        if let nsEvent = NSEvent(cgEvent: event),
           let chars = nsEvent.charactersIgnoringModifiers,
           !chars.isEmpty {
            if chars == " " { return "Space" }
            if chars == "\t" { return "Tab" }
            if chars == "\r" { return "Return" }
            let cleaned = chars.trimmingCharacters(in: .whitespacesAndNewlines)
            if !cleaned.isEmpty {
                if cleaned.count == 1 {
                    return cleaned.uppercased()
                }
                return cleaned
            }
        }
        return "Key\(keyCode)"
    }

    private static let keyCodeMap: [Int: String] = [
        36: "Return",
        48: "Tab",
        49: "Space",
        51: "Delete",
        53: "Esc",
        71: "Clear",
        76: "Enter",
        96: "F5",
        97: "F6",
        98: "F7",
        99: "F3",
        100: "F8",
        101: "F9",
        103: "F11",
        105: "F13",
        106: "F16",
        107: "F14",
        109: "F10",
        111: "F12",
        113: "F15",
        114: "Help",
        115: "Home",
        116: "PageUp",
        117: "ForwardDelete",
        118: "F4",
        119: "End",
        120: "F2",
        121: "PageDown",
        122: "F1",
        123: "Left",
        124: "Right",
        125: "Down",
        126: "Up"
    ]
    
    private func handleMouseMove(event: CGEvent) {
        let now = CFAbsoluteTimeGetCurrent()
        guard now - lastMouseSampleTime >= mouseSampleInterval else { return }
        let currentPosition = event.location
        let statsManager = StatsManager.shared
        
        if let lastPosition = statsManager.lastMousePosition {
            // 计算移动距离
            let dx = currentPosition.x - lastPosition.x
            let dy = currentPosition.y - lastPosition.y
            let distance = sqrt(dx * dx + dy * dy)
            
            // 过滤掉异常的大距离（可能是鼠标跳跃）
            if distance < 500 {
                statsManager.addMouseDistance(distance)
            }
        }
        
        statsManager.lastMousePosition = currentPosition
        lastMouseSampleTime = now
    }
    
    private func handleScroll(event: CGEvent, appIdentity: AppIdentity?) {
        // 获取滚动距离
        let deltaY = event.getDoubleValueField(.scrollWheelEventDeltaAxis1)
        let deltaX = event.getDoubleValueField(.scrollWheelEventDeltaAxis2)
        
        // 计算总滚动距离
        let totalDelta = sqrt(deltaX * deltaX + deltaY * deltaY)
        
        StatsManager.shared.addScrollDistance(totalDelta * 10, appIdentity: appIdentity) // 放大系数使数据更直观
    }
}
