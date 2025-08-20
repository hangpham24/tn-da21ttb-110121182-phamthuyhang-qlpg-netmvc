using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FaceTestController : Controller
    {
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IAuthService _authService;
        private readonly ILogger<FaceTestController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FaceTestController(
            IFaceRecognitionService faceRecognitionService,
            INguoiDungService nguoiDungService,
            IAuthService authService,
            ILogger<FaceTestController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _faceRecognitionService = faceRecognitionService;
            _nguoiDungService = nguoiDungService;
            _authService = authService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("~/FaceTest/models/{fileName}")]
        public IActionResult GetModel(string fileName)
        {
            try
            {
                var modelsPath = Path.Combine(_webHostEnvironment.WebRootPath, "lib", "face-api", "models");
                var filePath = Path.Combine(modelsPath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                // Set appropriate content type based on file extension
                var contentType = fileName.EndsWith(".json") ? "application/json" : "application/octet-stream";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving model file: {FileName}", fileName);
                return NotFound();
            }
        }

        // Main test dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                var stats = await _faceRecognitionService.GetRecognitionStatsAsync();
                ViewBag.Stats = stats;

                var allMembers = await _nguoiDungService.GetMembersAsync();
                ViewBag.Members = allMembers;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading face test dashboard");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang test.";
                return View();
            }
        }

        // Test face registration
        [HttpPost]
        public async Task<IActionResult> TestRegisterFace([FromBody] TestRegisterFaceRequest request)
        {
            try
            {
                // Manual CSRF validation for AJAX requests
                var token = Request.Headers["RequestVerificationToken"].FirstOrDefault();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("CSRF token missing in face registration request");
                    return Json(new { success = false, message = "Token bảo mật không hợp lệ" });
                }

                if (request?.Descriptor == null || request.Descriptor.Length != 128)
                {
                    return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ" });
                }

                var startTime = DateTime.Now;
                var faceDescriptor = request.Descriptor.Select(d => (float)d).ToArray();

                // Validate face quality
                var qualityValid = await _faceRecognitionService.ValidateFaceQualityAsync(faceDescriptor);
                if (!qualityValid)
                {
                    return Json(new { 
                        success = false, 
                        message = "Chất lượng khuôn mặt không đủ tốt",
                        processingTime = (DateTime.Now - startTime).TotalMilliseconds
                    });
                }

                // Check for duplicates
                var isDuplicate = await _faceRecognitionService.IsFaceAlreadyRegisteredAsync(faceDescriptor);
                if (isDuplicate)
                {
                    return Json(new { 
                        success = false, 
                        message = "Khuôn mặt đã được đăng ký trước đó",
                        processingTime = (DateTime.Now - startTime).TotalMilliseconds
                    });
                }

                // Register face
                var result = await _faceRecognitionService.RegisterFaceAsync(request.MemberId, faceDescriptor);
                var processingTime = (DateTime.Now - startTime).TotalMilliseconds;

                if (result)
                {
                    _logger.LogInformation("Test face registration successful for member {MemberId} in {Time}ms", 
                        request.MemberId, processingTime);

                    return Json(new { 
                        success = true, 
                        message = "Đăng ký khuôn mặt thành công",
                        processingTime = processingTime
                    });
                }

                return Json(new { 
                    success = false, 
                    message = "Không thể đăng ký khuôn mặt",
                    processingTime = processingTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test face registration");
                return Json(new { success = false, message = "Lỗi hệ thống khi đăng ký khuôn mặt" });
            }
        }

        // Test face recognition
        [HttpPost]
        public async Task<IActionResult> TestRecognizeFace([FromBody] TestRecognizeFaceRequest request)
        {
            try
            {
                if (request?.Descriptor == null || request.Descriptor.Length != 128)
                {
                    return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ" });
                }

                var startTime = DateTime.Now;
                var faceDescriptor = request.Descriptor.Select(d => (float)d).ToArray();

                // Perform recognition
                var result = await _faceRecognitionService.RecognizeFaceAsync(faceDescriptor);
                var processingTime = (DateTime.Now - startTime).TotalMilliseconds;

                _logger.LogInformation("Test face recognition completed in {Time}ms with confidence {Confidence}", 
                    processingTime, result.Confidence);

                return Json(new { 
                    success = result.Success,
                    memberId = result.MemberId,
                    memberName = result.MemberName,
                    confidence = Math.Round(result.Confidence, 4),
                    message = result.Message,
                    processingTime = processingTime,
                    threshold = 0.6
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test face recognition");
                return Json(new { success = false, message = "Lỗi hệ thống khi nhận diện khuôn mặt" });
            }
        }

        // Get all registered faces
        [HttpGet]
        public async Task<IActionResult> GetAllFaces()
        {
            try
            {
                var faces = await _faceRecognitionService.GetAllFacesAsync();
                var faceList = faces.Select(f => new
                {
                    mauMatId = f.MauMatId,
                    nguoiDungId = f.NguoiDungId,
                    memberName = $"{f.NguoiDung?.Ho} {f.NguoiDung?.Ten}",
                    email = f.NguoiDung?.Email,
                    algorithm = f.ThuatToan,
                    registrationDate = f.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                    embeddingSize = f.Embedding?.Length ?? 0
                }).ToList();

                return Json(new { success = true, faces = faceList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all faces");
                return Json(new { success = false, message = "Lỗi khi tải danh sách khuôn mặt" });
            }
        }

        // Delete face
        [HttpDelete]
        public async Task<IActionResult> DeleteFace(int mauMatId)
        {
            try
            {
                var result = await _faceRecognitionService.DeleteFaceAsync(mauMatId);
                
                if (result)
                {
                    _logger.LogInformation("Test face deletion successful for face {FaceId}", mauMatId);
                    return Json(new { success = true, message = "Xóa khuôn mặt thành công" });
                }

                return Json(new { success = false, message = "Không thể xóa khuôn mặt" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting face {FaceId}", mauMatId);
                return Json(new { success = false, message = "Lỗi hệ thống khi xóa khuôn mặt" });
            }
        }

        // Test face similarity
        [HttpPost]
        public async Task<IActionResult> TestFaceSimilarity([FromBody] TestSimilarityRequest request)
        {
            try
            {
                if (request?.Descriptor1 == null || request.Descriptor1.Length != 128 ||
                    request?.Descriptor2 == null || request.Descriptor2.Length != 128)
                {
                    return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ" });
                }

                var startTime = DateTime.Now;
                var descriptor1 = request.Descriptor1.Select(d => (float)d).ToArray();
                var descriptor2 = request.Descriptor2.Select(d => (float)d).ToArray();

                var similarity = await _faceRecognitionService.CalculateSimilarityAsync(descriptor1, descriptor2);
                var processingTime = (DateTime.Now - startTime).TotalMilliseconds;

                return Json(new { 
                    success = true,
                    similarity = Math.Round(similarity, 4),
                    processingTime = processingTime,
                    isMatch = similarity >= 0.6,
                    threshold = 0.6
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating face similarity");
                return Json(new { success = false, message = "Lỗi khi tính toán độ tương đồng" });
            }
        }

        // Get performance metrics
        [HttpGet]
        public async Task<IActionResult> GetPerformanceMetrics()
        {
            try
            {
                var stats = await _faceRecognitionService.GetRecognitionStatsAsync();
                var allFaces = await _faceRecognitionService.GetAllFacesAsync();
                
                var metrics = new
                {
                    totalFaces = stats.TotalRegisteredFaces,
                    totalMembers = stats.TotalMembers,
                    registrationRate = stats.TotalMembers > 0 ? 
                        Math.Round((double)stats.TotalRegisteredFaces / stats.TotalMembers * 100, 1) : 0,
                    averageEmbeddingSize = allFaces.Any() ? 
                        Math.Round(allFaces.Average(f => f.Embedding?.Length ?? 0), 0) : 0,
                    algorithmsUsed = allFaces.GroupBy(f => f.ThuatToan)
                        .Select(g => new { algorithm = g.Key, count = g.Count() }).ToList(),
                    recentRegistrations = allFaces.Where(f => f.NgayTao >= DateTime.Now.AddDays(-7)).Count(),
                    oldestRegistration = allFaces.Any() ? 
                        allFaces.Min(f => f.NgayTao).ToString("dd/MM/yyyy") : "N/A",
                    newestRegistration = allFaces.Any() ? 
                        allFaces.Max(f => f.NgayTao).ToString("dd/MM/yyyy") : "N/A"
                };

                return Json(new { success = true, metrics = metrics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return Json(new { success = false, message = "Lỗi khi tải metrics" });
            }
        }

        // Bulk test recognition
        [HttpPost]
        public async Task<IActionResult> BulkTestRecognition([FromBody] BulkTestRequest request)
        {
            try
            {
                if (request?.Descriptors == null || !request.Descriptors.Any())
                {
                    return Json(new { success = false, message = "Không có dữ liệu test" });
                }

                var results = new List<object>();
                var startTime = DateTime.Now;

                foreach (var descriptor in request.Descriptors)
                {
                    if (descriptor.Length == 128)
                    {
                        var faceDescriptor = descriptor.Select(d => (float)d).ToArray();
                        var testStart = DateTime.Now;
                        var result = await _faceRecognitionService.RecognizeFaceAsync(faceDescriptor);
                        var testTime = (DateTime.Now - testStart).TotalMilliseconds;

                        results.Add(new
                        {
                            success = result.Success,
                            memberId = result.MemberId,
                            memberName = result.MemberName,
                            confidence = Math.Round(result.Confidence, 4),
                            processingTime = testTime
                        });
                    }
                }

                var totalTime = (DateTime.Now - startTime).TotalMilliseconds;
                var avgTime = results.Any() ? results.Average(r => (double)(r.GetType().GetProperty("processingTime")?.GetValue(r) ?? 0.0)) : 0;
                var successCount = results.Count(r => (bool)(r.GetType().GetProperty("success")?.GetValue(r) ?? false));

                return Json(new { 
                    success = true,
                    results = results,
                    summary = new
                    {
                        totalTests = results.Count,
                        successfulRecognitions = successCount,
                        failedRecognitions = results.Count - successCount,
                        successRate = results.Count > 0 ? Math.Round((double)successCount / results.Count * 100, 1) : 0,
                        totalTime = totalTime,
                        averageTime = Math.Round(avgTime, 2)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk test recognition");
                return Json(new { success = false, message = "Lỗi trong quá trình test hàng loạt" });
            }
        }
    }

    // Request DTOs
    public class TestRegisterFaceRequest
    {
        public int MemberId { get; set; }
        public double[] Descriptor { get; set; } = Array.Empty<double>();
    }

    public class TestRecognizeFaceRequest
    {
        public double[] Descriptor { get; set; } = Array.Empty<double>();
    }

    public class TestSimilarityRequest
    {
        public double[] Descriptor1 { get; set; } = Array.Empty<double>();
        public double[] Descriptor2 { get; set; } = Array.Empty<double>();
    }

    public class BulkTestRequest
    {
        public List<double[]> Descriptors { get; set; } = new List<double[]>();
    }
}
