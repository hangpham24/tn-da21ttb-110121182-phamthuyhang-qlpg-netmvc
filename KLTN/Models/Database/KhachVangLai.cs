using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLTN.Models.Database
{
    public class KhachVangLai
    {
        [Key]
        public int MaKVL { get; set; }

        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string? HoTen { get; set; }

        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày ghi nhận")]
        public DateTime NgayGhiNhan { get; set; } = DateTime.Now;

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá buổi tập")]
        public decimal? GiaTien { get; set; }

        // Navigation properties
        public virtual ICollection<DangKy>? DangKys { get; set; }
        public virtual ICollection<LichSuCheckIn>? LichSuCheckIns { get; set; }
        public virtual ICollection<PhienTap>? PhienTaps { get; set; }
    }
}
