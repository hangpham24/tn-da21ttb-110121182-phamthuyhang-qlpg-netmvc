using GymManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace GymManagement.Web.Attributes
{
    /// <summary>
    /// Attribute để validate quyền truy cập của Trainer
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class TrainerSecurityAttribute : ActionFilterAttribute
    {
        public string? RequiredPermission { get; set; }
        public bool ValidateClassAccess { get; set; } = false;
        public bool ValidateStudentAccess { get; set; } = false;
        public bool ValidateSalaryAccess { get; set; } = false;
        public string ClassIdParameter { get; set; } = "classId";
        public string StudentIdParameter { get; set; } = "studentId";
        public string TrainerIdParameter { get; set; } = "trainerId";

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            var trainerSecurityService = serviceProvider.GetService<ITrainerSecurityService>();
            var logger = serviceProvider.GetService<ILogger<TrainerSecurityAttribute>>();

            if (trainerSecurityService == null || logger == null)
            {
                logger?.LogError("TrainerSecurityService or Logger not found in DI container");
                context.Result = new StatusCodeResult(500);
                return;
            }

            try
            {
                var user = context.HttpContext.User;
                
                // Kiểm tra role cơ bản
                if (!user.IsInRole("Trainer") && !user.IsInRole("Admin"))
                {
                    trainerSecurityService.LogSecurityEvent("UNAUTHORIZED_ACCESS_ATTEMPT", user, new { 
                        Action = context.ActionDescriptor.DisplayName,
                        RequiredRole = "Trainer"
                    });
                    
                    context.Result = new ForbidResult();
                    return;
                }

                // Validate class access nếu được yêu cầu
                if (ValidateClassAccess)
                {
                    var classIdValue = GetParameterValue(context, ClassIdParameter);
                    if (classIdValue != null && int.TryParse(classIdValue, out int classId))
                    {
                        var hasAccess = await trainerSecurityService.ValidateTrainerClassAccessAsync(classId, user);
                        if (!hasAccess)
                        {
                            context.Result = new JsonResult(new { 
                                success = false, 
                                message = "Bạn không có quyền truy cập lớp học này." 
                            }) { StatusCode = 403 };
                            return;
                        }
                    }
                }

                // Validate student access nếu được yêu cầu
                if (ValidateStudentAccess)
                {
                    var studentIdValue = GetParameterValue(context, StudentIdParameter);
                    if (studentIdValue != null && int.TryParse(studentIdValue, out int studentId))
                    {
                        var hasAccess = await trainerSecurityService.ValidateTrainerStudentAccessAsync(studentId, user);
                        if (!hasAccess)
                        {
                            context.Result = new JsonResult(new { 
                                success = false, 
                                message = "Bạn không có quyền truy cập thông tin học viên này." 
                            }) { StatusCode = 403 };
                            return;
                        }
                    }
                }

                // Validate salary access nếu được yêu cầu
                if (ValidateSalaryAccess)
                {
                    var trainerIdValue = GetParameterValue(context, TrainerIdParameter);
                    if (trainerIdValue != null && int.TryParse(trainerIdValue, out int trainerId))
                    {
                        var hasAccess = await trainerSecurityService.ValidateTrainerSalaryAccessAsync(trainerId, user);
                        if (!hasAccess)
                        {
                            context.Result = new JsonResult(new { 
                                success = false, 
                                message = "Bạn không có quyền xem thông tin lương này." 
                            }) { StatusCode = 403 };
                            return;
                        }
                    }
                }

                // Log successful access
                trainerSecurityService.LogSecurityEvent("AUTHORIZED_ACCESS", user, new { 
                    Action = context.ActionDescriptor.DisplayName,
                    Parameters = context.ActionArguments.Keys.ToList()
                });

                await next();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in TrainerSecurityAttribute");
                trainerSecurityService.LogSecurityEvent("SECURITY_VALIDATION_ERROR", context.HttpContext.User, new { 
                    Action = context.ActionDescriptor.DisplayName,
                    Error = ex.Message
                });
                
                context.Result = new StatusCodeResult(500);
            }
        }

        private string? GetParameterValue(ActionExecutingContext context, string parameterName)
        {
            // Kiểm tra trong action parameters
            if (context.ActionArguments.ContainsKey(parameterName))
            {
                return context.ActionArguments[parameterName]?.ToString();
            }

            // Kiểm tra trong query string
            if (context.HttpContext.Request.Query.ContainsKey(parameterName))
            {
                return context.HttpContext.Request.Query[parameterName].FirstOrDefault();
            }

            // Kiểm tra trong route values
            if (context.RouteData.Values.ContainsKey(parameterName))
            {
                return context.RouteData.Values[parameterName]?.ToString();
            }

            return null;
        }
    }

    /// <summary>
    /// Attribute chuyên dụng cho việc validate class access
    /// </summary>
    public class ValidateTrainerClassAccessAttribute : TrainerSecurityAttribute
    {
        public ValidateTrainerClassAccessAttribute(string classIdParameter = "classId")
        {
            ValidateClassAccess = true;
            ClassIdParameter = classIdParameter;
        }
    }

    /// <summary>
    /// Attribute chuyên dụng cho việc validate student access
    /// </summary>
    public class ValidateTrainerStudentAccessAttribute : TrainerSecurityAttribute
    {
        public ValidateTrainerStudentAccessAttribute(string studentIdParameter = "studentId")
        {
            ValidateStudentAccess = true;
            StudentIdParameter = studentIdParameter;
        }
    }

    /// <summary>
    /// Attribute chuyên dụng cho việc validate salary access
    /// </summary>
    public class ValidateTrainerSalaryAccessAttribute : TrainerSecurityAttribute
    {
        public ValidateTrainerSalaryAccessAttribute(string trainerIdParameter = "trainerId")
        {
            ValidateSalaryAccess = true;
            TrainerIdParameter = trainerIdParameter;
        }
    }
}
