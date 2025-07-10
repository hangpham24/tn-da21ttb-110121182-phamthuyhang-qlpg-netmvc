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
    public class KhachVangLaisController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KhachVangLaisController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: KhachVangLais
        public async Task<IActionResult> Index()
        {
            return View(await _context.KhachVangLais.ToListAsync());
        }

        // GET: KhachVangLais/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khachVangLai = await _context.KhachVangLais
                .FirstOrDefaultAsync(m => m.MaKVL == id);
            if (khachVangLai == null)
            {
                return NotFound();
            }

            return View(khachVangLai);
        }

        // GET: KhachVangLais/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: KhachVangLais/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaKVL,HoTen,SoDienThoai,Email,NgayGhiNhan,GhiChu,GiaTien")] KhachVangLai khachVangLai)
        {
            if (ModelState.IsValid)
            {
                _context.Add(khachVangLai);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(khachVangLai);
        }

        // GET: KhachVangLais/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khachVangLai = await _context.KhachVangLais.FindAsync(id);
            if (khachVangLai == null)
            {
                return NotFound();
            }
            return View(khachVangLai);
        }

        // POST: KhachVangLais/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaKVL,HoTen,SoDienThoai,Email,NgayGhiNhan,GhiChu,GiaTien")] KhachVangLai khachVangLai)
        {
            if (id != khachVangLai.MaKVL)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(khachVangLai);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhachVangLaiExists(khachVangLai.MaKVL))
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
            return View(khachVangLai);
        }

        // GET: KhachVangLais/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var khachVangLai = await _context.KhachVangLais
                .FirstOrDefaultAsync(m => m.MaKVL == id);
            if (khachVangLai == null)
            {
                return NotFound();
            }

            return View(khachVangLai);
        }

        // POST: KhachVangLais/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var khachVangLai = await _context.KhachVangLais.FindAsync(id);
            if (khachVangLai != null)
            {
                _context.KhachVangLais.Remove(khachVangLai);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KhachVangLaiExists(int id)
        {
            return _context.KhachVangLais.Any(e => e.MaKVL == id);
        }
    }
}
