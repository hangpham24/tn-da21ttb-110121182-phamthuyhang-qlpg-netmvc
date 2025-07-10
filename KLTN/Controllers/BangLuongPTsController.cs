using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KLTN.Data;
using KLTN.Models.Database;

namespace KLTN.Controllers
{
    public class BangLuongPTsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BangLuongPTsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BangLuongPTs
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.BangLuongPTs
                .Include(b => b.HuanLuyenVien)
                .OrderByDescending(b => b.ThangNam);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: BangLuongPTs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bangLuongPT = await _context.BangLuongPTs
                .Include(b => b.HuanLuyenVien)
                .Include(b => b.PhienDaysTrongKyLuong)
                    .ThenInclude(p => p.LopHoc)
                .Include(b => b.PhienDaysTrongKyLuong)
                    .ThenInclude(p => p.GoiTap)
                .FirstOrDefaultAsync(m => m.MaLuong == id);
                
            if (bangLuongPT == null)
            {
                return NotFound();
            }

            // Nếu tổng doanh thu và tổng hoa hồng bằng 0 và có phiên dạy, thì tính tự động
            if ((bangLuongPT.TongDoanhThu == 0 || bangLuongPT.TongHoaHong == 0) && 
                bangLuongPT.PhienDaysTrongKyLuong != null && 
                bangLuongPT.PhienDaysTrongKyLuong.Any())
            {
                // Lấy tháng và năm từ bảng lương
                var thang = bangLuongPT.ThangNam.Month;
                var nam = bangLuongPT.ThangNam.Year;
                
                // Tính ngày đầu và cuối tháng
                var ngayDauThang = new DateTime(nam, thang, 1);
                var ngayCuoiThang = ngayDauThang.AddMonths(1).AddDays(-1);
                
                // Lấy tất cả phiên dạy trong tháng
                var phienDays = await _context.PhienDays
                    .Include(p => p.LopHoc)
                    .Include(p => p.GoiTap)
                    .Where(p => p.MaPT == bangLuongPT.MaPT && 
                                p.NgayDay >= ngayDauThang && 
                                p.NgayDay <= ngayCuoiThang &&
                                p.TrangThai == "DaHoanThanh")
                    .ToListAsync();
                    
                decimal tongDoanhThu = 0;
                decimal tongHoaHong = 0;
                
                foreach (var phien in phienDays)
                {
                    // Gán phiên dạy vào bảng lương hiện tại
                    phien.MaBangLuong = bangLuongPT.MaLuong;
                    
                    // Cộng dồn vào doanh thu
                    tongDoanhThu += phien.GiaTriBuoiDay;
                    
                    // Tính hoa hồng dựa trên loại phiên dạy
                    decimal phanTramHoaHong = 0;
                    
                    if (phien.LoaiDichVu == "LopHoc" && phien.MaLopHoc.HasValue)
                    {
                        var hoaHongLopHoc = await _context.PT_PhanCongHoaHongs
                            .FirstOrDefaultAsync(pt => pt.MaPT == bangLuongPT.MaPT && 
                                                   pt.MaLopHoc == phien.MaLopHoc.Value);
                        
                        if (hoaHongLopHoc != null)
                        {
                            phanTramHoaHong = hoaHongLopHoc.PhanTramHoaHong;
                        }
                    }
                    else if (phien.LoaiDichVu == "GoiTap" && phien.MaGoiTap.HasValue)
                    {
                        var hoaHongGoiTap = await _context.PT_PhanCongHoaHongs
                            .FirstOrDefaultAsync(pt => pt.MaPT == bangLuongPT.MaPT && 
                                                   pt.MaGoiTap == phien.MaGoiTap.Value);
                        
                        if (hoaHongGoiTap != null)
                        {
                            phanTramHoaHong = hoaHongGoiTap.PhanTramHoaHong;
                        }
                    }
                    
                    // Tính hoa hồng cho phiên dạy này
                    decimal hoaHongPhien = phien.GiaTriBuoiDay * (phanTramHoaHong / 100);
                    tongHoaHong += hoaHongPhien;
                }
                
                // Cập nhật bảng lương
                bangLuongPT.TongDoanhThu = tongDoanhThu;
                bangLuongPT.TongHoaHong = tongHoaHong;
                bangLuongPT.TongThanhToan = bangLuongPT.LuongCoBan + tongHoaHong;
                
                await _context.SaveChangesAsync();
                
                // Refresh lại đối tượng để hiển thị những giá trị vừa tính
                bangLuongPT = await _context.BangLuongPTs
                    .Include(b => b.HuanLuyenVien)
                    .Include(b => b.PhienDaysTrongKyLuong)
                        .ThenInclude(p => p.LopHoc)
                    .Include(b => b.PhienDaysTrongKyLuong)
                        .ThenInclude(p => p.GoiTap)
                    .FirstOrDefaultAsync(m => m.MaLuong == id);
            }

            // Lấy danh sách tỷ lệ hoa hồng
            ViewBag.PhanCongHoaHongs = await _context.PT_PhanCongHoaHongs
                .Where(pt => pt.MaPT == bangLuongPT.MaPT)
                .ToListAsync();

            return View(bangLuongPT);
        }

        // GET: BangLuongPTs/Create
        public IActionResult Create()
        {
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen");
            
            // Thiết lập tháng hiện tại làm mặc định
            ViewBag.ThangHienTai = DateTime.Now.ToString("MM/yyyy");
            
            return View();
        }

        // POST: BangLuongPTs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaLuong,MaPT,ThangNam,LuongCoBan,GhiChu")] BangLuongPT bangLuongPT)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem đã có bảng lương cho HLV này trong tháng này chưa
                var daCoLuong = await _context.BangLuongPTs
                    .AnyAsync(b => b.MaPT == bangLuongPT.MaPT && 
                                   b.ThangNam.Month == bangLuongPT.ThangNam.Month && 
                                   b.ThangNam.Year == bangLuongPT.ThangNam.Year);
                
                if (daCoLuong)
                {
                    ModelState.AddModelError("", "Đã tồn tại bảng lương cho huấn luyện viên này trong tháng này!");
                    ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen", bangLuongPT.MaPT);
                    return View(bangLuongPT);
                }
                
                // Thiết lập các giá trị mặc định
                bangLuongPT.TongDoanhThu = 0;
                bangLuongPT.TongHoaHong = 0;
                bangLuongPT.TongThanhToan = bangLuongPT.LuongCoBan; // Ban đầu chỉ có lương cơ bản
                bangLuongPT.TrangThai = "ChuaThanhToan";
                
                _context.Add(bangLuongPT);
                await _context.SaveChangesAsync();
                
                // Sau khi tạo xong, chuyển đến tính lương
                return RedirectToAction(nameof(TinhLuong), new { id = bangLuongPT.MaLuong });
            }
            
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen", bangLuongPT.MaPT);
            return View(bangLuongPT);
        }

        // GET: BangLuongPTs/TinhLuong/5
        public async Task<IActionResult> TinhLuong(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bangLuongPT = await _context.BangLuongPTs
                .Include(b => b.HuanLuyenVien)
                .FirstOrDefaultAsync(m => m.MaLuong == id);
                
            if (bangLuongPT == null)
            {
                return NotFound();
            }

            // Lấy tháng và năm từ bảng lương
            var thang = bangLuongPT.ThangNam.Month;
            var nam = bangLuongPT.ThangNam.Year;
            
            // Tính ngày đầu và cuối tháng
            var ngayDauThang = new DateTime(nam, thang, 1);
            var ngayCuoiThang = ngayDauThang.AddMonths(1).AddDays(-1);
            
            // Lấy tất cả phiên dạy trong tháng
            var phienDays = await _context.PhienDays
                .Include(p => p.LopHoc)
                .Include(p => p.GoiTap)
                .Where(p => p.MaPT == bangLuongPT.MaPT && 
                            p.NgayDay >= ngayDauThang && 
                            p.NgayDay <= ngayCuoiThang &&
                            p.TrangThai == "DaHoanThanh")
                .ToListAsync();
                
            decimal tongDoanhThu = 0;
            decimal tongHoaHong = 0;
            
            foreach (var phien in phienDays)
            {
                // Gán phiên dạy vào bảng lương hiện tại
                phien.MaBangLuong = bangLuongPT.MaLuong;
                
                // Cộng dồn vào doanh thu
                tongDoanhThu += phien.GiaTriBuoiDay;
                
                // Tính hoa hồng dựa trên loại phiên dạy
                decimal phanTramHoaHong = 0;
                
                if (phien.LoaiDichVu == "LopHoc" && phien.MaLopHoc.HasValue)
                {
                    var hoaHongLopHoc = await _context.PT_PhanCongHoaHongs
                        .FirstOrDefaultAsync(pt => pt.MaPT == bangLuongPT.MaPT && 
                                               pt.MaLopHoc == phien.MaLopHoc.Value);
                    
                    if (hoaHongLopHoc != null)
                    {
                        phanTramHoaHong = hoaHongLopHoc.PhanTramHoaHong;
                    }
                }
                else if (phien.LoaiDichVu == "GoiTap" && phien.MaGoiTap.HasValue)
                {
                    var hoaHongGoiTap = await _context.PT_PhanCongHoaHongs
                        .FirstOrDefaultAsync(pt => pt.MaPT == bangLuongPT.MaPT && 
                                               pt.MaGoiTap == phien.MaGoiTap.Value);
                    
                    if (hoaHongGoiTap != null)
                    {
                        phanTramHoaHong = hoaHongGoiTap.PhanTramHoaHong;
                    }
                }
                
                // Tính hoa hồng cho phiên dạy này
                decimal hoaHongPhien = phien.GiaTriBuoiDay * (phanTramHoaHong / 100);
                tongHoaHong += hoaHongPhien;
            }
            
            // Cập nhật bảng lương
            bangLuongPT.TongDoanhThu = tongDoanhThu;
            bangLuongPT.TongHoaHong = tongHoaHong;
            bangLuongPT.TongThanhToan = bangLuongPT.LuongCoBan + tongHoaHong;
            
            await _context.SaveChangesAsync();
            
            // Thêm thông báo thành công
            TempData["SuccessMessage"] = "Đã tính lương thành công. Tổng doanh thu: " + tongDoanhThu.ToString("#,##0") + " VNĐ. Tổng hoa hồng: " + tongHoaHong.ToString("#,##0") + " VNĐ.";
            
            return RedirectToAction(nameof(Details), new { id = bangLuongPT.MaLuong });
        }

        // GET: BangLuongPTs/ThanhToanLuong/5
        public async Task<IActionResult> ThanhToanLuong(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bangLuongPT = await _context.BangLuongPTs.FindAsync(id);
            if (bangLuongPT == null)
            {
                return NotFound();
            }
            
            bangLuongPT.TrangThai = "DaThanhToan";
            bangLuongPT.NgayThanhToan = DateTime.Now;
            
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Details), new { id = bangLuongPT.MaLuong });
        }

        // GET: BangLuongPTs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bangLuongPT = await _context.BangLuongPTs.FindAsync(id);
            if (bangLuongPT == null)
            {
                return NotFound();
            }
            
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen", bangLuongPT.MaPT);
            return View(bangLuongPT);
        }

        // POST: BangLuongPTs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLuong,MaPT,ThangNam,TongDoanhThu,TongHoaHong,LuongCoBan,TongThanhToan,TrangThai,NgayThanhToan,GhiChu")] BangLuongPT bangLuongPT)
        {
            if (id != bangLuongPT.MaLuong)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bangLuongPT);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BangLuongPTExists(bangLuongPT.MaLuong))
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
            
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen", bangLuongPT.MaPT);
            return View(bangLuongPT);
        }

        // GET: BangLuongPTs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bangLuongPT = await _context.BangLuongPTs
                .Include(b => b.HuanLuyenVien)
                .FirstOrDefaultAsync(m => m.MaLuong == id);
                
            if (bangLuongPT == null)
            {
                return NotFound();
            }

            return View(bangLuongPT);
        }

        // POST: BangLuongPTs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bangLuongPT = await _context.BangLuongPTs.FindAsync(id);
            if (bangLuongPT != null)
            {
                // Gỡ bỏ liên kết với các phiên dạy
                var phienDays = await _context.PhienDays
                    .Where(p => p.MaBangLuong == id)
                    .ToListAsync();
                    
                foreach (var phien in phienDays)
                {
                    phien.MaBangLuong = null;
                }
                
                _context.BangLuongPTs.Remove(bangLuongPT);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BangLuongPTExists(int id)
        {
            return _context.BangLuongPTs.Any(e => e.MaLuong == id);
        }
    }
}
