using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLTN.Models.Database
{
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Loại thanh toán")]
        public string LoaiThanhToan { get; set; } = string.Empty; // DangKy, GiaHan, DichVu, KhachVangLai
        
        [ForeignKey("DangKy")]
        [Display(Name = "Đăng ký (nếu có)")]
        public int? MaDangKy { get; set; }
        
        [ForeignKey("GiaHanDangKy")]
        [Display(Name = "Gia hạn đăng ký (nếu có)")]
        public int? MaGiaHan { get; set; }
        
        [ForeignKey("TaiKhoan")]
        [Display(Name = "Người dùng (Tài khoản)")]
        public int? MaTK_NguoiDung { get; set; }
        
        [ForeignKey("KhachVangLai")]
        [Display(Name = "Người dùng (Khách vãng lai)")]
        public int? MaKVL_NguoiDung { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Số tiền")]
        [DataType(DataType.Currency)]
        public decimal SoTien { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [StringLength(50)]
        [Display(Name = "Phương thức thanh toán")]
        public string PhuongThucThanhToan { get; set; } = "TienMat"; // TienMat, ChuyenKhoan, TheNganHang, ViDienTu, QRCode
        
        [Display(Name = "Ngày thanh toán")]
        public DateTime NgayThanhToan { get; set; } = DateTime.Now;
        
        [ForeignKey("NguoiThu")]
        [Display(Name = "Người thu (Tài khoản)")]
        public int? MaTKNguoiThu { get; set; }
        
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "ThanhCong"; // ThanhCong, ThatBai, ChoPheChuan, DaHuy
        
        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }
        
        // Thêm các thuộc tính cho thanh toán online
        [StringLength(100)]
        [Display(Name = "Mã giao dịch")]
        public string? MaGiaoDich { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Đơn vị thanh toán")]
        public string? DonViThanhToan { get; set; } // Tên ngân hàng, ví điện tử, ...
        
        [StringLength(100)]
        [Display(Name = "Tài khoản/Thẻ thanh toán")]
        public string? TaiKhoanThanhToan { get; set; } // Số tài khoản, số thẻ (đã ẩn một phần)
        
        [StringLength(255)]
        [Display(Name = "URL hóa đơn điện tử")]
        public string? HoaDonDienTuUrl { get; set; }
        
        [Display(Name = "Đã xuất hóa đơn")]
        public bool DaXuatHoaDon { get; set; } = false;
        
        // Navigation properties
        public virtual TaiKhoan? NguoiThu { get; set; }
        public virtual DangKy? DangKy { get; set; }
        public virtual GiaHanDangKy? GiaHanDangKy { get; set; }
        public virtual TaiKhoan? NguoiDung_TaiKhoan { get; set; }
        public virtual KhachVangLai? NguoiDung_KhachVangLai { get; set; }
        public virtual ICollection<DoanhThu>? DoanhThus { get; set; }
    }
} 
