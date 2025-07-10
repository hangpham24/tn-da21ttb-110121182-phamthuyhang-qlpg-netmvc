using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KLTN.Models;
using System.Threading.Tasks;
using KLTN.Data;
using KLTN.Models.Database;
using Microsoft.AspNetCore.Authorization;
using KLTN.Models.ViewModels;

namespace KLTN.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            // Tạo viewmodel cho dashboard
            var viewModel = new AdminDashboardViewModel
            {
                TotalMembers = await _context.ThanhViens.CountAsync(),
                TotalTrainers = await _context.HuanLuyenViens.CountAsync(),
                TotalUsers = await _context.TaiKhoans.CountAsync(),
                TotalPackages = await _context.GoiTap.CountAsync(),
                TotalClasses = await _context.LopHoc.CountAsync(),
                TotalRegistrations = await _context.DangKys.CountAsync(),
                RecentRegistrations = await _context.DangKys
                    .Include(d => d.ThanhVien)
                    .Include(d => d.KhachVangLai)
                    .Include(d => d.GoiTap)
                    .Include(d => d.LopHoc)
                    .OrderByDescending(d => d.NgayDangKy)
                    .Take(5)
                    .ToListAsync(),
                TotalRevenue = await _context.ThanhToans.SumAsync(t => t.SoTien)
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Registrations()
        {
            var registrations = await _context.DangKys
                .Include(d => d.ThanhVien)
                .Include(d => d.KhachVangLai)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .OrderByDescending(d => d.NgayDangKy)
                .ToListAsync();

            return View(registrations);
        }

        public IActionResult Finance()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Finance(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParam"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["TypeSortParam"] = sortOrder == "type" ? "type_desc" : "type";
            ViewData["AmountSortParam"] = sortOrder == "amount" ? "amount_desc" : "amount";
            ViewData["StatusSortParam"] = sortOrder == "status" ? "status_desc" : "status";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var query = _context.ThanhToans
                .Include(t => t.DangKy)
                    .ThenInclude(dk => dk.ThanhVien)
                .Include(t => t.DangKy)
                    .ThenInclude(dk => dk.KhachVangLai)
                .Include(t => t.DangKy)
                    .ThenInclude(dk => dk.GoiTap)
                .Include(t => t.DangKy)
                    .ThenInclude(dk => dk.LopHoc)
                .Include(t => t.NguoiThu)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                query = query.Where(t =>
                    (t.DangKy.ThanhVien != null && t.DangKy.ThanhVien.HoTen.Contains(searchString)) ||
                    (t.DangKy.KhachVangLai != null && t.DangKy.KhachVangLai.HoTen.Contains(searchString)) ||
                    (t.NguoiThu != null && t.NguoiThu.TenDangNhap.Contains(searchString)) ||
                    (t.GhiChu != null && t.GhiChu.Contains(searchString)));
            }

            switch (sortOrder)
            {
                case "date_desc":
                    query = query.OrderByDescending(t => t.NgayThanhToan);
                    break;
                case "type":
                    query = query.OrderBy(t => t.LoaiThanhToan);
                    break;
                case "type_desc":
                    query = query.OrderByDescending(t => t.LoaiThanhToan);
                    break;
                case "amount":
                    query = query.OrderBy(t => t.SoTien);
                    break;
                case "amount_desc":
                    query = query.OrderByDescending(t => t.SoTien);
                    break;
                case "status":
                    query = query.OrderBy(t => t.TrangThai);
                    break;
                case "status_desc":
                    query = query.OrderByDescending(t => t.TrangThai);
                    break;
                default:
                    query = query.OrderBy(t => t.NgayThanhToan);
                    break;
            }

            int pageSize = 10;
            return View(await PaginatedList<ThanhToan>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // Phương thức GET để hiển thị form chuẩn hóa quyền
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult NormalizeRoles()
        {
            return View();
        }

        // Phương thức POST để thực hiện chuẩn hóa quyền
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> NormalizeRoles(bool confirm = true)
        {
            try
            {
                // Tìm tất cả quyền trong cơ sở dữ liệu
                var allRoles = await _context.Quyens.ToListAsync();
                
                // Tìm quyền "ThanhVien" và "Thành viên"
                var thanhVienRole = allRoles.FirstOrDefault(r => r.TenQuyen == "ThanhVien");
                var thanhVienSpaceRole = allRoles.FirstOrDefault(r => r.TenQuyen == "Thành viên");
                
                // Tìm quyền "HuanLuyenVien" và "Huấn luyện viên"
                var huanLuyenVienRole = allRoles.FirstOrDefault(r => r.TenQuyen == "HuanLuyenVien");
                var huanLuyenVienSpaceRole = allRoles.FirstOrDefault(r => r.TenQuyen == "Huấn luyện viên");
                
                // Nếu cả hai quyền ThanhVien tồn tại, hợp nhất chúng
                if (thanhVienRole != null && thanhVienSpaceRole != null)
                {
                    // Lấy tất cả tài khoản có quyền "ThanhVien"
                    var thanhVienAccounts = await _context.TaiKhoans
                        .Where(t => t.MaQuyen == thanhVienRole.MaQuyen)
                        .ToListAsync();
                    
                    // Cập nhật tất cả tài khoản để sử dụng quyền "Thành viên"
                    foreach (var account in thanhVienAccounts)
                    {
                        account.MaQuyen = thanhVienSpaceRole.MaQuyen;
                    }
                    
                    // Xóa quyền "ThanhVien"
                    _context.Quyens.Remove(thanhVienRole);
                }
                
                // Nếu cả hai quyền HuanLuyenVien tồn tại, hợp nhất chúng
                if (huanLuyenVienRole != null && huanLuyenVienSpaceRole != null)
                {
                    // Lấy tất cả tài khoản có quyền "HuanLuyenVien"
                    var huanLuyenVienAccounts = await _context.TaiKhoans
                        .Where(t => t.MaQuyen == huanLuyenVienRole.MaQuyen)
                        .ToListAsync();
                    
                    // Cập nhật tất cả tài khoản để sử dụng quyền "Huấn luyện viên"
                    foreach (var account in huanLuyenVienAccounts)
                    {
                        account.MaQuyen = huanLuyenVienSpaceRole.MaQuyen;
                    }
                    
                    // Xóa quyền "HuanLuyenVien"
                    _context.Quyens.Remove(huanLuyenVienRole);
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = "Chuẩn hóa quyền thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi chuẩn hóa quyền: " + ex.Message;
                return View();
            }
        }
    }
} 