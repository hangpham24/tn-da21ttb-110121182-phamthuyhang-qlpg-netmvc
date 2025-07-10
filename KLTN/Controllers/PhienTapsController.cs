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
    [Authorize(Roles = "Admin")]
    public class PhienTapsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PhienTapsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PhienTaps
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PhienTap.Include(p => p.HuanLuyenVien).Include(p => p.KhachVangLai).Include(p => p.ThanhVien);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PhienTaps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phienTap = await _context.PhienTap
                .Include(p => p.HuanLuyenVien)
                .Include(p => p.KhachVangLai)
                .Include(p => p.ThanhVien)
                .FirstOrDefaultAsync(m => m.MaPhien == id);
            if (phienTap == null)
            {
                return NotFound();
            }

            return View(phienTap);
        }

        // GET: PhienTaps/Create
        public IActionResult Create()
        {
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen");
            ViewData["MaKhachVangLai"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen");
            ViewData["MaThanhVien"] = new SelectList(_context.ThanhViens, "MaTV", "HoTen");
            return View();
        }

        // POST: PhienTaps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaPhien,MaThanhVien,MaKhachVangLai,MaPT,NgayTap,GhiChu,TinhTrang")] PhienTap phienTap)
        {
            if (ModelState.IsValid)
            {
                _context.Add(phienTap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen", phienTap.MaPT);
            ViewData["MaKhachVangLai"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", phienTap.MaKhachVangLai);
            ViewData["MaThanhVien"] = new SelectList(_context.ThanhViens, "MaTV", "HoTen", phienTap.MaThanhVien);
            return View(phienTap);
        }

        // GET: PhienTaps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phienTap = await _context.PhienTap.FindAsync(id);
            if (phienTap == null)
            {
                return NotFound();
            }
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen", phienTap.MaPT);
            ViewData["MaKhachVangLai"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", phienTap.MaKhachVangLai);
            ViewData["MaThanhVien"] = new SelectList(_context.ThanhViens, "MaTV", "HoTen", phienTap.MaThanhVien);
            return View(phienTap);
        }

        // POST: PhienTaps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhien,MaThanhVien,MaKhachVangLai,MaPT,NgayTap,GhiChu,TinhTrang")] PhienTap phienTap)
        {
            if (id != phienTap.MaPhien)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phienTap);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhienTapExists(phienTap.MaPhien))
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
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "HoTen", phienTap.MaPT);
            ViewData["MaKhachVangLai"] = new SelectList(_context.KhachVangLais, "MaKVL", "HoTen", phienTap.MaKhachVangLai);
            ViewData["MaThanhVien"] = new SelectList(_context.ThanhViens, "MaTV", "HoTen", phienTap.MaThanhVien);
            return View(phienTap);
        }

        // GET: PhienTaps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phienTap = await _context.PhienTap
                .Include(p => p.HuanLuyenVien)
                .Include(p => p.KhachVangLai)
                .Include(p => p.ThanhVien)
                .FirstOrDefaultAsync(m => m.MaPhien == id);
            if (phienTap == null)
            {
                return NotFound();
            }

            return View(phienTap);
        }

        // POST: PhienTaps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phienTap = await _context.PhienTap.FindAsync(id);
            if (phienTap != null)
            {
                _context.PhienTap.Remove(phienTap);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhienTapExists(int id)
        {
            return _context.PhienTap.Any(e => e.MaPhien == id);
        }
    }
}
