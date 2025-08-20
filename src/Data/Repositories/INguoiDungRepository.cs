using GymManagement.Web.Data.Models;
using System.Linq.Expressions;

namespace GymManagement.Web.Data.Repositories
{
    public interface INguoiDungRepository : IRepository<NguoiDung>
    {
        Task<IEnumerable<NguoiDung>> GetByLoaiNguoiDungAsync(string loaiNguoiDung);
        Task<NguoiDung?> GetByEmailAsync(string email);
        Task<NguoiDung?> GetBySoDienThoaiAsync(string soDienThoai);
        Task<IEnumerable<NguoiDung>> GetActiveUsersAsync();
        Task<IEnumerable<NguoiDung>> GetHuanLuyenViensAsync();
        Task<IEnumerable<NguoiDung>> GetThanhViensAsync();
        Task<IEnumerable<NguoiDung>> GetMembersAsync();
        Task<IEnumerable<NguoiDung>> GetTrainersAsync();
        Task<NguoiDung?> GetWithTaiKhoanAsync(int nguoiDungId);
        Task<IEnumerable<NguoiDung>> GetAllWithTaiKhoanAsync();
        Task<(IEnumerable<NguoiDung> Items, int TotalCount)> GetPagedWithTaiKhoanAsync(
            int pageNumber, int pageSize,
            Expression<Func<NguoiDung, bool>>? filter = null,
            Func<IQueryable<NguoiDung>, IOrderedQueryable<NguoiDung>>? orderBy = null);
    }
}
