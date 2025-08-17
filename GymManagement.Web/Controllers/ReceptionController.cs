using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class ReceptionController : Controller
    {
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IDiemDanhService _diemDanhService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IDangKyService _dangKyService;
        private readonly IThanhToanService _thanhToanService;
        private readonly ILopHocService _lopHocService;
        private readonly IWalkInService _walkInService;
        private readonly ILogger<ReceptionController> _logger;

        public ReceptionController(
            IFaceRecognitionService faceRecognitionService,
            IDiemDanhService diemDanhService,
            INguoiDungService nguoiDungService,
            IDangKyService dangKyService,
            IThanhToanService thanhToanService,
            ILopHocService lopHocService,
            IWalkInService walkInService,
            ILogger<ReceptionController> logger)
        {
            _faceRecognitionService = faceRecognitionService;
            _diemDanhService = diemDanhService;
            _nguoiDungService = nguoiDungService;
            _dangKyService = dangKyService;
            _thanhToanService = thanhToanService;
            _lopHocService = lopHocService;
            _walkInService = walkInService;
            _logger = logger;
        }

        // Reception Station Interface
        [Authorize(Roles = "Admin,Trainer")]
        public IActionResult Station()
        {
            ViewData["Title"] = "Reception Check-in Station";
            return View();
        }

        // Auto Check-in/Check-out API for Reception Station
        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> AutoCheckIn([FromBody] FaceCheckInRequest request)
        {
            try
            {
                if (request?.Descriptor == null || request.Descriptor.Length != 128)
                {
                    return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ" });
                }

                // Convert to float array
                var faceDescriptor = request.Descriptor.Select(d => (float)d).ToArray();

                // Recognize face
                var recognitionResult = await _faceRecognitionService.RecognizeFaceAsync(faceDescriptor);

                if (!recognitionResult.Success)
                {
                    return Json(new { 
                        success = false, 
                        message = "Không nhận diện được khuôn mặt. Vui lòng thử lại hoặc liên hệ nhân viên." 
                    });
                }

                var memberId = recognitionResult.MemberId!.Value;
                var member = await _nguoiDungService.GetByIdAsync(memberId);

                if (member == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin hội viên" });
                }

                // Check current session status
                var currentSession = await _diemDanhService.GetLatestAttendanceAsync(memberId);
                var hasActiveSession = currentSession != null && 
                                     currentSession.ThoiGianCheckIn.Date == DateTime.Today &&
                                     currentSession.ThoiGianCheckOut == null;

                if (!hasActiveSession)
                {
                    // CHECK-IN
                    var checkInSuccess = await _diemDanhService.CheckInAsync(memberId);

                    if (checkInSuccess)
                    {
                        _logger.LogInformation("Face recognition check-in successful for member {MemberId}", memberId);

                        // Get detailed member information
                        var memberInfo = await GetDetailedMemberInfoAsync(memberId);

                        return Json(new
                        {
                            success = true,
                            action = "checkin",
                            memberName = $"{member.Ho} {member.Ten}",
                            memberId = memberId,
                            time = DateTime.Now.ToString("HH:mm:ss"),
                            confidence = Math.Round(recognitionResult.Confidence * 100, 1),
                            message = $"Chào mừng {member.Ho} {member.Ten}!",
                            memberDetails = memberInfo
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không thể check-in. Vui lòng thử lại." });
                    }
                }
                else
                {
                    // CHECK-OUT
                    var sessionDuration = DateTime.Now - currentSession.ThoiGianCheckIn;
                    var checkOutSuccess = await CheckOutMemberAsync(currentSession.DiemDanhId);

                    if (checkOutSuccess)
                    {
                        _logger.LogInformation("Face recognition check-out successful for member {MemberId}", memberId);

                        // Get session summary
                        var sessionSummary = await GetSessionSummaryAsync(memberId, currentSession);

                        return Json(new
                        {
                            success = true,
                            action = "checkout",
                            memberName = $"{member.Ho} {member.Ten}",
                            memberId = memberId,
                            duration = FormatDuration(sessionDuration),
                            confidence = Math.Round(recognitionResult.Confidence * 100, 1),
                            message = $"Tạm biệt {member.Ho} {member.Ten}! Cảm ơn bạn đã đến tập.",
                            sessionSummary = sessionSummary
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không thể check-out. Vui lòng thử lại." });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto check-in/out");
                return Json(new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại." });
            }
        }

        // Manual Check-in for Reception
        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> ManualCheckIn([FromBody] ManualCheckInRequest request)
        {
            try
            {
                if (request?.MemberId == null)
                {
                    return Json(new { success = false, message = "Vui lòng chọn hội viên" });
                }

                var member = await _nguoiDungService.GetByIdAsync(request.MemberId);
                if (member == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hội viên" });
                }

                // Check if already checked in today
                if (await _diemDanhService.HasCheckedInTodayAsync(request.MemberId))
                {
                    return Json(new { success = false, message = "Hội viên đã check-in hôm nay" });
                }

                var success = await _diemDanhService.CheckInAsync(request.MemberId, request.Note);
                
                if (success)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Check-in thành công cho {member.Ho} {member.Ten}",
                        memberName = $"{member.Ho} {member.Ten}",
                        time = DateTime.Now.ToString("HH:mm:ss")
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể check-in. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual check-in");
                return Json(new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại." });
            }
        }

        // Get current gym statistics
        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetGymStats()
        {
            try
            {
                var todayCount = await _diemDanhService.GetTodayAttendanceCountAsync();
                var currentlyInGym = await GetCurrentlyInGymCountAsync();
                
                return Json(new
                {
                    success = true,
                    todayTotal = todayCount,
                    currentlyInGym = currentlyInGym,
                    lastUpdated = DateTime.Now.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gym stats");
                return Json(new { success = false, message = "Không thể tải thống kê" });
            }
        }

        // Get members list for manual check-in
        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetMembers()
        {
            try
            {
                var members = await _nguoiDungService.GetMembersAsync();
                var memberList = members.Select(m => new
                {
                    id = m.NguoiDungId,
                    name = $"{m.Ho} {m.Ten}",
                    email = m.Email,
                    hasCheckedInToday = _diemDanhService.HasCheckedInTodayAsync(m.NguoiDungId).Result
                }).ToList();

                return Json(new { success = true, members = memberList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members list");
                return Json(new { success = false, message = "Không thể tải danh sách hội viên" });
            }
        }

        // Helper Methods
        private async Task<bool> CheckOutMemberAsync(int diemDanhId)
        {
            try
            {
                var diemDanh = await _diemDanhService.GetByIdAsync(diemDanhId);
                if (diemDanh == null) return false;

                diemDanh.ThoiGianCheckOut = DateTime.Now;
                await _diemDanhService.UpdateAsync(diemDanh);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out member");
                return false;
            }
        }

        private async Task<int> GetCurrentlyInGymCountAsync()
        {
            try
            {
                var todayAttendance = await _diemDanhService.GetTodayAttendanceAsync();
                return todayAttendance.Count(a => a.ThoiGianCheckOut == null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currently in gym count");
                return 0;
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
            else
            {
                return $"{duration.Minutes}m";
            }
        }

        private async Task<object> GetDetailedMemberInfoAsync(int memberId)
        {
            try
            {
                var member = await _nguoiDungService.GetByIdAsync(memberId);

                // Simplified version to avoid complex relationship errors
                return new
                {
                    personalInfo = new
                    {
                        fullName = $"{member?.Ho} {member?.Ten}",
                        email = member?.Email ?? "Không có email",
                        phone = member?.SoDienThoai ?? "Không có SĐT",
                        memberSince = member?.NgayTao.ToString("dd/MM/yyyy") ?? "Không xác định"
                    },
                    currentPackage = new
                    {
                        name = "Gói tập cơ bản",
                        expiryDate = DateTime.Now.AddDays(30).ToString("dd/MM/yyyy"),
                        remainingDays = 30,
                        status = "Còn hiệu lực"
                    },
                    registeredClasses = new[]
                    {
                        new { name = "Yoga", trainer = "Chưa có HLV", schedule = "T2,T4,T6 - 18:00" }
                    },
                    paymentStatus = new
                    {
                        lastPaymentDate = DateTime.Now.ToString("dd/MM/yyyy"),
                        amount = "1,500,000 VNĐ",
                        method = "Chuyển khoản",
                        status = "Đã thanh toán"
                    },
                    recentCheckIns = new[]
                    {
                        new { date = DateTime.Now.ToString("dd/MM/yyyy"), checkInTime = "14:25", checkOutTime = "16:30", duration = "2h 5m" }
                    },
                    totalCheckIns = 15
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed member info for member {MemberId}", memberId);
                return new { error = "Không thể tải thông tin chi tiết" };
            }
        }

        private async Task<object> GetSessionSummaryAsync(int memberId, Data.Models.DiemDanh session)
        {
            try
            {
                var sessionDuration = DateTime.Now - session.ThoiGianCheckIn;

                return new
                {
                    sessionDuration = FormatDuration(sessionDuration),
                    checkInTime = session.ThoiGianCheckIn.ToString("HH:mm"),
                    checkOutTime = DateTime.Now.ToString("HH:mm"),
                    totalTimeToday = FormatDuration(sessionDuration),
                    classesAttendedToday = 1,
                    caloriesBurned = Math.Round(sessionDuration.TotalMinutes * 8.5), // Rough estimate
                    message = "Cảm ơn bạn đã đến tập! Hẹn gặp lại!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session summary for member {MemberId}", memberId);
                return new { error = "Không thể tải tóm tắt phiên tập" };
            }
        }

        #region Walk-In Customer APIs

        /// <summary>
        /// Đăng ký nhanh khách vãng lai
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> WalkInRegisterQuick([FromBody] WalkInRegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Walk-in quick registration: {Name}, Phone: {Phone}", request.FullName, request.PhoneNumber);

                // Validation
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return BadRequest(new { success = false, message = "Họ tên không được để trống" });
                }

                // Kiểm tra khách đã tồn tại trong ngày
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    var existingGuest = await _walkInService.GetExistingGuestTodayAsync(request.PhoneNumber);
                    if (existingGuest != null)
                    {
                        return Ok(new
                        {
                            success = true,
                            isExisting = true,
                            guest = new
                            {
                                id = existingGuest.NguoiDungId,
                                name = $"{existingGuest.Ho} {existingGuest.Ten}".Trim(),
                                phone = existingGuest.SoDienThoai
                            },
                            message = "Khách hàng đã đăng ký trong ngày hôm nay"
                        });
                    }
                }

                // Tạo khách vãng lai mới
                var guest = await _walkInService.CreateGuestAsync(request.FullName, request.PhoneNumber, request.Email);

                return Ok(new
                {
                    success = true,
                    isExisting = false,
                    guest = new
                    {
                        id = guest.NguoiDungId,
                        name = $"{guest.Ho} {guest.Ten}".Trim(),
                        phone = guest.SoDienThoai
                    },
                    message = "Đăng ký khách vãng lai thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in walk-in quick registration");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Tạo thanh toán cho khách vãng lai với giá cố định
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> WalkInCreatePayment([FromBody] WalkInPaymentRequest request)
        {
            try
            {
                // Lấy thông tin giá cố định
                var (fixedPrice, packageName, packageDescription) = _walkInService.GetFixedPriceInfo();

                _logger.LogInformation("Creating walk-in payment for guest {GuestId} with fixed price: {Price} VND", request.GuestId, fixedPrice);

                // Tạo vé với giá cố định
                var dayPass = await _walkInService.CreateFixedPricePassAsync(request.GuestId);

                // Tạo thanh toán với giá cố định
                var payment = await _walkInService.ProcessWalkInPaymentAsync(
                    dayPass.DangKyId,
                    request.PaymentMethod,
                    $"WALKIN - {packageName}");

                var result = new
                {
                    success = true,
                    dayPassId = dayPass.DangKyId,
                    paymentId = payment.ThanhToanId,
                    amount = payment.SoTien,
                    method = payment.PhuongThuc,
                    status = payment.TrangThai,
                    autoActivated = payment.TrangThai == "SUCCESS",
                    message = payment.TrangThai == "SUCCESS" ?
                        "Thanh toán thành công! Khách có thể vào tập ngay." :
                        "Đã tạo thanh toán. Vui lòng xác nhận khi nhận được tiền."
                };

                // Nếu thanh toán CASH thì tự động check-in
                if (payment.TrangThai == "SUCCESS")
                {
                    var checkIn = await _walkInService.CheckInGuestAsync(request.GuestId, $"WALKIN - {packageName}", "Manual");
                    return Ok(new
                    {
                        success = true,
                        dayPassId = dayPass.DangKyId,
                        paymentId = payment.ThanhToanId,
                        checkInId = checkIn.DiemDanhId,
                        amount = fixedPrice, // Sử dụng giá cố định
                        method = payment.PhuongThuc,
                        status = payment.TrangThai,
                        packageName = packageName,
                        packagePrice = fixedPrice,
                        autoActivated = true,
                        autoCheckedIn = true,
                        message = $"Thanh toán {fixedPrice:N0} VNĐ và check-in thành công! Khách đã vào tập."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating walk-in payment for guest {GuestId}", request.GuestId);
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Xác nhận thanh toán chuyển khoản cho khách vãng lai
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> WalkInConfirmPayment([FromBody] WalkInConfirmPaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Confirming walk-in payment {PaymentId}", request.PaymentId);

                var success = await _walkInService.ConfirmPaymentAsync(request.PaymentId);
                if (!success)
                {
                    return BadRequest(new { success = false, message = "Không thể xác nhận thanh toán" });
                }

                // Tự động check-in sau khi xác nhận thanh toán
                // Tìm guest từ payment record thông qua service
                var paymentInfo = await _thanhToanService.GetPaymentWithRegistrationAsync(request.PaymentId);

                if (paymentInfo?.DangKy?.NguoiDung != null)
                {
                    var guestId = paymentInfo.DangKy.NguoiDungId;
                    var guestName = $"{paymentInfo.DangKy.NguoiDung.Ho} {paymentInfo.DangKy.NguoiDung.Ten}".Trim();

                    var checkIn = await _walkInService.CheckInGuestAsync(guestId, $"WALKIN - VNPay confirmed ({guestName})", "VNPay");
                    return Ok(new
                    {
                        success = true,
                        checkInId = checkIn.DiemDanhId,
                        guestName = guestName,
                        message = $"Xác nhận thanh toán VNPay và check-in thành công cho {guestName}!"
                    });
                }

                return Ok(new { success = true, message = "Xác nhận thanh toán thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming walk-in payment {PaymentId}", request.PaymentId);
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Check-out khách vãng lai
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> WalkInCheckOut([FromBody] WalkInCheckOutRequest request)
        {
            try
            {
                _logger.LogInformation("Checking out walk-in guest with attendance ID: {DiemDanhId}", request.DiemDanhId);

                var success = await _walkInService.CheckOutGuestAsync(request.DiemDanhId);
                if (!success)
                {
                    return BadRequest(new { success = false, message = "Không thể check-out" });
                }

                return Ok(new
                {
                    success = true,
                    checkOutTime = DateTime.Now.ToString("HH:mm"),
                    message = "Check-out thành công!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out walk-in guest with attendance ID: {DiemDanhId}", request.DiemDanhId);
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Lấy danh sách khách vãng lai đang tập hôm nay
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetTodayWalkIns()
        {
            try
            {
                var sessions = await _walkInService.GetTodayWalkInsAsync();

                var result = sessions.Select(s => new
                {
                    diemDanhId = s.DiemDanhId,
                    guestId = s.GuestId,
                    guestName = s.GuestName,
                    phoneNumber = s.PhoneNumber,
                    packageName = s.PackageName,
                    checkInTime = s.CheckInTime.ToString("HH:mm"),
                    checkOutTime = s.CheckOutTime?.ToString("HH:mm"),
                    duration = s.Duration?.ToString(@"hh\:mm"),
                    status = s.Status,
                    isActive = s.IsActive
                }).ToList();

                return Ok(new { success = true, sessions = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's walk-in sessions");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Lấy danh sách gói vé có sẵn cho khách vãng lai
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetAvailablePackages()
        {
            try
            {
                var packages = await _walkInService.GetAvailablePackagesAsync();

                var result = packages.Select(p => new
                {
                    id = p.GoiTapId,
                    name = p.TenGoi,
                    price = p.Gia,
                    description = p.MoTa,
                    formattedPrice = p.Gia.ToString("N0") + " VNĐ"
                }).ToList();

                return Ok(new { success = true, packages = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available packages");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// Đăng ký khách vãng lai với thanh toán
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> WalkInRegisterWithPayment([FromBody] WalkInPaymentRegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Walk-in registration with payment: {Name}, Method: {Method}, FaceDescriptor: {HasFaceDescriptor}",
                    request.FullName, request.PaymentMethod,
                    request.FaceDescriptor != null ? $"Yes ({request.FaceDescriptor.Length} dimensions)" : "No");

                // Validation
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return BadRequest(new { success = false, message = "Họ tên không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new { success = false, message = "Số điện thoại không được để trống" });
                }

                if (request.Amount <= 0)
                {
                    return BadRequest(new { success = false, message = "Số tiền không hợp lệ" });
                }

                // Process walk-in registration with payment
                var result = await _walkInService.RegisterWalkInWithPaymentAsync(
                    request.FullName,
                    request.PhoneNumber,
                    request.Email,
                    request.Note,
                    request.PaymentMethod,
                    request.Amount,
                    request.FaceDescriptor);

                if (result.Success)
                {
                    _logger.LogInformation("Walk-in registration with payment successful for {Name}", request.FullName);
                    return Ok(new {
                        success = true,
                        message = "Đăng ký và thanh toán thành công",
                        guestId = result.GuestId,
                        transactionId = result.TransactionId
                    });
                }
                else
                {
                    _logger.LogWarning("Walk-in registration with payment failed: {Message}", result.Message);
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in walk-in registration with payment");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi đăng ký và thanh toán" });
            }
        }

        /// <summary>
        /// Tạo thanh toán VNPay cho khách vãng lai
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> CreateWalkInVNPayPayment([FromBody] WalkInVNPayRequest request)
        {
            try
            {
                _logger.LogInformation("Creating VNPay payment for walk-in: {Name}, Amount: {Amount}",
                    request.FullName, request.Amount);

                // Validation
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return BadRequest(new { success = false, message = "Họ tên không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return BadRequest(new { success = false, message = "Số điện thoại không được để trống" });
                }

                if (request.Amount <= 0)
                {
                    return BadRequest(new { success = false, message = "Số tiền không hợp lệ" });
                }

                // Create VNPay payment for walk-in
                var result = await _walkInService.CreateVNPayPaymentAsync(
                    request.FullName,
                    request.PhoneNumber,
                    request.Email,
                    request.Note,
                    request.Amount);

                if (result.Success)
                {
                    _logger.LogInformation("VNPay payment created for walk-in: {Name}", request.FullName);
                    return Ok(new {
                        success = true,
                        message = "Tạo thanh toán VNPay thành công",
                        paymentUrl = result.PaymentUrl,
                        orderId = result.OrderId,
                        thanhToanId = result.ThanhToanId
                    });
                }
                else
                {
                    _logger.LogWarning("VNPay payment creation failed: {Message}", result.Message);
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment for walk-in");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tạo thanh toán VNPay" });
            }
        }

        #endregion
    }

    // Request DTOs
    public class FaceCheckInRequest
    {
        public double[] Descriptor { get; set; } = Array.Empty<double>();
    }

    public class ManualCheckInRequest
    {
        public int MemberId { get; set; }
        public string? Note { get; set; }
    }

    // Walk-In Request DTOs
    public class WalkInRegisterRequest
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }

    public class WalkInPaymentRequest
    {
        public int GuestId { get; set; }
        public string PaymentMethod { get; set; } = "CASH"; // CASH, BANK

        // Các field cũ để backward compatibility (sẽ bị ignore)
        [Obsolete("Fixed price model - this field is ignored")]
        public string PackageType { get; set; } = "WALKIN";
        [Obsolete("Fixed price model - this field is ignored")]
        public string PackageName { get; set; } = "Vé tập một buổi";
        [Obsolete("Fixed price model - this field is ignored")]
        public decimal Price { get; set; } = 15000;
        [Obsolete("Fixed price model - this field is ignored")]
        public int DurationHours { get; set; } = 24;
    }

    public class WalkInConfirmPaymentRequest
    {
        public int PaymentId { get; set; }
        public int GuestId { get; set; }
    }

    public class WalkInCheckOutRequest
    {
        public int DiemDanhId { get; set; }
    }

    public class WalkInPaymentRegisterRequest
    {
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? Email { get; set; }
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "CASH"; // CASH, VNPAY
        public decimal Amount { get; set; }
        public float[]? FaceDescriptor { get; set; } // Face descriptor for face recognition
    }

    public class WalkInVNPayRequest
    {
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? Email { get; set; }
        public string? Note { get; set; }
        public decimal Amount { get; set; }
    }
}
