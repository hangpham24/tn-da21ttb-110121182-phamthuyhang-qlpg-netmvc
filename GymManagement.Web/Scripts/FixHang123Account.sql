-- Script để thêm NguoiDung cho tài khoản hang123
-- Chạy script này trong SQL Server Management Studio

USE [GymManagementDB]
GO

-- Kiểm tra tài khoản hang123 có tồn tại không
SELECT * FROM TaiKhoan WHERE TenDangNhap = 'hang123'

-- Lấy ID của tài khoản hang123
DECLARE @TaiKhoanId NVARCHAR(450)
SELECT @TaiKhoanId = Id FROM TaiKhoan WHERE TenDangNhap = 'hang123'

IF @TaiKhoanId IS NOT NULL
BEGIN
    -- Kiểm tra xem đã có NguoiDung chưa
    IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE TaiKhoanId = @TaiKhoanId)
    BEGIN
        -- Thêm NguoiDung mới cho tài khoản hang123
        INSERT INTO NguoiDung (
            Ho, 
            Ten, 
            GioiTinh, 
            NgaySinh, 
            SoDienThoai, 
            DiaChi, 
            TaiKhoanId, 
            NgayTao, 
            NgayCapNhat
        )
        VALUES (
            N'Nguyễn', 
            N'Hoàng', 
            N'Nam', 
            '1995-01-01', 
            '0123456789', 
            N'Hà Nội', 
            @TaiKhoanId, 
            GETDATE(), 
            GETDATE()
        )
        
        PRINT 'Đã thêm NguoiDung cho tài khoản hang123'
    END
    ELSE
    BEGIN
        PRINT 'Tài khoản hang123 đã có NguoiDung'
    END
END
ELSE
BEGIN
    PRINT 'Không tìm thấy tài khoản hang123'
END

-- Kiểm tra kết quả
SELECT 
    tk.TenDangNhap,
    tk.Email,
    nd.Ho,
    nd.Ten,
    nd.GioiTinh,
    nd.NgaySinh,
    nd.SoDienThoai
FROM TaiKhoan tk
LEFT JOIN NguoiDung nd ON tk.Id = nd.TaiKhoanId
WHERE tk.TenDangNhap = 'hang123'
