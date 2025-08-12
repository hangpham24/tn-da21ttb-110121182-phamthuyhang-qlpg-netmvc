-- =====================================================
-- SCRIPT THÊM CÁC CỘT MỚI CHO MÔ HÌNH LỚP HỌC CỐ ĐỊNH
-- =====================================================

-- 1. Thêm cột NgayBatDau và NgayKetThuc vào bảng LopHoc
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LopHocs' AND COLUMN_NAME = 'NgayBatDauKhoa')
BEGIN
    ALTER TABLE LopHocs ADD NgayBatDauKhoa DATE NULL;
    PRINT 'Added NgayBatDauKhoa column to LopHocs table';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LopHocs' AND COLUMN_NAME = 'NgayKetThucKhoa')
BEGIN
    ALTER TABLE LopHocs ADD NgayKetThucKhoa DATE NULL;
    PRINT 'Added NgayKetThucKhoa column to LopHocs table';
END

-- 2. Thêm cột LoaiDangKy vào bảng LopHoc
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LopHocs' AND COLUMN_NAME = 'LoaiDangKy')
BEGIN
    ALTER TABLE LopHocs ADD LoaiDangKy NVARCHAR(20) DEFAULT 'CLASS';
    PRINT 'Added LoaiDangKy column to LopHocs table';
END

-- 3. Thêm cột LoaiDangKy vào bảng DangKy
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DangKys' AND COLUMN_NAME = 'LoaiDangKy')
BEGIN
    ALTER TABLE DangKys ADD LoaiDangKy NVARCHAR(20) DEFAULT 'PACKAGE';
    PRINT 'Added LoaiDangKy column to DangKys table';
END

-- 4. Thêm cột TrangThaiChiTiet vào bảng DangKy
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DangKys' AND COLUMN_NAME = 'TrangThaiChiTiet')
BEGIN
    ALTER TABLE DangKys ADD TrangThaiChiTiet NVARCHAR(50) NULL;
    PRINT 'Added TrangThaiChiTiet column to DangKys table';
END

-- 5. Cập nhật dữ liệu mẫu cho các lớp học hiện tại
UPDATE LopHocs 
SET NgayBatDauKhoa = DATEADD(day, 7, GETDATE()),  -- Bắt đầu sau 1 tuần
    NgayKetThucKhoa = DATEADD(day, 37, GETDATE())  -- Kết thúc sau 30 ngày học
WHERE NgayBatDauKhoa IS NULL;

PRINT 'Updated sample data for existing classes';

-- 6. Cập nhật dữ liệu hiện tại cho DangKy
UPDATE DangKys 
SET LoaiDangKy = CASE 
    WHEN LopHocId IS NOT NULL THEN 'CLASS'
    WHEN GoiTapId IS NOT NULL THEN 'PACKAGE'
    ELSE 'PACKAGE'
END
WHERE LoaiDangKy IS NULL OR LoaiDangKy = 'PACKAGE';

PRINT 'Updated LoaiDangKy for existing registrations';

-- 7. Cập nhật trạng thái chi tiết
UPDATE DangKys 
SET TrangThaiChiTiet = CASE 
    WHEN TrangThai = 'ACTIVE' AND LopHocId IS NOT NULL THEN 'ENROLLED'
    WHEN TrangThai = 'ACTIVE' AND GoiTapId IS NOT NULL THEN 'ACTIVE_PACKAGE'
    WHEN TrangThai = 'CANCELLED' THEN 'CANCELLED'
    ELSE 'ACTIVE'
END
WHERE TrangThaiChiTiet IS NULL;

PRINT 'Updated TrangThaiChiTiet for existing registrations';

-- 8. Thêm ràng buộc để đảm bảo tính hợp lệ
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.CHECK_CONSTRAINTS WHERE CONSTRAINT_NAME = 'CK_LopHocs_NgayKhoa')
BEGIN
    ALTER TABLE LopHocs 
    ADD CONSTRAINT CK_LopHocs_NgayKhoa 
    CHECK (NgayKetThucKhoa >= NgayBatDauKhoa OR NgayKetThucKhoa IS NULL OR NgayBatDauKhoa IS NULL);
    PRINT 'Added check constraint for LopHocs date validation';
END

-- 9. Tạo index để tối ưu performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LopHocs_NgayKhoa')
BEGIN
    CREATE INDEX IX_LopHocs_NgayKhoa ON LopHocs(NgayBatDauKhoa, NgayKetThucKhoa);
    PRINT 'Created index IX_LopHocs_NgayKhoa';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DangKys_LoaiDangKy')
BEGIN
    CREATE INDEX IX_DangKys_LoaiDangKy ON DangKys(LoaiDangKy, TrangThai);
    PRINT 'Created index IX_DangKys_LoaiDangKy';
END

PRINT '✅ Database schema update completed successfully!';
