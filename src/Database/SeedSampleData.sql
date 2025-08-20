-- =============================================
-- Seed Sample Data for Dashboard Testing
-- =============================================

USE [GymManagement]
GO

-- Thêm thành viên mẫu
IF NOT EXISTS (SELECT 1 FROM NguoiDungs WHERE Email = 'member1@test.com')
BEGIN
    INSERT INTO NguoiDungs (LoaiNguoiDung, Ho, Ten, GioiTinh, NgaySinh, SoDienThoai, Email, NgayThamGia, TrangThai)
    VALUES 
    ('THANHVIEN', N'Nguyễn', N'Văn A', N'Nam', '1990-01-15', '0901234567', 'member1@test.com', GETDATE(), 'ACTIVE'),
    ('THANHVIEN', N'Trần', N'Thị B', N'Nữ', '1992-05-20', '0901234568', 'member2@test.com', GETDATE(), 'ACTIVE'),
    ('THANHVIEN', N'Lê', N'Văn C', N'Nam', '1988-12-10', '0901234569', 'member3@test.com', GETDATE(), 'ACTIVE'),
    ('THANHVIEN', N'Phạm', N'Thị D', N'Nữ', '1995-03-25', '0901234570', 'member4@test.com', GETDATE(), 'ACTIVE'),
    ('THANHVIEN', N'Hoàng', N'Văn E', N'Nam', '1987-08-30', '0901234571', 'member5@test.com', GETDATE(), 'ACTIVE');
    
    PRINT N'✅ Đã thêm 5 thành viên mẫu';
END

-- Thêm HLV mẫu
IF NOT EXISTS (SELECT 1 FROM NguoiDungs WHERE Email = 'trainer1@test.com')
BEGIN
    INSERT INTO NguoiDungs (LoaiNguoiDung, Ho, Ten, GioiTinh, NgaySinh, SoDienThoai, Email, NgayThamGia, TrangThai)
    VALUES 
    ('HLV', N'Nguyễn', N'Thành Nam', N'Nam', '1985-06-15', '0911234567', 'trainer1@test.com', GETDATE(), 'ACTIVE'),
    ('HLV', N'Trần', N'Thị Hoa', N'Nữ', '1987-09-20', '0911234568', 'trainer2@test.com', GETDATE(), 'ACTIVE');
    
    PRINT N'✅ Đã thêm 2 HLV mẫu';
END

-- Thêm lớp học mẫu
IF NOT EXISTS (SELECT 1 FROM LopHocs WHERE TenLop = N'Yoga Cơ Bản')
BEGIN
    DECLARE @HlvId1 INT = (SELECT TOP 1 NguoiDungId FROM NguoiDungs WHERE LoaiNguoiDung = 'HLV' AND Email = 'trainer1@test.com');
    DECLARE @HlvId2 INT = (SELECT TOP 1 NguoiDungId FROM NguoiDungs WHERE LoaiNguoiDung = 'HLV' AND Email = 'trainer2@test.com');
    
    INSERT INTO LopHocs (TenLop, MoTa, HlvId, SucChua, ThuTrongTuan, GioBatDau, GioKetThuc, TrangThai, NgayBatDauKhoa, NgayKetThucKhoa)
    VALUES 
    (N'Yoga Cơ Bản', N'Lớp yoga dành cho người mới bắt đầu', @HlvId1, 20, N'Thứ 2, Thứ 4, Thứ 6', '07:00:00', '08:30:00', 'OPEN', GETDATE(), DATEADD(month, 3, GETDATE())),
    (N'Gym Nâng Cao', N'Lớp tập gym cho người có kinh nghiệm', @HlvId2, 15, N'Thứ 3, Thứ 5, Thứ 7', '18:00:00', '19:30:00', 'OPEN', GETDATE(), DATEADD(month, 3, GETDATE())),
    (N'Cardio Buổi Sáng', N'Lớp cardio tăng cường sức khỏe tim mạch', @HlvId1, 25, N'Thứ 2, Thứ 3, Thứ 4, Thứ 5, Thứ 6', '06:00:00', '07:00:00', 'OPEN', GETDATE(), DATEADD(month, 3, GETDATE()));
    
    PRINT N'✅ Đã thêm 3 lớp học mẫu';
END

-- Thêm đăng ký mẫu
IF NOT EXISTS (SELECT 1 FROM DangKys WHERE NguoiDungId IN (SELECT NguoiDungId FROM NguoiDungs WHERE LoaiNguoiDung = 'THANHVIEN'))
BEGIN
    DECLARE @GoiId1 INT = (SELECT TOP 1 GoiTapId FROM GoiTaps WHERE TenGoi LIKE N'%Cơ Bản%');
    DECLARE @GoiId2 INT = (SELECT TOP 1 GoiTapId FROM GoiTaps WHERE TenGoi LIKE N'%Tiêu Chuẩn%');
    DECLARE @LopId1 INT = (SELECT TOP 1 LopHocId FROM LopHocs WHERE TenLop = N'Yoga Cơ Bản');
    DECLARE @LopId2 INT = (SELECT TOP 1 LopHocId FROM LopHocs WHERE TenLop = N'Gym Nâng Cao');
    
    DECLARE @Member1 INT = (SELECT TOP 1 NguoiDungId FROM NguoiDungs WHERE Email = 'member1@test.com');
    DECLARE @Member2 INT = (SELECT TOP 1 NguoiDungId FROM NguoiDungs WHERE Email = 'member2@test.com');
    DECLARE @Member3 INT = (SELECT TOP 1 NguoiDungId FROM NguoiDungs WHERE Email = 'member3@test.com');
    
    INSERT INTO DangKys (NguoiDungId, GoiTapId, LopHocId, NgayDangKy, NgayBatDau, NgayKetThuc, TrangThai, SoBuoiDaDung)
    VALUES 
    (@Member1, @GoiId1, NULL, GETDATE(), GETDATE(), DATEADD(month, 1, GETDATE()), 'ACTIVE', 5),
    (@Member2, @GoiId2, @LopId1, GETDATE(), GETDATE(), DATEADD(month, 3, GETDATE()), 'ACTIVE', 8),
    (@Member3, @GoiId1, @LopId2, GETDATE(), GETDATE(), DATEADD(month, 1, GETDATE()), 'ACTIVE', 3);
    
    PRINT N'✅ Đã thêm 3 đăng ký mẫu';
END

-- Thêm thanh toán mẫu
IF NOT EXISTS (SELECT 1 FROM ThanhToans WHERE PhuongThuc = 'CASH')
BEGIN
    DECLARE @DangKyId1 INT = (SELECT TOP 1 DangKyId FROM DangKys);
    DECLARE @DangKyId2 INT = (SELECT DangKyId FROM DangKys ORDER BY DangKyId OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY);
    DECLARE @DangKyId3 INT = (SELECT DangKyId FROM DangKys ORDER BY DangKyId OFFSET 2 ROWS FETCH NEXT 1 ROWS ONLY);
    
    INSERT INTO ThanhToans (DangKyId, SoTien, PhuongThuc, TrangThai, NgayThanhToan, GhiChu)
    VALUES 
    (@DangKyId1, 500000, 'CASH', 'SUCCESS', GETDATE(), N'Thanh toán tiền mặt'),
    (@DangKyId2, 1200000, 'VNPAY', 'SUCCESS', GETDATE(), N'Thanh toán VNPay'),
    (@DangKyId3, 500000, 'BANK_TRANSFER', 'SUCCESS', DATEADD(day, -1, GETDATE()), N'Chuyển khoản ngân hàng');
    
    -- Thêm một số thanh toán trong tuần qua
    INSERT INTO ThanhToans (DangKyId, SoTien, PhuongThuc, TrangThai, NgayThanhToan, GhiChu)
    VALUES 
    (@DangKyId1, 300000, 'CASH', 'SUCCESS', DATEADD(day, -2, GETDATE()), N'Thanh toán bổ sung'),
    (@DangKyId2, 150000, 'VNPAY', 'SUCCESS', DATEADD(day, -3, GETDATE()), N'Phí dịch vụ'),
    (@DangKyId3, 200000, 'CASH', 'SUCCESS', DATEADD(day, -4, GETDATE()), N'Thanh toán PT'),
    (@DangKyId1, 400000, 'BANK_TRANSFER', 'SUCCESS', DATEADD(day, -5, GETDATE()), N'Gia hạn gói'),
    (@DangKyId2, 250000, 'VNPAY', 'SUCCESS', DATEADD(day, -6, GETDATE()), N'Phụ phí');
    
    PRINT N'✅ Đã thêm 8 thanh toán mẫu';
END

-- Thêm điểm danh mẫu
IF NOT EXISTS (SELECT 1 FROM DiemDanhs WHERE NgayDiemDanh = CAST(GETDATE() AS DATE))
BEGIN
    DECLARE @Member1 INT = (SELECT TOP 1 NguoiDungId FROM NguoiDungs WHERE Email = 'member1@test.com');
    DECLARE @Member2 INT = (SELECT TOP 1 NguoiDungId FROM NguoiDungs WHERE Email = 'member2@test.com');
    DECLARE @Member3 INT = (SELECT TOP 1 NguoiDungId FROM NguoiDungs WHERE Email = 'member3@test.com');
    DECLARE @LopId1 INT = (SELECT TOP 1 LopHocId FROM LopHocs WHERE TenLop = N'Yoga Cơ Bản');
    
    -- Điểm danh hôm nay
    INSERT INTO DiemDanhs (ThanhVienId, LopHocId, NgayDiemDanh, GioDiemDanh, TrangThai, LoaiCheckIn)
    VALUES 
    (@Member1, @LopId1, GETDATE(), GETDATE(), 'PRESENT', 'Manual'),
    (@Member2, @LopId1, GETDATE(), GETDATE(), 'PRESENT', 'FaceRecognition'),
    (@Member3, NULL, GETDATE(), GETDATE(), 'PRESENT', 'Manual');
    
    -- Điểm danh các ngày trước
    INSERT INTO DiemDanhs (ThanhVienId, LopHocId, NgayDiemDanh, GioDiemDanh, TrangThai, LoaiCheckIn)
    VALUES 
    (@Member1, @LopId1, DATEADD(day, -1, GETDATE()), DATEADD(day, -1, GETDATE()), 'PRESENT', 'Manual'),
    (@Member2, NULL, DATEADD(day, -1, GETDATE()), DATEADD(day, -1, GETDATE()), 'PRESENT', 'FaceRecognition'),
    (@Member1, @LopId1, DATEADD(day, -2, GETDATE()), DATEADD(day, -2, GETDATE()), 'PRESENT', 'Manual'),
    (@Member3, NULL, DATEADD(day, -2, GETDATE()), DATEADD(day, -2, GETDATE()), 'PRESENT', 'Manual'),
    (@Member2, @LopId1, DATEADD(day, -3, GETDATE()), DATEADD(day, -3, GETDATE()), 'PRESENT', 'FaceRecognition');
    
    PRINT N'✅ Đã thêm 8 điểm danh mẫu';
END

PRINT N'🎉 Hoàn thành việc thêm dữ liệu mẫu cho Dashboard!';
