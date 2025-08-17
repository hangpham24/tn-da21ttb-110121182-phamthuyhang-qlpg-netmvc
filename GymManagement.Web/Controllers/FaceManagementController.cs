using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class FaceManagementController : Controller
    {
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IAuthService _authService;
        private readonly ILogger<FaceManagementController> _logger;

        public FaceManagementController(
            IFaceRecognitionService faceRecognitionService,
            INguoiDungService nguoiDungService,
            IAuthService authService,
            ILogger<FaceManagementController> logger)
        {
            _faceRecognitionService = faceRecognitionService;
            _nguoiDungService = nguoiDungService;
            _authService = authService;
            _logger = logger;
        }

        // Face Management Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var faces = await _faceRecognitionService.GetMemberFacesAsync(currentUser.NguoiDungId.Value);
                ViewBag.FaceCount = faces.Count();
                
                return View(faces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading face management dashboard");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang quản lý khuôn mặt.";
                return View();
            }
        }

        // Face Registration Page
        public IActionResult Register()
        {
            ViewData["Title"] = "Đăng ký khuôn mặt";
            return View();
        }

        // Register new face
        [HttpPost]
        public async Task<IActionResult> RegisterFace([FromBody] RegisterFaceRequest request)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                if (request?.Descriptor == null || request.Descriptor.Length != 128)
                {
                    return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ" });
                }

                // Convert to float array
                var faceDescriptor = request.Descriptor.Select(d => (float)d).ToArray();

                // Validate face quality
                if (!await _faceRecognitionService.ValidateFaceQualityAsync(faceDescriptor))
                {
                    return Json(new { success = false, message = "Chất lượng khuôn mặt không đủ tốt. Vui lòng thử lại." });
                }

                // Check for duplicates
                if (await _faceRecognitionService.IsFaceAlreadyRegisteredAsync(faceDescriptor, currentUser.NguoiDungId.Value))
                {
                    return Json(new { success = false, message = "Khuôn mặt này đã được đăng ký. Vui lòng thử với góc độ khác." });
                }

                // Register face
                var success = await _faceRecognitionService.RegisterFaceAsync(currentUser.NguoiDungId.Value, faceDescriptor);

                if (success)
                {
                    _logger.LogInformation("Face registered successfully for user {UserId}", currentUser.NguoiDungId.Value);
                    return Json(new { success = true, message = "Đăng ký khuôn mặt thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể đăng ký khuôn mặt. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering face");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng ký khuôn mặt." });
            }
        }

        // Get user's faces
        [HttpGet]
        public async Task<IActionResult> GetUserFaces()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var faces = await _faceRecognitionService.GetMemberFacesAsync(currentUser.NguoiDungId.Value);
                var faceList = faces.Select(f => new
                {
                    id = f.MauMatId,
                    createdDate = f.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                    algorithm = f.ThuatToan
                }).ToList();

                return Json(new { success = true, faces = faceList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user faces");
                return Json(new { success = false, message = "Không thể tải danh sách khuôn mặt" });
            }
        }

        // Update existing face
        [HttpPost]
        public async Task<IActionResult> UpdateFace([FromBody] UpdateFaceRequest request)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                if (request?.NewDescriptor == null || request.NewDescriptor.Length != 128)
                {
                    return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ" });
                }

                // Verify ownership
                var faces = await _faceRecognitionService.GetMemberFacesAsync(currentUser.NguoiDungId.Value);
                if (!faces.Any(f => f.MauMatId == request.MauMatId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền sửa khuôn mặt này" });
                }

                // Convert to float array
                var faceDescriptor = request.NewDescriptor.Select(d => (float)d).ToArray();

                // Validate face quality
                if (!await _faceRecognitionService.ValidateFaceQualityAsync(faceDescriptor))
                {
                    return Json(new { success = false, message = "Chất lượng khuôn mặt không đủ tốt. Vui lòng thử lại." });
                }

                // Update face
                var success = await _faceRecognitionService.UpdateFaceAsync(request.MauMatId, faceDescriptor);

                if (success)
                {
                    _logger.LogInformation("Face updated successfully - ID: {FaceId}", request.MauMatId);
                    return Json(new { success = true, message = "Cập nhật khuôn mặt thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể cập nhật khuôn mặt. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating face");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật khuôn mặt." });
            }
        }

        // Delete face
        [HttpDelete]
        public async Task<IActionResult> DeleteFace(int mauMatId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Verify ownership
                var faces = await _faceRecognitionService.GetMemberFacesAsync(currentUser.NguoiDungId.Value);
                if (!faces.Any(f => f.MauMatId == mauMatId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa khuôn mặt này" });
                }

                // Check if this is the last face
                if (faces.Count() == 1)
                {
                    return Json(new { 
                        success = false, 
                        message = "Đây là khuôn mặt cuối cùng. Xóa sẽ tắt tính năng nhận diện. Bạn có chắc chắn?",
                        isLastFace = true
                    });
                }

                // Delete face
                var success = await _faceRecognitionService.DeleteFaceAsync(mauMatId);

                if (success)
                {
                    _logger.LogInformation("Face deleted successfully - ID: {FaceId}", mauMatId);
                    return Json(new { success = true, message = "Xóa khuôn mặt thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa khuôn mặt. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting face");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khuôn mặt." });
            }
        }

        // Force delete last face
        [HttpDelete]
        public async Task<IActionResult> ForceDeleteFace(int mauMatId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Verify ownership
                var faces = await _faceRecognitionService.GetMemberFacesAsync(currentUser.NguoiDungId.Value);
                if (!faces.Any(f => f.MauMatId == mauMatId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa khuôn mặt này" });
                }

                // Delete face
                var success = await _faceRecognitionService.DeleteFaceAsync(mauMatId);

                if (success)
                {
                    _logger.LogInformation("Last face deleted successfully - ID: {FaceId}", mauMatId);
                    return Json(new { 
                        success = true, 
                        message = "Xóa khuôn mặt thành công! Tính năng nhận diện đã bị tắt.",
                        faceRecognitionDisabled = true
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa khuôn mặt. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force deleting face");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khuôn mặt." });
            }
        }

        // Admin: Manage all faces
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminManage()
        {
            try
            {
                var stats = await _faceRecognitionService.GetRecognitionStatsAsync();
                ViewBag.Stats = stats;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin face management");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang quản lý.";
                return View();
            }
        }

        // Admin: Get all faces in system
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllFaces()
        {
            try
            {
                var allFaces = await _faceRecognitionService.GetAllFacesAsync();
                var faceList = allFaces.Select(f => new
                {
                    id = f.MauMatId,
                    memberId = f.NguoiDungId,
                    memberName = f.NguoiDung != null ? $"{f.NguoiDung.Ho} {f.NguoiDung.Ten}" : "Không xác định",
                    memberEmail = f.NguoiDung?.Email ?? "Không có email",
                    createdDate = f.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                    algorithm = f.ThuatToan
                }).ToList();

                return Json(new { success = true, faces = faceList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all faces");
                return Json(new { success = false, message = "Không thể tải danh sách khuôn mặt" });
            }
        }

        // Admin: Register face for any member
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminRegisterFace([FromBody] AdminRegisterFaceRequest request)
        {
            try
            {
                if (request?.Descriptor == null || request.Descriptor.Length != 128)
                {
                    return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ" });
                }

                // Convert to float array
                var faceDescriptor = request.Descriptor.Select(d => (float)d).ToArray();

                // Validate face quality
                if (!await _faceRecognitionService.ValidateFaceQualityAsync(faceDescriptor))
                {
                    return Json(new { success = false, message = "Chất lượng khuôn mặt không đủ tốt. Vui lòng thử lại." });
                }

                // Check for duplicates
                if (await _faceRecognitionService.IsFaceAlreadyRegisteredAsync(faceDescriptor, request.MemberId))
                {
                    return Json(new { success = false, message = "Khuôn mặt này đã được đăng ký." });
                }

                // Register face
                var success = await _faceRecognitionService.RegisterFaceAsync(request.MemberId, faceDescriptor);

                if (success)
                {
                    _logger.LogInformation("Admin registered face for member {MemberId}", request.MemberId);
                    return Json(new { success = true, message = "Đăng ký khuôn mặt thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể đăng ký khuôn mặt. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin registering face");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng ký khuôn mặt." });
            }
        }

        // Admin: Delete any face
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeleteFace(int mauMatId)
        {
            try
            {
                var success = await _faceRecognitionService.DeleteFaceAsync(mauMatId);

                if (success)
                {
                    _logger.LogInformation("Admin deleted face {FaceId}", mauMatId);
                    return Json(new { success = true, message = "Xóa khuôn mặt thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa khuôn mặt. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin deleting face");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khuôn mặt." });
            }
        }

        // Admin: Delete all faces for a member (when deleting member account)
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeleteAllMemberFaces(int memberId)
        {
            try
            {
                var success = await _faceRecognitionService.DeleteAllMemberFacesAsync(memberId);

                if (success)
                {
                    _logger.LogInformation("Admin deleted all faces for member {MemberId}", memberId);
                    return Json(new { success = true, message = "Xóa tất cả khuôn mặt của hội viên thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa khuôn mặt. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin deleting all member faces");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khuôn mặt." });
            }
        }

        // Helper method to get current user
        private async Task<Data.Models.TaiKhoan?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _authService.GetUserByIdAsync(userId);
        }
    }

    // Request DTOs
    public class RegisterFaceRequest
    {
        public double[] Descriptor { get; set; } = Array.Empty<double>();
    }

    public class UpdateFaceRequest
    {
        public int MauMatId { get; set; }
        public double[] NewDescriptor { get; set; } = Array.Empty<double>();
    }

    public class AdminRegisterFaceRequest
    {
        public int MemberId { get; set; }
        public double[] Descriptor { get; set; } = Array.Empty<double>();
    }
}
