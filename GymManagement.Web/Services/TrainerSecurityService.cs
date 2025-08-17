using GymManagement.Web.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GymManagement.Web.Services
{
    /// <summary>
    /// Service chuyên dụng để xử lý bảo mật và validation cho Trainer
    /// </summary>
    public interface ITrainerSecurityService
    {
        Task<bool> ValidateTrainerClassAccessAsync(int classId, ClaimsPrincipal user);
        Task<bool> ValidateTrainerStudentAccessAsync(int studentId, ClaimsPrincipal user);
        Task<bool> ValidateTrainerSalaryAccessAsync(int salaryTrainerId, ClaimsPrincipal user);
        Task<bool> ValidateTrainerAttendanceAccessAsync(int attendanceId, ClaimsPrincipal user);
        void LogSecurityEvent(string eventType, ClaimsPrincipal user, object? data = null);
        Task<List<LopHoc>> GetTrainerClassesSecureAsync(ClaimsPrincipal user);
    }

    public class TrainerSecurityService : ITrainerSecurityService
    {
        private readonly ILopHocService _lopHocService;
        private readonly IDiemDanhService _diemDanhService;
        private readonly IBangLuongService _bangLuongService;
        private readonly IAuthService _authService;
        private readonly ILogger<TrainerSecurityService> _logger;

        public TrainerSecurityService(
            ILopHocService lopHocService,
            IDiemDanhService diemDanhService,
            IBangLuongService bangLuongService,
            IAuthService authService,
            ILogger<TrainerSecurityService> logger)
        {
            _lopHocService = lopHocService;
            _diemDanhService = diemDanhService;
            _bangLuongService = bangLuongService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<bool> ValidateTrainerClassAccessAsync(int classId, ClaimsPrincipal user)
        {
            try
            {
                if (!user.IsInRole("Trainer"))
                {
                    LogSecurityEvent("UNAUTHORIZED_ROLE_ACCESS", user, new { ClassId = classId, RequiredRole = "Trainer" });
                    return false;
                }

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    LogSecurityEvent("MISSING_USER_ID", user, new { ClassId = classId });
                    return false;
                }

                var userAccount = await _authService.GetUserByIdAsync(userId);
                if (userAccount?.NguoiDungId == null)
                {
                    LogSecurityEvent("USER_NOT_FOUND", user, new { UserId = userId, ClassId = classId });
                    return false;
                }

                var lopHoc = await _lopHocService.GetByIdAsync(classId);
                if (lopHoc == null)
                {
                    LogSecurityEvent("CLASS_NOT_FOUND", user, new { ClassId = classId });
                    return false;
                }

                var hasAccess = lopHoc.HlvId == userAccount.NguoiDungId;
                if (!hasAccess)
                {
                    LogSecurityEvent("UNAUTHORIZED_CLASS_ACCESS", user, new { 
                        ClassId = classId,
                        TrainerId = userAccount.NguoiDungId,
                        ClassTrainerId = lopHoc.HlvId 
                    });
                }

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating trainer class access for class {ClassId}", classId);
                LogSecurityEvent("VALIDATION_ERROR", user, new { ClassId = classId, Error = ex.Message });
                return false;
            }
        }

        public async Task<bool> ValidateTrainerStudentAccessAsync(int studentId, ClaimsPrincipal user)
        {
            try
            {
                if (!user.IsInRole("Trainer"))
                {
                    LogSecurityEvent("UNAUTHORIZED_ROLE_ACCESS", user, new { StudentId = studentId, RequiredRole = "Trainer" });
                    return false;
                }

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    LogSecurityEvent("MISSING_USER_ID", user, new { StudentId = studentId });
                    return false;
                }

                var userAccount = await _authService.GetUserByIdAsync(userId);
                if (userAccount?.NguoiDungId == null)
                {
                    LogSecurityEvent("USER_NOT_FOUND", user, new { UserId = userId, StudentId = studentId });
                    return false;
                }

                // Check if student is in any of trainer's classes
                var trainerClasses = await _lopHocService.GetClassesByTrainerAsync(userAccount.NguoiDungId.Value);
                var hasAccess = false;

                foreach (var lopHoc in trainerClasses)
                {
                    var lopHocDetail = await _lopHocService.GetByIdAsync(lopHoc.LopHocId);
                    if (lopHocDetail?.DangKys?.Any(d => d.NguoiDungId == studentId) == true)
                    {
                        hasAccess = true;
                        break;
                    }
                }

                if (!hasAccess)
                {
                    LogSecurityEvent("UNAUTHORIZED_STUDENT_ACCESS", user, new { 
                        StudentId = studentId,
                        TrainerId = userAccount.NguoiDungId 
                    });
                }

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating trainer student access for student {StudentId}", studentId);
                LogSecurityEvent("VALIDATION_ERROR", user, new { StudentId = studentId, Error = ex.Message });
                return false;
            }
        }

        public async Task<bool> ValidateTrainerSalaryAccessAsync(int salaryTrainerId, ClaimsPrincipal user)
        {
            try
            {
                // Admin can access all salaries
                if (user.IsInRole("Admin"))
                {
                    return true;
                }

                if (!user.IsInRole("Trainer"))
                {
                    LogSecurityEvent("UNAUTHORIZED_ROLE_ACCESS", user, new { SalaryTrainerId = salaryTrainerId, RequiredRole = "Trainer" });
                    return false;
                }

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    LogSecurityEvent("MISSING_USER_ID", user, new { SalaryTrainerId = salaryTrainerId });
                    return false;
                }

                var userAccount = await _authService.GetUserByIdAsync(userId);
                if (userAccount?.NguoiDungId == null)
                {
                    LogSecurityEvent("USER_NOT_FOUND", user, new { UserId = userId, SalaryTrainerId = salaryTrainerId });
                    return false;
                }

                var hasAccess = salaryTrainerId == userAccount.NguoiDungId;
                if (!hasAccess)
                {
                    LogSecurityEvent("UNAUTHORIZED_SALARY_ACCESS", user, new { 
                        SalaryTrainerId = salaryTrainerId,
                        CurrentTrainerId = userAccount.NguoiDungId 
                    });
                }

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating trainer salary access for trainer {TrainerId}", salaryTrainerId);
                LogSecurityEvent("VALIDATION_ERROR", user, new { SalaryTrainerId = salaryTrainerId, Error = ex.Message });
                return false;
            }
        }

        public async Task<bool> ValidateTrainerAttendanceAccessAsync(int attendanceId, ClaimsPrincipal user)
        {
            try
            {
                if (!user.IsInRole("Trainer") && !user.IsInRole("Admin"))
                {
                    LogSecurityEvent("UNAUTHORIZED_ROLE_ACCESS", user, new { AttendanceId = attendanceId });
                    return false;
                }

                // Admin can access all attendance records
                if (user.IsInRole("Admin"))
                {
                    return true;
                }

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    LogSecurityEvent("MISSING_USER_ID", user, new { AttendanceId = attendanceId });
                    return false;
                }

                var userAccount = await _authService.GetUserByIdAsync(userId);
                if (userAccount?.NguoiDungId == null)
                {
                    LogSecurityEvent("USER_NOT_FOUND", user, new { UserId = userId, AttendanceId = attendanceId });
                    return false;
                }

                var attendance = await _diemDanhService.GetByIdAsync(attendanceId);
                if (attendance?.LopHoc == null)
                {
                    LogSecurityEvent("ATTENDANCE_NOT_FOUND", user, new { AttendanceId = attendanceId });
                    return false;
                }

                var hasAccess = attendance.LopHoc.HlvId == userAccount.NguoiDungId;
                if (!hasAccess)
                {
                    LogSecurityEvent("UNAUTHORIZED_ATTENDANCE_ACCESS", user, new { 
                        AttendanceId = attendanceId,
                        TrainerId = userAccount.NguoiDungId,
                        ClassTrainerId = attendance.LopHoc.HlvId 
                    });
                }

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating trainer attendance access for attendance {AttendanceId}", attendanceId);
                LogSecurityEvent("VALIDATION_ERROR", user, new { AttendanceId = attendanceId, Error = ex.Message });
                return false;
            }
        }

        public async Task<List<LopHoc>> GetTrainerClassesSecureAsync(ClaimsPrincipal user)
        {
            try
            {
                if (!user.IsInRole("Trainer"))
                {
                    LogSecurityEvent("UNAUTHORIZED_ROLE_ACCESS", user, new { Action = "GetTrainerClasses", RequiredRole = "Trainer" });
                    return new List<LopHoc>();
                }

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    LogSecurityEvent("MISSING_USER_ID", user, new { Action = "GetTrainerClasses" });
                    return new List<LopHoc>();
                }

                var userAccount = await _authService.GetUserByIdAsync(userId);
                if (userAccount?.NguoiDungId == null)
                {
                    LogSecurityEvent("USER_NOT_FOUND", user, new { UserId = userId, Action = "GetTrainerClasses" });
                    return new List<LopHoc>();
                }

                var classes = await _lopHocService.GetClassesByTrainerAsync(userAccount.NguoiDungId.Value);
                LogSecurityEvent("TRAINER_CLASSES_ACCESSED", user, new { 
                    TrainerId = userAccount.NguoiDungId,
                    ClassCount = classes.Count() 
                });

                return classes.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trainer classes securely");
                LogSecurityEvent("VALIDATION_ERROR", user, new { Action = "GetTrainerClasses", Error = ex.Message });
                return new List<LopHoc>();
            }
        }

        public void LogSecurityEvent(string eventType, ClaimsPrincipal user, object? data = null)
        {
            try
            {
                var username = user.Identity?.Name ?? "Unknown";
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                var roles = string.Join(",", user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value));

                if (data != null)
                {
                    _logger.LogWarning("SECURITY EVENT: {EventType} - User: {Username} ({UserId}) Roles: {Roles} Data: {@Data}", 
                        eventType, username, userId, roles, data);
                }
                else
                {
                    _logger.LogWarning("SECURITY EVENT: {EventType} - User: {Username} ({UserId}) Roles: {Roles}", 
                        eventType, username, userId, roles);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event: {EventType}", eventType);
            }
        }
    }
}
