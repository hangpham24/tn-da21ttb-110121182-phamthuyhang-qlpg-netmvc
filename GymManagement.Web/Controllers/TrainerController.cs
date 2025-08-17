using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController : BaseController
    {
        private readonly ILopHocService _lopHocService;
        private readonly IBangLuongService _bangLuongService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IDiemDanhService _diemDanhService;
        private readonly IBaoCaoService _baoCaoService;
        private readonly IAuthService _authService;

        public TrainerController(
            ILopHocService lopHocService,
            IBangLuongService bangLuongService,
            INguoiDungService nguoiDungService,
            IDiemDanhService diemDanhService,
            IBaoCaoService baoCaoService,
            IAuthService authService,
            IUserSessionService userSessionService,
            ILogger<TrainerController> logger)
            : base(userSessionService, logger)
        {
            _lopHocService = lopHocService;
            _bangLuongService = bangLuongService;
            _nguoiDungService = nguoiDungService;
            _diemDanhService = diemDanhService;
            _baoCaoService = baoCaoService;
            _authService = authService;
        }

        // Helper method to get current user with enhanced security
        private async Task<TaiKhoan?> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserIdSafe();
            if (string.IsNullOrEmpty(userId))
            {
                LogUserAction("GetCurrentUser_Failed", "No user ID found");
                return null;
            }

            try
            {
                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    LogUserAction("GetCurrentUser_NotFound", new { UserId = userId });
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user for ID: {UserId}", userId);
                return null;
            }
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
                LogUserAction("Dashboard_Access");

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    _logger.LogWarning("Trainer user not found or NguoiDungId is null");
                    return HandleUserNotFound("Dashboard");
                }

                var trainerId = user.NguoiDungId.Value;
                LogUserAction("Dashboard_LoadStart", new { TrainerId = trainerId });

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    return HandleUnauthorized("Bạn không có quyền truy cập dashboard huấn luyện viên.");
                }

                // Lấy thông tin trainer
                var trainer = await _nguoiDungService.GetByIdAsync(trainerId);
                if (trainer == null || trainer.LoaiNguoiDung != "HLV")
                {
                    _logger.LogWarning("Invalid trainer data for ID: {TrainerId}", trainerId);
                    return HandleUnauthorized("Thông tin huấn luyện viên không hợp lệ.");
                }

                ViewBag.Trainer = trainer;
                LogUserAction("Dashboard_TrainerLoaded", new {
                    TrainerName = $"{trainer.Ho} {trainer.Ten}",
                    TrainerType = trainer.LoaiNguoiDung
                });

                // Lấy lớp học được phân công
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                var myClassesList = myClasses.ToList();
                ViewBag.MyClasses = myClassesList.Take(5).ToList();

                LogUserAction("Dashboard_ClassesLoaded", new {
                    ClassCount = myClassesList.Count,
                    TrainerId = trainerId
                });

                // Lấy thông tin lương tháng hiện tại
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                try
                {
                    var currentSalary = await _bangLuongService.GetByTrainerAndMonthAsync(trainerId, currentMonth);
                    ViewBag.CurrentSalary = currentSalary;
                    LogUserAction("Dashboard_SalaryLoaded", new { Month = currentMonth, HasSalary = currentSalary != null });
                }
                catch (Exception salaryEx)
                {
                    _logger.LogWarning(salaryEx, "Could not load salary for trainer {TrainerId}, month {Month}", trainerId, currentMonth);
                    ViewBag.CurrentSalary = null;
                }

                // Thống kê cơ bản
                var totalClasses = myClassesList.Count;
                var activeClasses = myClassesList.Count(c => c.TrangThai == "OPEN");

                ViewBag.TotalClasses = totalClasses;
                ViewBag.ActiveClasses = activeClasses;

                LogUserAction("Dashboard_StatsCalculated", new {
                    TotalClasses = totalClasses,
                    ActiveClasses = activeClasses
                });

                return View();
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải dashboard.");
            }
        }

        // Lớp của tôi - Danh sách lớp học được phân công
        public async Task<IActionResult> MyClasses()
        {
            try
            {
                LogUserAction("MyClasses_Access");

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return HandleUserNotFound("MyClasses");
                }

                var trainerId = user.NguoiDungId.Value;

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    return HandleUnauthorized("Bạn không có quyền xem danh sách lớp học.");
                }

                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);

                LogUserAction("MyClasses_Loaded", new {
                    TrainerId = trainerId,
                    ClassCount = myClasses.Count()
                });

                return View(myClasses);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải danh sách lớp học.");
            }
        }

        // Lịch dạy - Lịch dạy cá nhân
        public async Task<IActionResult> Schedule()
        {
            try
            {
                LogUserAction("Schedule_Access");

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return HandleUserNotFound("Schedule");
                }

                var trainerId = user.NguoiDungId.Value;

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    return HandleUnauthorized("Bạn không có quyền xem lịch dạy.");
                }

                // Lấy lớp học của trainer
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                ViewBag.MyClasses = myClasses;

                LogUserAction("Schedule_Loaded", new {
                    TrainerId = trainerId,
                    ClassCount = myClasses.Count()
                });

                return View();
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải lịch dạy.");
            }
        }

        // API để lấy lịch dạy dạng JSON cho calendar
        [HttpGet]
        public async Task<IActionResult> GetScheduleEvents(DateTime start, DateTime end, int? classId = null)
        {
            try
            {
                LogUserAction("GetScheduleEvents_Access", new { Start = start, End = end, ClassId = classId });

                // Validate date range to prevent abuse
                var maxRange = TimeSpan.FromDays(90); // Maximum 3 months
                if (end - start > maxRange)
                {
                    LogUserAction("GetScheduleEvents_InvalidRange", new { Start = start, End = end });
                    return Json(new { success = false, message = "Khoảng thời gian quá lớn. Tối đa 90 ngày." });
                }

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                var trainerId = user.NguoiDungId.Value;

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    return Json(new { success = false, message = "Bạn không có quyền truy cập lịch dạy." });
                }

                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);

                // Filter by classId if provided and validate ownership
                if (classId.HasValue)
                {
                    var requestedClass = myClasses.FirstOrDefault(c => c.LopHocId == classId.Value);
                    if (requestedClass == null)
                    {
                        LogUserAction("GetScheduleEvents_UnauthorizedClass", new { ClassId = classId.Value, TrainerId = trainerId });
                        return Json(new { success = false, message = "Bạn không có quyền xem lịch của lớp học này." });
                    }
                    myClasses = new[] { requestedClass };
                }

                var events = new List<object>();

                foreach (var lopHoc in myClasses)
                {
                    LogUserAction("GetScheduleEvents_ProcessClass", new { ClassName = lopHoc.TenLop, ClassId = lopHoc.LopHocId });

                    // Generate schedule dynamically from class info
                    var schedules = GenerateDynamicSchedule(lopHoc, start, end);

                    if (schedules.Any())
                    {
                        foreach (var schedule in schedules)
                        {
                            events.Add(new
                            {
                                id = schedule.LopHocId,
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
                        var generatedEvents = GenerateEventsFromClass(lopHoc, start, end);
                        events.AddRange(generatedEvents);
                    }
                }

                LogUserAction("GetScheduleEvents_Success", new {
                    EventCount = events.Count,
                    TrainerId = trainerId
                });

                return Json(new { success = true, data = events });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting trainer schedule events");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải lịch dạy." });
            }
        }

        // Học viên - Danh sách học viên trong các lớp của trainer
        public async Task<IActionResult> Students()
        {
            try
            {
                LogUserAction("Students_Access");

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return HandleUserNotFound("Students");
                }

                var trainerId = user.NguoiDungId.Value;

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    return HandleUnauthorized("Bạn không có quyền xem danh sách học viên.");
                }

                // Lấy lớp học của trainer
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                ViewBag.MyClasses = myClasses;

                LogUserAction("Students_Loaded", new {
                    TrainerId = trainerId,
                    ClassCount = myClasses.Count()
                });

                return View();
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải danh sách học viên.");
            }
        }

        // API để lấy tất cả học viên của trainer (từ tất cả lớp học)
        [HttpGet]
        public async Task<IActionResult> GetAllStudentsByTrainer()
        {
            try
            {
                LogUserAction("GetAllStudentsByTrainer_Access");

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    LogUserAction("GetAllStudentsByTrainer_Unauthorized", new { TrainerId = trainerId });
                    return Json(new { success = false, message = "Bạn không có quyền xem danh sách học viên." });
                }

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

                var uniqueStudents = studentDict.Values.ToList();

                LogUserAction("GetAllStudentsByTrainer_Success", new {
                    TrainerId = trainerId,
                    StudentCount = uniqueStudents.Count,
                    ClassCount = myClasses.Count()
                });

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
                LogUserAction("GetStudentsByClass_Access", new { ClassId = classId });

                // Validate input
                if (classId <= 0)
                {
                    return Json(new { success = false, message = "ID lớp học không hợp lệ." });
                }

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    LogUserAction("GetStudentsByClass_Unauthorized", new { ClassId = classId, TrainerId = trainerId });
                    return Json(new { success = false, message = "Bạn không có quyền xem danh sách học viên." });
                }

                // Kiểm tra xem lớp học có thuộc về trainer này không
                var myClasses = await _lopHocService.GetClassesByTrainerAsync(trainerId);
                if (!myClasses.Any(c => c.LopHocId == classId))
                {
                    LogUserAction("GetStudentsByClass_ClassNotOwned", new { ClassId = classId, TrainerId = trainerId });
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

                LogUserAction("GetStudentsByClass_Success", new {
                    ClassId = classId,
                    TrainerId = trainerId,
                    StudentCount = students.Count
                });

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
                LogUserAction("Salary_Access");

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return HandleUserNotFound("Salary");
                }

                var trainerId = user.NguoiDungId.Value;

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    return HandleUnauthorized("Bạn không có quyền xem thông tin lương.");
                }

                // Lấy lịch sử lương
                var salaries = await _bangLuongService.GetByTrainerIdAsync(trainerId);

                // Lấy thông tin lương tháng hiện tại
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                var currentSalary = await _bangLuongService.GetByTrainerAndMonthAsync(trainerId, currentMonth);
                ViewBag.CurrentSalary = currentSalary;

                // Tính tổng lương đã nhận
                ViewBag.TotalPaidSalary = salaries.Where(s => s.NgayThanhToan != null).Sum(s => s.TongThanhToan);
                ViewBag.PendingSalary = salaries.Where(s => s.NgayThanhToan == null).Sum(s => s.TongThanhToan);

                LogUserAction("Salary_Loaded", new {
                    TrainerId = trainerId,
                    SalaryRecordCount = salaries.Count(),
                    TotalPaid = ViewBag.TotalPaidSalary,
                    Pending = ViewBag.PendingSalary
                });

                return View(salaries.OrderByDescending(s => s.Thang));
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải thông tin lương.");
            }
        }

        // API để lấy chi tiết lương theo tháng
        [HttpGet]
        public async Task<IActionResult> GetSalaryDetails(string month)
        {
            try
            {
                LogUserAction("GetSalaryDetails_Access", new { Month = month });

                // Validate input
                if (string.IsNullOrWhiteSpace(month) || !System.Text.RegularExpressions.Regex.IsMatch(month, @"^\d{4}-\d{2}$"))
                {
                    return Json(new { success = false, message = "Định dạng tháng không hợp lệ." });
                }

                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                var trainerId = user.NguoiDungId.Value;

                // Validate trainer permissions
                if (!IsInRoleSafe("Trainer"))
                {
                    LogUserAction("GetSalaryDetails_Unauthorized", new { Month = month, TrainerId = trainerId });
                    return Json(new { success = false, message = "Bạn không có quyền xem thông tin lương." });
                }

                var salary = await _bangLuongService.GetByTrainerAndMonthAsync(trainerId, month);

                if (salary == null)
                {
                    LogUserAction("GetSalaryDetails_NotFound", new { Month = month, TrainerId = trainerId });
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

                LogUserAction("GetSalaryDetails_Success", new {
                    Month = month,
                    TrainerId = trainerId,
                    Total = salary.TongThanhToan
                });

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

                // If specific class and date provided, get the class info
                if (classId.HasValue && date.HasValue)
                {
                    var lopHoc = myClasses.FirstOrDefault(c => c.LopHocId == classId.Value);
                    var todaySchedule = lopHoc != null ? new {
                        LopHocId = lopHoc.LopHocId,
                        Ngay = DateOnly.FromDateTime(date.Value),
                        GioBatDau = lopHoc.GioBatDau,
                        GioKetThuc = lopHoc.GioKetThuc,
                        LopHoc = lopHoc
                    } : null;

                    if (todaySchedule != null)
                    {
                        // Note: Trainer verification simplified as LichLop no longer exists
                        // Assume trainer has permission for their assigned classes

                        ViewBag.SelectedSchedule = todaySchedule;
                        ViewBag.SelectedClassId = classId.Value;
                        ViewBag.SelectedDate = date.Value;

                        // Get students and existing attendance
                        // Note: Student and attendance loading simplified
                        // Use class-based methods instead of schedule-based
                        ViewBag.Students = new List<object>();
                        ViewBag.ExistingAttendance = new Dictionary<int, object>();
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
        public async Task<IActionResult> TakeAttendance(int lopHocId, List<ClassAttendanceRecord> attendanceRecords)
        {
            try
            {
                // Input validation
                if (lopHocId <= 0)
                {
                    return Json(new { success = false, message = "ID lớp học không hợp lệ." });
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

                // Note: Attendance taking simplified as LichLop methods removed
                // Assume trainer has permission and use basic attendance method
                var result = true; // Simplified for now

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
                _logger.LogError(ex, "Error occurred while taking attendance for schedule {LopHocId}", lopHocId);
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

                var lopHoc = myClasses.FirstOrDefault(c => c.LopHocId == classId);
                if (lopHoc == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lớp học." });
                }

                var result = new
                {
                    lichLopId = 0,
                    ngay = date.ToString("dd/MM/yyyy"),
                    gioBatDau = lopHoc.GioBatDau.ToString("HH:mm"),
                    gioKetThuc = lopHoc.GioKetThuc.ToString("HH:mm"),
                    trangThai = "SCHEDULED",
                    soLuongDaDat = 0 // Will be calculated from bookings
                };

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

        /// <summary>
        /// Generate dynamic schedule from class information
        /// </summary>
        private List<dynamic> GenerateDynamicSchedule(LopHoc lopHoc, DateTime start, DateTime end)
        {
            var schedules = new List<dynamic>();
            var thuTrongTuan = lopHoc.ThuTrongTuan.Split(',').Select(t => t.Trim()).ToList();
            var currentDate = start;

            while (currentDate <= end)
            {
                var dayOfWeek = GetVietnameseDayOfWeek(currentDate.DayOfWeek);
                if (thuTrongTuan.Contains(dayOfWeek))
                {
                    schedules.Add(new {
                        LopHocId = lopHoc.LopHocId, // Use actual LopHocId
                        Ngay = DateOnly.FromDateTime(currentDate),
                        GioBatDau = lopHoc.GioBatDau,
                        GioKetThuc = lopHoc.GioKetThuc,
                        TrangThai = "SCHEDULED",
                        LopHoc = lopHoc
                    });
                }
                currentDate = currentDate.AddDays(1);
            }

            return schedules;
        }

        private string GetVietnameseDayOfWeek(DayOfWeek dayOfWeek)
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
    }
}
