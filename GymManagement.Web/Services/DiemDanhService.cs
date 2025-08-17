using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;

namespace GymManagement.Web.Services
{
    public class DiemDanhService : IDiemDanhService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDiemDanhRepository _diemDanhRepository;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly IThongBaoService _thongBaoService;
        private readonly IFaceRecognitionService _faceRecognitionService;

        public DiemDanhService(
            IUnitOfWork unitOfWork,
            IDiemDanhRepository diemDanhRepository,
            INguoiDungRepository nguoiDungRepository,
            IThongBaoService thongBaoService,
            IFaceRecognitionService faceRecognitionService)
        {
            _unitOfWork = unitOfWork;
            _diemDanhRepository = diemDanhRepository;
            _nguoiDungRepository = nguoiDungRepository;
            _thongBaoService = thongBaoService;
            _faceRecognitionService = faceRecognitionService;
        }

        public async Task<IEnumerable<DiemDanh>> GetAllAsync()
        {
            return await _diemDanhRepository.GetAllAsync();
        }

        public async Task<DiemDanh?> GetByIdAsync(int id)
        {
            return await _diemDanhRepository.GetByIdAsync(id);
        }

        public async Task<DiemDanh> CreateAsync(DiemDanh diemDanh)
        {
            var created = await _diemDanhRepository.AddAsync(diemDanh);
            await _unitOfWork.SaveChangesAsync();
            return created;
        }

        public async Task<DiemDanh> UpdateAsync(DiemDanh diemDanh)
        {
            await _diemDanhRepository.UpdateAsync(diemDanh);
            await _unitOfWork.SaveChangesAsync();
            return diemDanh;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var diemDanh = await _diemDanhRepository.GetByIdAsync(id);
            if (diemDanh == null) return false;

            await _diemDanhRepository.DeleteAsync(diemDanh);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DiemDanh>> GetByMemberIdAsync(int thanhVienId)
        {
            return await _diemDanhRepository.GetByThanhVienIdAsync(thanhVienId);
        }

        public async Task<IEnumerable<DiemDanh>> GetTodayAttendanceAsync()
        {
            return await _diemDanhRepository.GetTodayAttendanceAsync();
        }

        public async Task<DiemDanh?> GetLatestAttendanceAsync(int thanhVienId)
        {
            return await _diemDanhRepository.GetLatestAttendanceAsync(thanhVienId);
        }

        public async Task<DateTime?> GetFirstAttendanceDateAsync(int nguoiDungId)
        {
            var attendances = await _diemDanhRepository.GetByNguoiDungIdAsync(nguoiDungId);
            var firstAttendance = attendances.OrderBy(d => d.ThoiGianCheckIn).FirstOrDefault();
            return firstAttendance?.ThoiGianCheckIn;
        }

        public async Task<IEnumerable<DiemDanh>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            var allAttendances = await _diemDanhRepository.GetAllAsync();
            return allAttendances.Where(d => d.ThoiGianCheckIn >= fromDate && d.ThoiGianCheckIn <= toDate);
        }

        public async Task<bool> CheckInAsync(int thanhVienId, string? anhMinhChung = null)
        {
            // Check if member exists and is active
            var thanhVien = await _nguoiDungRepository.GetByIdAsync(thanhVienId);
            if (thanhVien == null || thanhVien.TrangThai != "ACTIVE" || thanhVien.LoaiNguoiDung != "THANHVIEN")
                return false;

            // Check if already checked in today
            if (await _diemDanhRepository.HasAttendanceToday(thanhVienId))
                return false;

            // Create attendance record
            var diemDanh = new DiemDanh
            {
                ThanhVienId = thanhVienId,
                ThoiGian = DateTime.Now,
                KetQuaNhanDang = true, // Manual check-in is always successful
                AnhMinhChung = anhMinhChung
            };

            await _diemDanhRepository.AddAsync(diemDanh);
            await _unitOfWork.SaveChangesAsync();

            // Create workout session record
            var buoiTap = new BuoiTap
            {
                ThanhVienId = thanhVienId,
                ThoiGianVao = DateTime.Now,
                GhiChu = "Check-in thủ công"
            };

            await _unitOfWork.Context.BuoiTaps.AddAsync(buoiTap);
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            await _thongBaoService.CreateNotificationAsync(
                thanhVienId,
                "Check-in thành công",
                $"Bạn đã check-in thành công lúc {DateTime.Now:HH:mm dd/MM/yyyy}",
                "APP"
            );

            return true;
        }

        public async Task<bool> CheckInWithClassAsync(int thanhVienId, int? lopHocId = null, string? anhMinhChung = null)
        {
            // Check if member exists and is active
            var thanhVien = await _nguoiDungRepository.GetByIdAsync(thanhVienId);
            if (thanhVien == null || thanhVien.TrangThai != "ACTIVE" || thanhVien.LoaiNguoiDung != "THANHVIEN")
                return false;

            // Check if already checked in today
            if (await _diemDanhRepository.HasAttendanceToday(thanhVienId))
                return false;

            // Validate class if provided
            if (lopHocId.HasValue)
            {
                var lopHoc = await _unitOfWork.Context.LopHocs.FindAsync(lopHocId.Value);
                if (lopHoc == null || lopHoc.TrangThai != "OPEN")
                    return false;
            }

            // Create attendance record
            var diemDanh = new DiemDanh
            {
                ThanhVienId = thanhVienId,
                ThoiGian = DateTime.Now,
                KetQuaNhanDang = true,
                AnhMinhChung = anhMinhChung,
                GhiChu = lopHocId.HasValue ? $"Check-in vào lớp học ID: {lopHocId}" : "Check-in tự do"
            };

            await _diemDanhRepository.AddAsync(diemDanh);
            await _unitOfWork.SaveChangesAsync();

            // Create workout session record with class info
            var buoiTap = new BuoiTap
            {
                ThanhVienId = thanhVienId,
                LopHocId = lopHocId,
                ThoiGianVao = DateTime.Now,
                GhiChu = lopHocId.HasValue ? "Check-in vào lớp học" : "Check-in tự do"
            };

            await _unitOfWork.Context.BuoiTaps.AddAsync(buoiTap);
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            var className = lopHocId.HasValue ?
                (await _unitOfWork.Context.LopHocs.FindAsync(lopHocId.Value))?.TenLop ?? "Lớp học" :
                "tập tự do";

            await _thongBaoService.CreateNotificationAsync(
                thanhVienId,
                "Check-in thành công",
                $"Bạn đã check-in thành công {className} lúc {DateTime.Now:HH:mm dd/MM/yyyy}",
                "APP"
            );

            return true;
        }

        public async Task<bool> CheckInWithFaceRecognitionAsync(int thanhVienId, byte[] faceImage)
        {
            // Check if member exists and is active
            var thanhVien = await _nguoiDungRepository.GetByIdAsync(thanhVienId);
            if (thanhVien == null || thanhVien.TrangThai != "ACTIVE" || thanhVien.LoaiNguoiDung != "THANHVIEN")
                return false;

            // Check if already checked in today
            if (await _diemDanhRepository.HasAttendanceToday(thanhVienId))
                return false;

            // TODO: Implement face recognition logic
            // For now, we'll simulate face recognition
            bool faceRecognitionResult = await SimulateFaceRecognition(thanhVienId, faceImage);

            // Create attendance record
            var diemDanh = new DiemDanh
            {
                ThanhVienId = thanhVienId,
                ThoiGian = DateTime.Now,
                KetQuaNhanDang = faceRecognitionResult,
                AnhMinhChung = $"face_recognition_{DateTime.Now:yyyyMMddHHmmss}.jpg"
            };

            await _diemDanhRepository.AddAsync(diemDanh);

            if (faceRecognitionResult)
            {
                // Create workout session record
                var buoiTap = new BuoiTap
                {
                    ThanhVienId = thanhVienId,
                    ThoiGianVao = DateTime.Now,
                    GhiChu = "Check-in bằng nhận diện khuôn mặt"
                };

                await _unitOfWork.Context.BuoiTaps.AddAsync(buoiTap);

                // Send success notification
                await _thongBaoService.CreateNotificationAsync(
                    thanhVienId,
                    "Check-in thành công",
                    $"Bạn đã check-in thành công bằng nhận diện khuôn mặt lúc {DateTime.Now:HH:mm dd/MM/yyyy}",
                    "APP"
                );
            }
            else
            {
                // Send failure notification
                await _thongBaoService.CreateNotificationAsync(
                    thanhVienId,
                    "Check-in thất bại",
                    "Không thể nhận diện khuôn mặt. Vui lòng thử lại hoặc liên hệ nhân viên.",
                    "APP"
                );
            }

            await _unitOfWork.SaveChangesAsync();
            return faceRecognitionResult;
        }

        public async Task<bool> HasCheckedInTodayAsync(int thanhVienId)
        {
            return await _diemDanhRepository.HasAttendanceToday(thanhVienId);
        }

        public async Task<int> GetTodayAttendanceCountAsync()
        {
            return await _diemDanhRepository.CountAttendanceByDateAsync(DateTime.Today);
        }

        public async Task<int> GetMemberAttendanceCountAsync(int thanhVienId, DateTime startDate, DateTime endDate)
        {
            return await _diemDanhRepository.CountAttendanceByMemberAsync(thanhVienId, startDate, endDate);
        }

        public async Task<int> GetAttendanceCountByUserIdAsync(int thanhVienId)
        {
            var attendances = await _diemDanhRepository.GetByThanhVienIdAsync(thanhVienId);
            return attendances.Count();
        }

        public async Task<IEnumerable<DiemDanh>> GetAttendanceReportAsync(DateTime startDate, DateTime endDate)
        {
            return await _diemDanhRepository.GetByDateRangeAsync(startDate, endDate);
        }

        private async Task<bool> SimulateFaceRecognition(int thanhVienId, byte[] faceImage)
        {
            // This is a placeholder for actual face recognition implementation
            // In a real system, you would:
            // 1. Load the member's face template from MauMat table
            // 2. Compare it with the provided face image
            // 3. Return true if similarity is above threshold

            await Task.Delay(1000); // Simulate processing time

            // For demo purposes, return true 90% of the time
            var random = new Random();
            return random.NextDouble() > 0.1;
        }

        // Note: GetAttendanceByClassScheduleAsync method removed as LichLop no longer exists

        // Note: TakeClassAttendanceAsync method removed as it used LichLop
        // Use TakeAttendanceAsync with lopHocId and date instead

        // Note: Methods using LichLop have been removed
        // Use class-based attendance methods instead

        public async Task<IEnumerable<object>> GetAvailableClassesAsync()
        {
            var classes = await _unitOfWork.Context.LopHocs
                .Where(l => l.TrangThai == "OPEN")
                .Select(l => new
                {
                    LopHocId = l.LopHocId,
                    TenLop = l.TenLop,
                    MoTa = l.MoTa,
                    SucChua = l.SucChua,
                    GioBatDau = l.GioBatDau.ToString(@"hh\:mm"),
                    GioKetThuc = l.GioKetThuc.ToString(@"hh\:mm"),
                    ThuTrongTuan = l.ThuTrongTuan
                })
                .ToListAsync();

            return classes;
        }

        public async Task<string?> GetClassNameAsync(int classId)
        {
            var lopHoc = await _unitOfWork.Context.LopHocs.FindAsync(classId);
            return lopHoc?.TenLop;
        }

        public async Task<FaceRecognitionResult> RecognizeFaceAsync(float[] faceDescriptor)
        {
            try
            {
                // Use the injected face recognition service
                return await _faceRecognitionService.RecognizeFaceAsync(faceDescriptor);
            }
            catch (Exception ex)
            {
                return new FaceRecognitionResult
                {
                    Success = false,
                    Message = $"Lỗi khi nhận diện khuôn mặt: {ex.Message}"
                };
            }
        }

        // Face Recognition specific methods
        public async Task<bool> CheckOutAsync(int diemDanhId)
        {
            try
            {
                var diemDanh = await _diemDanhRepository.GetByIdAsync(diemDanhId);
                if (diemDanh == null)
                    return false;

                // Update DiemDanh checkout time
                diemDanh.ThoiGianCheckOut = DateTime.Now;
                await _diemDanhRepository.UpdateAsync(diemDanh);

                // ✅ FIX: Update BuoiTap checkout time
                var buoiTap = await _unitOfWork.Context.BuoiTaps
                    .FirstOrDefaultAsync(b => b.ThanhVienId == diemDanh.ThanhVienId
                                           && b.ThoiGianVao.Date == DateTime.Today
                                           && b.ThoiGianRa == null);

                if (buoiTap != null)
                {
                    buoiTap.ThoiGianRa = DateTime.Now;
                    _unitOfWork.Context.BuoiTaps.Update(buoiTap);
                }

                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<DiemDanh?> GetActiveSessionAsync(int thanhVienId)
        {
            try
            {
                return await _unitOfWork.Context.DiemDanhs
                    .Where(d => d.ThanhVienId == thanhVienId &&
                               d.ThoiGianCheckIn.Date == DateTime.Today &&
                               d.ThoiGianCheckOut == null)
                    .OrderByDescending(d => d.ThoiGianCheckIn)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
