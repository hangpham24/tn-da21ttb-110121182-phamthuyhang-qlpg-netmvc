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
    public class ThanhToansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThanhToansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ThanhToans
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ThanhToans.Include(t => t.DangKy).Include(t => t.GiaHanDangKy).Include(t => t.NguoiThu);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ThanhToans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thanhToan = await _context.ThanhToans
                .Include(t => t.DangKy)
                .Include(t => t.GiaHanDangKy)
                .Include(t => t.NguoiThu)
                .FirstOrDefaultAsync(m => m.MaThanhToan == id);
            if (thanhToan == null)
            {
                return NotFound();
            }

            return View(thanhToan);
        }

        // GET: ThanhToans/Create
        public IActionResult Create()
        {
            ViewData["MaDangKy"] = new SelectList(_context.DangKys, "MaDangKy", "LoaiDangKy");
            ViewData["MaGiaHan"] = new SelectList(_context.GiaHanDangKys, "MaGiaHan", "TrangThai");
            ViewData["MaTKNguoiThu"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash");
            return View();
        }

        // POST: ThanhToans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaThanhToan,LoaiThanhToan,MaDangKy,MaGiaHan,MaTK_NguoiDung,MaKVL_NguoiDung,SoTien,PhuongThucThanhToan,NgayThanhToan,MaTKNguoiThu,TrangThai,GhiChu,MaGiaoDich,DonViThanhToan,TaiKhoanThanhToan,HoaDonDienTuUrl,DaXuatHoaDon")] ThanhToan thanhToan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(thanhToan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaDangKy"] = new SelectList(_context.DangKys, "MaDangKy", "LoaiDangKy", thanhToan.MaDangKy);
            ViewData["MaGiaHan"] = new SelectList(_context.GiaHanDangKys, "MaGiaHan", "TrangThai", thanhToan.MaGiaHan);
            ViewData["MaTKNguoiThu"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", thanhToan.MaTKNguoiThu);
            return View(thanhToan);
        }

        // GET: ThanhToans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thanhToan = await _context.ThanhToans.FindAsync(id);
            if (thanhToan == null)
            {
                return NotFound();
            }
            ViewData["MaDangKy"] = new SelectList(_context.DangKys, "MaDangKy", "LoaiDangKy", thanhToan.MaDangKy);
            ViewData["MaGiaHan"] = new SelectList(_context.GiaHanDangKys, "MaGiaHan", "TrangThai", thanhToan.MaGiaHan);
            ViewData["MaTKNguoiThu"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", thanhToan.MaTKNguoiThu);
            return View(thanhToan);
        }

        // POST: ThanhToans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaThanhToan,LoaiThanhToan,MaDangKy,MaGiaHan,MaTK_NguoiDung,MaKVL_NguoiDung,SoTien,PhuongThucThanhToan,NgayThanhToan,MaTKNguoiThu,TrangThai,GhiChu,MaGiaoDich,DonViThanhToan,TaiKhoanThanhToan,HoaDonDienTuUrl,DaXuatHoaDon")] ThanhToan thanhToan)
        {
            if (id != thanhToan.MaThanhToan)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(thanhToan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThanhToanExists(thanhToan.MaThanhToan))
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
            ViewData["MaDangKy"] = new SelectList(_context.DangKys, "MaDangKy", "LoaiDangKy", thanhToan.MaDangKy);
            ViewData["MaGiaHan"] = new SelectList(_context.GiaHanDangKys, "MaGiaHan", "TrangThai", thanhToan.MaGiaHan);
            ViewData["MaTKNguoiThu"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", thanhToan.MaTKNguoiThu);
            return View(thanhToan);
        }

        // GET: ThanhToans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thanhToan = await _context.ThanhToans
                .Include(t => t.DangKy)
                .Include(t => t.GiaHanDangKy)
                .Include(t => t.NguoiThu)
                .FirstOrDefaultAsync(m => m.MaThanhToan == id);
            if (thanhToan == null)
            {
                return NotFound();
            }

            return View(thanhToan);
        }

        // POST: ThanhToans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var thanhToan = await _context.ThanhToans.FindAsync(id);
            if (thanhToan != null)
            {
                _context.ThanhToans.Remove(thanhToan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ThanhToanExists(int id)
        {
            return _context.ThanhToans.Any(e => e.MaThanhToan == id);
        }
    }
}
