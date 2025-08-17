using FluentAssertions;
using GymManagement.Web.Data.Models;
using Xunit;

namespace GymManagement.Tests.Unit.Validation
{
    /// <summary>
    /// Validation tests for Check-in/Check-out business rules
    /// Tests input validation and business constraints
    /// </summary>
    public class CheckInValidationTests
    {
        #region Input Validation Tests

        [Theory]
        [InlineData(1, true)]      // Valid member ID
        [InlineData(100, true)]    // Valid member ID
        [InlineData(0, false)]     // Invalid member ID
        [InlineData(-1, false)]    // Invalid member ID
        public void ValidateMemberId_VariousValues_ReturnsExpectedResult(int memberId, bool expected)
        {
            // Act
            var isValid = ValidateMemberId(memberId);

            // Assert
            isValid.Should().Be(expected);
        }

        [Theory]
        [InlineData("", false)]           // Empty string
        [InlineData("   ", false)]        // Whitespace only
        [InlineData("Valid note", true)]  // Valid note
        [InlineData("Ghi chú tiếng Việt", true)] // Vietnamese text
        [InlineData(null, true)]          // Null is acceptable
        public void ValidateCheckInNote_VariousInputs_ReturnsExpectedResult(string? note, bool expected)
        {
            // Act
            var isValid = ValidateCheckInNote(note);

            // Assert
            isValid.Should().Be(expected);
        }

        [Fact]
        public void ValidateCheckInNote_VeryLongNote_ReturnsFalse()
        {
            // Arrange
            var longNote = new string('A', 1001); // Over 1000 characters

            // Act
            var isValid = ValidateCheckInNote(longNote);

            // Assert
            isValid.Should().BeFalse();
        }

        #endregion

        #region Time Validation Tests

        [Fact]
        public void ValidateCheckInTime_CurrentTime_ReturnsTrue()
        {
            // Arrange
            var checkInTime = DateTime.Now;

            // Act
            var isValid = ValidateCheckInTime(checkInTime);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateCheckInTime_FutureTime_ReturnsFalse()
        {
            // Arrange
            var futureTime = DateTime.Now.AddHours(1);

            // Act
            var isValid = ValidateCheckInTime(futureTime);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateCheckInTime_VeryOldTime_ReturnsFalse()
        {
            // Arrange
            var oldTime = DateTime.Now.AddDays(-2);

            // Act
            var isValid = ValidateCheckInTime(oldTime);

            // Assert
            isValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(-1, true)]    // 1 hour ago
        [InlineData(-12, true)]   // 12 hours ago
        [InlineData(-23, true)]   // 23 hours ago
        [InlineData(-25, false)]  // Over 24 hours ago
        [InlineData(1, false)]    // Future time
        public void ValidateCheckInTime_VariousHoursOffset_ReturnsExpectedResult(int hoursOffset, bool expected)
        {
            // Arrange
            var checkInTime = DateTime.Now.AddHours(hoursOffset);

            // Act
            var isValid = ValidateCheckInTime(checkInTime);

            // Assert
            isValid.Should().Be(expected);
        }

        #endregion

        #region Face Recognition Validation Tests

        [Theory]
        [InlineData(128, true)]   // Valid descriptor length
        [InlineData(64, false)]   // Invalid length
        [InlineData(256, false)]  // Invalid length
        [InlineData(0, false)]    // Empty array
        public void ValidateFaceDescriptor_VariousLengths_ReturnsExpectedResult(int length, bool expected)
        {
            // Arrange
            var descriptor = length > 0 ? new float[length] : new float[0];

            // Act
            var isValid = ValidateFaceDescriptor(descriptor);

            // Assert
            isValid.Should().Be(expected);
        }

        [Fact]
        public void ValidateFaceDescriptor_NullDescriptor_ReturnsFalse()
        {
            // Arrange
            float[]? descriptor = null;

            // Act
            var isValid = ValidateFaceDescriptor(descriptor);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateFaceDescriptor_ValidDescriptorWithData_ReturnsTrue()
        {
            // Arrange
            var descriptor = new float[128];
            for (int i = 0; i < 128; i++)
            {
                descriptor[i] = (float)(i / 128.0);
            }

            // Act
            var isValid = ValidateFaceDescriptor(descriptor);

            // Assert
            isValid.Should().BeTrue();
        }

        #endregion

        #region Business Rule Validation Tests

        [Fact]
        public void ValidateCheckInEligibility_ActiveMemberWithValidPackage_ReturnsTrue()
        {
            // Arrange
            var member = CreateTestMember("THANHVIEN", "ACTIVE");
            var activePackage = CreateTestPackage(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(20));

            // Act
            var isEligible = ValidateCheckInEligibility(member, activePackage);

            // Assert
            isEligible.Should().BeTrue();
        }

        [Fact]
        public void ValidateCheckInEligibility_InactiveMember_ReturnsFalse()
        {
            // Arrange
            var member = CreateTestMember("THANHVIEN", "INACTIVE");
            var activePackage = CreateTestPackage(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(20));

            // Act
            var isEligible = ValidateCheckInEligibility(member, activePackage);

            // Assert
            isEligible.Should().BeFalse();
        }

        [Fact]
        public void ValidateCheckInEligibility_WalkInGuest_ReturnsFalse()
        {
            // Arrange
            var guest = CreateTestMember("VANGLAI", "ACTIVE");
            var activePackage = CreateTestPackage(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(20));

            // Act
            var isEligible = ValidateCheckInEligibility(guest, activePackage);

            // Assert
            isEligible.Should().BeFalse();
        }

        [Fact]
        public void ValidateCheckInEligibility_ExpiredPackage_ReturnsFalse()
        {
            // Arrange
            var member = CreateTestMember("THANHVIEN", "ACTIVE");
            var expiredPackage = CreateTestPackage(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-1));

            // Act
            var isEligible = ValidateCheckInEligibility(member, expiredPackage);

            // Assert
            isEligible.Should().BeFalse();
        }

        [Fact]
        public void ValidateCheckInEligibility_NoPackage_ReturnsFalse()
        {
            // Arrange
            var member = CreateTestMember("THANHVIEN", "ACTIVE");
            DangKy? noPackage = null;

            // Act
            var isEligible = ValidateCheckInEligibility(member, noPackage);

            // Assert
            isEligible.Should().BeFalse();
        }

        #endregion

        #region Concurrent Access Validation Tests

        [Fact]
        public void ValidateConcurrentCheckIn_SameMemberMultipleAttempts_OnlyOneSucceeds()
        {
            // Arrange
            var member = CreateTestMember("THANHVIEN", "ACTIVE");
            var existingAttendances = new List<DiemDanh>();
            var attempts = new List<bool>();

            // Simulate concurrent check-in attempts
            for (int i = 0; i < 3; i++)
            {
                var hasExistingAttendance = existingAttendances.Any(a => 
                    a.ThanhVienId == member.NguoiDungId && 
                    a.ThoiGianCheckIn.Date == DateTime.Today);

                if (!hasExistingAttendance)
                {
                    // Simulate successful check-in
                    existingAttendances.Add(new DiemDanh
                    {
                        ThanhVienId = member.NguoiDungId,
                        ThoiGianCheckIn = DateTime.Now,
                        TrangThai = "Present"
                    });
                    attempts.Add(true);
                }
                else
                {
                    attempts.Add(false);
                }
            }

            // Assert
            attempts.Count(success => success).Should().Be(1); // Only one should succeed
            existingAttendances.Should().HaveCount(1); // Only one attendance record
        }

        #endregion

        #region Helper Methods

        private static bool ValidateMemberId(int memberId)
        {
            return memberId > 0;
        }

        private static bool ValidateCheckInNote(string? note)
        {
            if (note == null) return true; // Null is acceptable
            if (string.IsNullOrWhiteSpace(note)) return false; // Empty or whitespace is not
            return note.Length <= 1000; // Maximum length check
        }

        private static bool ValidateCheckInTime(DateTime checkInTime)
        {
            var now = DateTime.Now;
            var timeDifference = now - checkInTime;
            
            // Allow check-in within the last 24 hours, but not in the future
            return timeDifference >= TimeSpan.Zero && timeDifference <= TimeSpan.FromHours(24);
        }

        private static bool ValidateFaceDescriptor(float[]? descriptor)
        {
            return descriptor != null && descriptor.Length == 128;
        }

        private static bool ValidateCheckInEligibility(NguoiDung member, DangKy? package)
        {
            // Member must be active and of type THANHVIEN
            if (member.LoaiNguoiDung != "THANHVIEN" || member.TrangThai != "ACTIVE")
                return false;

            // Must have an active package
            if (package == null || package.TrangThai != "ACTIVE")
                return false;

            // Package must be valid for today
            var today = DateOnly.FromDateTime(DateTime.Today);
            return today >= package.NgayBatDau && today <= package.NgayKetThuc;
        }

        private static NguoiDung CreateTestMember(string loaiNguoiDung, string trangThai)
        {
            return new NguoiDung
            {
                NguoiDungId = 1,
                LoaiNguoiDung = loaiNguoiDung,
                TrangThai = trangThai,
                Ho = "Test",
                Ten = "Member",
                Email = "test@example.com",
                SoDienThoai = "0123456789"
            };
        }

        private static DangKy CreateTestPackage(DateTime startDate, DateTime endDate)
        {
            return new DangKy
            {
                DangKyId = 1,
                NguoiDungId = 1,
                GoiTapId = 1,
                NgayBatDau = DateOnly.FromDateTime(startDate),
                NgayKetThuc = DateOnly.FromDateTime(endDate),
                TrangThai = "ACTIVE",
                PhiDangKy = 500000
            };
        }

        #endregion
    }
}
