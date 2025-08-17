using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TestTinTucController : Controller
    {
        private readonly GymDbContext _context;
        private readonly ILogger<TestTinTucController> _logger;

        public TestTinTucController(GymDbContext context, ILogger<TestTinTucController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: TestTinTuc/TestUpdate/5
        public async Task<IActionResult> TestUpdate(int id)
        {
            try
            {
                // 1. Get tin tuc
                var tinTuc = await _context.TinTucs.FindAsync(id);
                if (tinTuc == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin tức" });
                }

                // 2. Log current state
                _logger.LogInformation("Before update: TieuDe = {TieuDe}, MoTaNgan = {MoTaNgan}", 
                    tinTuc.TieuDe, tinTuc.MoTaNgan);

                // 3. Update fields
                tinTuc.TieuDe = tinTuc.TieuDe + " - Updated " + DateTime.Now.ToString("HH:mm:ss");
                tinTuc.MoTaNgan = "Updated: " + tinTuc.MoTaNgan;
                tinTuc.NgayCapNhat = DateTime.Now;

                // 4. Save changes
                await _context.SaveChangesAsync();

                // 5. Verify update
                var updatedTinTuc = await _context.TinTucs.FindAsync(id);
                _logger.LogInformation("After update: TieuDe = {TieuDe}, MoTaNgan = {MoTaNgan}", 
                    updatedTinTuc.TieuDe, updatedTinTuc.MoTaNgan);

                return Json(new 
                { 
                    success = true, 
                    message = "Update thành công",
                    data = new 
                    {
                        id = updatedTinTuc.TinTucId,
                        tieuDe = updatedTinTuc.TieuDe,
                        moTaNgan = updatedTinTuc.MoTaNgan,
                        ngayCapNhat = updatedTinTuc.NgayCapNhat
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestUpdate");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: TestTinTuc/CheckEntityState/5
        public async Task<IActionResult> CheckEntityState(int id)
        {
            try
            {
                var tinTuc = await _context.TinTucs.FindAsync(id);
                if (tinTuc == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin tức" });
                }

                var entry = _context.Entry(tinTuc);
                var state = entry.State.ToString();

                return Json(new
                {
                    success = true,
                    entityState = state,
                    data = new
                    {
                        id = tinTuc.TinTucId,
                        tieuDe = tinTuc.TieuDe,
                        moTaNgan = tinTuc.MoTaNgan
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
