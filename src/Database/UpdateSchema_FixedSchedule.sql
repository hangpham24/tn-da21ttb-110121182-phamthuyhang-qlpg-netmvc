-- =====================================================
-- SCRIPT CẬP NHẬT DATABASE CHO MÔ HÌNH LỚP HỌC CỐ ĐỊNH
-- =====================================================

-- 1. Thêm cột NgayBatDau và NgayKetThuc vào bảng LopHoc
ALTER TABLE LopHoc 
ADD NgayBatDauKhoa DATE NULL,
    NgayKetThucKhoa DATE NULL;

-- 2. Cập nhật dữ liệu mẫu cho các lớp học hiện tại
UPDATE LopHoc 
SET NgayBatDauKhoa = DATEADD(day, 7, GETDATE()),  -- Bắt đầu sau 1 tuần
    NgayKetThucKhoa = DATEADD(day, 37, GETDATE())  -- Kết thúc sau 30 ngày học
WHERE NgayBatDauKhoa IS NULL;

-- 3. Thêm ràng buộc để đảm bảo tính hợp lệ
ALTER TABLE LopHoc 
ADD CONSTRAINT CK_LopHoc_NgayKhoa 
CHECK (NgayKetThucKhoa >= NgayBatDauKhoa);

-- 4. Thêm cột để đánh dấu loại đăng ký
ALTER TABLE DangKy 
ADD LoaiDangKy NVARCHAR(20) DEFAULT 'PACKAGE';  -- 'PACKAGE' hoặc 'CLASS'

-- 5. Cập nhật dữ liệu hiện tại
UPDATE DangKy 
SET LoaiDangKy = CASE 
    WHEN LopHocId IS NOT NULL THEN 'CLASS'
    WHEN GoiTapId IS NOT NULL THEN 'PACKAGE'
    ELSE 'PACKAGE'
END;

-- 6. Thêm cột trạng thái chi tiết cho đăng ký lớp học
ALTER TABLE DangKy 
ADD TrangThaiChiTiet NVARCHAR(50) NULL;

-- Cập nhật trạng thái chi tiết
UPDATE DangKy 
SET TrangThaiChiTiet = CASE 
    WHEN TrangThai = 'ACTIVE' AND LopHocId IS NOT NULL THEN 'ENROLLED'
    WHEN TrangThai = 'ACTIVE' AND GoiTapId IS NOT NULL THEN 'ACTIVE_PACKAGE'
    WHEN TrangThai = 'CANCELLED' THEN 'CANCELLED'
    ELSE 'ACTIVE'
END;

-- 7. Tạo index để tối ưu performance
CREATE INDEX IX_LopHoc_NgayKhoa ON LopHoc(NgayBatDauKhoa, NgayKetThucKhoa);
CREATE INDEX IX_DangKy_LoaiDangKy ON DangKy(LoaiDangKy, TrangThai);

-- 8. Thêm stored procedure để đăng ký lớp học theo mô hình mới
CREATE OR ALTER PROCEDURE sp_RegisterClassFixed
    @NguoiDungId INT,
    @LopHocId INT,
    @Result BIT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NgayBatDau DATE, @NgayKetThuc DATE, @SucChua INT, @CurrentCount INT;
    
    -- Lấy thông tin lớp học
    SELECT @NgayBatDau = NgayBatDauKhoa, 
           @NgayKetThuc = NgayKetThucKhoa,
           @SucChua = SucChua
    FROM LopHoc 
    WHERE LopHocId = @LopHocId AND TrangThai = 'OPEN';
    
    -- Kiểm tra lớp học tồn tại
    IF @NgayBatDau IS NULL
    BEGIN
        SET @Result = 0;
        SET @Message = N'Lớp học không tồn tại hoặc đã đóng';
        RETURN;
    END
    
    -- Kiểm tra đã đăng ký chưa
    IF EXISTS (SELECT 1 FROM DangKy 
               WHERE NguoiDungId = @NguoiDungId 
               AND LopHocId = @LopHocId 
               AND TrangThai = 'ACTIVE')
    BEGIN
        SET @Result = 0;
        SET @Message = N'Bạn đã đăng ký lớp học này rồi';
        RETURN;
    END
    
    -- Kiểm tra sức chứa
    SELECT @CurrentCount = COUNT(*) 
    FROM DangKy 
    WHERE LopHocId = @LopHocId AND TrangThai = 'ACTIVE';
    
    IF @CurrentCount >= @SucChua
    BEGIN
        SET @Result = 0;
        SET @Message = N'Lớp học đã đầy';
        RETURN;
    END
    
    -- Tạo đăng ký mới
    INSERT INTO DangKy (NguoiDungId, LopHocId, NgayBatDau, NgayKetThuc, 
                        TrangThai, NgayTao, LoaiDangKy, TrangThaiChiTiet)
    VALUES (@NguoiDungId, @LopHocId, @NgayBatDau, @NgayKetThuc, 
            'ACTIVE', GETDATE(), 'CLASS', 'ENROLLED');
    
    SET @Result = 1;
    SET @Message = N'Đăng ký thành công';
END;

-- 9. Thêm view để dễ dàng truy vấn thông tin lớp học
CREATE OR ALTER VIEW vw_LopHocInfo AS
SELECT 
    l.LopHocId,
    l.TenLop,
    l.NgayBatDauKhoa,
    l.NgayKetThucKhoa,
    l.GioBatDau,
    l.GioKetThuc,
    l.ThuTrongTuan,
    l.SucChua,
    l.TrangThai,
    l.MoTa,
    h.Ho + ' ' + h.Ten AS TenHLV,
    COUNT(d.DangKyId) AS SoLuongDaDangKy,
    (l.SucChua - COUNT(d.DangKyId)) AS SoChoConLai
FROM LopHoc l
LEFT JOIN NguoiDung h ON l.HlvId = h.NguoiDungId
LEFT JOIN DangKy d ON l.LopHocId = d.LopHocId AND d.TrangThai = 'ACTIVE'
GROUP BY l.LopHocId, l.TenLop, l.NgayBatDauKhoa, l.NgayKetThucKhoa,
         l.GioBatDau, l.GioKetThuc, l.ThuTrongTuan, l.SucChua, 
         l.TrangThai, l.MoTa, h.Ho, h.Ten;

PRINT N'✅ Cập nhật database thành công cho mô hình lớp học cố định!';
