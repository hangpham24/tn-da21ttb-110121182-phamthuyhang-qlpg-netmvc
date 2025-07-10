using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLTN.Models.Database
{
    public class CapNhatAnhNhanDien
    {
        [Key]
        public int MaCapNhat { get; set; }

        [Required]
        [ForeignKey("TaiKhoan")]
        public int MaTK { get; set; }

        public byte[]? AnhCu { get; set; }

        [Required]
        public byte[] AnhMoi { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime ThoiGianCapNhat { get; set; } = DateTime.Now;

        [ForeignKey("NguoiCapNhatTaiKhoan")]
        public int? NguoiCapNhat { get; set; }
        
        [StringLength(255)]
        public string? LyDo { get; set; }

        // Navigation properties
        public virtual TaiKhoan? TaiKhoan { get; set; }
        public virtual TaiKhoan? NguoiCapNhatTaiKhoan { get; set; }
    }
} 