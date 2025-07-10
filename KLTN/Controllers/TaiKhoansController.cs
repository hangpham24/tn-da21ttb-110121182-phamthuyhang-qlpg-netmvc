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
    public class TaiKhoansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaiKhoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TaiKhoans
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["LastLoginSortParm"] = sortOrder == "lastlogin" ? "lastlogin_desc" : "lastlogin";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var taiKhoans = _context.TaiKhoans
                .Include(t => t.Quyen)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                taiKhoans = taiKhoans.Where(t => t.TenDangNhap.Contains(searchString)
                || t.Quyen.TenQuyen.Contains(searchString)
                || t.NgayTao.ToString().Contains(searchString)
                || t.LanDangNhapCuoi.ToString().Contains(searchString)
                );
            }

            taiKhoans = sortOrder switch
            {
                "name_desc" => taiKhoans.OrderByDescending(t => t.TenDangNhap),
                "status" => taiKhoans.OrderBy(t => t.TrangThai),
                "status_desc" => taiKhoans.OrderByDescending(t => t.TrangThai),
                "date" => taiKhoans.OrderBy(t => t.NgayTao),
                "date_desc" => taiKhoans.OrderByDescending(t => t.NgayTao),
                "lastlogin" => taiKhoans.OrderBy(t => t.LanDangNhapCuoi),
                "lastlogin_desc" => taiKhoans.OrderByDescending(t => t.LanDangNhapCuoi),
                _ => taiKhoans.OrderBy(t => t.TenDangNhap),
            };

            const int pageSize = 5;
            var totalItems = await taiKhoans.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            pageNumber = pageNumber ?? 1;
            pageNumber = Math.Max(1, Math.Min(pageNumber.Value, totalPages));

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;

            var items = await taiKhoans
                .Skip((pageNumber.Value - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        // GET: TaiKhoans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.Quyen)
                .FirstOrDefaultAsync(m => m.MaTK == id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        // GET: TaiKhoans/Create
        public IActionResult Create()
        {
            ViewData["MaQuyen"] = new SelectList(_context.Quyens, "MaQuyen", "TenQuyen");
            return View();
        }

        // POST: TaiKhoans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaTK,TenDangNhap,MatKhauHash,MaQuyen,TrangThai,NgayTao,LanDangNhapCuoi")] TaiKhoan taiKhoan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(taiKhoan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaQuyen"] = new SelectList(_context.Quyens, "MaQuyen", "TenQuyen", taiKhoan.MaQuyen);
            return View(taiKhoan);
        }

        // GET: TaiKhoans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan == null)
            {
                return NotFound();
            }
            ViewData["MaQuyen"] = new SelectList(_context.Quyens, "MaQuyen", "TenQuyen", taiKhoan.MaQuyen);
            return View(taiKhoan);
        }

        // POST: TaiKhoans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaTK,TenDangNhap,MatKhauHash,MaQuyen,TrangThai,NgayTao,LanDangNhapCuoi")] TaiKhoan taiKhoan)
        {
            if (id != taiKhoan.MaTK)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taiKhoan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaiKhoanExists(taiKhoan.MaTK))
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
            ViewData["MaQuyen"] = new SelectList(_context.Quyens, "MaQuyen", "TenQuyen", taiKhoan.MaQuyen);
            return View(taiKhoan);
        }

        // GET: TaiKhoans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.Quyen)
                .FirstOrDefaultAsync(m => m.MaTK == id);
            if (taiKhoan == null)
            {
                return NotFound();
            }

            return View(taiKhoan);
        }

        // POST: TaiKhoans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(id);
            if (taiKhoan != null)
            {
                _context.TaiKhoans.Remove(taiKhoan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TaiKhoanExists(int id)
        {
            return _context.TaiKhoans.Any(e => e.MaTK == id);
        }
    }
}
