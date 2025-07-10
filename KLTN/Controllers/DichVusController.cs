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
    public class DichVusController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DichVusController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Public Actions (Không cần xác thực)

        // GET: DichVus/Public - Trang danh sách dịch vụ dành cho khách hàng 
        [Route("services")]
        public async Task<IActionResult> PublicIndex(string search, string loaiDichVu)
        {
            // Khởi tạo truy vấn cơ bản
            var query = _context.DichVus
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .AsQueryable();
            
            // Áp dụng bộ lọc tìm kiếm nếu có
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(d => 
                    (d.TenDichVu != null && d.TenDichVu.ToLower().Contains(search)) ||
                    (d.MoTa != null && d.MoTa.ToLower().Contains(search)) ||
                    (d.LoaiDichVu != null && d.LoaiDichVu.ToLower().Contains(search))
                );
            }
            
            // Lọc theo loại dịch vụ nếu được chọn
            if (!string.IsNullOrEmpty(loaiDichVu) && loaiDichVu != "TatCa")
            {
                query = query.Where(d => d.LoaiDichVu == loaiDichVu);
            }
            
            // Chuẩn bị dropdown loại dịch vụ
            var loaiDichVuItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "TatCa", Text = "Tất cả dịch vụ", Selected = string.IsNullOrEmpty(loaiDichVu) || loaiDichVu == "TatCa" },
                new SelectListItem { Value = "GoiTap", Text = "Gói tập", Selected = loaiDichVu == "GoiTap" },
                new SelectListItem { Value = "LopHoc", Text = "Lớp học", Selected = loaiDichVu == "LopHoc" }
            };
            
            ViewBag.LoaiDichVu = loaiDichVuItems;
            
            // Trả về kết quả đã lọc
            return View("~/Views/DichVus/PublicIndex.cshtml", await query.ToListAsync());
        }

        // GET: DichVus/PublicDetails/5 - Xem chi tiết dịch vụ dành cho khách hàng
        [Route("DichVus/Details/{id}")]
        public async Task<IActionResult> PublicDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .FirstOrDefaultAsync(m => m.MaDichVu == id);
            if (dichVu == null)
            {
                return NotFound();
            }

            return View("~/Views/DichVus/PublicDetails.cshtml", dichVu);
        }
        
        // GET: DichVus/GoiTap - Danh sách gói tập
        [Route("DichVus/GoiTap")]
        public async Task<IActionResult> GoiTap(string search)
        {
            var query = _context.DichVus
                .Include(d => d.GoiTap)
                .Where(d => d.LoaiDichVu == "GoiTap" && d.GoiTap != null)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(d => 
                    (d.TenDichVu != null && d.TenDichVu.ToLower().Contains(search)) ||
                    (d.MoTa != null && d.MoTa.ToLower().Contains(search)) ||
                    (d.GoiTap.TenGoi != null && d.GoiTap.TenGoi.ToLower().Contains(search))
                );
            }
            
            return View("~/Views/DichVus/GoiTap.cshtml", await query.ToListAsync());
        }
        
        // GET: DichVus/LopHoc - Danh sách lớp học
        [Route("DichVus/LopHoc")]
        public async Task<IActionResult> LopHoc(string search)
        {
            var query = _context.DichVus
                .Include(d => d.LopHoc)
                .Where(d => d.LoaiDichVu == "LopHoc" && d.LopHoc != null)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(d => 
                    (d.TenDichVu != null && d.TenDichVu.ToLower().Contains(search)) ||
                    (d.MoTa != null && d.MoTa.ToLower().Contains(search)) ||
                    (d.LopHoc.TenLop != null && d.LopHoc.TenLop.ToLower().Contains(search))
                );
            }
            
            return View("~/Views/DichVus/LopHoc.cshtml", await query.ToListAsync());
        }
        
        // GET: DichVus/GetRelatedServices - Lấy dịch vụ liên quan cho partial view
        public async Task<IActionResult> GetRelatedServices(int id, string loaiDichVu)
        {
            // Lấy các dịch vụ cùng loại nhưng không phải dịch vụ hiện tại
            var query = _context.DichVus
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Where(d => d.MaDichVu != id && d.LoaiDichVu == loaiDichVu)
                .AsQueryable();
                
            // Lấy 3 dịch vụ ngẫu nhiên
            var relatedServices = await query
                .OrderBy(d => Guid.NewGuid()) // Sắp xếp ngẫu nhiên
                .Take(3)
                .ToListAsync();
                
            // Nếu không đủ dịch vụ cùng loại, bổ sung thêm dịch vụ khác loại
            if (relatedServices.Count < 3)
            {
                var additionalServices = await _context.DichVus
                    .Include(d => d.GoiTap)
                    .Include(d => d.LopHoc)
                    .Where(d => d.MaDichVu != id && !relatedServices.Select(r => r.MaDichVu).Contains(d.MaDichVu))
                    .OrderBy(d => Guid.NewGuid())
                    .Take(3 - relatedServices.Count)
                    .ToListAsync();
                    
                relatedServices.AddRange(additionalServices);
            }
            
            return PartialView("~/Views/DichVus/_RelatedServicesPartial.cshtml", relatedServices);
        }

        #endregion

        #region Admin Actions (Cần xác thực)

        // GET: DichVus - Trang quản lý dịch vụ dành cho Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string search, string loaiDichVu)
        {
            // Khởi tạo truy vấn cơ bản
            var query = _context.DichVus
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .AsQueryable();
            
            // Áp dụng bộ lọc tìm kiếm nếu có
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(d => 
                    (d.TenDichVu != null && d.TenDichVu.ToLower().Contains(search)) ||
                    (d.MoTa != null && d.MoTa.ToLower().Contains(search)) ||
                    (d.LoaiDichVu != null && d.LoaiDichVu.ToLower().Contains(search))
                );
            }
            
            // Lọc theo loại dịch vụ nếu được chọn
            if (!string.IsNullOrEmpty(loaiDichVu) && loaiDichVu != "TatCa")
            {
                query = query.Where(d => d.LoaiDichVu == loaiDichVu);
            }
            
            // Chuẩn bị dropdown loại dịch vụ
            var loaiDichVuItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "TatCa", Text = "Tất cả dịch vụ", Selected = string.IsNullOrEmpty(loaiDichVu) || loaiDichVu == "TatCa" },
                new SelectListItem { Value = "GoiTap", Text = "Gói tập", Selected = loaiDichVu == "GoiTap" },
                new SelectListItem { Value = "LopHoc", Text = "Lớp học", Selected = loaiDichVu == "LopHoc" }
            };
            
            ViewBag.LoaiDichVu = loaiDichVuItems;
            
            // Trả về kết quả đã lọc
            return View(await query.ToListAsync());
        }

        // GET: DichVus/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .FirstOrDefaultAsync(m => m.MaDichVu == id);
            if (dichVu == null)
            {
                return NotFound();
            }

            return View(dichVu);
        }

        // GET: DichVus/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi");
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "TenLop");
            return View();
        }

        // POST: DichVus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("MaDichVu,TenDichVu,MoTa,LoaiDichVu,GiaBatDau,HinhAnhURL,MaGoiTap,MaLopHoc")] DichVu dichVu)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dichVu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", dichVu.MaGoiTap);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "TenLop", dichVu.MaLopHoc);
            return View(dichVu);
        }

        // GET: DichVus/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null)
            {
                return NotFound();
            }
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", dichVu.MaGoiTap);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "TenLop", dichVu.MaLopHoc);
            return View(dichVu);
        }

        // POST: DichVus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("MaDichVu,TenDichVu,MoTa,LoaiDichVu,GiaBatDau,HinhAnhURL,MaGoiTap,MaLopHoc")] DichVu dichVu)
        {
            if (id != dichVu.MaDichVu)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dichVu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DichVuExists(dichVu.MaDichVu))
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
            ViewData["MaGoiTap"] = new SelectList(_context.GoiTap, "MaGoi", "TenGoi", dichVu.MaGoiTap);
            ViewData["MaLopHoc"] = new SelectList(_context.LopHoc, "MaLop", "TenLop", dichVu.MaLopHoc);
            return View(dichVu);
        }

        // GET: DichVus/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var dichVu = await _context.DichVus
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .FirstOrDefaultAsync(m => m.MaDichVu == id);
            if (dichVu == null)
            {
                return NotFound();
            }

            return View(dichVu);
        }

        // POST: DichVus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu != null)
            {
                _context.DichVus.Remove(dichVu);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DichVuExists(int id)
        {
            return _context.DichVus.Any(e => e.MaDichVu == id);
        }
        
        #endregion
    }
}
