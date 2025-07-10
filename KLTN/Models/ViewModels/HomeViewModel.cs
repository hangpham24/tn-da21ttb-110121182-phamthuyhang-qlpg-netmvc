using System.Collections.Generic;
using KLTN.Models.Database;

namespace KLTN.Models.ViewModels
{
    public class HomeViewModel
    {
        // Danh sách dịch vụ nổi bật (kết hợp cả gói tập và lớp học)
        public List<DichVu> DichVuNoiBat { get; set; } = new List<DichVu>();
        
        // Danh sách huấn luyện viên nổi bật
        public List<HuanLuyenVien> HuanLuyenVienNoiBat { get; set; } = new List<HuanLuyenVien>();
        
        // Danh sách tin tức mới nhất
        public List<TinTuc> TinTucMoiNhat { get; set; } = new List<TinTuc>();
        
        // Các thống kê tổng quan
        public int SoGoiTap { get; set; }
        public int SoLopHoc { get; set; }
        public int SoHuanLuyenVien { get; set; }
        public int SoThanhVien { get; set; }
        
        // Khuyến mãi đang áp dụng (nếu có)
        public List<KhuyenMai> KhuyenMaiDangApDung { get; set; } = new List<KhuyenMai>();
    }
} 