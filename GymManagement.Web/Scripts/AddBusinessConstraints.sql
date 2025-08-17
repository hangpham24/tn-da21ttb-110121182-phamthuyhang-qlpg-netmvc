-- =============================================
-- Script: Add Business Logic Constraints
-- Purpose: Thêm các constraints để đảm bảo business rules
-- Date: 2025-01-27
-- =============================================

USE [GymManagement]
GO

-- 1. Constraint: GioKetThuc phải lớn hơn GioBatDau
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_LopHoc_TimeRange')
BEGIN
    ALTER TABLE LopHoc 
    ADD CONSTRAINT CK_LopHoc_TimeRange 
    CHECK (GioKetThuc > GioBatDau);
    PRINT 'Added constraint: CK_LopHoc_TimeRange';
END
ELSE
    PRINT 'Constraint CK_LopHoc_TimeRange already exists';

-- 2. Constraint: SucChua phải lớn hơn 0
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_LopHoc_Capacity')
BEGIN
    ALTER TABLE LopHoc 
    ADD CONSTRAINT CK_LopHoc_Capacity 
    CHECK (SucChua > 0 AND SucChua <= 100);
    PRINT 'Added constraint: CK_LopHoc_Capacity';
END
ELSE
    PRINT 'Constraint CK_LopHoc_Capacity already exists';

-- 3. Constraint: ThoiLuong phải hợp lý (15-300 phút)
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_LopHoc_Duration')
BEGIN
    ALTER TABLE LopHoc 
    ADD CONSTRAINT CK_LopHoc_Duration 
    CHECK (ThoiLuong IS NULL OR (ThoiLuong >= 15 AND ThoiLuong <= 300));
    PRINT 'Added constraint: CK_LopHoc_Duration';
END
ELSE
    PRINT 'Constraint CK_LopHoc_Duration already exists';

-- 4. Constraint: TrangThai chỉ được phép có các giá trị hợp lệ
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_LopHoc_Status')
BEGIN
    ALTER TABLE LopHoc 
    ADD CONSTRAINT CK_LopHoc_Status 
    CHECK (TrangThai IN ('OPEN', 'CLOSED', 'FULL', 'CANCELLED'));
    PRINT 'Added constraint: CK_LopHoc_Status';
END
ELSE
    PRINT 'Constraint CK_LopHoc_Status already exists';

-- 5. Constraint: DangKy NgayKetThuc phải sau NgayBatDau
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_DangKy_DateRange')
BEGIN
    ALTER TABLE DangKy 
    ADD CONSTRAINT CK_DangKy_DateRange 
    CHECK (NgayKetThuc > NgayBatDau);
    PRINT 'Added constraint: CK_DangKy_DateRange';
END
ELSE
    PRINT 'Constraint CK_DangKy_DateRange already exists';

-- 6. Constraint: DangKy TrangThai hợp lệ
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_DangKy_Status')
BEGIN
    ALTER TABLE DangKy 
    ADD CONSTRAINT CK_DangKy_Status 
    CHECK (TrangThai IN ('ACTIVE', 'EXPIRED', 'CANCELLED'));
    PRINT 'Added constraint: CK_DangKy_Status';
END
ELSE
    PRINT 'Constraint CK_DangKy_Status already exists';

-- 7. Constraint: LichLop GioKetThuc > GioBatDau
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_LichLop_TimeRange')
BEGIN
    ALTER TABLE LichLop 
    ADD CONSTRAINT CK_LichLop_TimeRange 
    CHECK (GioKetThuc > GioBatDau);
    PRINT 'Added constraint: CK_LichLop_TimeRange';
END
ELSE
    PRINT 'Constraint CK_LichLop_TimeRange already exists';

-- 8. Constraint: LichLop TrangThai hợp lệ
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_LichLop_Status')
BEGIN
    ALTER TABLE LichLop 
    ADD CONSTRAINT CK_LichLop_Status 
    CHECK (TrangThai IN ('SCHEDULED', 'CANCELLED', 'FINISHED'));
    PRINT 'Added constraint: CK_LichLop_Status';
END
ELSE
    PRINT 'Constraint CK_LichLop_Status already exists';

-- 9. Index: Cải thiện performance cho queries thường dùng
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LopHoc_TrangThai_ThuTrongTuan')
BEGIN
    CREATE INDEX IX_LopHoc_TrangThai_ThuTrongTuan 
    ON LopHoc (TrangThai, ThuTrongTuan);
    PRINT 'Added index: IX_LopHoc_TrangThai_ThuTrongTuan';
END
ELSE
    PRINT 'Index IX_LopHoc_TrangThai_ThuTrongTuan already exists';

-- 10. Index: Cải thiện performance cho DangKy queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DangKy_NguoiDungId_TrangThai')
BEGIN
    CREATE INDEX IX_DangKy_NguoiDungId_TrangThai 
    ON DangKy (NguoiDungId, TrangThai) 
    INCLUDE (LopHocId, NgayBatDau, NgayKetThuc);
    PRINT 'Added index: IX_DangKy_NguoiDungId_TrangThai';
END
ELSE
    PRINT 'Index IX_DangKy_NguoiDungId_TrangThai already exists';

-- 11. Index: Cải thiện performance cho LichLop queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LichLop_LopHocId_Ngay')
BEGIN
    CREATE INDEX IX_LichLop_LopHocId_Ngay 
    ON LichLop (LopHocId, Ngay) 
    INCLUDE (TrangThai, GioBatDau, GioKetThuc);
    PRINT 'Added index: IX_LichLop_LopHocId_Ngay';
END
ELSE
    PRINT 'Index IX_LichLop_LopHocId_Ngay already exists';

PRINT 'All business constraints and indexes have been processed successfully!';
