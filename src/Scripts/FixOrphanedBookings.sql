-- Script để fix các booking "mồ côi" (booking của lớp đã hủy đăng ký)
-- Chạy script này để cập nhật các booking hiện tại

-- 1. Tìm các booking "mồ côi" - booking của user cho lớp mà user đã hủy đăng ký
SELECT 
    b.BookingId,
    b.ThanhVienId,
    b.LopHocId,
    b.Ngay,
    b.TrangThai as BookingStatus,
    dk.TrangThai as RegistrationStatus,
    u.Ho + ' ' + u.Ten as MemberName,
    lh.TenLop as ClassName
FROM Booking b
INNER JOIN NguoiDung u ON b.ThanhVienId = u.NguoiDungId
INNER JOIN LopHoc lh ON b.LopHocId = lh.LopHocId
LEFT JOIN DangKy dk ON dk.NguoiDungId = b.ThanhVienId 
                    AND dk.LopHocId = b.LopHocId 
                    AND dk.LoaiDangKy = 'CLASS'
WHERE b.TrangThai = 'BOOKED'
  AND (dk.TrangThai IS NULL OR dk.TrangThai = 'CANCELLED')
  AND b.Ngay >= CAST(GETDATE() AS DATE) -- Chỉ booking trong tương lai
ORDER BY b.Ngay, u.Ho, u.Ten;

-- 2. Cập nhật các booking "mồ côi" thành CANCELED
UPDATE b
SET 
    TrangThai = 'CANCELED',
    GhiChu = CASE 
        WHEN dk.TrangThai = 'CANCELLED' THEN 'Tự động hủy do hủy đăng ký lớp học'
        WHEN dk.TrangThai IS NULL THEN 'Tự động hủy do không có đăng ký lớp học hợp lệ'
        ELSE 'Tự động hủy do lỗi dữ liệu'
    END
FROM Booking b
INNER JOIN NguoiDung u ON b.ThanhVienId = u.NguoiDungId
INNER JOIN LopHoc lh ON b.LopHocId = lh.LopHocId
LEFT JOIN DangKy dk ON dk.NguoiDungId = b.ThanhVienId 
                    AND dk.LopHocId = b.LopHocId 
                    AND dk.LoaiDangKy = 'CLASS'
WHERE b.TrangThai = 'BOOKED'
  AND (dk.TrangThai IS NULL OR dk.TrangThai = 'CANCELLED')
  AND b.Ngay >= CAST(GETDATE() AS DATE); -- Chỉ booking trong tương lai

-- 3. Kiểm tra kết quả sau khi cập nhật
SELECT 
    'Đã cập nhật' as Status,
    COUNT(*) as SoLuongBooking
FROM Booking b
INNER JOIN NguoiDung u ON b.ThanhVienId = u.NguoiDungId
INNER JOIN LopHoc lh ON b.LopHocId = lh.LopHocId
LEFT JOIN DangKy dk ON dk.NguoiDungId = b.ThanhVienId 
                    AND dk.LopHocId = b.LopHocId 
                    AND dk.LoaiDangKy = 'CLASS'
WHERE b.TrangThai = 'CANCELED'
  AND b.GhiChu LIKE '%Tự động hủy%'
  AND b.Ngay >= CAST(GETDATE() AS DATE);

-- 4. Thống kê tổng quan
SELECT 
    b.TrangThai,
    COUNT(*) as SoLuong
FROM Booking b
WHERE b.Ngay >= CAST(GETDATE() AS DATE)
GROUP BY b.TrangThai
ORDER BY b.TrangThai;
