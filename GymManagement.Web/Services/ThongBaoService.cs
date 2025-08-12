using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Services
{
    public class ThongBaoService : IThongBaoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IThongBaoRepository _thongBaoRepository;
        private readonly IEmailService _emailService;

        public ThongBaoService(
            IUnitOfWork unitOfWork,
            IThongBaoRepository thongBaoRepository,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _thongBaoRepository = thongBaoRepository;
            _emailService = emailService;
        }

        public async Task<IEnumerable<ThongBao>> GetAllAsync()
        {
            return await _thongBaoRepository.GetAllAsync();
        }

        public async Task<ThongBao?> GetByIdAsync(int id)
        {
            return await _thongBaoRepository.GetByIdAsync(id);
        }

        public async Task<ThongBao> CreateAsync(ThongBao thongBao)
        {
            var created = await _thongBaoRepository.AddAsync(thongBao);
            await _unitOfWork.SaveChangesAsync();
            return created;
        }

        public async Task<ThongBao> UpdateAsync(ThongBao thongBao)
        {
            await _thongBaoRepository.UpdateAsync(thongBao);
            await _unitOfWork.SaveChangesAsync();
            return thongBao;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var thongBao = await _thongBaoRepository.GetByIdAsync(id);
            if (thongBao == null) return false;

            await _thongBaoRepository.DeleteAsync(thongBao);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ThongBao>> GetByUserIdAsync(int nguoiDungId)
        {
            return await _thongBaoRepository.GetByNguoiDungIdAsync(nguoiDungId);
        }

        public async Task<IEnumerable<ThongBao>> GetUnreadByUserIdAsync(int nguoiDungId)
        {
            return await _thongBaoRepository.GetUnreadByNguoiDungIdAsync(nguoiDungId);
        }

        public async Task<int> CountUnreadNotificationsAsync(int nguoiDungId)
        {
            return await _thongBaoRepository.CountUnreadNotificationsAsync(nguoiDungId);
        }

        public async Task<bool> MarkAsReadAsync(int thongBaoId)
        {
            await _thongBaoRepository.MarkAsReadAsync(thongBaoId);
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int nguoiDungId)
        {
            await _thongBaoRepository.MarkAllAsReadAsync(nguoiDungId);
            return true;
        }

        public async Task<ThongBao> CreateNotificationAsync(int nguoiDungId, string tieuDe, string noiDung, string kenh)
        {
            var thongBao = new ThongBao
            {
                NguoiDungId = nguoiDungId,
                TieuDe = tieuDe,
                NoiDung = noiDung,
                Kenh = kenh,
                NgayTao = DateTime.Now,
                DaDoc = false
            };

            var created = await _thongBaoRepository.AddAsync(thongBao);
            await _unitOfWork.SaveChangesAsync();

            // Send email if channel is EMAIL
            if (kenh == "EMAIL")
            {
                var nguoiDung = await _unitOfWork.Context.NguoiDungs.FindAsync(nguoiDungId);
                if (nguoiDung != null && !string.IsNullOrEmpty(nguoiDung.Email))
                {
                    await _emailService.SendEmailAsync(nguoiDung.Email, tieuDe, noiDung);
                }
            }

            return created;
        }

        public async Task SendBulkNotificationAsync(IEnumerable<int> nguoiDungIds, string tieuDe, string noiDung, string kenh)
        {
            var thongBaos = new List<ThongBao>();
            var emailTasks = new List<Task>();

            foreach (var nguoiDungId in nguoiDungIds)
            {
                var thongBao = new ThongBao
                {
                    NguoiDungId = nguoiDungId,
                    TieuDe = tieuDe,
                    NoiDung = noiDung,
                    Kenh = kenh,
                    NgayTao = DateTime.Now,
                    DaDoc = false
                };

                thongBaos.Add(thongBao);

                // Prepare email sending if channel is EMAIL
                if (kenh == "EMAIL")
                {
                    var nguoiDung = await _unitOfWork.Context.NguoiDungs.FindAsync(nguoiDungId);
                    if (nguoiDung != null && !string.IsNullOrEmpty(nguoiDung.Email))
                    {
                        emailTasks.Add(_emailService.SendEmailAsync(nguoiDung.Email, tieuDe, noiDung));
                    }
                }
            }

            // Save all notifications
            await _unitOfWork.Context.ThongBaos.AddRangeAsync(thongBaos);
            await _unitOfWork.SaveChangesAsync();

            // Send all emails concurrently
            if (emailTasks.Any())
            {
                await Task.WhenAll(emailTasks);
            }
        }

        public async Task SendNotificationToAllMembersAsync(string tieuDe, string noiDung, string kenh)
        {
            var members = await _unitOfWork.Context.NguoiDungs
                .Where(n => n.LoaiNguoiDung == "THANHVIEN" && n.TrangThai == "ACTIVE")
                .Select(n => n.NguoiDungId)
                .ToListAsync();

            await SendBulkNotificationAsync(members, tieuDe, noiDung, kenh);
        }

        public async Task<bool> DeleteOldNotificationsAsync(int daysOld = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysOld);
                var allNotifications = await _thongBaoRepository.GetAllAsync();
                var oldNotifications = allNotifications.Where(n => n.NgayTao < cutoffDate);
                
                foreach (var notification in oldNotifications)
                {
                    await _thongBaoRepository.DeleteAsync(notification);
                }
                
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<ThongBao>> GetRecentNotificationsAsync(int nguoiDungId, int count = 5)
        {
            var allNotifications = await GetByUserIdAsync(nguoiDungId);
            return allNotifications.Take(count);
        }
    }
}
