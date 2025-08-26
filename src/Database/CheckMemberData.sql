-- Kiểm tra dữ liệu học viên đăng ký lớp
SELECT 
    l.LopHocId,
    l.TenLop,
    l.HlvId,
    COUNT(d.DangKyId) as SoHocVien,
    STRING_AGG(CONCAT(n.Ho, ' ', n.Ten), ', ') as DanhSachHocVien
FROM LopHoc l
LEFT JOIN DangKy d ON l.LopHocId = d.LopHocId AND d.TrangThai = 'ACTIVE'
LEFT JOIN NguoiDung n ON d.NguoiDungId = n.NguoiDungId
GROUP BY l.LopHocId, l.TenLop, l.HlvId;
