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
    public class ThongBaosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThongBaosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ThongBaos
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ThongBaos
                .Include(t => t.TaiKhoan)
                .ThenInclude(t => t.ThanhVien);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ThongBaos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thongBao = await _context.ThongBaos
                .Include(t => t.TaiKhoan)
                .ThenInclude(t => t.ThanhVien)
                .FirstOrDefaultAsync(m => m.MaThongBao == id);
            if (thongBao == null)
            {
                return NotFound();
            }

            return View(thongBao);
        }

        // GET: ThongBaos/Create
        public IActionResult Create()
        {
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans.Include(t => t.ThanhVien), "MaTK", "TenDangNhap");
            return View();
        }

        // POST: ThongBaos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaThongBao,TieuDe,NoiDung,NgayGui,MaTK,DaDoc")] ThongBao thongBao)
        {
            if (ModelState.IsValid)
            {
                _context.Add(thongBao);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans.Include(t => t.ThanhVien), "MaTK", "TenDangNhap", thongBao.MaTK);
            return View(thongBao);
        }

        // GET: ThongBaos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thongBao = await _context.ThongBaos.FindAsync(id);
            if (thongBao == null)
            {
                return NotFound();
            }
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans.Include(t => t.ThanhVien), "MaTK", "TenDangNhap", thongBao.MaTK);
            return View(thongBao);
        }

        // POST: ThongBaos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaThongBao,TieuDe,NoiDung,NgayGui,MaTK,DaDoc")] ThongBao thongBao)
        {
            if (id != thongBao.MaThongBao)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(thongBao);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThongBaoExists(thongBao.MaThongBao))
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
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans.Include(t => t.ThanhVien), "MaTK", "TenDangNhap", thongBao.MaTK);
            return View(thongBao);
        }

        // GET: ThongBaos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thongBao = await _context.ThongBaos
                .Include(t => t.TaiKhoan)
                .ThenInclude(t => t.ThanhVien)
                .FirstOrDefaultAsync(m => m.MaThongBao == id);
            if (thongBao == null)
            {
                return NotFound();
            }

            return View(thongBao);
        }

        // POST: ThongBaos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var thongBao = await _context.ThongBaos.FindAsync(id);
            if (thongBao != null)
            {
                _context.ThongBaos.Remove(thongBao);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ThongBaoExists(int id)
        {
            return _context.ThongBaos.Any(e => e.MaThongBao == id);
        }
    }
}
