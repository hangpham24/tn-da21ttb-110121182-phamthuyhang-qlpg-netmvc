-- =============================================
-- Test Script: Tính năng Khách vãng lai
-- =============================================

USE [GymManagement]
GO

PRINT N'🧪 BẮT ĐẦU TEST TÍNH NĂNG KHÁCH VÃNG LAI'
PRINT N'=================================================='

-- 1. Kiểm tra gói vé đặc biệt đã được tạo chưa
PRINT N''
PRINT N'📋 1. KIỂM TRA GÓI VÉ ĐẶC BIỆT:'
SELECT 
    GoiTapId,
    TenGoi,
    ThoiHanThang,
    FORMAT(Gia, 'N0') + N' VNĐ' AS Gia,
    MoTa
FROM GoiTap 
WHERE ThoiHanThang = 0
ORDER BY Gia

-- 2. Tạo dữ liệu test khách vãng lai
PRINT N''
PRINT N'👤 2. TẠO DỮ LIỆU TEST KHÁCH VÃNG LAI:'

-- Xóa dữ liệu test cũ nếu có
DELETE FROM DiemDanh WHERE ThanhVienId IN (
    SELECT NguoiDungId FROM NguoiDung WHERE LoaiNguoiDung = 'VANGLAI' AND Ho LIKE 'Test%'
)
DELETE FROM ThanhToan WHERE DangKyId IN (
    SELECT DangKyId FROM DangKy WHERE NguoiDungId IN (
        SELECT NguoiDungId FROM NguoiDung WHERE LoaiNguoiDung = 'VANGLAI' AND Ho LIKE 'Test%'
    )
)
DELETE FROM DangKy WHERE NguoiDungId IN (
    SELECT NguoiDungId FROM NguoiDung WHERE LoaiNguoiDung = 'VANGLAI' AND Ho LIKE 'Test%'
)
DELETE FROM NguoiDung WHERE LoaiNguoiDung = 'VANGLAI' AND Ho LIKE 'Test%'

-- Tạo khách vãng lai test
INSERT INTO NguoiDung (LoaiNguoiDung, Ho, Ten, SoDienThoai, Email, NgayThamGia, TrangThai, NgayTao)
VALUES 
('VANGLAI', 'Test', 'Khách A', '0123456789', 'testA@example.com', CAST(GETDATE() AS DATE), 'ACTIVE', GETDATE()),
('VANGLAI', 'Test', 'Khách B', '0987654321', 'testB@example.com', CAST(GETDATE() AS DATE), 'ACTIVE', GETDATE()),
('VANGLAI', 'Test', 'Khách C', '0111222333', NULL, CAST(GETDATE() AS DATE), 'ACTIVE', GETDATE())

PRINT N'✅ Đã tạo 3 khách vãng lai test'

-- 3. Tạo vé ngày (DAYPASS) cho khách test
PRINT N''
PRINT N'🎫 3. TẠO VÉ NGÀY CHO KHÁCH TEST:'

DECLARE @GuestA_ID INT = (SELECT NguoiDungId FROM NguoiDung WHERE Ho = 'Test' AND Ten = 'Khách A')
DECLARE @GuestB_ID INT = (SELECT NguoiDungId FROM NguoiDung WHERE Ho = 'Test' AND Ten = 'Khách B')
DECLARE @GuestC_ID INT = (SELECT NguoiDungId FROM NguoiDung WHERE Ho = 'Test' AND Ten = 'Khách C')

-- Vé ngày cho khách A (đã thanh toán)
INSERT INTO DangKy (NguoiDungId, LoaiDangKy, NgayBatDau, NgayKetThuc, PhiDangKy, TrangThai, NgayTao)
VALUES (@GuestA_ID, 'DAYPASS', CAST(GETDATE() AS DATE), CAST(GETDATE() AS DATE), 50000, 'ACTIVE', GETDATE())

-- Vé 3 giờ cho khách B (đã thanh toán)
INSERT INTO DangKy (NguoiDungId, LoaiDangKy, NgayBatDau, NgayKetThuc, PhiDangKy, TrangThai, NgayTao)
VALUES (@GuestB_ID, 'HOURPASS', CAST(GETDATE() AS DATE), CAST(GETDATE() AS DATE), 30000, 'ACTIVE', GETDATE())

-- Vé buổi chiều cho khách C (chưa thanh toán)
INSERT INTO DangKy (NguoiDungId, LoaiDangKy, NgayBatDau, NgayKetThuc, PhiDangKy, TrangThai, NgayTao)
VALUES (@GuestC_ID, 'DAYPASS', CAST(GETDATE() AS DATE), CAST(GETDATE() AS DATE), 40000, 'PENDING_PAYMENT', GETDATE())

PRINT N'✅ Đã tạo 3 vé test (2 ACTIVE, 1 PENDING_PAYMENT)'

-- 4. Tạo thanh toán cho vé
PRINT N''
PRINT N'💰 4. TẠO THANH TOÁN CHO VÉ:'

DECLARE @DangKyA_ID INT = (SELECT DangKyId FROM DangKy WHERE NguoiDungId = @GuestA_ID)
DECLARE @DangKyB_ID INT = (SELECT DangKyId FROM DangKy WHERE NguoiDungId = @GuestB_ID)
DECLARE @DangKyC_ID INT = (SELECT DangKyId FROM DangKy WHERE NguoiDungId = @GuestC_ID)

-- Thanh toán tiền mặt cho khách A
INSERT INTO ThanhToan (DangKyId, SoTien, PhuongThuc, TrangThai, NgayThanhToan, GhiChu)
VALUES (@DangKyA_ID, 50000, 'CASH', 'SUCCESS', GETDATE(), 'WALKIN - Vé ngày')

-- Thanh toán chuyển khoản cho khách B
INSERT INTO ThanhToan (DangKyId, SoTien, PhuongThuc, TrangThai, NgayThanhToan, GhiChu)
VALUES (@DangKyB_ID, 30000, 'BANK', 'SUCCESS', GETDATE(), 'WALKIN - Vé 3 giờ')

-- Thanh toán pending cho khách C
INSERT INTO ThanhToan (DangKyId, SoTien, PhuongThuc, TrangThai, NgayThanhToan, GhiChu)
VALUES (@DangKyC_ID, 40000, 'BANK', 'PENDING', GETDATE(), 'WALKIN - Vé buổi chiều')

PRINT N'✅ Đã tạo 3 thanh toán (2 SUCCESS, 1 PENDING)'

-- 5. Tạo điểm danh cho khách đã thanh toán
PRINT N''
PRINT N'📝 5. TẠO ĐIỂM DANH CHO KHÁCH ĐÃ THANH TOÁN:'

-- Check-in cho khách A (đang tập)
INSERT INTO DiemDanh (ThanhVienId, ThoiGianCheckIn, LoaiCheckIn, GhiChu, TrangThai)
VALUES (@GuestA_ID, DATEADD(HOUR, -2, GETDATE()), 'Manual', 'WALKIN_DAYPASS', 'Present')

-- Check-in và check-out cho khách B (đã tập xong)
INSERT INTO DiemDanh (ThanhVienId, ThoiGianCheckIn, ThoiGianCheckOut, LoaiCheckIn, GhiChu, TrangThai)
VALUES (@GuestB_ID, DATEADD(HOUR, -4, GETDATE()), DATEADD(HOUR, -1, GETDATE()), 'Manual', 'WALKIN_HOURPASS', 'Completed')

PRINT N'✅ Đã tạo điểm danh (1 đang tập, 1 đã xong)'

-- 6. Kiểm tra kết quả
PRINT N''
PRINT N'📊 6. KIỂM TRA KẾT QUẢ:'

-- Danh sách khách vãng lai hôm nay
PRINT N''
PRINT N'👥 DANH SÁCH KHÁCH VÃNG LAI HÔM NAY:'
SELECT 
    nd.NguoiDungId,
    CONCAT(nd.Ho, ' ', nd.Ten) AS HoTen,
    nd.SoDienThoai,
    dk.LoaiDangKy,
    FORMAT(dk.PhiDangKy, 'N0') + N' VNĐ' AS Gia,
    dk.TrangThai AS TrangThaiVe,
    tt.PhuongThuc,
    tt.TrangThai AS TrangThaiThanhToan,
    CASE 
        WHEN dd.ThoiGianCheckOut IS NULL AND dd.ThoiGianCheckIn IS NOT NULL THEN N'🟢 Đang tập'
        WHEN dd.ThoiGianCheckOut IS NOT NULL THEN N'⚫ Đã xong'
        ELSE N'⏳ Chưa vào'
    END AS TrangThaiTap
FROM NguoiDung nd
LEFT JOIN DangKy dk ON nd.NguoiDungId = dk.NguoiDungId
LEFT JOIN ThanhToan tt ON dk.DangKyId = tt.DangKyId
LEFT JOIN DiemDanh dd ON nd.NguoiDungId = dd.ThanhVienId AND CAST(dd.ThoiGianCheckIn AS DATE) = CAST(GETDATE() AS DATE)
WHERE nd.LoaiNguoiDung = 'VANGLAI' 
  AND nd.Ho LIKE 'Test%'
  AND CAST(nd.NgayTao AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY nd.NgayTao DESC

-- Thống kê doanh thu khách vãng lai
PRINT N''
PRINT N'💰 THỐNG KÊ DOANH THU KHÁCH VÃNG LAI HÔM NAY:'
SELECT 
    COUNT(*) AS SoLuotKhach,
    SUM(CASE WHEN tt.TrangThai = 'SUCCESS' THEN tt.SoTien ELSE 0 END) AS DoanhThuThanhCong,
    SUM(CASE WHEN tt.TrangThai = 'PENDING' THEN tt.SoTien ELSE 0 END) AS DoanhThuCho,
    SUM(CASE WHEN tt.PhuongThuc = 'CASH' AND tt.TrangThai = 'SUCCESS' THEN tt.SoTien ELSE 0 END) AS DoanhThuTienMat,
    SUM(CASE WHEN tt.PhuongThuc = 'BANK' AND tt.TrangThai = 'SUCCESS' THEN tt.SoTien ELSE 0 END) AS DoanhThuChuyenKhoan
FROM ThanhToan tt
JOIN DangKy dk ON tt.DangKyId = dk.DangKyId
JOIN NguoiDung nd ON dk.NguoiDungId = nd.NguoiDungId
WHERE nd.LoaiNguoiDung = 'VANGLAI'
  AND CAST(tt.NgayThanhToan AS DATE) = CAST(GETDATE() AS DATE)
  AND nd.Ho LIKE 'Test%'

-- Danh sách khách đang tập
PRINT N''
PRINT N'🏋️ KHÁCH ĐANG TẬP HIỆN TẠI:'
SELECT 
    CONCAT(nd.Ho, ' ', nd.Ten) AS HoTen,
    nd.SoDienThoai,
    dd.ThoiGianCheckIn,
    DATEDIFF(MINUTE, dd.ThoiGianCheckIn, GETDATE()) AS PhutDaTap,
    dd.GhiChu
FROM DiemDanh dd
JOIN NguoiDung nd ON dd.ThanhVienId = nd.NguoiDungId
WHERE nd.LoaiNguoiDung = 'VANGLAI'
  AND dd.ThoiGianCheckOut IS NULL
  AND CAST(dd.ThoiGianCheckIn AS DATE) = CAST(GETDATE() AS DATE)
  AND nd.Ho LIKE 'Test%'

PRINT N''
PRINT N'🎉 HOÀN THÀNH TEST TÍNH NĂNG KHÁCH VÃNG LAI!'
PRINT N'=================================================='
PRINT N''
PRINT N'📝 HƯỚNG DẪN TEST TRÊN WEB:'
PRINT N'1. Chạy ứng dụng: dotnet run'
PRINT N'2. Truy cập: https://localhost:7139/Reception/Station'
PRINT N'3. Đăng nhập với tài khoản Admin'
PRINT N'4. Click tab "🚶 Khách vãng lai"'
PRINT N'5. Test các chức năng:'
PRINT N'   - Đăng ký nhanh'
PRINT N'   - Thanh toán CASH/BANK'
PRINT N'   - Xem danh sách khách đang tập'
PRINT N'   - Check-out khách'
PRINT N'6. Kiểm tra báo cáo: /BaoCao/Revenue'
PRINT N'   - Chọn nguồn "🚶 Khách vãng lai"'
PRINT N'   - Xem thống kê doanh thu'
