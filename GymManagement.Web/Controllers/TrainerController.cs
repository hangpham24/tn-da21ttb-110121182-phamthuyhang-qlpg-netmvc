using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController : Controller
    {
        private readonly ILopHocService _lopHocService;
        private readonly IBangLuongService _bangLuongService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IDiemDanhService _diemDanhService;
        private readonly IBaoCaoService _baoCaoService;
        private readonly IAuthService _authService;
        private readonly ILogger<TrainerController> _logger;

        public TrainerController(
            ILopHocService lopHocService,
            IBangLuongService bangLuongService,
            INguoiDungService nguoiDungService,
            IDiemDanhService diemDanhService,
            IBaoCaoService baoCaoService,
            IAuthService authService,
            ILogger<TrainerController> logger)
        {
            _lopHocService = lopHocService;
            _bangLuongService = bangLuongService;
            _nguoiDungService = nguoiDungService;
            _diemDanhService = diemDanhService;
            _baoCaoService = baoCaoService;
            _authService = authService;
            _logger = logger;
        }

        // Helper method to get current user
        private async Task<TaiKhoan?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _authService.GetUserByIdAsync(userId);
        }

        // Debug method để kiểm tra dữ liệu database
        [HttpGet]
        public async Task<IActionResult> DebugData()
        {
            try
            {
                var allTrainers = await _nguoiDungService.GetAllAsync();
                var trainers = allTrainers.Where(u => u.LoaiNguoiDung == "HLV" || u.LoaiNguoiDung == "TRAINER");
                
                var allClasses = await _lopHocService.GetAllAsync();
                
                var debugInfo = new
                {
                    TotalUsers = allTrainers.Count(),
                    Trainers = trainers.Select(t => new { 
                        Id = t.NguoiDungId, 
                        Name = $"{t.Ho} {t.Ten}", 
                        Type = t.LoaiNguoiDung,
                        Status = t.TrangThai
                    }),
                    TotalClasses = allClasses.Count(),
                    Classes = allClasses.Select(c => new { 
                        Id = c.LopHocId, 
                        Name = c.TenLop, 
                        HlvId = c.HlvId, 
                        Status = c.TrangThai 
                    })
                };
                
                return Json(debugInfo);
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message });
            }
        }

        // Dashboard - Trang chủ của Trainer
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    _logger.LogWarning("Trainer user not found or NguoiDungId is null");
                    return RedirectToAction("Login", "Auth");
                }

                var trainerId = user.NguoiDungId.Value;
                _logger.LogInformation("Loading dashboard for trainer with NguoiDungId: {TrainerId}", trainerId);

                // Lấy thông tin trainer
                var trainer = await _nguoiDungService.GetByIdAsync(trainerId);
                ViewBag.Trainer = trainer;
                _logger.LogInformation("Trainer info loaded: {TrainerName}, Type: {TrainerType}", 
                    trainer != null ? $"{trainer.Ho} {trainer.Ten}" : "NULL", 
                    trainer?.LoaiNguoiDung);

                // Lấy lớp học được phân công
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                var myClassesList = myClasses.ToList(); // Convert to list để có thể log
                ViewBag.MyClasses = myClassesList.Take(5).ToList(); // Hiển thị 5 lớp gần nhất
                
                _logger.LogInformation("Found {ClassCount} classes for trainer {TrainerId}", myClassesList.Count, trainerId);
                foreach (var cls in myClassesList.Take(3)) // Log first 3 classes
                {
                    _logger.LogInformation("Class: {ClassName}, HlvId: {HlvId}, Status: {Status}", 
                        cls.TenLop, cls.HlvId, cls.TrangThai);
                }

                // Lấy thông tin lương tháng hiện tại (tạm bỏ qua để fix dashboard)
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                // var currentSalary = await _bangLuongService.GetByTrainerAndMonthAsync(trainerId, currentMonth);
                ViewBag.CurrentSalary = null; // Tạm set null
                _logger.LogInformation("Current salary loading skipped for month {Month}", currentMonth);

                // Thống kê cơ bản với debug chi tiết
                var totalClasses = myClassesList.Count;
                var activeClasses = myClassesList.Count(c => c.TrangThai == "OPEN");
                
                ViewBag.TotalClasses = totalClasses;
                ViewBag.ActiveClasses = activeClasses;
                
                _logger.LogInformation("DEBUG: myClassesList count = {Count}", myClassesList.Count);
                _logger.LogInformation("DEBUG: ViewBag.TotalClasses = {Total}", totalClasses);
                _logger.LogInformation("DEBUG: ViewBag.ActiveClasses = {Active}", activeClasses);
                
                // Log chi tiết từng lớp học
                foreach (var cls in myClassesList)
                {
                    _logger.LogInformation("DEBUG Class: {Name}, Status: {Status}, HlvId: {HlvId}", 
                        cls.TenLop, cls.TrangThai, cls.HlvId);
                }
                
                _logger.LogInformation("Dashboard stats - Total: {Total}, Active: {Active}", 
                    totalClasses, activeClasses);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading trainer dashboard for user {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dashboard.";
                return View();
            }
        }

        // Lớp của tôi - Danh sách lớp học được phân công
        public async Task<IActionResult> MyClasses()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    _logger.LogWarning("Trainer user not found or NguoiDungId is null");
                    return RedirectToAction("Login", "Auth");
                }

                var trainerId = user.NguoiDungId.Value;
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);

                return View(myClasses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading trainer classes for user {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách lớp học.";
                return View(new List<LopHoc>());
            }
        }

        // Lịch dạy - Lịch dạy cá nhân
        public async Task<IActionResult> Schedule()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    _logger.LogWarning("Trainer user not found or NguoiDungId is null");
                    return RedirectToAction("Login", "Auth");
                }

                var trainerId = user.NguoiDungId.Value;
                
                // Lấy lớp học của trainer
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                ViewBag.MyClasses = myClasses;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading trainer schedule for user {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải lịch dạy.";
                return View();
            }
        }

        // API để lấy lịch dạy dạng JSON cho calendar
        [HttpGet]
        public async Task<IActionResult> GetScheduleEvents(DateTime start, DateTime end, int? classId = null)
        {
            try
            {
                _logger.LogInformation("GetScheduleEvents called with start: {Start}, end: {End}, classId: {ClassId}", start, end, classId);
                
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    _logger.LogWarning("GetScheduleEvents: User not found or NguoiDungId is null");
                    return Json(new List<object>());
                }

                var trainerId = user.NguoiDungId.Value;
                _logger.LogInformation("GetScheduleEvents: TrainerId = {TrainerId}", trainerId);
                
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                
                // Filter by classId if provided
                if (classId.HasValue)
                {
                    myClasses = myClasses.Where(c => c.LopHocId == classId.Value);
                    _logger.LogInformation("Filtered to class ID {ClassId}, found {ClassCount} classes", classId.Value, myClasses.Count());
                }
                else
                {
                    _logger.LogInformation("GetScheduleEvents: Found {ClassCount} classes for trainer (no filter)", myClasses.Count());
                }

                var events = new List<object>();
                
                foreach (var lopHoc in myClasses)
                {
                    _logger.LogInformation("Checking schedules for class: {ClassName} (ID: {ClassId})", lopHoc.TenLop, lopHoc.LopHocId);
                    var schedules = await _lopHocService.GetClassScheduleAsync(lopHoc.LopHocId, start, end);
                    _logger.LogInformation("Found {ScheduleCount} schedules for class {ClassName}", schedules.Count(), lopHoc.TenLop);
                    
                    if (schedules.Any())
                    {
                        foreach (var schedule in schedules)
                        {
                            _logger.LogInformation("Adding event from schedule: {ClassName} on {Date} at {Time}", 
                                lopHoc.TenLop, schedule.Ngay, schedule.GioBatDau);
                            
                            events.Add(new
                            {
                                id = schedule.LichLopId,
                                title = lopHoc.TenLop,
                                start = schedule.Ngay.ToDateTime(schedule.GioBatDau),
                                end = schedule.Ngay.ToDateTime(schedule.GioKetThuc),
                                backgroundColor = schedule.TrangThai == "SCHEDULED" ? "#3b82f6" : "#ef4444",
                                borderColor = schedule.TrangThai == "SCHEDULED" ? "#2563eb" : "#dc2626",
                                extendedProps = new
                                {
                                    status = schedule.TrangThai,
                                    capacity = lopHoc.SucChua,
                                    booked = schedule.SoLuongDaDat
                                }
                            });
                        }
                    }
                    else
                    {
                        // Generate events from class schedule if no LichLop exists
                        _logger.LogInformation("No schedules found, generating from class info: {ClassName}", lopHoc.TenLop);
                        var generatedEvents = GenerateEventsFromClass(lopHoc, start, end);
                        events.AddRange(generatedEvents);
                    }
                }

                _logger.LogInformation("GetScheduleEvents: Returning {EventCount} events", events.Count);
                return Json(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting trainer schedule events");
                return Json(new List<object>());
            }
        }

        // Học viên - Danh sách học viên trong các lớp của trainer
        public async Task<IActionResult> Students()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    _logger.LogWarning("Trainer user not found or NguoiDungId is null");
                    return RedirectToAction("Login", "Auth");
                }

                var trainerId = user.NguoiDungId.Value;

                // Lấy lớp học của trainer
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                ViewBag.MyClasses = myClasses;

                // Lấy danh sách học viên từ các lớp học (sẽ được load qua AJAX)
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading trainer students for user {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách học viên.";
                return View();
            }
        }

        // API để lấy tất cả học viên của trainer (từ tất cả lớp học)
        [HttpGet]
        public async Task<IActionResult> GetAllStudentsByTrainer()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                
                var studentDict = new Dictionary<int, object>();
                
                foreach (var lopHoc in myClasses)
                {
                    var lopHocDetail = await _lopHocService.GetByIdAsync(lopHoc.LopHocId);
                    if (lopHocDetail?.DangKys != null)
                    {
                        foreach (var dangKy in lopHocDetail.DangKys.Where(d => d.NguoiDung != null))
                        {
                            var studentId = dangKy.NguoiDung!.NguoiDungId;
                            if (!studentDict.ContainsKey(studentId))
                            {
                                studentDict[studentId] = new
                                {
                                    id = studentId,
                                    name = $"{dangKy.NguoiDung.Ho} {dangKy.NguoiDung.Ten}".Trim(),
                                    email = dangKy.NguoiDung.Email,
                                    phone = dangKy.NguoiDung.SoDienThoai,
                                    registrationDate = dangKy.NgayBatDau.ToString("dd/MM/yyyy"),
                                    expiryDate = dangKy.NgayKetThuc.ToString("dd/MM/yyyy"),
                                    status = GetStudentStatus(dangKy),
                                    isActive = dangKy.TrangThai == "ACTIVE" && dangKy.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today),
                                    className = lopHoc.TenLop,
                                    classId = lopHoc.LopHocId
                                };
                            }
                        }
                    }
                }
                
                // Get unique students and sort by name
                var uniqueStudents = studentDict.Values.ToList();

                return Json(new { success = true, data = uniqueStudents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all students for trainer");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách học viên." });
            }
        }

        // API để lấy danh sách học viên theo lớp học
        [HttpGet]
        public async Task<IActionResult> GetStudentsByClass(int classId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;

                // Kiểm tra xem lớp học có thuộc về trainer này không
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                if (!myClasses.Any(c => c.LopHocId == classId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền xem học viên của lớp này." });
                }

                // Lấy danh sách học viên (thông qua đăng ký)
                var lopHoc = await _lopHocService.GetByIdAsync(classId);
                if (lopHoc?.DangKys == null)
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                var students = lopHoc.DangKys
                    .Where(d => d.NguoiDung != null)
                    .Select(d => new
                    {
                        id = d.NguoiDung!.NguoiDungId,
                        name = $"{d.NguoiDung.Ho} {d.NguoiDung.Ten}".Trim(),
                        email = d.NguoiDung.Email,
                        phone = d.NguoiDung.SoDienThoai,
                        registrationDate = d.NgayBatDau.ToString("dd/MM/yyyy"),
                        expiryDate = d.NgayKetThuc.ToString("dd/MM/yyyy"),
                        status = GetStudentStatus(d),
                        isActive = d.TrangThai == "ACTIVE" && d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today)
                    })
                    .OrderBy(s => s.name)
                    .ToList();

                return Json(new { success = true, data = students });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting students for class {ClassId}", classId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách học viên." });
            }
        }

        // Lương - Thông tin lương và hoa hồng
        public async Task<IActionResult> Salary()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    _logger.LogWarning("Trainer user not found or NguoiDungId is null");
                    return RedirectToAction("Login", "Auth");
                }

                var trainerId = user.NguoiDungId.Value;

                // Lấy lịch sử lương
                var salaries = await _bangLuongService.GetByTrainerIdAsync(trainerId);

                // Lấy thông tin lương tháng hiện tại
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                var currentSalary = await _bangLuongService.GetByTrainerAndMonthAsync(trainerId, currentMonth);
                ViewBag.CurrentSalary = currentSalary;

                // Tính tổng lương đã nhận
                ViewBag.TotalPaidSalary = salaries.Where(s => s.NgayThanhToan != null).Sum(s => s.TongThanhToan);
                ViewBag.PendingSalary = salaries.Where(s => s.NgayThanhToan == null).Sum(s => s.TongThanhToan);

                return View(salaries.OrderByDescending(s => s.Thang));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading trainer salary for user {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin lương.";
                return View(new List<BangLuong>());
            }
        }

        // API để lấy chi tiết lương theo tháng
        [HttpGet]
        public async Task<IActionResult> GetSalaryDetails(string month)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;
                var salary = await _bangLuongService.GetByTrainerAndMonthAsync(trainerId, month);

                if (salary == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin lương cho tháng này." });
                }

                // Tính hoa hồng chi tiết
                var commission = await _bangLuongService.CalculateCommissionAsync(trainerId, month);

                var result = new
                {
                    success = true,
                    data = new
                    {
                        month = salary.Thang,
                        baseSalary = salary.LuongCoBan,
                        commission = salary.TienHoaHong,
                        calculatedCommission = commission,
                        total = salary.TongThanhToan,
                        paymentDate = salary.NgayThanhToan?.ToString("dd/MM/yyyy"),
                        isPaid = salary.NgayThanhToan != null,
                        note = salary.GhiChu
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting salary details for month {Month}", month);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải chi tiết lương." });
            }
        }

        // Attendance Management for Trainers
        public async Task<IActionResult> Attendance(int? classId, DateTime? date)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    _logger.LogWarning("Trainer user not found or NguoiDungId is null");
                    return RedirectToAction("Login", "Auth");
                }

                var trainerId = user.NguoiDungId.Value;

                // Get trainer's classes
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                ViewBag.MyClasses = myClasses;

                // If specific class and date provided, get the schedule
                if (classId.HasValue && date.HasValue)
                {
                    var schedules = await _lopHocService.GetClassScheduleAsync(classId.Value, date.Value, date.Value);
                    var todaySchedule = schedules.FirstOrDefault(s => s.Ngay == DateOnly.FromDateTime(date.Value));

                    if (todaySchedule != null)
                    {
                        // Verify trainer owns this class
                        var canTakeAttendance = await _diemDanhService.CanTrainerTakeAttendanceAsync(trainerId, todaySchedule.LichLopId);
                        if (!canTakeAttendance)
                        {
                            TempData["ErrorMessage"] = "Bạn không có quyền điểm danh cho lớp học này.";
                            return View();
                        }

                        ViewBag.SelectedSchedule = todaySchedule;
                        ViewBag.SelectedClassId = classId.Value;
                        ViewBag.SelectedDate = date.Value;

                        // Get students and existing attendance
                        var students = await _diemDanhService.GetStudentsInClassScheduleAsync(todaySchedule.LichLopId);
                        var existingAttendance = await _diemDanhService.GetAttendanceByClassScheduleAsync(todaySchedule.LichLopId);

                        ViewBag.Students = students;
                        ViewBag.ExistingAttendance = existingAttendance.ToDictionary(a => a.ThanhVienId ?? 0, a => a);
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading attendance page for trainer {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang điểm danh.";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> TakeAttendance(int lichLopId, List<ClassAttendanceRecord> attendanceRecords)
        {
            try
            {
                // Input validation
                if (lichLopId <= 0)
                {
                    return Json(new { success = false, message = "ID lịch lớp không hợp lệ." });
                }

                if (attendanceRecords == null || !attendanceRecords.Any())
                {
                    return Json(new { success = false, message = "Danh sách điểm danh không được để trống." });
                }

                // Validate attendance records
                var validStatuses = new[] { "Present", "Absent", "Late" };
                foreach (var record in attendanceRecords)
                {
                    if (record.ThanhVienId <= 0)
                    {
                        return Json(new { success = false, message = "ID thành viên không hợp lệ." });
                    }

                    if (string.IsNullOrEmpty(record.TrangThai) || !validStatuses.Contains(record.TrangThai))
                    {
                        return Json(new { success = false, message = "Trạng thái điểm danh không hợp lệ." });
                    }

                    // Validate note length if provided
                    if (!string.IsNullOrEmpty(record.GhiChu) && record.GhiChu.Length > 500)
                    {
                        return Json(new { success = false, message = "Ghi chú không được vượt quá 500 ký tự." });
                    }
                }

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;

                // Verify trainer can take attendance for this class
                var canTakeAttendance = await _diemDanhService.CanTrainerTakeAttendanceAsync(trainerId, lichLopId);
                if (!canTakeAttendance)
                {
                    return Json(new { success = false, message = "Bạn không có quyền điểm danh cho lớp học này." });
                }

                // Take attendance
                var result = await _diemDanhService.TakeClassAttendanceAsync(lichLopId, attendanceRecords);

                if (result)
                {
                    return Json(new { success = true, message = "Điểm danh thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra khi điểm danh." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while taking attendance for schedule {LichLopId}", lichLopId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi điểm danh." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetClassSchedules(int classId, DateTime date)
        {
            try
            {
                // Validate date parameter to prevent security risks
                var minDate = DateTime.Now.AddMonths(-1);
                var maxDate = DateTime.Now.AddMonths(1);

                if (date < minDate || date > maxDate)
                {
                    return Json(new { success = false, message = "Ngày không hợp lệ. Chỉ có thể xem lịch trong khoảng 1 tháng trước và sau." });
                }

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;

                // Verify trainer owns this class
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                if (!myClasses.Any(c => c.LopHocId == classId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền truy cập lớp học này." });
                }

                var schedules = await _lopHocService.GetClassScheduleAsync(classId, date, date);
                var result = schedules.Select(s => new
                {
                    lichLopId = s.LichLopId,
                    ngay = s.Ngay.ToString("dd/MM/yyyy"),
                    gioBatDau = s.GioBatDau.ToString("HH:mm"),
                    gioKetThuc = s.GioKetThuc.ToString("HH:mm"),
                    trangThai = s.TrangThai,
                    soLuongDaDat = s.SoLuongDaDat
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting class schedules for class {ClassId} on {Date}", classId, date);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải lịch học." });
            }
        }

        // Helper method to determine student status
        private string GetStudentStatus(DangKy dangKy)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            if (dangKy.TrangThai != "ACTIVE")
                return "INACTIVE";
            
            if (dangKy.NgayKetThuc < today)
                return "EXPIRED";
            
            return "ACTIVE";
        }

        // Helper method to generate events from class schedule when no LichLop exists
        private List<object> GenerateEventsFromClass(LopHoc lopHoc, DateTime start, DateTime end)
        {
            var events = new List<object>();
            
            if (string.IsNullOrEmpty(lopHoc.ThuTrongTuan))
                return events;

            // Parse days of week from ThuTrongTuan string
            var daysOfWeek = lopHoc.ThuTrongTuan.Split(',')
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();

            // Map Vietnamese day names to DayOfWeek
            var dayMapping = new Dictionary<string, DayOfWeek>
            {
                { "Thứ 2", DayOfWeek.Monday },
                { "Thứ 3", DayOfWeek.Tuesday },
                { "Thứ 4", DayOfWeek.Wednesday },
                { "Thứ 5", DayOfWeek.Thursday },
                { "Thứ 6", DayOfWeek.Friday },
                { "Thứ 7", DayOfWeek.Saturday },
                { "Chủ nhật", DayOfWeek.Sunday }
            };

            // Generate events for date range
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                var dayName = GetVietnameseDayName(date.DayOfWeek);
                
                if (daysOfWeek.Contains(dayName))
                {
                    var eventStart = date.Add(lopHoc.GioBatDau.ToTimeSpan());
                    var eventEnd = date.Add(lopHoc.GioKetThuc.ToTimeSpan());
                    
                    events.Add(new
                    {
                        id = $"generated_{lopHoc.LopHocId}_{date:yyyyMMdd}",
                        title = lopHoc.TenLop,
                        start = eventStart,
                        end = eventEnd,
                        backgroundColor = "#3b82f6",
                        borderColor = "#2563eb",
                        extendedProps = new
                        {
                            status = "SCHEDULED",
                            capacity = lopHoc.SucChua,
                            booked = 0,
                            isGenerated = true
                        }
                    });
                }
            }

            return events;
        }

        private string GetVietnameseDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
        }

        // API to get detailed student information
        [HttpGet]
        public async Task<IActionResult> GetStudentDetails(int studentId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;

                // Get student information
                var student = await _nguoiDungService.GetByIdAsync(studentId);
                if (student == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin học viên." });
                }

                // Verify that this trainer can access this student (through class assignments)
                var trainerClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                var hasAccess = trainerClasses.Any(c => c.DangKys != null && 
                    c.DangKys.Any(d => d.NguoiDungId == studentId));

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xem thông tin học viên này." });
                }

                // Get student's registrations in trainer's classes
                var studentRegistrations = trainerClasses
                    .Where(c => c.DangKys != null)
                    .SelectMany(c => c.DangKys.Where(d => d.NguoiDungId == studentId)
                        .Select(d => new
                        {
                            className = c.TenLop,
                            classId = c.LopHocId,
                            registrationDate = d.NgayBatDau.ToString("dd/MM/yyyy"),
                            expiryDate = d.NgayKetThuc.ToString("dd/MM/yyyy"),
                            status = GetStudentStatus(d),
                            isActive = d.TrangThai == "ACTIVE" && d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today)
                        }))
                    .ToList();

                var result = new
                {
                    success = true,
                    data = new
                    {
                        id = student.NguoiDungId,
                        name = $"{student.Ho} {student.Ten}".Trim(),
                        email = student.Email,
                        phone = student.SoDienThoai,
                        address = student.DiaChi,
                        dateOfBirth = student.NgaySinh?.ToString("dd/MM/yyyy"),
                        gender = student.GioiTinh,
                        joinDate = student.NgayThamGia.ToString("dd/MM/yyyy"),
                        avatar = student.AnhDaiDien,
                        registrations = studentRegistrations
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting student details for student {StudentId}", studentId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thông tin học viên." });
            }
        }
    }
}
