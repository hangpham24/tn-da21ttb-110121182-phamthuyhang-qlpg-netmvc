using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Web.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var statusCodeResult = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>();

            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Trang bạn đang tìm kiếm không tồn tại.";
                    ViewBag.ErrorTitle = "Không tìm thấy trang";
                    ViewBag.StatusCode = 404;
                    _logger.LogWarning("404 Error Occurred. Path = {Path} and QueryString = {QueryString}",
                        statusCodeResult?.OriginalPath, statusCodeResult?.OriginalQueryString);
                    break;
                case 403:
                    ViewBag.ErrorMessage = "Bạn không có quyền truy cập vào trang này.";
                    ViewBag.ErrorTitle = "Truy cập bị từ chối";
                    ViewBag.StatusCode = 403;
                    _logger.LogWarning("403 Error Occurred. Path = {Path} and QueryString = {QueryString}",
                        statusCodeResult?.OriginalPath, statusCodeResult?.OriginalQueryString);
                    break;
                case 500:
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
                    ViewBag.ErrorTitle = "Lỗi hệ thống";
                    ViewBag.StatusCode = 500;
                    _logger.LogError("500 Error Occurred. Path = {Path} and QueryString = {QueryString}",
                        statusCodeResult?.OriginalPath, statusCodeResult?.OriginalQueryString);
                    break;
                default:
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi không xác định.";
                    ViewBag.ErrorTitle = "Lỗi";
                    ViewBag.StatusCode = statusCode;
                    _logger.LogError("Error Occurred. StatusCode = {StatusCode}, Path = {Path} and QueryString = {QueryString}",
                        statusCode, statusCodeResult?.OriginalPath, statusCodeResult?.OriginalQueryString);
                    break;
            }

            return View("Error");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            ViewBag.ErrorMessage = "Đã xảy ra lỗi không xác định.";
            ViewBag.ErrorTitle = "Lỗi";
            ViewBag.StatusCode = 500;
            return View();
        }
    }
}
