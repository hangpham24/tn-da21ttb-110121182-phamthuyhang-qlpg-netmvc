-- Fix ThoiGian field in DiemDanhs table
-- Set ThoiGian = ThoiGianCheckIn for all records where ThoiGian is null or default

UPDATE DiemDanhs 
SET ThoiGian = ThoiGianCheckIn 
WHERE ThoiGian IS NULL 
   OR ThoiGian = '1900-01-01 00:00:00.000'
   OR ThoiGian < '2020-01-01';

-- Verify the update
SELECT 
    DiemDanhId,
    ThanhVienId,
    ThoiGian,
    ThoiGianCheckIn,
    TrangThai
FROM DiemDanhs 
WHERE ThanhVienId = 47
ORDER BY ThoiGian DESC;
