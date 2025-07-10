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
    public class BaoCaoTaiChinhsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoTaiChinhsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BaoCaoTaiChinhs
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.BaoCaoTaiChinhs.Include(b => b.TaiKhoan);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: BaoCaoTaiChinhs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var baoCaoTaiChinh = await _context.BaoCaoTaiChinhs
                .Include(b => b.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaBaoCao == id);
            if (baoCaoTaiChinh == null)
            {
                return NotFound();
            }

            return View(baoCaoTaiChinh);
        }

        // GET: BaoCaoTaiChinhs/Create
        public IActionResult Create()
        {
            ViewData["NguoiLap"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap");
            return View();
        }

        // POST: BaoCaoTaiChinhs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaBaoCao,Thang,Nam,TongDoanhThu,NgayLapBaoCao,NguoiLap,TrangThai,GhiChu")] BaoCaoTaiChinh baoCaoTaiChinh)
        {
            if (ModelState.IsValid)
            {
                _context.Add(baoCaoTaiChinh);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["NguoiLap"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", baoCaoTaiChinh.NguoiLap);
            return View(baoCaoTaiChinh);
        }

        // GET: BaoCaoTaiChinhs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var baoCaoTaiChinh = await _context.BaoCaoTaiChinhs.FindAsync(id);
            if (baoCaoTaiChinh == null)
            {
                return NotFound();
            }
            ViewData["NguoiLap"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", baoCaoTaiChinh.NguoiLap);
            return View(baoCaoTaiChinh);
        }

        // POST: BaoCaoTaiChinhs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaBaoCao,Thang,Nam,TongDoanhThu,NgayLapBaoCao,NguoiLap,TrangThai,GhiChu")] BaoCaoTaiChinh baoCaoTaiChinh)
        {
            if (id != baoCaoTaiChinh.MaBaoCao)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(baoCaoTaiChinh);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BaoCaoTaiChinhExists(baoCaoTaiChinh.MaBaoCao))
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
            ViewData["NguoiLap"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", baoCaoTaiChinh.NguoiLap);
            return View(baoCaoTaiChinh);
        }

        // GET: BaoCaoTaiChinhs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var baoCaoTaiChinh = await _context.BaoCaoTaiChinhs
                .Include(b => b.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaBaoCao == id);
            if (baoCaoTaiChinh == null)
            {
                return NotFound();
            }

            return View(baoCaoTaiChinh);
        }

        // POST: BaoCaoTaiChinhs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var baoCaoTaiChinh = await _context.BaoCaoTaiChinhs.FindAsync(id);
            if (baoCaoTaiChinh != null)
            {
                _context.BaoCaoTaiChinhs.Remove(baoCaoTaiChinh);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BaoCaoTaiChinhExists(int id)
        {
            return _context.BaoCaoTaiChinhs.Any(e => e.MaBaoCao == id);
        }
    }
}
