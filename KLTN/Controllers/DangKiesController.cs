using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KLTN.Data;
using KLTN.Models.Database;
using Microsoft.AspNetCore.Authorization;
using KLTN.Models.ViewModels;
using System.Security.Claims;

namespace KLTN.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DangKiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DangKiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsThanhVien()
        {
            if (!User.Identity.IsAuthenticated) return false;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return false;

            var taiKhoan = _context.TaiKhoans
                .Include(t => t.Quyen)
                .FirstOrDefault(t => t.MaTK.ToString() == userId);

            if (taiKhoan?.Quyen == null) return false;

            // Chuẩn hóa tên quyền bằng cách bỏ dấu và khoảng trắng
            var normalizedRole = taiKhoan.Quyen.TenQuyen
                .Replace(" ", "")
                .Replace("á", "a")
                .Replace("à", "a")
                .Replace("ã", "a")
                .Replace("ạ", "a")
                .Replace("ă", "a")
                .Replace("ắ", "a")
                .Replace("ằ", "a")
                .Replace("ẳ", "a")
                .Replace("ẵ", "a")
                .Replace("ặ", "a")
                .Replace("â", "a")
                .Replace("ấ", "a")
                .Replace("ầ", "a")
                .Replace("ẩ", "a")
                .Replace("ẫ", "a")
                .Replace("ậ", "a")
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ẻ", "e")
                .Replace("ẽ", "e")
                .Replace("ẹ", "e")
                .Replace("ê", "e")
                .Replace("ế", "e")
                .Replace("ề", "e")
                .Replace("ể", "e")
                .Replace("ễ", "e")
                .Replace("ệ", "e")
                .Replace("í", "i")
                .Replace("ì", "i")
                .Replace("ỉ", "i")
                .Replace("ĩ", "i")
                .Replace("ị", "i")
                .Replace("ó", "o")
                .Replace("ò", "o")
                .Replace("ỏ", "o")
                .Replace("õ", "o")
                .Replace("ọ", "o")
                .Replace("ô", "o")
                .Replace("ố", "o")
                .Replace("ồ", "o")
                .Replace("ổ", "o")
                .Replace("ỗ", "o")
                .Replace("ộ", "o")
                .Replace("ơ", "o")
                .Replace("ớ", "o")
                .Replace("ờ", "o")
                .Replace("ở", "o")
                .Replace("ỡ", "o")
                .Replace("ợ", "o")
                .Replace("ú", "u")
                .Replace("ù", "u")
                .Replace("ủ", "u")
                .Replace("ũ", "u")
                .Replace("ụ", "u")
                .Replace("ư", "u")
                .Replace("ứ", "u")
                .Replace("ừ", "u")
                .Replace("ử", "u")
                .Replace("ữ", "u")
                .Replace("ự", "u")
                .Replace("ý", "y")
                .Replace("ỳ", "y")
                .Replace("ỷ", "y")
                .Replace("ỹ", "y")
                .Replace("ỵ", "y")
                .ToLower();

            return normalizedRole == "thanhvien";
        }

        // GET: DangKies
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateStartSortParm"] = String.IsNullOrEmpty(sortOrder) ? "datestart_desc" : "";
            ViewData["DateEndSortParm"] = sortOrder == "dateend" ? "dateend_desc" : "dateend";
            ViewData["TypeSortParm"] = sortOrder == "type" ? "type_desc" : "type";
            ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["MemberSortParm"] = sortOrder == "member" ? "member_desc" : "member";
            
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var dangKies = _context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.KhachVangLai)
                .Include(d => d.LopHoc)
                .Include(d => d.ThanhVien)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                dangKies = dangKies.Where(d => 
                    (d.ThanhVien != null && d.ThanhVien.HoTen.Contains(searchString)) ||
                    (d.KhachVangLai != null && d.KhachVangLai.HoTen.Contains(searchString)) ||
                    (d.GoiTap != null && d.GoiTap.TenGoi.Contains(searchString)) ||
                    (d.LopHoc != null && d.LopHoc.TenLop.Contains(searchString)) ||
                    (d.LoaiDangKy != null && d.LoaiDangKy.Contains(searchString)) ||
                    (d.TrangThai != null && d.TrangThai.Contains(searchString))
                );
            }

            dangKies = sortOrder switch
            {
                "datestart_desc" => dangKies.OrderByDescending(d => d.NgayBatDau),
                "dateend" => dangKies.OrderBy(d => d.NgayKetThuc),
                "dateend_desc" => dangKies.OrderByDescending(d => d.NgayKetThuc),
                "type" => dangKies.OrderBy(d => d.LoaiDangKy),
                "type_desc" => dangKies.OrderByDescending(d => d.LoaiDangKy),
                "status" => dangKies.OrderBy(d => d.TrangThai),
                "status_desc" => dangKies.OrderByDescending(d => d.TrangThai),
                "member" => dangKies.OrderBy(d => d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai != null ? d.KhachVangLai.HoTen : ""),
                "member_desc" => dangKies.OrderByDescending(d => d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai != null ? d.KhachVangLai.HoTen : ""),
                _ => dangKies.OrderBy(d => d.NgayBatDau),
            };

            const int pageSize = 10;
            var totalItems = await dangKies.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            pageNumber = pageNumber ?? 1;
            pageNumber = Math.Max(1, Math.Min(pageNumber.Value, Math.Max(1, totalPages)));

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;

            var items = await dangKies
                .Skip((pageNumber.Value - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        // GET: DangKies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dangKy = await _context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.KhachVangLai)
                .Include(d => d.LopHoc)
                .Include(d => d.ThanhVien)
                .FirstOrDefaultAsync(m => m.MaDangKy == id);
            if (dangKy == null)
            {
                return NotFound();
            }

            return View(dangKy);
        }

        // GET: DangKies/Create
        public IActionResult Create()
        {
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi");
            ViewData["MaKhachVangLai"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen");
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "TenLop");
            ViewData["MaTV"] = new SelectList(_context.ThanhViens, "MaTV", "HoTen");
            return View();
        }

        // POST: DangKies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDangKy,MaTV,MaKhachVangLai,MaGoiTap,MaLopHoc,NgayBatDau,NgayKetThuc,LoaiDangKy,SoBuoi,TrangThai,GhiChu,NgayDangKy")] DangKy dangKy)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dangKy);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", dangKy.MaGoiTap);
            ViewData["MaKhachVangLai"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", dangKy.MaKhachVangLai);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "TenLop", dangKy.MaLopHoc);
            ViewData["MaTV"] = new SelectList(_context.ThanhViens, "MaTV", "HoTen", dangKy.MaTV);
            return View(dangKy);
        }

        // GET: DangKies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dangKy = await _context.DangKys.FindAsync(id);
            if (dangKy == null)
            {
                return NotFound();
            }
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", dangKy.MaGoiTap);
            ViewData["MaKhachVangLai"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", dangKy.MaKhachVangLai);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "TenLop", dangKy.MaLopHoc);
            ViewData["MaTV"] = new SelectList(_context.ThanhViens, "MaTV", "HoTen", dangKy.MaTV);
            return View(dangKy);
        }

        // POST: DangKies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDangKy,MaTV,MaKhachVangLai,MaGoiTap,MaLopHoc,NgayBatDau,NgayKetThuc,LoaiDangKy,SoBuoi,TrangThai,GhiChu,NgayDangKy")] DangKy dangKy)
        {
            if (id != dangKy.MaDangKy)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dangKy);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DangKyExists(dangKy.MaDangKy))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", dangKy.MaGoiTap);
            ViewData["MaKhachVangLai"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", dangKy.MaKhachVangLai);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "TenLop", dangKy.MaLopHoc);
            ViewData["MaTV"] = new SelectList(_context.ThanhViens, "MaTV", "HoTen", dangKy.MaTV);
            return View(dangKy);
        }

        // GET: DangKies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dangKy = await _context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.KhachVangLai)
                .Include(d => d.LopHoc)
                .Include(d => d.ThanhVien)
                .FirstOrDefaultAsync(m => m.MaDangKy == id);
            if (dangKy == null)
            {
                return NotFound();
            }

            return View(dangKy);
        }

        // POST: DangKies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dangKy = await _context.DangKys.FindAsync(id);
            if (dangKy != null)
            {
                _context.DangKys.Remove(dangKy);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DangKyExists(int id)
        {
            return _context.DangKys.Any(e => e.MaDangKy == id);
        }

        // --- THÊM MỚI: CHỨC NĂNG ĐĂNG KÝ ONLINE ---

        // GET: DangKies/DangKyOnline/5?loaiDichVu=GoiTap
        [AllowAnonymous]
        public async Task<IActionResult> DangKyOnline(int? dichVuId, string loaiDichVu)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("DangKyOnline", new { dichVuId, loaiDichVu }) });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.Quyen)
                .FirstOrDefaultAsync(t => t.MaTK.ToString() == userId);

            if (taiKhoan?.Quyen?.TenQuyen != "Thành viên")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (dichVuId == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus
                .Include(d => d.LopHoc)
                .FirstOrDefaultAsync(d => d.MaDichVu == dichVuId);

            if (dichVu == null)
            {
                return NotFound();
            }

            var thanhVien = await _context.ThanhViens
                .FirstOrDefaultAsync(tv => tv.MaTK.ToString() == userId);

            if (thanhVien == null)
            {
                return NotFound();
            }

            var model = new DangKyOnlineViewModel
            {
                DichVuId = dichVu.MaDichVu,
                TenDichVu = dichVu.TenDichVu,
                LoaiDichVu = dichVu.LoaiDichVu,
                GiaDichVu = dichVu.GiaBatDau,
                TenThanhVien = thanhVien.HoTen,
                Email = thanhVien.Email,
                SoDienThoai = thanhVien.SoDienThoai,
                NgayBatDau = DateTime.Now.Date
            };

            if (dichVu.LoaiDichVu == "LopHoc" && dichVu.LopHoc != null)
            {
                model.TenLopHoc = dichVu.LopHoc.TenLop;
                model.LichHoc = dichVu.LopHoc.NgayTrongTuan;
                model.SoLuongToiDa = dichVu.LopHoc.SoLuongToiDa;
            }

            return View(model);
        }

        // POST: DangKies/DangKyOnline
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> DangKyOnline(DangKyOnlineViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.Quyen)
                .FirstOrDefaultAsync(t => t.MaTK.ToString() == userId);

            if (taiKhoan?.Quyen?.TenQuyen != "Thành viên")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                var thanhVien = await _context.ThanhViens
                    .FirstOrDefaultAsync(tv => tv.MaTK.ToString() == userId);

                if (thanhVien == null)
                {
                    return NotFound();
                }

                var dichVu = await _context.DichVus
                    .Include(d => d.LopHoc)
                    .FirstOrDefaultAsync(d => d.MaDichVu == model.DichVuId);

                if (dichVu == null)
                {
                    return NotFound();
                }

                // Kiểm tra số lượng học viên nếu là lớp học
                if (dichVu.LoaiDichVu == "LopHoc" && dichVu.LopHoc != null)
                {
                    var soLuongHienTai = await _context.DangKys
                        .CountAsync(dk => dk.MaLopHoc == dichVu.LopHoc.MaLop && dk.TrangThai == "Đã thanh toán");

                    if (soLuongHienTai >= dichVu.LopHoc.SoLuongToiDa)
                    {
                        ModelState.AddModelError("", "Lớp học đã đầy. Vui lòng chọn lớp khác.");
                        return View(model);
                    }
                }

                // Kiểm tra đăng ký trùng
                var dangKyTrung = await _context.DangKys
                    .FirstOrDefaultAsync(dk =>
                        dk.MaTV == thanhVien.MaTV &&
                        ((dichVu.LopHoc != null && dk.MaLopHoc == dichVu.LopHoc.MaLop) || dk.MaGoiTap == dichVu.MaGoiTap) &&
                        dk.TrangThai == "Đã thanh toán" &&
                        dk.NgayKetThuc > DateTime.Now);

                if (dangKyTrung != null)
                {
                    ModelState.AddModelError("", "Bạn đã đăng ký dịch vụ này và vẫn còn hiệu lực.");
                    return View(model);
                }

                // Kiểm tra đăng ký chờ thanh toán
                var dangKyChoThanhToan = await _context.DangKys
                    .FirstOrDefaultAsync(dk =>
                        dk.MaTV == thanhVien.MaTV &&
                        dk.TrangThai == "Chờ thanh toán");

                if (dangKyChoThanhToan != null)
                {
                    ModelState.AddModelError("", "Bạn có một đăng ký đang chờ thanh toán.");
                    return View(model);
                }

                var dangKy = new DangKy
                {
                    MaTV = thanhVien.MaTV,
                    MaLopHoc = dichVu.LopHoc?.MaLop,
                    MaGoiTap = dichVu.MaGoiTap,
                    NgayBatDau = model.NgayBatDau,
                    NgayKetThuc = model.NgayBatDau.AddMonths(1),
                    LoaiDangKy = dichVu.LoaiDichVu,
                    TrangThai = "Chờ thanh toán",
                    GhiChu = model.GhiChu,
                    NgayDangKy = DateTime.Now
                };

                _context.Add(dangKy);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(DangKyThanhCong), new { id = dangKy.MaDangKy });
            }

            return View(model);
        }

        // GET: DangKies/DangKyThanhCong/5
        [Authorize(Roles = "Thành viên,ThanhVien")]
        public async Task<IActionResult> DangKyThanhCong(int id)
        {
            var dangKy = await _context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Include(d => d.ThanhVien)
                .Include(d => d.ThanhToans)
                .FirstOrDefaultAsync(m => m.MaDangKy == id);

            if (dangKy == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người đang đăng nhập có phải là chủ của đăng ký này không
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var thanhVien = await _context.ThanhViens.FirstOrDefaultAsync(tv => tv.MaTK.ToString() == userId);

            if (thanhVien == null || dangKy.MaTV != thanhVien.MaTV)
            {
                return Forbid();
            }

            return View(dangKy);
        }

        // GET: DangKies/LichSuDangKy
        [Authorize(Roles = "Thành viên,ThanhVien")]
        public async Task<IActionResult> LichSuDangKy()
        {
            // Lấy ID của thành viên đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var thanhVien = await _context.ThanhViens.FirstOrDefaultAsync(tv => tv.MaTK.ToString() == userId);

            if (thanhVien == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin thành viên.";
                return RedirectToAction("Index", "Home");
            }

            var dangKies = await _context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Where(d => d.MaTV == thanhVien.MaTV)
                .OrderByDescending(d => d.NgayDangKy)
                .ToListAsync();

            return View(dangKies);
        }
    }
}
