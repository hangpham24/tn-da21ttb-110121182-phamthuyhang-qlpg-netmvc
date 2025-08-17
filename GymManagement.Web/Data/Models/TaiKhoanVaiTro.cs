using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class TaiKhoanVaiTro
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string TaiKhoanId { get; set; } = string.Empty;

        [Required]
        public string VaiTroId { get; set; } = string.Empty;

        public DateTime NgayGan { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual TaiKhoan TaiKhoan { get; set; } = null!;
        public virtual VaiTro VaiTro { get; set; } = null!;
    }
}
