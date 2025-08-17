-- Script to create missing ThanhToan records for existing DangKy records
-- This will fix the revenue report showing 0

USE [HANG_FIX];

PRINT '🔧 Creating missing ThanhToan records for existing DangKy records...';

-- Check current state
PRINT '📊 Current state:';
SELECT 
    'DangKys' as TableName,
    COUNT(*) as RecordCount,
    SUM(ISNULL(PhiDangKy, 0)) as TotalAmount
FROM DangKys
UNION ALL
SELECT 
    'ThanhToans' as TableName,
    COUNT(*) as RecordCount,
    SUM(SoTien) as TotalAmount
FROM ThanhToans;

-- Create ThanhToan records for DangKy records that don't have payments
INSERT INTO ThanhToans (
    DangKyId,
    SoTien,
    NgayThanhToan,
    PhuongThuc,
    TrangThai,
    GhiChu
)
SELECT 
    d.DangKyId,
    ISNULL(d.PhiDangKy, 
        CASE 
            WHEN d.GoiTapId IS NOT NULL THEN ISNULL(g.Gia, 500000)  -- Default package price
            WHEN d.LopHocId IS NOT NULL THEN ISNULL(l.GiaTuyChinh, 200000)  -- Default class price
            ELSE 100000  -- Default walk-in price
        END
    ) as SoTien,
    ISNULL(d.NgayTao, GETDATE()) as NgayThanhToan,
    'CASH' as PhuongThuc,  -- Assume cash payment for existing registrations
    CASE 
        WHEN d.TrangThai = 'ACTIVE' THEN 'SUCCESS'
        WHEN d.TrangThai = 'CANCELED' THEN 'REFUND'
        ELSE 'PENDING'
    END as TrangThai,
    CONCAT(
        'BACKFILL - ',
        CASE 
            WHEN d.GoiTapId IS NOT NULL THEN CONCAT('Gói tập: ', g.TenGoi)
            WHEN d.LopHocId IS NOT NULL THEN CONCAT('Lớp học: ', l.TenLop)
            ELSE 'Đăng ký tại quầy'
        END,
        ' - Tạo tự động từ đăng ký hiện có'
    ) as GhiChu
FROM DangKys d
LEFT JOIN GoiTaps g ON d.GoiTapId = g.GoiTapId
LEFT JOIN LopHocs l ON d.LopHocId = l.LopHocId
LEFT JOIN ThanhToans t ON d.DangKyId = t.DangKyId
WHERE t.ThanhToanId IS NULL  -- Only create for registrations without payments
    AND d.PhiDangKy > 0;  -- Only create for registrations with fees

-- Show results
PRINT '✅ ThanhToan records created successfully!';

PRINT '📊 Updated state:';
SELECT 
    'DangKys' as TableName,
    COUNT(*) as RecordCount,
    SUM(ISNULL(PhiDangKy, 0)) as TotalAmount
FROM DangKys
UNION ALL
SELECT 
    'ThanhToans' as TableName,
    COUNT(*) as RecordCount,
    SUM(SoTien) as TotalAmount
FROM ThanhToans;

-- Show created payments
PRINT '💰 Created payments:';
SELECT 
    t.ThanhToanId,
    t.DangKyId,
    t.SoTien,
    t.NgayThanhToan,
    t.PhuongThuc,
    t.TrangThai,
    LEFT(t.GhiChu, 50) + '...' as GhiChu
FROM ThanhToans t
WHERE t.GhiChu LIKE 'BACKFILL%'
ORDER BY t.ThanhToanId;

PRINT '🎉 Script completed successfully!';
