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
    public class DoanhThusController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoanhThusController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DoanhThus
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.DoanhThu.Include(d => d.TaiKhoan).Include(d => d.ThanhToan);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: DoanhThus/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doanhThu = await _context.DoanhThu
                .Include(d => d.TaiKhoan)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(m => m.MaDoanhThu == id);
            if (doanhThu == null)
            {
                return NotFound();
            }

            return View(doanhThu);
        }

        // GET: DoanhThus/Create
        public IActionResult Create()
        {
            ViewData["NguoiTao"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash");
            ViewData["MaThanhToan"] = new SelectList(_context.ThanhToans, "MaThanhToan", "LoaiThanhToan");
            return View();
        }

        // POST: DoanhThus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDoanhThu,MaThanhToan,SoTien,Ngay,GhiChu,NgayTao,NguoiTao")] DoanhThu doanhThu)
        {
            if (ModelState.IsValid)
            {
                _context.Add(doanhThu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["NguoiTao"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", doanhThu.NguoiTao);
            ViewData["MaThanhToan"] = new SelectList(_context.ThanhToans, "MaThanhToan", "LoaiThanhToan", doanhThu.MaThanhToan);
            return View(doanhThu);
        }

        // GET: DoanhThus/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doanhThu = await _context.DoanhThu.FindAsync(id);
            if (doanhThu == null)
            {
                return NotFound();
            }
            ViewData["NguoiTao"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", doanhThu.NguoiTao);
            ViewData["MaThanhToan"] = new SelectList(_context.ThanhToans, "MaThanhToan", "LoaiThanhToan", doanhThu.MaThanhToan);
            return View(doanhThu);
        }

        // POST: DoanhThus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDoanhThu,MaThanhToan,SoTien,Ngay,GhiChu,NgayTao,NguoiTao")] DoanhThu doanhThu)
        {
            if (id != doanhThu.MaDoanhThu)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doanhThu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoanhThuExists(doanhThu.MaDoanhThu))
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
            ViewData["NguoiTao"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", doanhThu.NguoiTao);
            ViewData["MaThanhToan"] = new SelectList(_context.ThanhToans, "MaThanhToan", "LoaiThanhToan", doanhThu.MaThanhToan);
            return View(doanhThu);
        }

        // GET: DoanhThus/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doanhThu = await _context.DoanhThu
                .Include(d => d.TaiKhoan)
                .Include(d => d.ThanhToan)
                .FirstOrDefaultAsync(m => m.MaDoanhThu == id);
            if (doanhThu == null)
            {
                return NotFound();
            }

            return View(doanhThu);
        }

        // POST: DoanhThus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doanhThu = await _context.DoanhThu.FindAsync(id);
            if (doanhThu != null)
            {
                _context.DoanhThu.Remove(doanhThu);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DoanhThuExists(int id)
        {
            return _context.DoanhThu.Any(e => e.MaDoanhThu == id);
        }
    }
}
