using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Services
{
    public interface IDiemDanhService
    {
        Task<IEnumerable<DiemDanh>> GetAllAsync();
        Task<DiemDanh?> GetByIdAsync(int id);
        Task<DiemDanh> CreateAsync(DiemDanh diemDanh);
        Task<DiemDanh> UpdateAsync(DiemDanh diemDanh);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<DiemDanh>> GetByMemberIdAsync(int thanhVienId);
        Task<IEnumerable<DiemDanh>> GetTodayAttendanceAsync();
        Task<DiemDanh?> GetLatestAttendanceAsync(int thanhVienId);
        Task<bool> CheckInAsync(int thanhVienId, string? anhMinhChung = null);
        Task<bool> CheckInWithFaceRecognitionAsync(int thanhVienId, byte[] faceImage);
        Task<bool> CheckOutAsync(int diemDanhId);
        Task<DiemDanh?> GetActiveSessionAsync(int thanhVienId);
        Task<bool> HasCheckedInTodayAsync(int thanhVienId);
        Task<int> GetTodayAttendanceCountAsync();
        Task<int> GetMemberAttendanceCountAsync(int thanhVienId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<DiemDanh>> GetAttendanceReportAsync(DateTime startDate, DateTime endDate);

        // Methods for Trainer attendance management
        Task<IEnumerable<DiemDanh>> GetAttendanceByClassScheduleAsync(int lichLopId);
        Task<bool> TakeClassAttendanceAsync(int lichLopId, List<ClassAttendanceRecord> attendanceRecords);
        Task<IEnumerable<NguoiDung>> GetStudentsInClassScheduleAsync(int lichLopId);
        Task<bool> CanTrainerTakeAttendanceAsync(int trainerId, int lichLopId);
        Task<DateTime?> GetFirstAttendanceDateAsync(int nguoiDungId);
        Task<IEnumerable<DiemDanh>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    }

    // DTO for class attendance
    public class ClassAttendanceRecord
    {
        public int ThanhVienId { get; set; }
        public string TrangThai { get; set; } = "Present"; // Present, Absent, Late
        public string? GhiChu { get; set; }
    }
}
