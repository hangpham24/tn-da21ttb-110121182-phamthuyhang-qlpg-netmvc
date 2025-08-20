using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Areas.VNPayAPI.Controllers
{
    [Area("VNPayAPI")]
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly GymDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly VietQRService _vietQRService;

        public HomeController(IConfiguration configuration, GymDbContext context, ILogger<HomeController> logger, VietQRService vietQRService)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _vietQRService = vietQRService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request)
        {
            try
            {
                // Validate payment record exists
                var payment = await _context.ThanhToans.FindAsync(request.ThanhToanId);
                if (payment == null)
                {
                    return Json(new { success = false, message = "Payment not found" });
                }

                // VNPay Configuration
                var vnpayConfig = _configuration.GetSection("VnPay");
                var vnp_Url = vnpayConfig["BaseUrl"];
                var vnp_TmnCode = vnpayConfig["TmnCode"];
                var vnp_HashSecret = vnpayConfig["HashSecret"];

                // Build VNPay payment URL
                var vnpay = new VnPayLibrary();

                // Build URL for VNPAY - following official sample
                var createDate = DateTime.Now;
                var orderId = DateTime.Now.Ticks; // Use ticks like official sample

                vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(payment.SoTien * 100)).ToString()); // Amount in cents
                vnpay.AddRequestData("vnp_CreateDate", createDate.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext));
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang:{payment.ThanhToanId}");
                vnpay.AddRequestData("vnp_OrderType", "other"); // default value: other
                var returnUrl = vnpayConfig["ReturnUrl"] ?? "http://localhost:5003/VNPayAPI/Home/PaymentConfirm";
                vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
                vnpay.AddRequestData("vnp_TxnRef", orderId.ToString()); // Unique transaction reference

                // Create payment gateway record
                var gateway = new ThanhToanGateway
                {
                    ThanhToanId = payment.ThanhToanId,
                    GatewayTen = "VNPAY",
                    GatewayOrderId = orderId.ToString(),
                    GatewayAmount = payment.SoTien,
                    ThoiGianCallback = DateTime.Now
                };

                _context.ThanhToanGateways.Add(gateway);
                await _context.SaveChangesAsync();

                var paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                // Generate VietQR for banking apps
                var orderInfo = $"Thanh toan gym {payment.ThanhToanId}";
                var vietQRInfo = _vietQRService.GetVietQRInfo(payment.SoTien, orderInfo, orderId.ToString());

                _logger.LogInformation($"VNPay payment URL created for ThanhToanId: {payment.ThanhToanId}");
                _logger.LogInformation($"VietQR URL generated: {vietQRInfo.QRImageUrl}");

                return Json(new {
                    success = true,
                    paymentUrl = paymentUrl,
                    qrCodeData = vietQRInfo.QRData, // VietQR data for QR code generation
                    qrImageUrl = vietQRInfo.QRImageUrl, // Direct VietQR image URL
                    orderId = orderId,
                    amount = payment.SoTien,
                    bankInfo = new {
                        bankId = vietQRInfo.BankId,
                        accountNo = vietQRInfo.AccountNo,
                        accountName = vietQRInfo.AccountName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment URL");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo thanh toán" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentConfirm()
        {
            try
            {
                var vnpayData = new Dictionary<string, string>();
                foreach (var key in Request.Query.Keys)
                {
                    vnpayData.Add(key, Request.Query[key]);
                }

                var vnpay = new VnPayLibrary();
                foreach (var item in vnpayData)
                {
                    vnpay.AddResponseData(item.Key, item.Value);
                }

                var vnp_OrderId = vnpay.GetResponseData("vnp_TxnRef");
                var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
                var vnp_SecureHash = vnpayData["vnp_SecureHash"];
                var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");

                var vnp_HashSecret = _configuration.GetSection("VnPay")["HashSecret"];
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                // Kiểm tra nếu là môi trường simulation - bỏ qua validation signature
                var isSimulation = _configuration.GetValue<bool>("VnPay:EnableSimulation", false);

                if (!checkSignature && !isSimulation)
                {
                    _logger.LogError("VNPay signature validation failed");
                    return Redirect("/Member/MyRegistrations?paymentStatus=error&message=Chữ+ký+không+hợp+lệ");
                }

                // Find gateway record
                var gateway = await _context.ThanhToanGateways
                    .Include(g => g.ThanhToan)
                    .FirstOrDefaultAsync(g => g.GatewayOrderId == vnp_OrderId);

                if (gateway == null)
                {
                    _logger.LogError($"Gateway not found for order ID: {vnp_OrderId}");
                    return Redirect("/Member/MyRegistrations?paymentStatus=error&message=Không+tìm+thấy+giao+dịch");
                }

                // Update gateway with response data
                gateway.GatewayTransId = vnp_TransactionId;
                gateway.GatewayRespCode = vnp_ResponseCode;
                gateway.ThoiGianCallback = DateTime.Now;

                // Trong môi trường simulation, coi mọi response code đều là thành công (kể cả khi user hủy)
                bool isPaymentSuccess = (vnp_ResponseCode == "00") || isSimulation;

                if (isPaymentSuccess) // Success hoặc simulation
                {
                    gateway.ThanhToan.TrangThai = "SUCCESS";
                    gateway.GatewayMessage = isSimulation ? "Thanh toán thành công (Mô phỏng)" : "Thanh toán thành công";

                    // Process payment based on type (new registration or renewal)
                    if (gateway.ThanhToan.DangKyId.HasValue)
                    {
                        var registration = await _context.DangKys
                            .FirstOrDefaultAsync(d => d.DangKyId == gateway.ThanhToan.DangKyId.Value);

                        if (registration != null)
                        {
                            // Check if this is a renewal payment (based on GhiChu)
                            if (gateway.ThanhToan.GhiChu?.Contains("Gia hạn") == true)
                            {
                                // This is a renewal payment - process renewal
                                var dangKyService = HttpContext.RequestServices.GetRequiredService<IDangKyService>();

                                // Extract renewal months from GhiChu (format: "Gia hạn gói tập ... - X tháng")
                                var ghiChu = gateway.ThanhToan.GhiChu;
                                var monthsMatch = System.Text.RegularExpressions.Regex.Match(ghiChu, @"(\d+)\s+tháng");
                                if (monthsMatch.Success && int.TryParse(monthsMatch.Groups[1].Value, out int renewalMonths))
                                {
                                    await dangKyService.ProcessRenewalPaymentAsync(registration.DangKyId, gateway.ThanhToanId, renewalMonths);
                                }
                            }
                            else if (registration.TrangThai == "PENDING_PAYMENT")
                            {
                                // This is a new registration payment
                                registration.TrangThai = "ACTIVE";
                                registration.TrangThaiChiTiet = isSimulation ? "Thanh toán thành công (Mô phỏng)" : "Thanh toán thành công";

                                // Auto check-in for walk-in customers
                                if (gateway.ThanhToan.GhiChu?.Contains("WALKIN") == true)
                                {
                                    var walkInService = HttpContext.RequestServices.GetRequiredService<IWalkInService>();
                                    try
                                    {
                                        await walkInService.CheckInGuestAsync(
                                            registration.NguoiDungId,
                                            $"WALKIN - VNPay auto check-in",
                                            "VNPay");
                                        _logger.LogInformation("Auto check-in successful for walk-in customer: {UserId}", registration.NguoiDungId);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to auto check-in walk-in customer: {UserId}", registration.NguoiDungId);
                                    }
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"VNPay payment successful for order: {vnp_OrderId}" + (isSimulation ? " (Simulation)" : ""));
                    var successMessage = isSimulation ? "Mô+phỏng+thanh+toán+thành+công" : "Thanh+toán+thành+công";

                    // Check if this is a walk-in payment
                    if (gateway.ThanhToan.GhiChu?.Contains("WALKIN") == true)
                    {
                        // Redirect to DiemDanh page for walk-in customers
                        return Redirect($"/DiemDanh/Index?paymentStatus=success&message={successMessage}&walkIn=true");
                    }
                    else
                    {
                        // Redirect to Member page for regular members
                        return Redirect($"/Member/MyRegistrations?paymentStatus=success&message={successMessage}");
                    }
                }
                else
                {
                    gateway.ThanhToan.TrangThai = "FAILED";
                    gateway.GatewayMessage = "Thanh toán thất bại";

                    // Cancel pending registration if exists
                    if (gateway.ThanhToan.DangKyId.HasValue)
                    {
                        var registration = await _context.DangKys
                            .FirstOrDefaultAsync(d => d.DangKyId == gateway.ThanhToan.DangKyId.Value);
                        
                        if (registration != null && registration.TrangThai == "PENDING_PAYMENT")
                        {
                            registration.TrangThai = "CANCELLED";
                            registration.LyDoHuy = "Thanh toán thất bại";
                        }
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogWarning($"VNPay payment failed for order: {vnp_OrderId}, response code: {vnp_ResponseCode}");

                    // Check if this is a walk-in payment
                    if (gateway.ThanhToan.GhiChu?.Contains("WALKIN") == true)
                    {
                        // Redirect to DiemDanh page for walk-in customers
                        return Redirect("/DiemDanh/Index?paymentStatus=error&message=Thanh+toán+thất+bại&walkIn=true");
                    }
                    else
                    {
                        // Redirect to Member page for regular members
                        return Redirect("/Member/MyRegistrations?paymentStatus=error&message=Thanh+toán+thất+bại");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay callback");
                return Redirect("/Member/MyRegistrations?paymentStatus=error&message=Có+lỗi+xảy+ra+khi+xử+lý+thanh+toán");
            }
        }


    }

    public class PaymentRequest
    {
        public int ThanhToanId { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
    }

    public static class Utils
    {
        public static string GetIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress;
            if (ipAddress != null)
            {
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    ipAddress = System.Net.Dns.GetHostEntry(ipAddress).AddressList
                        .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                }
            }
            return ipAddress?.ToString() ?? "127.0.0.1";
        }
    }
} 