using KLTN.Data;
using KLTN.Models.Database;
using KLTN.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace KLTN.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            // Lấy dịch vụ nổi bật (bao gồm thông tin liên kết từ GoiTap và LopHoc)
            viewModel.DichVuNoiBat = await _context.DichVus
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Where(d => !string.IsNullOrEmpty(d.HinhAnhURL))
                .OrderByDescending(d => d.GiaBatDau)
                .Take(3)
                .ToListAsync();

            // Lấy huấn luyện viên nổi bật (huấn luyện viên đang hoạt động, có kinh nghiệm, giới hạn 4 người)
            viewModel.HuanLuyenVienNoiBat = await _context.HuanLuyenViens
                .Where(h => h.TrangThaiHLV == "HoatDong" && h.KinhNghiem != null)
                .Take(4)
                .ToListAsync();

            // Lấy tin tức mới nhất (tin tức đang hiển thị, sắp xếp theo ngày đăng mới nhất, giới hạn 3 tin)
            viewModel.TinTucMoiNhat = await _context.TinTucs
                .Where(t => t.HienThi == true)
                .OrderByDescending(t => t.NgayDang)
                .Take(3)
                .ToListAsync();

            // Thống kê
            viewModel.SoGoiTap = await _context.GoiTap.CountAsync();
            viewModel.SoLopHoc = await _context.LopHoc.Where(l => l.TrangThai == "DangMo").CountAsync();
            viewModel.SoHuanLuyenVien = await _context.HuanLuyenViens.Where(h => h.TrangThaiHLV == "HoatDong").CountAsync();
            viewModel.SoThanhVien = await _context.ThanhViens.CountAsync();

            // Khuyến mãi đang áp dụng (nếu cần hiển thị)
            /*
            viewModel.KhuyenMaiDangApDung = await _context.KhuyenMais
                .Where(k => k.NgayBatDau <= DateTime.Now && k.NgayKetThuc >= DateTime.Now)
                .Take(2)
                .ToListAsync();
            */

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
