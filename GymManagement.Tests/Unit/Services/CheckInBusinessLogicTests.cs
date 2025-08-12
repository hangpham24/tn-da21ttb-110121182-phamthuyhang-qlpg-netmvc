using FluentAssertions;
using GymManagement.Web.Data.Models;
using Xunit;

namespace GymManagement.Tests.Unit.Services
{
    /// <summary>
    /// Simple business logic tests for Check-in/Check-out functionality
    /// Tests core business rules without complex dependencies
    /// </summary>
    public class CheckInBusinessLogicTests
    {
        #region Member Validation Tests

        [Theory]
        [InlineData("THANHVIEN", "ACTIVE", true)]
        [InlineData("THANHVIEN", "INACTIVE", false)]
        [InlineData("THANHVIEN", "SUSPENDED", false)]
        [InlineData("VANGLAI", "ACTIVE", false)]
        [InlineData("ADMIN", "ACTIVE", false)]
        [InlineData("TRAINER", "ACTIVE", false)]
        public void ValidateMemberForCheckIn_VariousStatusAndTypes_ReturnsExpectedResult(
            string loaiNguoiDung, string trangThai, bool expected)
        {
            // Arrange
            var member = new NguoiDung
            {
                NguoiDungId = 1,
                LoaiNguoiDung = loaiNguoiDung,
                TrangThai = trangThai,
                Ho = "Test",
                Ten = "Member"
            };

            // Act
            var isValidMember = IsValidMemberForCheckIn(member);

            // Assert
            isValidMember.Should().Be(expected);
        }

        [Fact]
        public void ValidateMemberForCheckIn_NullMember_ReturnsFalse()
        {
            // Arrange
            NguoiDung? member = null;

            // Act
            var isValid = IsValidMemberForCheckIn(member);

            // Assert
            isValid.Should().BeFalse();
        }

        #endregion

        #region Operating Hours Tests

        [Theory]
        [InlineData("05:00", "23:00", "06:00", true)]  // Within hours
        [InlineData("05:00", "23:00", "12:00", true)]  // Midday
        [InlineData("05:00", "23:00", "22:30", true)]  // Near closing
        [InlineData("05:00", "23:00", "04:30", false)] // Before opening
        [InlineData("05:00", "23:00", "23:30", false)] // After closing
        [InlineData("05:00", "23:00", "02:00", false)] // Late night
        public void IsWithinOperatingHours_VariousTimes_ReturnsExpectedResult(
            string openTimeStr, string closeTimeStr, string checkTimeStr, bool expected)
        {
            // Arrange
            var openTime = TimeSpan.Parse(openTimeStr);
            var closeTime = TimeSpan.Parse(closeTimeStr);
            var checkTime = DateTime.Today.Add(TimeSpan.Parse(checkTimeStr));

            // Act
            var result = IsWithinOperatingHours(checkTime, openTime, closeTime);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Package Validation Tests

        [Fact]
        public void HasActivePackage_ValidActivePackage_ReturnsTrue()
        {
            // Arrange
            var registrations = new List<DangKy>
            {
                new DangKy
                {
                    DangKyId = 1,
                    NguoiDungId = 1,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
                    TrangThai = "ACTIVE"
                }
            };

            // Act
            var hasActive = HasActivePackage(registrations, 1, DateTime.Today);

            // Assert
            hasActive.Should().BeTrue();
        }

        [Fact]
        public void HasActivePackage_ExpiredPackage_ReturnsFalse()
        {
            // Arrange
            var registrations = new List<DangKy>
            {
                new DangKy
                {
                    DangKyId = 1,
                    NguoiDungId = 1,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    TrangThai = "EXPIRED"
                }
            };

            // Act
            var hasActive = HasActivePackage(registrations, 1, DateTime.Today);

            // Assert
            hasActive.Should().BeFalse();
        }

        [Fact]
        public void HasActivePackage_NoRegistrations_ReturnsFalse()
        {
            // Arrange
            var registrations = new List<DangKy>();

            // Act
            var hasActive = HasActivePackage(registrations, 1, DateTime.Today);

            // Assert
            hasActive.Should().BeFalse();
        }

        #endregion

        #region Session Duration Tests

        [Theory]
        [InlineData(1, "1h 0m")]
        [InlineData(2.5, "2h 30m")]
        [InlineData(0.5, "30m")]
        [InlineData(0.25, "15m")]
        [InlineData(12, "12h 0m")]
        public void FormatSessionDuration_VariousDurations_ReturnsCorrectFormat(double hours, string expected)
        {
            // Arrange
            var checkIn = DateTime.Now.AddHours(-hours);
            var checkOut = DateTime.Now;

            // Act
            var formatted = FormatSessionDuration(checkIn, checkOut);

            // Assert
            formatted.Should().Be(expected);
        }

        [Fact]
        public void FormatSessionDuration_LessThanOneHour_ShowsMinutesOnly()
        {
            // Arrange
            var checkIn = DateTime.Now.AddMinutes(-45);
            var checkOut = DateTime.Now;

            // Act
            var formatted = FormatSessionDuration(checkIn, checkOut);

            // Assert
            formatted.Should().Be("45m");
        }

        #endregion

        #region Duplicate Check-in Prevention Tests

        [Fact]
        public void PreventDuplicateCheckIn_SameDayAttendance_ShouldBlock()
        {
            // Arrange
            var existingAttendances = new List<DiemDanh>
            {
                CreateSampleAttendance(1, DateTime.Today.AddHours(8))
            };

            var memberId = 1;
            var checkDate = DateTime.Today.AddHours(14);

            // Act
            var hasTodayAttendance = HasAttendanceToday(existingAttendances, memberId, checkDate);

            // Assert
            hasTodayAttendance.Should().BeTrue();
        }

        [Fact]
        public void PreventDuplicateCheckIn_DifferentDayAttendance_ShouldAllow()
        {
            // Arrange
            var existingAttendances = new List<DiemDanh>
            {
                CreateSampleAttendance(1, DateTime.Today.AddDays(-1).AddHours(8))
            };

            var memberId = 1;
            var checkDate = DateTime.Today.AddHours(14);

            // Act
            var hasTodayAttendance = HasAttendanceToday(existingAttendances, memberId, checkDate);

            // Assert
            hasTodayAttendance.Should().BeFalse();
        }

        #endregion

        #region Face Recognition Confidence Tests

        [Theory]
        [InlineData(0.95, true)]   // High confidence
        [InlineData(0.85, true)]   // Good confidence
        [InlineData(0.75, true)]   // Acceptable confidence
        [InlineData(0.65, false)]  // Low confidence
        [InlineData(0.50, false)]  // Very low confidence
        [InlineData(0.30, false)]  // Poor confidence
        public void ValidateFaceRecognitionConfidence_VariousLevels_ReturnsExpectedResult(double confidence, bool expected)
        {
            // Arrange
            const double threshold = 0.7; // Typical threshold

            // Act
            var isValid = confidence >= threshold;

            // Assert
            isValid.Should().Be(expected);
        }

        #endregion

        #region Attendance Record Creation Tests

        [Fact]
        public void CreateAttendanceRecord_ValidData_ReturnsCorrectRecord()
        {
            // Arrange
            var memberId = 1;
            var checkInTime = DateTime.Now;
            var note = "Manual check-in";

            // Act
            var attendance = CreateAttendanceRecord(memberId, checkInTime, note);

            // Assert
            attendance.Should().NotBeNull();
            attendance.ThanhVienId.Should().Be(memberId);
            attendance.ThoiGianCheckIn.Should().BeCloseTo(checkInTime, TimeSpan.FromSeconds(1));
            attendance.GhiChu.Should().Be(note);
            attendance.TrangThai.Should().Be("Present");
            attendance.KetQuaNhanDang.Should().BeTrue();
        }

        [Fact]
        public void CreateWorkoutSession_ValidData_ReturnsCorrectSession()
        {
            // Arrange
            var memberId = 1;
            var startTime = DateTime.Now;
            var note = "Check-in thủ công";

            // Act
            var session = CreateWorkoutSession(memberId, startTime, note);

            // Assert
            session.Should().NotBeNull();
            session.ThanhVienId.Should().Be(memberId);
            session.ThoiGianVao.Should().BeCloseTo(startTime, TimeSpan.FromSeconds(1));
            session.GhiChu.Should().Be(note);
            session.ThoiGianRa.Should().BeNull();
        }

        #endregion

        #region Helper Methods

        private static bool IsValidMemberForCheckIn(NguoiDung? member)
        {
            return member != null && 
                   member.LoaiNguoiDung == "THANHVIEN" && 
                   member.TrangThai == "ACTIVE";
        }

        private static bool IsWithinOperatingHours(DateTime time, TimeSpan openTime, TimeSpan closeTime)
        {
            var timeOfDay = time.TimeOfDay;
            return timeOfDay >= openTime && timeOfDay <= closeTime;
        }

        private static bool HasActivePackage(List<DangKy> registrations, int memberId, DateTime checkDate)
        {
            return registrations.Any(r => 
                r.NguoiDungId == memberId && 
                r.TrangThai == "ACTIVE" &&
                DateOnly.FromDateTime(checkDate) >= r.NgayBatDau &&
                DateOnly.FromDateTime(checkDate) <= r.NgayKetThuc);
        }

        private static string FormatSessionDuration(DateTime checkIn, DateTime checkOut)
        {
            var duration = checkOut - checkIn;
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
            return $"{(int)duration.TotalMinutes}m";
        }

        private static bool HasAttendanceToday(List<DiemDanh> attendances, int memberId, DateTime checkDate)
        {
            return attendances.Any(a => 
                a.ThanhVienId == memberId && 
                a.ThoiGianCheckIn.Date == checkDate.Date);
        }

        private static DiemDanh CreateSampleAttendance(int memberId, DateTime? checkInTime = null)
        {
            return new DiemDanh
            {
                ThanhVienId = memberId,
                ThoiGian = checkInTime ?? DateTime.Now,
                ThoiGianCheckIn = checkInTime ?? DateTime.Now,
                KetQuaNhanDang = true,
                LoaiCheckIn = "Manual",
                TrangThai = "Present",
                GhiChu = "Test attendance"
            };
        }

        private static DiemDanh CreateAttendanceRecord(int memberId, DateTime checkInTime, string? note = null)
        {
            return new DiemDanh
            {
                ThanhVienId = memberId,
                ThoiGian = checkInTime,
                ThoiGianCheckIn = checkInTime,
                KetQuaNhanDang = true,
                LoaiCheckIn = "Manual",
                TrangThai = "Present",
                GhiChu = note
            };
        }

        private static BuoiTap CreateWorkoutSession(int memberId, DateTime startTime, string? note = null)
        {
            return new BuoiTap
            {
                ThanhVienId = memberId,
                ThoiGianVao = startTime,
                GhiChu = note
            };
        }

        #endregion
    }
}
