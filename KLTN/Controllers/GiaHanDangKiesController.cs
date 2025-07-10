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
    public class GiaHanDangKiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GiaHanDangKiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GiaHanDangKies
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.GiaHanDangKys.Include(g => g.DangKy).Include(g => g.NguoiThu);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: GiaHanDangKies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var giaHanDangKy = await _context.GiaHanDangKys
                .Include(g => g.DangKy)
                .Include(g => g.NguoiThu)
                .FirstOrDefaultAsync(m => m.MaGiaHan == id);
            if (giaHanDangKy == null)
            {
                return NotFound();
            }

            return View(giaHanDangKy);
        }

        // GET: GiaHanDangKies/Create
        public IActionResult Create(int? maDangKy)
        {
            var dangKyList = _context.DangKys
                .Include(d => d.ThanhVien)
                .Include(d => d.KhachVangLai)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Select(d => new {
                    d.MaDangKy,
                    Display = 
                        d.LoaiDangKy == "GoiTap"
                            ? (d.GoiTap.TenGoi + " - " + (d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai.HoTen))
                            : (d.LopHoc.TenLop + " - " + (d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai.HoTen))
                }).ToList();

            ViewData["MaDangKy"] = new SelectList(dangKyList, "MaDangKy", "Display", maDangKy);
            ViewData["MaTKNguoiThu"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap");

            if (maDangKy.HasValue)
            {
                var dangKyCu = _context.DangKys
                    .Include(d => d.GoiTap)
                    .Include(d => d.LopHoc)
                    .FirstOrDefault(d => d.MaDangKy == maDangKy.Value);
                if (dangKyCu != null)
                {
                    ViewBag.NgayKetThucCu = dangKyCu.NgayKetThuc;
                    ViewBag.SoBuoiCu = dangKyCu.SoBuoi;
                    ViewBag.LoaiDangKyCu = dangKyCu.LoaiDangKy;
                    ViewBag.GhiChuCu = dangKyCu.GhiChu;
                    ViewBag.MaGoiTapCu = dangKyCu.MaGoiTap;
                    ViewBag.MaLopHocCu = dangKyCu.MaLopHoc;
                    ViewBag.NgayBatDauCu = dangKyCu.NgayBatDau;
                    ViewBag.NgayDangKyCu = dangKyCu.NgayDangKy;
                    ViewBag.TenGoiTapCu = dangKyCu.GoiTap?.TenGoi;
                    ViewBag.TenLopHocCu = dangKyCu.LopHoc?.TenLop;
                }
            }
            return View();
        }

        // POST: GiaHanDangKies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaGiaHan,MaDangKy,NgayKetThucMoi,SoBuoiThem,SoTien,NgayGiaHan,MaTKNguoiThu,TrangThai,GhiChu")] GiaHanDangKy giaHanDangKy)
        {
            if (ModelState.IsValid)
            {
                _context.Add(giaHanDangKy);
                // Cập nhật đăng ký gốc
                var dangKy = await _context.DangKys.FindAsync(giaHanDangKy.MaDangKy);
                if (dangKy != null)
                {
                    dangKy.NgayKetThuc = giaHanDangKy.NgayKetThucMoi;
                    if (giaHanDangKy.SoBuoiThem.HasValue)
                    {
                        dangKy.SoBuoi = giaHanDangKy.SoBuoiThem;
                    }
                    dangKy.TrangThai = "DangHoatDong";
                    _context.Update(dangKy);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var dangKyList = _context.DangKys
                .Include(d => d.ThanhVien)
                .Include(d => d.KhachVangLai)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Select(d => new {
                    d.MaDangKy,
                    Display = 
                        d.LoaiDangKy == "GoiTap"
                            ? (d.GoiTap.TenGoi + " - " + (d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai.HoTen))
                            : (d.LopHoc.TenLop + " - " + (d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai.HoTen))
                }).ToList();

            ViewData["MaDangKy"] = new SelectList(dangKyList, "MaDangKy", "Display", giaHanDangKy.MaDangKy);
            ViewData["MaTKNguoiThu"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", giaHanDangKy.MaTKNguoiThu);
            return View(giaHanDangKy);
        }

        // GET: GiaHanDangKies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var giaHanDangKy = await _context.GiaHanDangKys.FindAsync(id);
            if (giaHanDangKy == null)
            {
                return NotFound();
            }
            var dangKyList = _context.DangKys
                .Include(d => d.ThanhVien)
                .Include(d => d.KhachVangLai)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Select(d => new {
                    d.MaDangKy,
                    Display = 
                        d.LoaiDangKy == "GoiTap"
                            ? (d.GoiTap.TenGoi + " - " + (d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai.HoTen))
                            : (d.LopHoc.TenLop + " - " + (d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai.HoTen))
                }).ToList();

            ViewData["MaDangKy"] = new SelectList(dangKyList, "MaDangKy", "Display", giaHanDangKy.MaDangKy);
            ViewData["MaTKNguoiThu"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", giaHanDangKy.MaTKNguoiThu);
            return View(giaHanDangKy);
        }

        // POST: GiaHanDangKies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaGiaHan,MaDangKy,NgayKetThucMoi,SoBuoiThem,SoTien,NgayGiaHan,MaTKNguoiThu,TrangThai,GhiChu")] GiaHanDangKy giaHanDangKy)
        {
            if (id != giaHanDangKy.MaGiaHan)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(giaHanDangKy);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GiaHanDangKyExists(giaHanDangKy.MaGiaHan))
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
            var dangKyList = _context.DangKys
                .Include(d => d.ThanhVien)
                .Include(d => d.KhachVangLai)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Select(d => new {
                    d.MaDangKy,
                    Display = 
                        d.LoaiDangKy == "GoiTap"
                            ? (d.GoiTap.TenGoi + " - " + (d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai.HoTen))
                            : (d.LopHoc.TenLop + " - " + (d.ThanhVien != null ? d.ThanhVien.HoTen : d.KhachVangLai.HoTen))
                }).ToList();

            ViewData["MaDangKy"] = new SelectList(dangKyList, "MaDangKy", "Display", giaHanDangKy.MaDangKy);
            ViewData["MaTKNguoiThu"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", giaHanDangKy.MaTKNguoiThu);
            return View(giaHanDangKy);
        }

        // GET: GiaHanDangKies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var giaHanDangKy = await _context.GiaHanDangKys
                .Include(g => g.DangKy)
                .Include(g => g.NguoiThu)
                .FirstOrDefaultAsync(m => m.MaGiaHan == id);
            if (giaHanDangKy == null)
            {
                return NotFound();
            }

            return View(giaHanDangKy);
        }

        // POST: GiaHanDangKies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var giaHanDangKy = await _context.GiaHanDangKys.FindAsync(id);
            if (giaHanDangKy != null)
            {
                _context.GiaHanDangKys.Remove(giaHanDangKy);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GiaHanDangKyExists(int id)
        {
            return _context.GiaHanDangKys.Any(e => e.MaGiaHan == id);
        }
    }
}
