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
        Task<bool> CheckInWithClassAsync(int thanhVienId, int? lopHocId = null, string? anhMinhChung = null);
        Task<bool> CheckInWithFaceRecognitionAsync(int thanhVienId, byte[] faceImage);
        Task<bool> CheckOutAsync(int diemDanhId);
        Task<DiemDanh?> GetActiveSessionAsync(int thanhVienId);
        Task<bool> HasCheckedInTodayAsync(int thanhVienId);
        Task<int> GetTodayAttendanceCountAsync();
        Task<int> GetMemberAttendanceCountAsync(int thanhVienId, DateTime startDate, DateTime endDate);
        Task<int> GetAttendanceCountByUserIdAsync(int thanhVienId);
        Task<IEnumerable<DiemDanh>> GetAttendanceReportAsync(DateTime startDate, DateTime endDate);

        // Note: Methods using LichLop have been removed
        // Use class-based attendance methods instead
        Task<DateTime?> GetFirstAttendanceDateAsync(int nguoiDungId);
        Task<IEnumerable<DiemDanh>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<IEnumerable<object>> GetAvailableClassesAsync();
        Task<string?> GetClassNameAsync(int classId);
        Task<FaceRecognitionResult> RecognizeFaceAsync(float[] faceDescriptor);
    }

    // DTO for class attendance
    public class ClassAttendanceRecord
    {
        public int ThanhVienId { get; set; }
        public string TrangThai { get; set; } = "Present"; // Present, Absent, Late
        public string? GhiChu { get; set; }
    }
}
