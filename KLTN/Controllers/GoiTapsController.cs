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
    public class GoiTapsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GoiTapsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GoiTaps
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DurationSortParm"] = sortOrder == "duration" ? "duration_desc" : "duration";
            ViewData["PriceSortParm"] = sortOrder == "price" ? "price_desc" : "price";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var goiTaps = _context.GoiTap.Include(g => g.KhuyenMai).AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                goiTaps = goiTaps.Where(g => g.TenGoi.Contains(searchString));
            }

            goiTaps = sortOrder switch
            {
                "name_desc" => goiTaps.OrderByDescending(g => g.TenGoi),
                "duration" => goiTaps.OrderBy(g => g.ThoiHanThang),
                "duration_desc" => goiTaps.OrderByDescending(g => g.ThoiHanThang),
                "price" => goiTaps.OrderBy(g => g.GiaTien),
                "price_desc" => goiTaps.OrderByDescending(g => g.GiaTien),
                _ => goiTaps.OrderBy(g => g.TenGoi),
            };

            const int pageSize = 5;
            var totalItems = await goiTaps.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            pageNumber = pageNumber ?? 1;
            pageNumber = Math.Max(1, Math.Min(pageNumber.Value, totalPages));

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;

            var items = await goiTaps
                .Skip((pageNumber.Value - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        // GET: GoiTaps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var goiTap = await _context.GoiTap
                .Include(g => g.KhuyenMai)
                .FirstOrDefaultAsync(m => m.MaGoi == id);
            if (goiTap == null)
            {
                return NotFound();
            }

            return View(goiTap);
        }

        // GET: GoiTaps/Create
        public IActionResult Create()
        {
            ViewData["MaKM"] = new SelectList(_context.KhuyenMais, "MaKM", "TenKM");
            return View();
        }

        // POST: GoiTaps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaGoi,TenGoi,MoTa,ThoiHanThang,GiaTien,SoLanTapToiDa,LoaiGoiTap,MaKM")] GoiTap goiTap)
        {
            if (ModelState.IsValid)
            {
                _context.Add(goiTap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaKM"] = new SelectList(_context.KhuyenMais, "MaKM", "TenKM", goiTap.MaKM);
            return View(goiTap);
        }

        // GET: GoiTaps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var goiTap = await _context.GoiTap.FindAsync(id);
            if (goiTap == null)
            {
                return NotFound();
            }
            ViewData["MaKM"] = new SelectList(_context.KhuyenMais, "MaKM", "TenKM", goiTap.MaKM);
            return View(goiTap);
        }

        // POST: GoiTaps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaGoi,TenGoi,MoTa,ThoiHanThang,GiaTien,SoLanTapToiDa,LoaiGoiTap,MaKM")] GoiTap goiTap)
        {
            if (id != goiTap.MaGoi)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(goiTap);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GoiTapExists(goiTap.MaGoi))
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
            ViewData["MaKM"] = new SelectList(_context.KhuyenMais, "MaKM", "TenKM", goiTap.MaKM);
            return View(goiTap);
        }

        // GET: GoiTaps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var goiTap = await _context.GoiTap
                .Include(g => g.KhuyenMai)
                .FirstOrDefaultAsync(m => m.MaGoi == id);
            if (goiTap == null)
            {
                return NotFound();
            }

            return View(goiTap);
        }

        // POST: GoiTaps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var goiTap = await _context.GoiTap.FindAsync(id);
            if (goiTap != null)
            {
                _context.GoiTap.Remove(goiTap);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GoiTapExists(int id)
        {
            return _context.GoiTap.Any(e => e.MaGoi == id);
        }
    }
}
