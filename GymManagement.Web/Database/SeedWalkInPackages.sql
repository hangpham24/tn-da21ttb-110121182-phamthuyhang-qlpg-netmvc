-- =============================================
-- Seed Data: Gói vé đặc biệt cho khách vãng lai
-- =============================================

USE [GymManagement]
GO

-- Kiểm tra và thêm gói vé ngày nếu chưa có
IF NOT EXISTS (SELECT 1 FROM GoiTap WHERE TenGoi = N'Vé ngày')
BEGIN
    INSERT INTO GoiTap (TenGoi, ThoiHanThang, Gia, MoTa)
    VALUES (N'Vé ngày', 0, 50000, N'Vé tập 1 ngày cho khách vãng lai - không giới hạn thời gian trong ngày')
    PRINT N'✅ Đã thêm gói "Vé ngày"'
END
ELSE
BEGIN
    PRINT N'ℹ️ Gói "Vé ngày" đã tồn tại'
END

-- Kiểm tra và thêm gói vé 3 giờ nếu chưa có
IF NOT EXISTS (SELECT 1 FROM GoiTap WHERE TenGoi = N'Vé 3 giờ')
BEGIN
    INSERT INTO GoiTap (TenGoi, ThoiHanThang, Gia, MoTa)
    VALUES (N'Vé 3 giờ', 0, 30000, N'Vé tập 3 tiếng cho khách vãng lai - giới hạn 3 giờ kể từ check-in')
    PRINT N'✅ Đã thêm gói "Vé 3 giờ"'
END
ELSE
BEGIN
    PRINT N'ℹ️ Gói "Vé 3 giờ" đã tồn tại'
END

-- Kiểm tra và thêm gói vé buổi sáng nếu chưa có
IF NOT EXISTS (SELECT 1 FROM GoiTap WHERE TenGoi = N'Vé buổi sáng')
BEGIN
    INSERT INTO GoiTap (TenGoi, ThoiHanThang, Gia, MoTa)
    VALUES (N'Vé buổi sáng', 0, 35000, N'Vé tập buổi sáng cho khách vãng lai - từ 6:00 đến 12:00')
    PRINT N'✅ Đã thêm gói "Vé buổi sáng"'
END
ELSE
BEGIN
    PRINT N'ℹ️ Gói "Vé buổi sáng" đã tồn tại'
END

-- Kiểm tra và thêm gói vé buổi chiều nếu chưa có
IF NOT EXISTS (SELECT 1 FROM GoiTap WHERE TenGoi = N'Vé buổi chiều')
BEGIN
    INSERT INTO GoiTap (TenGoi, ThoiHanThang, Gia, MoTa)
    VALUES (N'Vé buổi chiều', 0, 40000, N'Vé tập buổi chiều cho khách vãng lai - từ 12:00 đến 18:00')
    PRINT N'✅ Đã thêm gói "Vé buổi chiều"'
END
ELSE
BEGIN
    PRINT N'ℹ️ Gói "Vé buổi chiều" đã tồn tại'
END

-- Kiểm tra và thêm gói vé buổi tối nếu chưa có
IF NOT EXISTS (SELECT 1 FROM GoiTap WHERE TenGoi = N'Vé buổi tối')
BEGIN
    INSERT INTO GoiTap (TenGoi, ThoiHanThang, Gia, MoTa)
    VALUES (N'Vé buổi tối', 0, 45000, N'Vé tập buổi tối cho khách vãng lai - từ 18:00 đến 22:00')
    PRINT N'✅ Đã thêm gói "Vé buổi tối"'
END
ELSE
BEGIN
    PRINT N'ℹ️ Gói "Vé buổi tối" đã tồn tại'
END

-- Hiển thị tất cả gói vé đã tạo
PRINT N''
PRINT N'📋 DANH SÁCH GÓI VÉ KHÁCH VÃNG LAI:'
SELECT 
    GoiTapId,
    TenGoi,
    FORMAT(Gia, 'N0') + N' VNĐ' AS Gia,
    MoTa
FROM GoiTap 
WHERE ThoiHanThang = 0
ORDER BY Gia

PRINT N''
PRINT N'🎉 Hoàn thành seed data gói vé khách vãng lai!'
