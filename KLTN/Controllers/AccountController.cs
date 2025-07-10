using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KLTN.Models;
using KLTN.Models.Authentication;
using KLTN.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using KLTN.Models.ViewModels;
using KLTN.Models.Database;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace KLTN.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _context.TaiKhoans
                    .Include(t => t.Quyen)
                    .FirstOrDefaultAsync(t => t.TenDangNhap == model.TenDangNhap);

                if (user != null && VerifyPassword(model.Password, user.MatKhauHash))
                {
                    if (user.TrangThai == "Khoa")
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa.");
                        return View(model);
                    }

                    if (user.TrangThai == "ChuaKichHoat")
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn chưa được kích hoạt.");
                        return View(model);
                    }

                    // Cập nhật thời gian đăng nhập cuối
                    user.LanDangNhapCuoi = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Tạo claims cho người dùng
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.MaTK.ToString()),
                        new Claim(ClaimTypes.Name, user.TenDangNhap),
                        new Claim(ClaimTypes.Role, user.Quyen?.TenQuyen ?? "ThanhVien")
                    };

                    // Lấy email từ ThanhVien hoặc HuanLuyenVien
                    string? email = null;
                    var thanhVien = await _context.ThanhViens.FirstOrDefaultAsync(tv => tv.MaTK == user.MaTK);
                    if (thanhVien != null && !string.IsNullOrEmpty(thanhVien.Email))
                    {
                        email = thanhVien.Email;
                    }
                    else
                    {
                        var hlv = await _context.HuanLuyenViens.FirstOrDefaultAsync(h => h.MaTK == user.MaTK);
                        if (hlv != null && !string.IsNullOrEmpty(hlv.Email))
                        {
                            email = hlv.Email;
                        }
                    }

                    if (!string.IsNullOrEmpty(email))
                    {
                        claims.Add(new Claim(ClaimTypes.Email, email));
                    }

                    // Tạo identity và đăng nhập
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("Người dùng {0} đã đăng nhập.", user.TenDangNhap);

                    // Chuyển hướng đến trang được yêu cầu hoặc trang chủ
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem tên đăng nhập đã tồn tại hay chưa
                if (await _context.TaiKhoans.AnyAsync(x => x.TenDangNhap == model.TenDangNhap))
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
                    return View(model);
                }

                // Kiểm tra xem email đã tồn tại hay chưa
                if (!string.IsNullOrEmpty(model.Email))
                {
                    if (await _context.ThanhViens.AnyAsync(t => t.Email == model.Email) ||
                        await _context.HuanLuyenViens.AnyAsync(h => h.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng.");
                        return View(model);
                    }
                }

                // Lấy quyền từ cơ sở dữ liệu dựa trên lựa chọn của người dùng
                var quyen = await _context.Quyens.FirstOrDefaultAsync(q => q.TenQuyen == model.QuyenDangKy);
                
                // Nếu không tìm thấy quyền chính xác, thử tìm quyền tương tự
                if (quyen == null && model.QuyenDangKy == "ThanhVien")
                {
                    quyen = await _context.Quyens.FirstOrDefaultAsync(q => q.TenQuyen == "Thành viên");
                }
                else if (quyen == null && model.QuyenDangKy == "Thành viên")
                {
                    quyen = await _context.Quyens.FirstOrDefaultAsync(q => q.TenQuyen == "ThanhVien");
                }
                else if (quyen == null && model.QuyenDangKy == "HuanLuyenVien")
                {
                    quyen = await _context.Quyens.FirstOrDefaultAsync(q => q.TenQuyen == "Huấn luyện viên");
                }
                else if (quyen == null && model.QuyenDangKy == "Huấn luyện viên")
                {
                    quyen = await _context.Quyens.FirstOrDefaultAsync(q => q.TenQuyen == "HuanLuyenVien");
                }
                
                if (quyen == null)
                {
                    // Chuẩn hóa tên quyền
                    string tenQuyen = model.QuyenDangKy;
                    string moTa;
                    
                    if (model.QuyenDangKy == "ThanhVien" || model.QuyenDangKy == "Thành viên")
                    {
                        tenQuyen = "Thành viên"; // Dùng tên chuẩn có dấu cách
                        moTa = "Thành viên của phòng gym";
                    }
                    else if (model.QuyenDangKy == "HuanLuyenVien" || model.QuyenDangKy == "Huấn luyện viên")
                    {
                        tenQuyen = "Huấn luyện viên"; // Dùng tên chuẩn có dấu cách
                        moTa = "Huấn luyện viên của phòng gym";
                    }
                    else
                    {
                        moTa = "Quyền người dùng";
                    }
                    
                    // Nếu chưa có, tạo mới quyền
                    quyen = new Quyen
                    {
                        TenQuyen = tenQuyen,
                        MoTa = moTa
                    };
                    _context.Quyens.Add(quyen);
                    await _context.SaveChangesAsync();
                }

                // Tạo tài khoản mới
                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = model.TenDangNhap,
                    MatKhauHash = HashPassword(model.Password),
                    MaQuyen = quyen.MaQuyen,
                    TrangThai = "HoatDong",
                    NgayTao = DateTime.Now
                };

                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync();

                // Xử lý ảnh đại diện nếu có
                string? avatarPath = null;
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    // Đảm bảo thư mục tồn tại
                    string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "img", "avt");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    // Tạo tên file duy nhất
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AvatarFile.FileName);
                    string filePath = Path.Combine(uploadDir, fileName);

                    // Lưu file ảnh
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(fileStream);
                    }

                    // Lưu đường dẫn
                    avatarPath = "/img/avt/" + fileName;
                }

                // Tạo thông tin người dùng dựa trên quyền với thông tin cơ bản
                if (model.QuyenDangKy == "HuanLuyenVien" || model.QuyenDangKy == "Huấn luyện viên")
                {
                    // Tạo huấn luyện viên mới với thông tin cơ bản
                    var huanLuyenVien = new HuanLuyenVien
                    {
                        HoTen = model.HoTen,
                        Email = model.Email,
                        SoDienThoai = model.SoDienThoai,
                        ChuyenMon = model.ChuyenMon,
                        MaTK = taiKhoan.MaTK,
                        TrangThaiHLV = "ChoPheDuyet", // Huấn luyện viên cần được phê duyệt trước khi hoạt động
                        AnhDaiDien = avatarPath
                    };

                    _context.HuanLuyenViens.Add(huanLuyenVien);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Huấn luyện viên {0} đã tạo tài khoản mới.", model.TenDangNhap);
                }
                else
                {
                    // Tạo thành viên mới với thông tin cơ bản
                    var thanhVien = new ThanhVien
                    {
                        HoTen = model.HoTen,
                        Email = model.Email,
                        SoDienThoai = model.SoDienThoai,
                        MaTK = taiKhoan.MaTK,
                        AnhDaiDien = avatarPath
                    };

                    _context.ThanhViens.Add(thanhVien);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Thành viên {0} đã tạo tài khoản mới.", model.TenDangNhap);
                }

                // Đăng nhập người dùng sau khi đăng ký
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTK.ToString()),
                    new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                    new Claim(ClaimTypes.Role, model.QuyenDangKy)
                };

                if (!string.IsNullOrEmpty(model.Email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, model.Email));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Thêm thông báo cho người dùng về việc hoàn thiện thông tin cá nhân
                TempData["StatusMessage"] = "Đăng ký tài khoản thành công! Vui lòng cập nhật thêm thông tin cá nhân của bạn.";

                // Chuyển hướng đến trang cập nhật thông tin cá nhân sau khi đăng ký
                return RedirectToAction("EditProfile");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Người dùng đã đăng xuất.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            // Kiểm tra nếu người dùng đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                // Lấy thông tin về URL bị từ chối
                ViewBag.ReturnUrl = returnUrl;
                
                // Kiểm tra nếu URL chứa DangKyOnline và LopHoc
                if (!string.IsNullOrEmpty(returnUrl) && 
                    returnUrl.Contains("DangKies/DangKyOnline") && 
                    returnUrl.Contains("loaiDichVu=LopHoc"))
                {
                    // Lấy dichVuId từ returnUrl
                    string dichVuIdStr = returnUrl.Split(new[] { "dichVuId=" }, StringSplitOptions.None)[1];
                    if (dichVuIdStr.Contains("&"))
                    {
                        dichVuIdStr = dichVuIdStr.Split('&')[0];
                    }
                    
                    // Thêm thông báo hướng dẫn
                    TempData["ErrorMessage"] = "Bạn cần có quyền Thành viên để đăng ký lớp học. Vui lòng liên hệ quản trị viên để được cấp quyền.";
                    
                    // Chuyển hướng về trang chi tiết dịch vụ
                    if (int.TryParse(dichVuIdStr, out int dichVuId))
                    {
                        return RedirectToAction("PublicDetails", "DichVus", new { id = dichVuId });
                    }
                }
            }
            
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try 
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login");
            }

                // Tìm tài khoản
                var taiKhoan = await _context.TaiKhoans
                    .Include(t => t.Quyen)
                .Include(t => t.ThanhVien)
                .Include(t => t.HuanLuyenVien)
                .FirstOrDefaultAsync(t => t.MaTK == userIdInt);

                if (taiKhoan == null)
                {
                    return NotFound("Không tìm thấy thông tin tài khoản.");
                }
                
                return View(taiKhoan);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Registrations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login");
            }

            // Kiểm tra vai trò người dùng
            var roleId = (await _context.TaiKhoans.FindAsync(userIdInt))?.MaQuyen;
            var roleName = roleId != null ? (await _context.Quyens.FindAsync(roleId))?.TenQuyen : null;

            // Lấy thông tin thành viên
            var thanhVien = await _context.ThanhViens
                .FirstOrDefaultAsync(tv => tv.MaTK == userIdInt);

            if (thanhVien != null)
            {
            // Lấy danh sách đăng ký của thành viên
            var dangKys = await _context.DangKys
                .Include(dk => dk.GoiTap)
                .Include(dk => dk.LopHoc)
                .Include(dk => dk.ThanhToans)
                .Include(dk => dk.GiaHanDangKys)
                .Where(dk => dk.MaTV == thanhVien.MaTV)
                .OrderByDescending(dk => dk.NgayDangKy)
                .ToListAsync();

            return View(dangKys);
            }
            
            // Kiểm tra nếu là huấn luyện viên
            var huanLuyenVien = await _context.HuanLuyenViens
                .FirstOrDefaultAsync(hlv => hlv.MaTK == userIdInt);
            
            if (huanLuyenVien != null)
            {
                // Lấy danh sách lớp học do huấn luyện viên phụ trách
                var lopHocs = await _context.LopHoc
                    .Where(lh => lh.MaPT == huanLuyenVien.MaPT)
                    .ToListAsync();
                
                // Lấy danh sách đăng ký cho các lớp học này
                var maLopHocs = lopHocs.Select(lh => lh.MaLop).ToList();
                var dangKys = await _context.DangKys
                    .Include(dk => dk.GoiTap)
                    .Include(dk => dk.LopHoc)
                    .Include(dk => dk.ThanhVien)
                    .Include(dk => dk.ThanhToans)
                    .Where(dk => maLopHocs.Contains(dk.MaLopHoc ?? 0))
                    .OrderByDescending(dk => dk.NgayDangKy)
                    .ToListAsync();
                
                // Sử dụng cùng một View nhưng với danh sách đăng ký liên quan đến huấn luyện viên
                return View(dangKys);
            }

            // Nếu không tìm thấy thông tin thành viên hoặc huấn luyện viên
            // Trả về danh sách trống thay vì NotFound
            return View(new List<DangKy>());
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login");
            }

                EditProfileModel model = new EditProfileModel();

                // Tìm trong bảng ThanhVien
            var thanhVien = await _context.ThanhViens
                    .FirstOrDefaultAsync(t => t.MaTK == userIdInt);

                if (thanhVien != null)
                {
                    model.HoTen = thanhVien.HoTen ?? string.Empty;
                    model.NgaySinh = thanhVien.NgaySinh;
                    model.GioiTinh = thanhVien.GioiTinh;
                    model.SoDienThoai = thanhVien.SoDienThoai;
                    model.Email = thanhVien.Email;
                    model.DiaChi = thanhVien.DiaChi;
                    model.CurrentAvatar = thanhVien.AnhDaiDien;
                    return View(model);
                }

                // Nếu không có trong ThanhVien, tìm trong HuanLuyenVien
                var huanLuyenVien = await _context.HuanLuyenViens
                    .FirstOrDefaultAsync(h => h.MaTK == userIdInt);

                if (huanLuyenVien != null)
                {
                    model.HoTen = huanLuyenVien.HoTen ?? string.Empty;
                    model.NgaySinh = huanLuyenVien.NgaySinh;
                    model.GioiTinh = huanLuyenVien.GioiTinh;
                    model.SoDienThoai = huanLuyenVien.SoDienThoai;
                    model.Email = huanLuyenVien.Email;
                    model.DiaChi = huanLuyenVien.DiaChi;
                    model.CurrentAvatar = huanLuyenVien.AnhDaiDien;
                    return View(model);
                }

                // Nếu không tìm thấy ở đâu cả thì trả về model rỗng
                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Profile");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditProfile(EditProfileModel model)
        {
            try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login");
            }

                // Tìm trong bảng ThanhVien
            var thanhVien = await _context.ThanhViens
                .FirstOrDefaultAsync(tv => tv.MaTK == userIdInt);

                if (thanhVien != null)
            {
            // Cập nhật thông tin thành viên
            thanhVien.HoTen = model.HoTen;
            thanhVien.NgaySinh = model.NgaySinh;
            thanhVien.GioiTinh = model.GioiTinh;
            thanhVien.SoDienThoai = model.SoDienThoai;
            thanhVien.Email = model.Email;
            thanhVien.DiaChi = model.DiaChi;

                    // Xử lý ảnh đại diện nếu có
                    if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                    {
                        string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "img", "avt");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AvatarFile.FileName);
                        string filePath = Path.Combine(uploadDir, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.AvatarFile.CopyToAsync(fileStream);
                        }

                        thanhVien.AnhDaiDien = "/img/avt/" + fileName;
                    }

                    await _context.SaveChangesAsync();
                    TempData["StatusMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }

                // Nếu không có trong ThanhVien, tìm trong HuanLuyenVien
                var huanLuyenVien = await _context.HuanLuyenViens
                    .FirstOrDefaultAsync(h => h.MaTK == userIdInt);

                if (huanLuyenVien != null)
                {
                    // Cập nhật thông tin huấn luyện viên
                    huanLuyenVien.HoTen = model.HoTen;
                    huanLuyenVien.NgaySinh = model.NgaySinh;
                    huanLuyenVien.GioiTinh = model.GioiTinh;
                    huanLuyenVien.SoDienThoai = model.SoDienThoai;
                    huanLuyenVien.Email = model.Email;
                    huanLuyenVien.DiaChi = model.DiaChi;

                    // Xử lý ảnh đại diện nếu có
                    if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                    {
                        string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "img", "avt");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AvatarFile.FileName);
                        string filePath = Path.Combine(uploadDir, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.AvatarFile.CopyToAsync(fileStream);
                        }

                        huanLuyenVien.AnhDaiDien = "/img/avt/" + fileName;
                    }

                    await _context.SaveChangesAsync();
                    TempData["StatusMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }

                // Nếu không tìm thấy trong cả hai bảng, tạo mới một bản ghi thành viên
            var taiKhoan = await _context.TaiKhoans.FindAsync(userIdInt);
            if (taiKhoan != null)
            {
                    string avatarPath = null;
                    if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                    {
                        string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "img", "avt");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AvatarFile.FileName);
                        string filePath = Path.Combine(uploadDir, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.AvatarFile.CopyToAsync(fileStream);
                        }

                        avatarPath = "/img/avt/" + fileName;
                    }

                    // Tạo một bản ghi thành viên mới cho người dùng
                    var thanhVienMoi = new ThanhVien
                    {
                        MaTK = userIdInt,
                        HoTen = model.HoTen,
                        NgaySinh = model.NgaySinh,
                        GioiTinh = model.GioiTinh,
                        SoDienThoai = model.SoDienThoai,
                        Email = model.Email,
                        DiaChi = model.DiaChi,
                        AnhDaiDien = avatarPath
                    };

                    _context.ThanhViens.Add(thanhVienMoi);
                    await _context.SaveChangesAsync();
                    TempData["StatusMessage"] = "Tạo và cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }

                TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản.";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật thông tin: " + ex.Message;
            return RedirectToAction("Profile");
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.TaiKhoans.FindAsync(userIdInt);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin tài khoản.");
            }

            // Kiểm tra mật khẩu hiện tại
            if (!VerifyPassword(model.CurrentPassword, user.MatKhauHash))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            // Cập nhật mật khẩu mới
            user.MatKhauHash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Profile");
        }

        // Phương thức để băm mật khẩu
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // Phương thức để xác minh mật khẩu
        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
        }

        // GET: Account/TestHashPassword
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

        [HttpGet]
        public async Task<IActionResult> UpdateToMemberRole()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login");
            }
            
            // Kiểm tra xem người dùng đã có vai trò Thành viên chưa
            if (User.IsInRole("Thành viên") || User.IsInRole("ThanhVien"))
            {
                TempData["InfoMessage"] = "Bạn đã có quyền Thành viên.";
                return RedirectToAction("Index", "Home");
            }
            
            // Lấy thông tin tài khoản
            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.Quyen)
                .FirstOrDefaultAsync(t => t.MaTK == userIdInt);
            
            if (taiKhoan == null)
            {
                return NotFound("Không tìm thấy thông tin tài khoản.");
            }
            
            // Lấy quyền Thành viên
            var quyenThanhVien = await _context.Quyens
                .FirstOrDefaultAsync(q => q.TenQuyen == "Thành viên" || q.TenQuyen == "ThanhVien");
            
            if (quyenThanhVien == null)
            {
                // Tạo quyền Thành viên nếu chưa có
                quyenThanhVien = new Models.Database.Quyen
                {
                    TenQuyen = "Thành viên",
                    MoTa = "Thành viên của phòng gym"
                };
                _context.Quyens.Add(quyenThanhVien);
                await _context.SaveChangesAsync();
            }
            
            // Cập nhật quyền cho tài khoản
            taiKhoan.MaQuyen = quyenThanhVien.MaQuyen;
            taiKhoan.Quyen = quyenThanhVien;
            
            await _context.SaveChangesAsync();
            
            // Kiểm tra xem đã có thông tin thành viên chưa
            var thanhVien = await _context.ThanhViens.FirstOrDefaultAsync(tv => tv.MaTK == userIdInt);
            
            if (thanhVien == null)
            {
                // Tạo thông tin thành viên mới
                thanhVien = new Models.Database.ThanhVien
                {
                    MaTK = userIdInt,
                    HoTen = taiKhoan.TenDangNhap
                };
                
                _context.ThanhViens.Add(thanhVien);
                await _context.SaveChangesAsync();
                
                // Chuyển hướng đến trang cập nhật hồ sơ
                TempData["SuccessMessage"] = "Bạn đã được cấp quyền Thành viên. Vui lòng cập nhật thông tin cá nhân.";
                return RedirectToAction("EditProfile");
            }
            
            // Cập nhật lại thông tin đăng nhập (claims)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTK.ToString()),
                new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role, quyenThanhVien.TenQuyen)
            };
            
            if (!string.IsNullOrEmpty(thanhVien.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, thanhVien.Email));
            }
            
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            
            TempData["SuccessMessage"] = "Bạn đã được cấp quyền Thành viên thành công.";
            return RedirectToAction("Index", "Home");
        }
    }
} 