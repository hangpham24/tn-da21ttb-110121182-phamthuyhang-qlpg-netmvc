using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class LichLop
    {
        public int LichLopId { get; set; }
        
        [Required]
        public int LopHocId { get; set; }
        
        [Required]
        public DateOnly Ngay { get; set; }
        
        [Required]
        public TimeOnly GioBatDau { get; set; }
        
        [Required]
        public TimeOnly GioKetThuc { get; set; }
        
        [StringLength(20)]
        public string TrangThai { get; set; } = "SCHEDULED"; // OPEN/CANCELED/FINISHED

        public int SoLuongDaDat { get; set; } = 0;

        // Navigation properties
        public virtual LopHoc LopHoc { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
