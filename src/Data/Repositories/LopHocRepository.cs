using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class LopHocRepository : Repository<LopHoc>, ILopHocRepository
    {
        public LopHocRepository(GymDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get all classes with optimized loading
        /// </summary>
        public override async Task<IEnumerable<LopHoc>> GetAllAsync()
        {
            return await _dbSet
                .Include(x => x.Hlv)
                .Include(x => x.DangKys.Where(d => d.TrangThai == "ACTIVE"))
                    .ThenInclude(d => d.NguoiDung)
                .AsSplitQuery() // Prevent cartesian explosion
                .OrderBy(x => x.TenLop)
                .ToListAsync();
        }

        /// <summary>
        /// Get active classes with optimized loading
        /// </summary>
        public async Task<IEnumerable<LopHoc>> GetActiveClassesAsync()
        {
            return await _dbSet
                .Where(x => x.TrangThai == "OPEN")
                .Include(x => x.Hlv)
                .Include(x => x.DangKys.Where(d => d.TrangThai == "ACTIVE"))
                    .ThenInclude(d => d.NguoiDung)
                .AsSplitQuery() // Prevent cartesian explosion
                .OrderBy(x => x.TenLop)
                .ToListAsync();
        }

        /// <summary>
        /// Get class by ID with all related data
        /// </summary>
        public override async Task<LopHoc?> GetByIdAsync(int id)
        {
            return await _context.LopHocs
                .Include(x => x.Hlv)
                .Include(x => x.DangKys.Where(d => d.TrangThai == "ACTIVE"))
                    .ThenInclude(d => d.NguoiDung)

                .Include(x => x.Bookings.Where(b => b.Ngay >= DateOnly.FromDateTime(DateTime.Today)))
                .Include(x => x.BuoiTaps)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.LopHocId == id);
        }

        /// <summary>
        /// Get classes by trainer
        /// </summary>
        public async Task<IEnumerable<LopHoc>> GetByHuanLuyenVienAsync(int hlvId)
        {
            return await _dbSet
                .Where(x => x.HlvId == hlvId)
                .Include(x => x.Hlv)
                .Include(x => x.DangKys.Where(d => d.TrangThai == "ACTIVE"))
                .OrderBy(x => x.TenLop)
                .ToListAsync();
        }

        /// <summary>
        /// Get classes by day of week
        /// </summary>
        public async Task<IEnumerable<LopHoc>> GetByThuTrongTuanAsync(string thuTrongTuan)
        {
            return await _dbSet
                .Where(x => x.ThuTrongTuan.Contains(thuTrongTuan))
                .Include(x => x.Hlv)
                .OrderBy(x => x.GioBatDau)
                .ToListAsync();
        }

        /// <summary>
        /// Get active classes with minimal data for performance
        /// </summary>
        public async Task<IEnumerable<LopHoc>> GetActiveClassesWithDetailsAsync()
        {
            return await _dbSet
                .Where(x => x.TrangThai == "OPEN")
                .Select(x => new LopHoc
                {
                    LopHocId = x.LopHocId,
                    TenLop = x.TenLop,
                    HlvId = x.HlvId,
                    SucChua = x.SucChua,
                    GioBatDau = x.GioBatDau,
                    GioKetThuc = x.GioKetThuc,
                    ThuTrongTuan = x.ThuTrongTuan,
                    GiaTuyChinh = x.GiaTuyChinh,
                    TrangThai = x.TrangThai,
                    MoTa = x.MoTa,
                    ThoiLuong = x.ThoiLuong,
                    NgayBatDauKhoa = x.NgayBatDauKhoa,
                    NgayKetThucKhoa = x.NgayKetThucKhoa,
                    LoaiDangKy = x.LoaiDangKy,
                    Hlv = x.Hlv != null ? new NguoiDung
                    {
                        NguoiDungId = x.Hlv.NguoiDungId,
                        Ho = x.Hlv.Ho,
                        Ten = x.Hlv.Ten,
                        Email = x.Hlv.Email,
                        TrangThai = x.Hlv.TrangThai
                    } : null,
                    DangKys = x.DangKys
                        .Where(d => d.TrangThai == "ACTIVE" && d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today))
                        .Select(d => new DangKy
                        {
                            DangKyId = d.DangKyId,
                            NguoiDungId = d.NguoiDungId,
                            TrangThai = d.TrangThai,
                            NgayBatDau = d.NgayBatDau,
                            NgayKetThuc = d.NgayKetThuc
                        }).ToList()
                })
                .OrderBy(x => x.TenLop)
                .ToListAsync();
        }

        /// <summary>
        /// Get available classes for a specific date
        /// </summary>
        public async Task<IEnumerable<LopHoc>> GetAvailableClassesAsync(DateOnly date)
        {
            var dayOfWeek = GetVietnameseDayOfWeek(date.DayOfWeek);
            
            return await _dbSet
                .Where(x => x.TrangThai == "OPEN" && x.ThuTrongTuan.Contains(dayOfWeek))
                .Include(x => x.Hlv)
                .Include(x => x.DangKys.Where(d => d.TrangThai == "ACTIVE"))
                .Where(x => x.DangKys.Count(d => d.TrangThai == "ACTIVE") < x.SucChua)
                .ToListAsync();
        }



        /// <summary>
        /// Get classes with available slots
        /// </summary>
        public async Task<IEnumerable<LopHoc>> GetClassesWithAvailableSlotsAsync()
        {
            return await _dbSet
                .Where(x => x.TrangThai == "OPEN")
                .Include(x => x.Hlv)
                .Include(x => x.DangKys.Where(d => d.TrangThai == "ACTIVE"))
                .Where(x => x.DangKys.Count(d => d.TrangThai == "ACTIVE") < x.SucChua)
                .OrderBy(x => x.TenLop)
                .ToListAsync();
        }

        /// <summary>
        /// Get classes by trainer ID
        /// </summary>
        public async Task<IEnumerable<LopHoc>> GetClassesByTrainerAsync(int trainerId)
        {
            return await _dbSet
                .Where(x => x.HlvId == trainerId)
                .Include(x => x.DangKys.Where(d => d.TrangThai == "ACTIVE"))
                .OrderBy(x => x.TenLop)
                .ToListAsync();
        }

        /// <summary>
        /// Helper method to convert DayOfWeek to Vietnamese
        /// </summary>
        private string GetVietnameseDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
        }
    }
}
