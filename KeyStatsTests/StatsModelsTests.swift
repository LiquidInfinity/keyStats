import XCTest
@testable import KeyStatsCore

final class StatsModelsTests: XCTestCase {
    func testDailyStatsInitNormalizesDateToStartOfDay() {
        let date = Date(timeIntervalSince1970: 1_710_099_123)

        let stats = DailyStats(date: date)

        XCTAssertEqual(stats.date, Calendar.current.startOfDay(for: date))
    }

    func testDailyStatsCorrectionRateCountsDeleteVariants() {
        var stats = DailyStats(date: Date())
        stats.keyPresses = 20
        stats.keyPressCounts = [
            "Delete": 2,
            "Shift + Delete": 3,
            "Command+ForwardDelete": 1,
            "Space": 9
        ]

        XCTAssertEqual(stats.correctionRate, 0.3, accuracy: 0.0001)
    }

    func testDailyStatsInputRatioHandlesZeroClicks() {
        var stats = DailyStats(date: Date())
        stats.keyPresses = 8

        XCTAssertEqual(stats.inputRatio, .infinity)
    }

    func testDailyStatsHasAnyActivityDetectsNestedAppStats() {
        var stats = DailyStats(date: Date())
        stats.appStats["com.test.app"] = AppStats(bundleId: "com.test.app", displayName: "Test")

        XCTAssertTrue(stats.hasAnyActivity)
    }

    func testDailyStatsCodableBackfillsLegacyOtherClicks() throws {
        let json = """
        {
          "date": 1710028800,
          "keyPresses": 4,
          "otherClicks": 7,
          "mouseDistance": 15.5,
          "scrollDistance": 9
        }
        """.data(using: .utf8)!

        let decoded = try JSONDecoder().decode(DailyStats.self, from: json)

        XCTAssertEqual(decoded.sideBackClicks, 7)
        XCTAssertEqual(decoded.sideForwardClicks, 0)
        XCTAssertEqual(decoded.totalClicks, 7)
        XCTAssertEqual(decoded.mouseDistance, 15.5, accuracy: 0.0001)
    }

    func testAllTimeStatsInitialStartsEmpty() {
        let stats = AllTimeStats.initial()

        XCTAssertEqual(stats.totalKeyPresses, 0)
        XCTAssertEqual(stats.totalClicks, 0)
        XCTAssertEqual(stats.correctionRate, 0)
        XCTAssertEqual(stats.inputRatio, 0)
        XCTAssertNil(stats.firstDate)
        XCTAssertNil(stats.lastDate)
    }

    func testAllTimeStatsCorrectionRateAndInputRatioUseAggregates() {
        let stats = AllTimeStats(
            totalKeyPresses: 12,
            totalLeftClicks: 2,
            totalRightClicks: 1,
            totalSideBackClicks: 1,
            totalSideForwardClicks: 0,
            totalMouseDistance: 0,
            totalScrollDistance: 0,
            keyPressCounts: [
                "Option + Delete": 2,
                "ForwardDelete": 1,
                "A": 9
            ],
            firstDate: nil,
            lastDate: nil,
            activeDays: 0,
            maxDailyKeyPresses: 0,
            maxDailyKeyPressesDate: nil,
            maxDailyClicks: 0,
            maxDailyClicksDate: nil,
            mostActiveWeekday: nil,
            keyActiveDays: 0,
            clickActiveDays: 0
        )

        XCTAssertEqual(stats.totalClicks, 4)
        XCTAssertEqual(stats.correctionRate, 0.25, accuracy: 0.0001)
        XCTAssertEqual(stats.inputRatio, 3.0, accuracy: 0.0001)
    }
}
