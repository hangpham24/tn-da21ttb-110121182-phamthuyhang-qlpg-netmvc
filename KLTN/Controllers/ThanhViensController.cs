using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KLTN.Data;
using KLTN.Models.Database;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace KLTN.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ThanhViensController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ThanhViensController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: ThanhViens
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["EmailSortParm"] = sortOrder == "email" ? "email_desc" : "email";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var thanhViens = _context.ThanhViens.Include(t => t.TaiKhoan).AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                thanhViens = thanhViens.Where(tv =>
                    tv.HoTen.Contains(searchString) ||
                    tv.Email.Contains(searchString) ||
                    tv.SoDienThoai.Contains(searchString) ||
                    tv.DiaChi.Contains(searchString)
                );
            }

            thanhViens = sortOrder switch
            {
                "name_desc" => thanhViens.OrderByDescending(tv => tv.HoTen),
                "date" => thanhViens.OrderBy(tv => tv.NgaySinh),
                "date_desc" => thanhViens.OrderByDescending(tv => tv.NgaySinh),
                "email" => thanhViens.OrderBy(tv => tv.Email),
                "email_desc" => thanhViens.OrderByDescending(tv => tv.Email),
                _ => thanhViens.OrderBy(tv => tv.HoTen),
            };

            const int pageSize = 5;
            var totalItems = await thanhViens.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            pageNumber = pageNumber ?? 1;
            pageNumber = Math.Max(1, Math.Min(pageNumber.Value, totalPages));

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;

            var items = await thanhViens
                .Skip((pageNumber.Value - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        // GET: ThanhViens/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thanhVien = await _context.ThanhViens
                .Include(t => t.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaTV == id);
            if (thanhVien == null)
            {
                return NotFound();
            }

            return View(thanhVien);
        }

        // GET: ThanhViens/Create
        public IActionResult Create()
        {
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash");
            return View();
        }

        // POST: ThanhViens/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaTV,HoTen,NgaySinh,GioiTinh,SoDienThoai,Email,DiaChi,NgayDangKy,NgayDangKyTV,AnhDaiDien,TrangThai,TrangThaiTV,AvatarFile")] ThanhVien thanhVien)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem email đã tồn tại trong bảng ThanhVien chưa
                if (_context.ThanhViens.Any(tv => tv.Email == thanhVien.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng cho một thành viên khác.");
                    return View(thanhVien);
                }

                // Kiểm tra xem tên đăng nhập (từ email) đã tồn tại trong bảng TaiKhoan chưa
                string tenDangNhap = thanhVien.Email;
                if (_context.TaiKhoans.Any(tk => tk.TenDangNhap == tenDangNhap))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng làm tên đăng nhập cho một tài khoản khác.");
                    return View(thanhVien);
                }

                // Xử lý upload ảnh đại diện nếu có
                if (thanhVien.AvatarFile != null && thanhVien.AvatarFile.Length > 0)
                {
                    // Đảm bảo thư mục tồn tại
                    string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "img", "avt");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    // Tạo tên file duy nhất
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(thanhVien.AvatarFile.FileName);
                    string filePath = Path.Combine(uploadDir, fileName);

                    // Lưu file ảnh
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await thanhVien.AvatarFile.CopyToAsync(fileStream);
                    }

                    // Lưu đường dẫn vào database
                    thanhVien.AnhDaiDien = "/img/avt/" + fileName;
                }

                // Tìm quyền "Thành viên"
                var quyenThanhVien = await _context.Quyens.FirstOrDefaultAsync(q => q.TenQuyen == "Thành viên");
                if (quyenThanhVien == null)
                {
                    // Nếu không có quyền "Thành viên", thử tìm "ThanhVien"
                    quyenThanhVien = await _context.Quyens.FirstOrDefaultAsync(q => q.TenQuyen == "ThanhVien");
                    
                    // Nếu vẫn không tìm thấy, tạo mới quyền
                    if (quyenThanhVien == null)
                    {
                        quyenThanhVien = new Quyen
                        {
                            TenQuyen = "Thành viên",
                            MoTa = "Quyền dành cho thành viên tập luyện"
                        };
                        _context.Quyens.Add(quyenThanhVien);
                        await _context.SaveChangesAsync(); // Lưu để có MaQuyen
                    }
                }

                // Tạo tài khoản mới với email làm tên đăng nhập và mật khẩu
                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = tenDangNhap,
                    // Mật khẩu được đặt là email và sau đó băm bằng SHA256
                    // Điều này đảm bảo khi người dùng đăng nhập với email và mật khẩu là email, nó sẽ khớp
                    MatKhauHash = HashPassword(tenDangNhap),
                    MaQuyen = quyenThanhVien.MaQuyen,
                    TrangThai = "HoatDong",
                    NgayTao = DateTime.Now
                };

                // Lưu tài khoản vào cơ sở dữ liệu
                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync(); // Lưu để có MaTK

                // Gán MaTK cho thành viên
                thanhVien.MaTK = taiKhoan.MaTK;

                // Lưu thành viên vào cơ sở dữ liệu
                _context.ThanhViens.Add(thanhVien);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo thành viên và tài khoản thành công. Tên đăng nhập và mật khẩu là email của thành viên.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", thanhVien.MaTK);
            return View(thanhVien);
        }

        // GET: ThanhViens/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thanhVien = await _context.ThanhViens.FindAsync(id);
            if (thanhVien == null)
            {
                return NotFound();
            }
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", thanhVien.MaTK);
            return View(thanhVien);
        }

        // POST: ThanhViens/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaTV,HoTen,NgaySinh,GioiTinh,SoDienThoai,Email,DiaChi,NgayDangKy,NgayDangKyTV,AnhDaiDien,MaTK,TrangThai,TrangThaiTV")] ThanhVien thanhVien)
        {
            if (id != thanhVien.MaTV)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(thanhVien);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThanhVienExists(thanhVien.MaTV))
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
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", thanhVien.MaTK);
            return View(thanhVien);
        }

        // GET: ThanhViens/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thanhVien = await _context.ThanhViens
                .Include(t => t.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaTV == id);
            if (thanhVien == null)
            {
                return NotFound();
            }

            return View(thanhVien);
        }

        // POST: ThanhViens/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var thanhVien = await _context.ThanhViens.FindAsync(id);
            if (thanhVien != null)
            {
                _context.ThanhViens.Remove(thanhVien);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ThanhVienExists(int id)
        {
            return _context.ThanhViens.Any(e => e.MaTV == id);
        }

        // Phương thức để băm mật khẩu bằng SHA256 (giống hệt với AccountController)
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // GET: ThanhViens/TestHashPassword
        public IActionResult TestHashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                password = "mymytran@gmail.com"; // Mật khẩu mẫu để test
            }
            
            string hashedPassword = HashPassword(password);
            ViewData["Password"] = password;
            ViewData["HashedPassword"] = hashedPassword;
            
            return View();
        }
    }
}
