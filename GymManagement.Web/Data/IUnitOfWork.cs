using GymManagement.Web.Data.Repositories;

namespace GymManagement.Web.Data
{
    public interface IUnitOfWork : IDisposable
    {
        // DbContext access
        GymDbContext Context { get; }

        // Repositories
        INguoiDungRepository NguoiDungs { get; }
        IGoiTapRepository GoiTaps { get; }
        ILopHocRepository LopHocs { get; }
        IRepository<Models.VaiTro> VaiTros { get; }
        IRepository<Models.TaiKhoan> TaiKhoans { get; }
        IRepository<Models.LichLop> LichLops { get; }
        IRepository<Models.KhuyenMai> KhuyenMais { get; }
        IRepository<Models.DangKy> DangKys { get; }
        IRepository<Models.ThanhToan> ThanhToans { get; }
        IRepository<Models.ThanhToanGateway> ThanhToanGateways { get; }
        IRepository<Models.Booking> Bookings { get; }
        IRepository<Models.BuoiHlv> BuoiHlvs { get; }
        IRepository<Models.BuoiTap> BuoiTaps { get; }
        IRepository<Models.MauMat> MauMats { get; }
        IRepository<Models.DiemDanh> DiemDanhs { get; }
        IRepository<Models.CauHinhHoaHong> CauHinhHoaHongs { get; }
        IRepository<Models.BangLuong> BangLuongs { get; }
        IRepository<Models.ThongBao> ThongBaos { get; }
        IRepository<Models.LichSuAnh> LichSuAnhs { get; }

        // Transaction methods
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
