using Microsoft.EntityFrameworkCore;
using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data
{
    public class GymDbContext : DbContext
    {
        public GymDbContext(DbContextOptions<GymDbContext> options) : base(options)
        {
        }

        // Authentication
        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<VaiTro> VaiTros { get; set; }
        public DbSet<TaiKhoanVaiTro> TaiKhoanVaiTros { get; set; }
        public DbSet<ExternalLogin> ExternalLogins { get; set; }

        // User Management
        public DbSet<NguoiDung> NguoiDungs { get; set; }

        // Sản phẩm - Khuyến mãi
        public DbSet<GoiTap> GoiTaps { get; set; }
        public DbSet<LopHoc> LopHocs { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }

        // Đăng ký - Thanh toán
        public DbSet<DangKy> DangKys { get; set; }
        public DbSet<ThanhToan> ThanhToans { get; set; }
        public DbSet<ThanhToanGateway> ThanhToanGateways { get; set; }

        // Đặt chỗ
        public DbSet<Booking> Bookings { get; set; }

        // Hoạt động - Check-in
        public DbSet<BuoiHlv> BuoiHlvs { get; set; }
        public DbSet<BuoiTap> BuoiTaps { get; set; }
        public DbSet<MauMat> MauMats { get; set; }
        public DbSet<DiemDanh> DiemDanhs { get; set; }

        // Lương
        public DbSet<BangLuong> BangLuongs { get; set; }

        // Hệ thống
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<LichSuAnh> LichSuAnhs { get; set; }
        public DbSet<TinTuc> TinTucs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Authentication tables
            modelBuilder.Entity<TaiKhoan>().ToTable("TaiKhoan");
            modelBuilder.Entity<VaiTro>().ToTable("VaiTro");
            modelBuilder.Entity<TaiKhoanVaiTro>().ToTable("TaiKhoanVaiTros");
            modelBuilder.Entity<ExternalLogin>().ToTable("ExternalLogins");

            // Cấu hình bảng TaiKhoan
            modelBuilder.Entity<TaiKhoan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TenDangNhap).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
                entity.Property(e => e.MatKhauHash).IsRequired();
                entity.Property(e => e.Salt).IsRequired();
                entity.HasIndex(e => e.TenDangNhap).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.KichHoat).HasDefaultValue(true);
                entity.Property(e => e.NgayTao).HasDefaultValueSql("GETUTCDATE()");
            });

            // Cấu hình bảng VaiTro
            modelBuilder.Entity<VaiTro>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TenVaiTro).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.TenVaiTro).IsUnique();
                entity.Property(e => e.MoTa).HasMaxLength(500);
                entity.Property(e => e.NgayTao).HasDefaultValueSql("GETUTCDATE()");
            });

            // Cấu hình bảng TaiKhoanVaiTro
            modelBuilder.Entity<TaiKhoanVaiTro>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TaiKhoanId, e.VaiTroId }).IsUnique();
                entity.HasOne(e => e.TaiKhoan)
                    .WithMany(e => e.TaiKhoanVaiTros)
                    .HasForeignKey(e => e.TaiKhoanId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.VaiTro)
                    .WithMany(e => e.TaiKhoanVaiTros)
                    .HasForeignKey(e => e.VaiTroId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.NgayGan).HasDefaultValueSql("GETUTCDATE()");
            });

            // Cấu hình bảng ExternalLogin
            modelBuilder.Entity<ExternalLogin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
                entity.Property(e => e.Provider).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ProviderKey).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ProviderDisplayName).HasMaxLength(200);
                entity.HasOne(e => e.TaiKhoan)
                    .WithMany(e => e.ExternalLogins)
                    .HasForeignKey(e => e.TaiKhoanId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.NgayTao).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure relationship between TaiKhoan and NguoiDung
            modelBuilder.Entity<TaiKhoan>()
                .HasOne(t => t.NguoiDung)
                .WithOne(n => n.TaiKhoan)
                .HasForeignKey<TaiKhoan>(t => t.NguoiDungId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình bảng NguoiDung
            modelBuilder.Entity<NguoiDung>(entity =>
            {
                entity.HasKey(e => e.NguoiDungId);
                entity.Property(e => e.NguoiDungId).ValueGeneratedOnAdd();
                entity.Property(e => e.LoaiNguoiDung).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Ho).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Ten).HasMaxLength(50);
                entity.Property(e => e.GioiTinh).HasMaxLength(10);
                entity.Property(e => e.SoDienThoai).HasMaxLength(15);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.NgayThamGia).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("ACTIVE");
            });



            // Cấu hình bảng GoiTap
            modelBuilder.Entity<GoiTap>(entity =>
            {
                entity.HasKey(e => e.GoiTapId);
                entity.Property(e => e.GoiTapId).ValueGeneratedOnAdd();
                entity.Property(e => e.TenGoi).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Gia).HasColumnType("decimal(12,2)").IsRequired();
                entity.Property(e => e.MoTa).HasMaxLength(500);
            });

            // Cấu hình bảng LopHoc
            modelBuilder.Entity<LopHoc>(entity =>
            {
                entity.HasKey(e => e.LopHocId);
                entity.Property(e => e.LopHocId).ValueGeneratedOnAdd();
                entity.Property(e => e.TenLop).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ThuTrongTuan).HasMaxLength(50).IsRequired();
                entity.Property(e => e.GiaTuyChinh).HasColumnType("decimal(12,2)");
                entity.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("OPEN");
                
                entity.HasOne(d => d.Hlv)
                    .WithMany(p => p.LopHocs)
                    .HasForeignKey(d => d.HlvId);
            });



            // Cấu hình bảng KhuyenMai
            modelBuilder.Entity<KhuyenMai>(entity =>
            {
                entity.HasKey(e => e.KhuyenMaiId);
                entity.Property(e => e.KhuyenMaiId).ValueGeneratedOnAdd();
                entity.Property(e => e.MaCode).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.MaCode).IsUnique();
                entity.Property(e => e.MoTa).HasMaxLength(300);
                entity.Property(e => e.KichHoat).HasDefaultValue(true);
            });

            // Cấu hình bảng DangKy
            modelBuilder.Entity<DangKy>(entity =>
            {
                entity.HasKey(e => e.DangKyId);
                entity.Property(e => e.DangKyId).ValueGeneratedOnAdd();
                entity.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("ACTIVE");
                entity.Property(e => e.NgayTao).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.PhiDangKy).HasColumnType("decimal(12,2)");

                entity.HasOne(d => d.NguoiDung)
                    .WithMany(p => p.DangKys)
                    .HasForeignKey(d => d.NguoiDungId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.GoiTap)
                    .WithMany(p => p.DangKys)
                    .HasForeignKey(d => d.GoiTapId);

                entity.HasOne(d => d.LopHoc)
                    .WithMany(p => p.DangKys)
                    .HasForeignKey(d => d.LopHocId);
            });

            // Cấu hình bảng ThanhToan
            modelBuilder.Entity<ThanhToan>(entity =>
            {
                entity.HasKey(e => e.ThanhToanId);
                entity.Property(e => e.ThanhToanId).ValueGeneratedOnAdd();
                entity.Property(e => e.SoTien).HasColumnType("decimal(12,2)").IsRequired();
                entity.Property(e => e.NgayThanhToan).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.PhuongThuc).HasMaxLength(20);
                entity.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("PENDING");
                entity.Property(e => e.GhiChu).HasMaxLength(200);
                
                entity.HasOne(d => d.DangKy)
                    .WithMany(p => p.ThanhToans)
                    .HasForeignKey(d => d.DangKyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Cấu hình bảng ThanhToanGateway
            modelBuilder.Entity<ThanhToanGateway>(entity =>
            {
                entity.HasKey(e => e.GatewayId);
                entity.Property(e => e.GatewayId).ValueGeneratedOnAdd();
                entity.Property(e => e.GatewayTen).HasMaxLength(30).HasDefaultValue("VNPAY");
                entity.Property(e => e.GatewayTransId).HasMaxLength(100);
                entity.Property(e => e.GatewayOrderId).HasMaxLength(100);
                entity.Property(e => e.GatewayAmount).HasColumnType("decimal(12,2)");
                entity.Property(e => e.GatewayRespCode).HasMaxLength(10);
                entity.Property(e => e.GatewayMessage).HasMaxLength(255);
                
                entity.HasOne(d => d.ThanhToan)
                    .WithOne(p => p.ThanhToanGateway)
                    .HasForeignKey<ThanhToanGateway>(d => d.ThanhToanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình bảng Booking
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.BookingId);
                entity.Property(e => e.BookingId).ValueGeneratedOnAdd();
                entity.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("BOOKED");
                
                entity.HasOne(d => d.ThanhVien)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.ThanhVienId);
                    
                entity.HasOne(d => d.LopHoc)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.LopHocId);
            });

            // Cấu hình bảng BuoiHlv
            modelBuilder.Entity<BuoiHlv>(entity =>
            {
                entity.HasKey(e => e.BuoiHlvId);
                entity.Property(e => e.BuoiHlvId).ValueGeneratedOnAdd();
                entity.Property(e => e.GhiChu).HasMaxLength(300);
                
                entity.HasOne(d => d.Hlv)
                    .WithMany(p => p.BuoiHlvs)
                    .HasForeignKey(d => d.HlvId);
                    
                entity.HasOne(d => d.ThanhVien)
                    .WithMany(p => p.BuoiHlvThanhViens)
                    .HasForeignKey(d => d.ThanhVienId);
                    
                entity.HasOne(d => d.LopHoc)
                    .WithMany(p => p.BuoiHlvs)
                    .HasForeignKey(d => d.LopHocId);
            });

            // Cấu hình bảng BuoiTap
            modelBuilder.Entity<BuoiTap>(entity =>
            {
                entity.HasKey(e => e.BuoiTapId);
                entity.Property(e => e.BuoiTapId).ValueGeneratedOnAdd();
                entity.Property(e => e.GhiChu).HasMaxLength(200);
                
                entity.HasOne(d => d.ThanhVien)
                    .WithMany(p => p.BuoiTaps)
                    .HasForeignKey(d => d.ThanhVienId);
                    
                entity.HasOne(d => d.LopHoc)
                    .WithMany(p => p.BuoiTaps)
                    .HasForeignKey(d => d.LopHocId);
            });

            // Cấu hình bảng MauMat
            modelBuilder.Entity<MauMat>(entity =>
            {
                entity.HasKey(e => e.MauMatId);
                entity.Property(e => e.MauMatId).ValueGeneratedOnAdd();
                entity.Property(e => e.NgayTao).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.ThuatToan).HasMaxLength(50).HasDefaultValue("ArcFace");
                
                entity.HasOne(d => d.NguoiDung)
                    .WithOne(p => p.MauMat)
                    .HasForeignKey<MauMat>(d => d.NguoiDungId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình bảng DiemDanh
            modelBuilder.Entity<DiemDanh>(entity =>
            {
                entity.HasKey(e => e.DiemDanhId);
                entity.Property(e => e.DiemDanhId).ValueGeneratedOnAdd();
                entity.Property(e => e.ThoiGian).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.AnhMinhChung).HasMaxLength(255);

                entity.HasOne(d => d.ThanhVien)
                    .WithMany(p => p.DiemDanhs)
                    .HasForeignKey(d => d.ThanhVienId);

                entity.HasOne(d => d.LopHoc)
                    .WithMany(p => p.DiemDanhs)
                    .HasForeignKey(d => d.LopHocId);
            });



            // Cấu hình bảng BangLuong
            modelBuilder.Entity<BangLuong>(entity =>
            {
                entity.HasKey(e => e.BangLuongId);
                entity.Property(e => e.BangLuongId).ValueGeneratedOnAdd();
                entity.Property(e => e.Thang).HasMaxLength(7).IsRequired();
                entity.Property(e => e.LuongCoBan).HasColumnType("decimal(12,2)").IsRequired();
                entity.Property(e => e.TienHoaHong).HasColumnType("decimal(12,2)").HasDefaultValue(0);
                
                entity.HasOne(d => d.Hlv)
                    .WithMany(p => p.BangLuongs)
                    .HasForeignKey(d => d.HlvId);
            });

            // Cấu hình bảng ThongBao
            modelBuilder.Entity<ThongBao>(entity =>
            {
                entity.HasKey(e => e.ThongBaoId);
                entity.Property(e => e.ThongBaoId).ValueGeneratedOnAdd();
                entity.Property(e => e.TieuDe).HasMaxLength(100);
                entity.Property(e => e.NoiDung).HasMaxLength(1000);
                entity.Property(e => e.NgayTao).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Kenh).HasMaxLength(10);
                entity.Property(e => e.DaDoc).HasDefaultValue(false);
                
                entity.HasOne(d => d.NguoiDung)
                    .WithMany(p => p.ThongBaos)
                    .HasForeignKey(d => d.NguoiDungId);
            });

            // Cấu hình bảng LichSuAnh
            modelBuilder.Entity<LichSuAnh>(entity =>
            {
                entity.HasKey(e => e.LichSuAnhId);
                entity.Property(e => e.LichSuAnhId).ValueGeneratedOnAdd();
                entity.Property(e => e.AnhCu).HasMaxLength(255);
                entity.Property(e => e.AnhMoi).HasMaxLength(255);
                entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LyDo).HasMaxLength(200);
                
                entity.HasOne(d => d.NguoiDung)
                    .WithMany(p => p.LichSuAnhs)
                    .HasForeignKey(d => d.NguoiDungId);
            });

            // Cấu hình bảng TinTuc
            modelBuilder.Entity<TinTuc>(entity =>
            {
                entity.HasKey(e => e.TinTucId);
                entity.Property(e => e.TinTucId).ValueGeneratedOnAdd();
                entity.Property(e => e.TieuDe).HasMaxLength(200).IsRequired();
                entity.Property(e => e.MoTaNgan).HasMaxLength(500).IsRequired();
                entity.Property(e => e.NoiDung).IsRequired();
                entity.Property(e => e.AnhDaiDien).HasMaxLength(500);
                entity.Property(e => e.NgayTao).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.TenTacGia).HasMaxLength(100);
                entity.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("DRAFT");
                entity.Property(e => e.Slug).HasMaxLength(200);
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.Property(e => e.MetaTitle).HasMaxLength(160);
                entity.Property(e => e.MetaDescription).HasMaxLength(160);
                entity.Property(e => e.MetaKeywords).HasMaxLength(500);
                
                entity.HasOne(d => d.TacGia)
                    .WithMany()
                    .HasForeignKey(d => d.TacGiaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
