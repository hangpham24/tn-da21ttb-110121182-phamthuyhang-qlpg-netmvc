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
    public class TinTucsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TinTucsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TinTucs
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.TinTucs.Include(t => t.TaiKhoan);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: /TinTucs/Public
        [AllowAnonymous]
        public async Task<IActionResult> PublicList(string category = null, string search = null)
        {
            // Get news that are visible (HienThi = true)
            var query = _context.TinTucs
                .Include(t => t.TaiKhoan)
                .Where(t => t.HienThi == true);

            // Filter by category if provided
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.DanhMuc == category);
            }

            // Filter by search term if provided
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.TieuDe.Contains(search) || t.MoTaNgan.Contains(search) || t.NoiDung.Contains(search));
            }

            // Order by date (newest first) and whether they are featured
            var tinTucs = await query
                .OrderByDescending(t => t.NoiBat)
                .ThenByDescending(t => t.NgayDang)
                .ToListAsync();

            return View(tinTucs);
        }

        // GET: /TinTucs/PublicDetails/5
        [AllowAnonymous]
        public async Task<IActionResult> PublicDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tinTuc = await _context.TinTucs
                .Include(t => t.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaTinTuc == id && m.HienThi == true);
                
            if (tinTuc == null)
            {
                return NotFound();
            }

            return View(tinTuc);
        }

        // GET: TinTucs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tinTuc = await _context.TinTucs
                .Include(t => t.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaTinTuc == id);
            if (tinTuc == null)
            {
                return NotFound();
            }

            return View(tinTuc);
        }

        // GET: TinTucs/Create
        public IActionResult Create()
        {
            // Get a list of accounts to populate the dropdown
            var taiKhoans = _context.TaiKhoans.ToList();
            if (taiKhoans != null && taiKhoans.Any())
            {
                ViewBag.NguoiDang = new SelectList(taiKhoans, "MaTK", "TenDangNhap");
            }
            else
            {
                ViewBag.NguoiDang = new SelectList(new List<SelectListItem>());
            }
            return View();
        }

        // POST: TinTucs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TieuDe,MoTaNgan,NoiDung,HinhAnhURL,NgayDang,TacGiaDisplay,NguoiDang,DanhMuc,HienThi,NoiBat")] TinTuc tinTuc)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tinTuc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            // Get a list of accounts to populate the dropdown
            var taiKhoans = _context.TaiKhoans.ToList();
            if (taiKhoans != null && taiKhoans.Any())
            {
                ViewBag.NguoiDang = new SelectList(taiKhoans, "MaTK", "TenDangNhap", tinTuc.NguoiDang);
            }
            else
            {
                ViewBag.NguoiDang = new SelectList(new List<SelectListItem>());
            }
            return View(tinTuc);
        }

        // GET: TinTucs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tinTuc = await _context.TinTucs.FindAsync(id);
            if (tinTuc == null)
            {
                return NotFound();
            }
            
            // Get a list of accounts to populate the dropdown
            var taiKhoans = _context.TaiKhoans.ToList();
            if (taiKhoans != null && taiKhoans.Any())
            {
                ViewBag.NguoiDang = new SelectList(taiKhoans, "MaTK", "TenDangNhap", tinTuc.NguoiDang);
            }
            else
            {
                ViewBag.NguoiDang = new SelectList(new List<SelectListItem>());
            }
            return View(tinTuc);
        }

        // POST: TinTucs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaTinTuc,TieuDe,MoTaNgan,NoiDung,HinhAnhURL,TacGiaDisplay,NguoiDang,NgayDang,DanhMuc,HienThi,NoiBat")] TinTuc tinTuc)
        {
            if (id != tinTuc.MaTinTuc)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tinTuc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TinTucExists(tinTuc.MaTinTuc))
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
            
            // Get a list of accounts to populate the dropdown
            var taiKhoans = _context.TaiKhoans.ToList();
            if (taiKhoans != null && taiKhoans.Any())
            {
                ViewBag.NguoiDang = new SelectList(taiKhoans, "MaTK", "TenDangNhap", tinTuc.NguoiDang);
            }
            else
            {
                ViewBag.NguoiDang = new SelectList(new List<SelectListItem>());
            }
            return View(tinTuc);
        }

        // GET: TinTucs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tinTuc = await _context.TinTucs
                .Include(t => t.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaTinTuc == id);
            if (tinTuc == null)
            {
                return NotFound();
            }

            return View(tinTuc);
        }

        // POST: TinTucs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tinTuc = await _context.TinTucs.FindAsync(id);
            if (tinTuc != null)
            {
                _context.TinTucs.Remove(tinTuc);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: TinTucs/ToggleVisibility/5
        public async Task<IActionResult> ToggleVisibility(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tinTuc = await _context.TinTucs.FindAsync(id);
            if (tinTuc == null)
            {
                return NotFound();
            }

            // Toggle the visibility
            tinTuc.HienThi = !tinTuc.HienThi;
            await _context.SaveChangesAsync();

            TempData["Message"] = tinTuc.HienThi ? 
                $"Tin tức \"{tinTuc.TieuDe}\" đã được hiển thị trên trang công khai" : 
                $"Tin tức \"{tinTuc.TieuDe}\" đã bị ẩn khỏi trang công khai";

            return RedirectToAction(nameof(Index));
        }

        private bool TinTucExists(int id)
        {
            return _context.TinTucs.Any(e => e.MaTinTuc == id);
        }
    }
}
