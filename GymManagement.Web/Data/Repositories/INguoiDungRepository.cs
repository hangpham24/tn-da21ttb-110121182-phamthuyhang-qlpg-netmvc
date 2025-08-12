using GymManagement.Web.Data.Models;

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
    }
}
