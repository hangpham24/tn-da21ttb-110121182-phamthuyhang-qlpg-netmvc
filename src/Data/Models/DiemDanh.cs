using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class DiemDanh
    {
        public int DiemDanhId { get; set; }

        public int? ThanhVienId { get; set; }

        public DateTime ThoiGian { get; set; }

        public bool? KetQuaNhanDang { get; set; }

        [StringLength(255)]
        public string? AnhMinhChung { get; set; }

        public DateTime ThoiGianCheckIn { get; set; } = DateTime.Now;

        // Add CheckOut functionality
        public DateTime? ThoiGianCheckOut { get; set; }

        // Direct class reference (LichLop table was removed)
        public int? LopHocId { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = "Present"; // Present, Absent, Late

        [StringLength(500)]
        public string? GhiChu { get; set; }

        // Face Recognition specific fields
        [StringLength(50)]
        public string? LoaiCheckIn { get; set; } = "FaceRecognition"; // Manual, FaceRecognition

        public double? DoTinCay { get; set; } // Confidence score for face recognition

        // Navigation properties
        public virtual NguoiDung? ThanhVien { get; set; }
        public virtual LopHoc? LopHoc { get; set; }
    }
}
