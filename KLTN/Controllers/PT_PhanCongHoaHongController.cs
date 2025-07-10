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
    public class PT_PhanCongHoaHongController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PT_PhanCongHoaHongController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PT_PhanCongHoaHong
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PT_PhanCongHoaHongs.Include(p => p.GoiTap).Include(p => p.HuanLuyenVien).Include(p => p.LopHoc);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PT_PhanCongHoaHong/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pT_PhanCongHoaHong = await _context.PT_PhanCongHoaHongs
                .Include(p => p.GoiTap)
                .Include(p => p.HuanLuyenVien)
                .Include(p => p.LopHoc)
                .FirstOrDefaultAsync(m => m.MaPhanCong == id);
            if (pT_PhanCongHoaHong == null)
            {
                return NotFound();
            }

            return View(pT_PhanCongHoaHong);
        }

        // GET: PT_PhanCongHoaHong/Create
        public IActionResult Create()
        {
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi");
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "MaPT");
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "NgayTrongTuan");
            return View();
        }

        // POST: PT_PhanCongHoaHong/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaPhanCong,MaPT,MaGoiTap,MaLopHoc,PhanTramHoaHong")] PT_PhanCongHoaHong pT_PhanCongHoaHong)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pT_PhanCongHoaHong);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", pT_PhanCongHoaHong.MaGoiTap);
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "MaPT", pT_PhanCongHoaHong.MaPT);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "NgayTrongTuan", pT_PhanCongHoaHong.MaLopHoc);
            return View(pT_PhanCongHoaHong);
        }

        // GET: PT_PhanCongHoaHong/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pT_PhanCongHoaHong = await _context.PT_PhanCongHoaHongs.FindAsync(id);
            if (pT_PhanCongHoaHong == null)
            {
                return NotFound();
            }
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", pT_PhanCongHoaHong.MaGoiTap);
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "MaPT", pT_PhanCongHoaHong.MaPT);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "NgayTrongTuan", pT_PhanCongHoaHong.MaLopHoc);
            return View(pT_PhanCongHoaHong);
        }

        // POST: PT_PhanCongHoaHong/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhanCong,MaPT,MaGoiTap,MaLopHoc,PhanTramHoaHong")] PT_PhanCongHoaHong pT_PhanCongHoaHong)
        {
            if (id != pT_PhanCongHoaHong.MaPhanCong)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pT_PhanCongHoaHong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PT_PhanCongHoaHongExists(pT_PhanCongHoaHong.MaPhanCong))
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
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", pT_PhanCongHoaHong.MaGoiTap);
            ViewData["MaPT"] = new SelectList(_context.HuanLuyenViens, "MaPT", "MaPT", pT_PhanCongHoaHong.MaPT);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "NgayTrongTuan", pT_PhanCongHoaHong.MaLopHoc);
            return View(pT_PhanCongHoaHong);
        }

        // GET: PT_PhanCongHoaHong/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pT_PhanCongHoaHong = await _context.PT_PhanCongHoaHongs
                .Include(p => p.GoiTap)
                .Include(p => p.HuanLuyenVien)
                .Include(p => p.LopHoc)
                .FirstOrDefaultAsync(m => m.MaPhanCong == id);
            if (pT_PhanCongHoaHong == null)
            {
                return NotFound();
            }

            return View(pT_PhanCongHoaHong);
        }

        // POST: PT_PhanCongHoaHong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pT_PhanCongHoaHong = await _context.PT_PhanCongHoaHongs.FindAsync(id);
            if (pT_PhanCongHoaHong != null)
            {
                _context.PT_PhanCongHoaHongs.Remove(pT_PhanCongHoaHong);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PT_PhanCongHoaHongExists(int id)
        {
            return _context.PT_PhanCongHoaHongs.Any(e => e.MaPhanCong == id);
        }
    }
}
