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

namespace KLTN.Controllers
{
    [Authorize]
    public class LichSuCheckInsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichSuCheckInsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LichSuCheckIns
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.LichSuCheckIns
                .Include(l => l.TaiKhoan)
                .Include(l => l.KhachVangLai);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: LichSuCheckIns/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichSuCheckIn = await _context.LichSuCheckIns
                .Include(l => l.TaiKhoan)
                .Include(l => l.KhachVangLai)
                .FirstOrDefaultAsync(m => m.MaCheckIn == id);
            if (lichSuCheckIn == null)
            {
                return NotFound();
            }

            return View(lichSuCheckIn);
        }

        // GET: LichSuCheckIns/Create
        public IActionResult Create()
        {
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap");
            ViewData["MaKVL"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen");
            return View();
        }

        // POST: LichSuCheckIns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaCheckIn,MaTK,MaKVL,ThoiGian,KetQuaNhanDien,AnhNhanDien")] LichSuCheckIn lichSuCheckIn)
        {
            if (ModelState.IsValid)
            {
                _context.Add(lichSuCheckIn);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", lichSuCheckIn.MaTK);
            ViewData["MaKVL"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", lichSuCheckIn.MaKVL);
            return View(lichSuCheckIn);
        }

        // GET: LichSuCheckIns/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichSuCheckIn = await _context.LichSuCheckIns.FindAsync(id);
            if (lichSuCheckIn == null)
            {
                return NotFound();
            }
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", lichSuCheckIn.MaTK);
            ViewData["MaKVL"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", lichSuCheckIn.MaKVL);
            return View(lichSuCheckIn);
        }

        // POST: LichSuCheckIns/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaCheckIn,MaTK,MaKVL,ThoiGian,KetQuaNhanDien,AnhNhanDien")] LichSuCheckIn lichSuCheckIn)
        {
            if (id != lichSuCheckIn.MaCheckIn)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lichSuCheckIn);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LichSuCheckInExists(lichSuCheckIn.MaCheckIn))
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
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "TenDangNhap", lichSuCheckIn.MaTK);
            ViewData["MaKVL"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", lichSuCheckIn.MaKVL);
            return View(lichSuCheckIn);
        }

        // GET: LichSuCheckIns/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichSuCheckIn = await _context.LichSuCheckIns
                .Include(l => l.TaiKhoan)
                .Include(l => l.KhachVangLai)
                .FirstOrDefaultAsync(m => m.MaCheckIn == id);
            if (lichSuCheckIn == null)
            {
                return NotFound();
            }

            return View(lichSuCheckIn);
        }

        // POST: LichSuCheckIns/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lichSuCheckIn = await _context.LichSuCheckIns.FindAsync(id);
            if (lichSuCheckIn != null)
            {
                _context.LichSuCheckIns.Remove(lichSuCheckIn);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LichSuCheckInExists(int id)
        {
            return _context.LichSuCheckIns.Any(e => e.MaCheckIn == id);
        }
    }
}
