import Foundation
import Sparkle

/// Manages Sparkle update checks and lifecycle.
final class UpdateManager {
    static let shared = UpdateManager()

    private let updaterController: SPUStandardUpdaterController
    private var updateAvailabilityHandlers: [UUID: (Bool) -> Void] = [:]
    private var sparkleNotificationObservers: [NSObjectProtocol] = []
    private(set) var hasAvailableUpdate = false

    private init() {
        updaterController = SPUStandardUpdaterController(
            startingUpdater: true,
            updaterDelegate: nil,
            userDriverDelegate: nil
        )
        registerSparkleObservers()
        probeForUpdateAvailability()
    }

    // MARK: - Updates

    func checkForUpdates() {
        if Thread.isMainThread {
            updaterController.checkForUpdates(nil)
        } else {
            DispatchQueue.main.async { [weak self] in
                self?.updaterController.checkForUpdates(nil)
            }
        }
    }

    func probeForUpdateAvailability() {
        let probe = { [weak self] in
            guard let self = self else { return }
            let updater = self.updaterController.updater
            guard !updater.sessionInProgress else { return }
            updater.checkForUpdateInformation()
        }

        if Thread.isMainThread {
            probe()
        } else {
            DispatchQueue.main.async {
                probe()
            }
        }
    }

    func addUpdateAvailabilityHandler(_ handler: @escaping (Bool) -> Void) -> UUID {
        let token = UUID()
        updateAvailabilityHandlers[token] = handler
        handler(hasAvailableUpdate)
        return token
    }

    func removeUpdateAvailabilityHandler(_ token: UUID) {
        updateAvailabilityHandlers.removeValue(forKey: token)
    }

    // MARK: - Private

    private func registerSparkleObservers() {
        let center = NotificationCenter.default
        let updater = updaterController.updater

        let didFindObserver = center.addObserver(
            forName: NSNotification.Name.SUUpdaterDidFindValidUpdate,
            object: updater,
            queue: .main
        ) { [weak self] _ in
            self?.setHasAvailableUpdate(true)
        }

        let didNotFindObserver = center.addObserver(
            forName: NSNotification.Name.SUUpdaterDidNotFindUpdate,
            object: updater,
            queue: .main
        ) { [weak self] _ in
            self?.setHasAvailableUpdate(false)
        }

        let willRestartObserver = center.addObserver(
            forName: NSNotification.Name.SUUpdaterWillRestart,
            object: updater,
            queue: .main
        ) { [weak self] _ in
            self?.setHasAvailableUpdate(false)
        }

        sparkleNotificationObservers = [didFindObserver, didNotFindObserver, willRestartObserver]
    }

    private func setHasAvailableUpdate(_ hasUpdate: Bool) {
        guard hasAvailableUpdate != hasUpdate else { return }
        hasAvailableUpdate = hasUpdate
        updateAvailabilityHandlers.values.forEach { $0(hasUpdate) }
    }
}
