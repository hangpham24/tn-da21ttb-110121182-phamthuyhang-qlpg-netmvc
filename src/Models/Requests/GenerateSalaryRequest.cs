using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Models.Requests
{
    public class GenerateSalaryRequest
    {
        [Required(ErrorMessage = "Tháng là bắt buộc")]
        [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Định dạng tháng không hợp lệ. Sử dụng format YYYY-MM.")]
        public string Month { get; set; } = string.Empty;
    }
}
