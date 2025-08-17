using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class ExternalLogin
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string TaiKhoanId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Provider { get; set; } = string.Empty; // Google, Facebook, etc.

        [Required]
        [StringLength(200)]
        public string ProviderKey { get; set; } = string.Empty; // External user ID

        [StringLength(200)]
        public string? ProviderDisplayName { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual TaiKhoan TaiKhoan { get; set; } = null!;
    }
}
