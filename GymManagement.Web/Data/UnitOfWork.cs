using GymManagement.Web.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace GymManagement.Web.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly GymDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repository instances
        private INguoiDungRepository? _nguoiDungs;
        private IGoiTapRepository? _goiTaps;
        private ILopHocRepository? _lopHocs;
        private IRepository<Models.VaiTro>? _vaiTros;
        private IRepository<Models.TaiKhoan>? _taiKhoans;

        private IRepository<Models.KhuyenMai>? _khuyenMais;
        private IRepository<Models.DangKy>? _dangKys;
        private IRepository<Models.ThanhToan>? _thanhToans;
        private IRepository<Models.ThanhToanGateway>? _thanhToanGateways;
        private IRepository<Models.Booking>? _bookings;
        private IRepository<Models.BuoiHlv>? _buoiHlvs;
        private IRepository<Models.BuoiTap>? _buoiTaps;
        private IRepository<Models.MauMat>? _mauMats;
        private IRepository<Models.DiemDanh>? _diemDanhs;
        private IRepository<Models.BangLuong>? _bangLuongs;
        private IRepository<Models.ThongBao>? _thongBaos;
        private IRepository<Models.LichSuAnh>? _lichSuAnhs;

        public UnitOfWork(GymDbContext context)
        {
            _context = context;
        }

        // DbContext access
        public GymDbContext Context => _context;

        // Repository properties
        public INguoiDungRepository NguoiDungs => 
            _nguoiDungs ??= new NguoiDungRepository(_context);

        public IGoiTapRepository GoiTaps => 
            _goiTaps ??= new GoiTapRepository(_context);

        public ILopHocRepository LopHocs => 
            _lopHocs ??= new LopHocRepository(_context);

        public IRepository<Models.VaiTro> VaiTros => 
            _vaiTros ??= new Repository<Models.VaiTro>(_context);

        public IRepository<Models.TaiKhoan> TaiKhoans => 
            _taiKhoans ??= new Repository<Models.TaiKhoan>(_context);



        public IRepository<Models.KhuyenMai> KhuyenMais => 
            _khuyenMais ??= new Repository<Models.KhuyenMai>(_context);

        public IRepository<Models.DangKy> DangKys => 
            _dangKys ??= new Repository<Models.DangKy>(_context);

        public IRepository<Models.ThanhToan> ThanhToans => 
            _thanhToans ??= new Repository<Models.ThanhToan>(_context);

        public IRepository<Models.ThanhToanGateway> ThanhToanGateways => 
            _thanhToanGateways ??= new Repository<Models.ThanhToanGateway>(_context);

        public IRepository<Models.Booking> Bookings => 
            _bookings ??= new Repository<Models.Booking>(_context);

        public IRepository<Models.BuoiHlv> BuoiHlvs => 
            _buoiHlvs ??= new Repository<Models.BuoiHlv>(_context);

        public IRepository<Models.BuoiTap> BuoiTaps => 
            _buoiTaps ??= new Repository<Models.BuoiTap>(_context);

        public IRepository<Models.MauMat> MauMats => 
            _mauMats ??= new Repository<Models.MauMat>(_context);

        public IRepository<Models.DiemDanh> DiemDanhs =>
            _diemDanhs ??= new Repository<Models.DiemDanh>(_context);

        public IRepository<Models.BangLuong> BangLuongs =>
            _bangLuongs ??= new Repository<Models.BangLuong>(_context);

        public IRepository<Models.ThongBao> ThongBaos => 
            _thongBaos ??= new Repository<Models.ThongBao>(_context);

        public IRepository<Models.LichSuAnh> LichSuAnhs => 
            _lichSuAnhs ??= new Repository<Models.LichSuAnh>(_context);

        // Transaction methods
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
