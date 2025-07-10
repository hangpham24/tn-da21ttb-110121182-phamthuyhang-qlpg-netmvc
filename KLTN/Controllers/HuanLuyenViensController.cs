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
    public class HuanLuyenViensController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HuanLuyenViensController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: HuanLuyenViens
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            int pageSize = 5;
            var query = _context.HuanLuyenViens.Include(h => h.TaiKhoan).AsQueryable();

            // Lọc theo từ khóa tìm kiếm nếu có
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(h => h.HoTen != null && h.HoTen.Contains(search));
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var items = await query
                .OrderBy(h => h.MaPT)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;

            return View(items);
        }

        // GET: HuanLuyenViens/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var huanLuyenVien = await _context.HuanLuyenViens
                .Include(h => h.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaPT == id);
            if (huanLuyenVien == null)
            {
                return NotFound();
            }

            return View(huanLuyenVien);
        }

        // GET: HuanLuyenViens/Create
        public IActionResult Create()
        {
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash");
            return View();
        }

        // POST: HuanLuyenViens/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaPT,MaTK,HoTen,NgaySinh,GioiTinh,SoDienThoai,Email,ChuyenMon,KinhNghiem,DiaChi,TrangThaiHLV")] HuanLuyenVien huanLuyenVien)
        {
            if (ModelState.IsValid)
            {
                _context.Add(huanLuyenVien);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", huanLuyenVien.MaTK);
            return View(huanLuyenVien);
        }

        // GET: HuanLuyenViens/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var huanLuyenVien = await _context.HuanLuyenViens.FindAsync(id);
            if (huanLuyenVien == null)
            {
                return NotFound();
            }
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", huanLuyenVien.MaTK);
            return View(huanLuyenVien);
        }

        // POST: HuanLuyenViens/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaPT,MaTK,HoTen,NgaySinh,GioiTinh,SoDienThoai,Email,ChuyenMon,KinhNghiem,DiaChi,TrangThaiHLV")] HuanLuyenVien huanLuyenVien)
        {
            if (id != huanLuyenVien.MaPT)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(huanLuyenVien);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HuanLuyenVienExists(huanLuyenVien.MaPT))
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
            ViewData["MaTK"] = new SelectList(_context.TaiKhoans, "MaTK", "MatKhauHash", huanLuyenVien.MaTK);
            return View(huanLuyenVien);
        }

        // GET: HuanLuyenViens/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var huanLuyenVien = await _context.HuanLuyenViens
                .Include(h => h.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaPT == id);
            if (huanLuyenVien == null)
            {
                return NotFound();
            }

            return View(huanLuyenVien);
        }

        // POST: HuanLuyenViens/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var huanLuyenVien = await _context.HuanLuyenViens.FindAsync(id);
            if (huanLuyenVien != null)
            {
                _context.HuanLuyenViens.Remove(huanLuyenVien);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: HuanLuyenViens/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var huanLuyenVien = await _context.HuanLuyenViens
                .Include(h => h.TaiKhoan)
                .FirstOrDefaultAsync(m => m.MaPT == id);

            if (huanLuyenVien == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái huấn luyện viên
            huanLuyenVien.TrangThaiHLV = status;

            // Cập nhật trạng thái tài khoản tương ứng
            if (huanLuyenVien.TaiKhoan != null)
            {
                if (status == "HoatDong")
                {
                    huanLuyenVien.TaiKhoan.TrangThai = "HoatDong";
                }
                else if (status == "Khoa")
                {
                    huanLuyenVien.TaiKhoan.TrangThai = "Khoa";
                }
                else if (status == "ChoPheDuyet")
                {
                    huanLuyenVien.TaiKhoan.TrangThai = "ChuaKichHoat";
                }
            }

            await _context.SaveChangesAsync();

            string statusMessage = "";
            switch (status)
            {
                case "HoatDong":
                    statusMessage = "Huấn luyện viên đã được kích hoạt thành công.";
                    break;
                case "Khoa":
                    statusMessage = "Huấn luyện viên đã bị khóa.";
                    break;
                case "ChoPheDuyet":
                    statusMessage = "Huấn luyện viên đã được chuyển về trạng thái chờ phê duyệt.";
                    break;
                default:
                    statusMessage = "Trạng thái huấn luyện viên đã được cập nhật.";
                    break;
            }

            TempData["StatusMessage"] = statusMessage;
            return RedirectToAction(nameof(Index));
        }

        private bool HuanLuyenVienExists(int id)
        {
            return _context.HuanLuyenViens.Any(e => e.MaPT == id);
        }
    }
}
