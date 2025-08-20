using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class VaiTro
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string TenVaiTro { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MoTa { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<TaiKhoanVaiTro> TaiKhoanVaiTros { get; set; } = new List<TaiKhoanVaiTro>();
    }
}
