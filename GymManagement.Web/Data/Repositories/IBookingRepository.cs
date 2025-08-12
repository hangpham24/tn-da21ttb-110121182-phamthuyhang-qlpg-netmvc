using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<IEnumerable<Booking>> GetByThanhVienIdAsync(int thanhVienId);
        Task<IEnumerable<Booking>> GetByLopHocIdAsync(int lopHocId);
        Task<IEnumerable<Booking>> GetByLichLopIdAsync(int lichLopId);
        Task<IEnumerable<Booking>> GetBookingsByDateAsync(DateTime date);
        Task<IEnumerable<Booking>> GetActiveBookingsAsync();
        Task<int> CountBookingsForClassAsync(int lopHocId, DateTime date);
        Task<int> CountBookingsForScheduleAsync(int lichLopId);
        Task<bool> HasBookingAsync(int thanhVienId, int? lopHocId, int? lichLopId, DateTime date);
        Task<Booking?> GetActiveBookingAsync(int thanhVienId, int? lopHocId, int? lichLopId, DateTime date);
    }
}
