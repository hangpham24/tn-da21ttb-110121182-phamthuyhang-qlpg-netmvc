using Microsoft.EntityFrameworkCore;
using KLTN.Models.Database;

namespace KLTN.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<Quyen> Quyens { get; set; }
        public DbSet<ThanhVien> ThanhViens { get; set; }
        public DbSet<HuanLuyenVien> HuanLuyenViens { get; set; }
        public DbSet<DangKy> DangKys { get; set; }
        public DbSet<GiaHanDangKy> GiaHanDangKys { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<GoiTap> GoiTap { get; set; }
        public DbSet<LopHoc> LopHoc { get; set; }
        public DbSet<DichVu> DichVus { get; set; }
        public DbSet<PhienDay> PhienDays { get; set; }
        public DbSet<BangLuongPT> BangLuongPTs { get; set; }
        public DbSet<LichSuCheckIn> LichSuCheckIns { get; set; }
        public DbSet<KhachVangLai> KhachVangLais { get; set; }
        public DbSet<TinTuc> TinTucs { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<BaoCaoTaiChinh> BaoCaoTaiChinhs { get; set; }
        public DbSet<ThanhToan> ThanhToans { get; set; }
        public DbSet<DoanhThu> DoanhThus { get; set; }
        public DbSet<CapNhatAnhNhanDien> CapNhatAnhNhanDiens { get; set; }
        public DbSet<PT_PhanCongHoaHong> PT_PhanCongHoaHongs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình các index và unique constraint
            modelBuilder.Entity<TaiKhoan>()
                .HasIndex(t => t.TenDangNhap)
                .IsUnique();

            // Cấu hình mối quan hệ 1-1 giữa TaiKhoan và ThanhVien
            modelBuilder.Entity<TaiKhoan>()
                .HasOne(t => t.ThanhVien)
                .WithOne(tv => tv.TaiKhoan)
                .HasForeignKey<ThanhVien>(tv => tv.MaTK);

            // Cấu hình mối quan hệ 1-1 giữa TaiKhoan và HuanLuyenVien
            modelBuilder.Entity<TaiKhoan>()
                .HasOne(t => t.HuanLuyenVien)
                .WithOne(hlv => hlv.TaiKhoan)
                .HasForeignKey<HuanLuyenVien>(hlv => hlv.MaTK);

            // Cấu hình mối quan hệ 1-N giữa Quyen và TaiKhoan
            modelBuilder.Entity<Quyen>()
                .HasMany(q => q.TaiKhoans)
                .WithOne(t => t.Quyen)
                .HasForeignKey(t => t.MaQuyen);

            // Cấu hình mối quan hệ 1-N giữa ThanhVien và DangKy
            modelBuilder.Entity<ThanhVien>()
                .HasMany(tv => tv.DangKys)
                .WithOne(dk => dk.ThanhVien)
                .HasForeignKey(dk => dk.MaTV);

            // Cấu hình mối quan hệ 1-N giữa DangKy và ThanhToan
            modelBuilder.Entity<DangKy>()
                .HasMany(dk => dk.ThanhToans)
                .WithOne(tt => tt.DangKy)
                .HasForeignKey(tt => tt.MaDangKy);

            // Cấu hình mối quan hệ 1-N giữa DangKy và GiaHanDangKy
            modelBuilder.Entity<DangKy>()
                .HasMany(dk => dk.GiaHanDangKys)
                .WithOne(gh => gh.DangKy)
                .HasForeignKey(gh => gh.MaDangKy);

            // Cấu hình mối quan hệ 1-N giữa GiaHanDangKy và ThanhToan
            modelBuilder.Entity<GiaHanDangKy>()
                .HasMany(gh => gh.ThanhToans)
                .WithOne(tt => tt.GiaHanDangKy)
                .HasForeignKey(tt => tt.MaGiaHan);

            // Cấu hình mối quan hệ 1-N giữa TaiKhoan và ThanhToan (người thu)
            modelBuilder.Entity<TaiKhoan>()
                .HasMany(tk => tk.ThanhToansLap)
                .WithOne(tt => tt.NguoiThu)
                .HasForeignKey(tt => tt.MaTKNguoiThu);

            // Cấu hình mối quan hệ 1-N giữa TaiKhoan và GiaHanDangKy (người thu)
            modelBuilder.Entity<TaiKhoan>()
                .HasMany(tk => tk.GiaHanDangKysLap)
                .WithOne(gh => gh.NguoiThu)
                .HasForeignKey(gh => gh.MaTKNguoiThu);

            // Cấu hình mối quan hệ 1-N giữa TaiKhoan và CapNhatAnhNhanDien (người cập nhật)
            modelBuilder.Entity<TaiKhoan>()
                .HasMany(t => t.CapNhatAnhNhanDienThucHien)
                .WithOne(c => c.NguoiCapNhatTaiKhoan)
                .HasForeignKey(c => c.NguoiCapNhat)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình mối quan hệ 1-N giữa TaiKhoan và CapNhatAnhNhanDien (người được cập nhật)
            modelBuilder.Entity<TaiKhoan>()
                .HasMany(t => t.AnhNhanDienDuocCapNhat)
                .WithOne(c => c.TaiKhoan)
                .HasForeignKey(c => c.MaTK)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình UNIQUE constraint cho PT_PhanCongHoaHong
            modelBuilder.Entity<PT_PhanCongHoaHong>()
                .HasIndex(p => new { p.MaPT, p.MaGoiTap }).IsUnique();
            modelBuilder.Entity<PT_PhanCongHoaHong>()
                .HasIndex(p => new { p.MaPT, p.MaLopHoc }).IsUnique();

            // Cấu hình mối quan hệ 1-N giữa ThanhToan và DoanhThu
            modelBuilder.Entity<ThanhToan>()
                .HasMany(tt => tt.DoanhThus)
                .WithOne(dt => dt.ThanhToan)
                .HasForeignKey(dt => dt.MaThanhToan);
        }
        public DbSet<KLTN.Models.Database.PhienTap> PhienTap { get; set; } = default!;
        public DbSet<KLTN.Models.Database.DoanhThu> DoanhThu { get; set; } = default!;
    }
}
