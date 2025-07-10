using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KLTN.Data;
using KLTN.Models.Database;
using KLTN.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace KLTN.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LopHocsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LopHocsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LopHocs
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string search, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["PtSortParm"] = sortOrder == "pt" ? "pt_desc" : "pt";
            ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["CurrentFilter"] = search;

            if (search != null)
            {
                pageNumber = 1;
            }
            else
            {
                search = currentFilter;
            }

            var lopHocs = _context.LopHoc.Include(l => l.HuanLuyenVien).AsQueryable();

            if (!String.IsNullOrEmpty(search))
            {
                lopHocs = lopHocs.Where(l => 
                    l.TenLop.Contains(search) ||
                    (l.HuanLuyenVien != null && l.HuanLuyenVien.HoTen.Contains(search)) ||
                    l.NgayTrongTuan.Contains(search) ||
                    l.TrangThai.Contains(search)
                );
            }

            lopHocs = sortOrder switch
            {
                "name_desc" => lopHocs.OrderByDescending(l => l.TenLop),
                "pt" => lopHocs.OrderBy(l => l.HuanLuyenVien.HoTen),
                "pt_desc" => lopHocs.OrderByDescending(l => l.HuanLuyenVien.HoTen),
                "status" => lopHocs.OrderBy(l => l.TrangThai),
                "status_desc" => lopHocs.OrderByDescending(l => l.TrangThai),
                _ => lopHocs.OrderBy(l => l.TenLop),
            };

            const int pageSize = 5;
            var totalItems = await lopHocs.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            pageNumber = pageNumber ?? 1;
            pageNumber = Math.Max(1, Math.Min(pageNumber.Value, Math.Max(1, totalPages)));

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;

            var items = await lopHocs
                .Skip((pageNumber.Value - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        // GET: LopHocs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lopHoc = await _context.LopHoc
                .Include(l => l.HuanLuyenVien)
                .FirstOrDefaultAsync(m => m.MaLop == id);
            if (lopHoc == null)
            {
                return NotFound();
            }

            return View(lopHoc);
        }

        // GET: LopHocs/Create
        public IActionResult Create()
        {
            var huanLuyenViens = _context.HuanLuyenViens.ToList();
            if (huanLuyenViens != null && huanLuyenViens.Any())
            {
                ViewBag.MaPT = new SelectList(huanLuyenViens, "MaPT", "HoTen");
            }
            else
            {
                ViewBag.MaPT = new SelectList(new List<SelectListItem>());
            }
            return View(new LopHocViewModel());
        }

        // POST: LopHocs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaLop,TenLop,MaPT,ThoiGianBatDau,ThoiGianKetThuc,SelectedDays,SoLuongToiDa,SoLuongHienTai,TrangThai,GhiChu")] LopHocViewModel lopHocVM)
        {
            if (ModelState.IsValid)
            {
                var lopHoc = lopHocVM.ToLopHoc();
                _context.Add(lopHoc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            var huanLuyenViens = _context.HuanLuyenViens.ToList();
            if (huanLuyenViens != null && huanLuyenViens.Any())
            {
                ViewBag.MaPT = new SelectList(huanLuyenViens, "MaPT", "HoTen", lopHocVM.MaPT);
            }
            else
            {
                ViewBag.MaPT = new SelectList(new List<SelectListItem>());
            }
            return View(lopHocVM);
        }

        // GET: LopHocs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lopHoc = await _context.LopHoc.FindAsync(id);
            if (lopHoc == null)
            {
                return NotFound();
            }
            
            var lopHocVM = LopHocViewModel.FromLopHoc(lopHoc);
            
            var huanLuyenViens = _context.HuanLuyenViens.ToList();
            if (huanLuyenViens != null && huanLuyenViens.Any())
            {
                ViewBag.MaPT = new SelectList(huanLuyenViens, "MaPT", "HoTen", lopHoc.MaPT);
            }
            else
            {
                ViewBag.MaPT = new SelectList(new List<SelectListItem>());
            }
            return View(lopHocVM);
        }

        // POST: LopHocs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLop,TenLop,MaPT,ThoiGianBatDau,ThoiGianKetThuc,SelectedDays,SoLuongToiDa,SoLuongHienTai,TrangThai,GhiChu")] LopHocViewModel lopHocVM)
        {
            if (id != lopHocVM.MaLop)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var lopHoc = lopHocVM.ToLopHoc();
                    _context.Update(lopHoc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LopHocExists(lopHocVM.MaLop))
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
            
            var huanLuyenViens = _context.HuanLuyenViens.ToList();
            if (huanLuyenViens != null && huanLuyenViens.Any())
            {
                ViewBag.MaPT = new SelectList(huanLuyenViens, "MaPT", "HoTen", lopHocVM.MaPT);
            }
            else
            {
                ViewBag.MaPT = new SelectList(new List<SelectListItem>());
            }
            return View(lopHocVM);
        }

        // GET: LopHocs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lopHoc = await _context.LopHoc
                .Include(l => l.HuanLuyenVien)
                .FirstOrDefaultAsync(m => m.MaLop == id);
            if (lopHoc == null)
            {
                return NotFound();
            }

            return View(lopHoc);
        }

        // POST: LopHocs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lopHoc = await _context.LopHoc.FindAsync(id);
            if (lopHoc != null)
            {
                _context.LopHoc.Remove(lopHoc);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LopHocExists(int id)
        {
            return _context.LopHoc.Any(e => e.MaLop == id);
        }
    }
}
