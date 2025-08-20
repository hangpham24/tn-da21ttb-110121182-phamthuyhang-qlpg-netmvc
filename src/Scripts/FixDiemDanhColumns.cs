using Microsoft.EntityFrameworkCore;
using GymManagement.Web.Data;

namespace GymManagement.Web.Scripts
{
    public class FixDiemDanhColumns
    {
        public static async Task ExecuteAsync(GymDbContext context)
        {
            var sql = @"
-- Check if columns exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DiemDanhs') AND name = 'DoTinCay')
BEGIN
    ALTER TABLE DiemDanhs ADD DoTinCay FLOAT NULL;
    PRINT 'Added DoTinCay column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DiemDanhs') AND name = 'LoaiCheckIn')
BEGIN
    ALTER TABLE DiemDanhs ADD LoaiCheckIn NVARCHAR(50) NULL;
    PRINT 'Added LoaiCheckIn column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DiemDanhs') AND name = 'ThoiGianCheckOut')
BEGIN
    ALTER TABLE DiemDanhs ADD ThoiGianCheckOut DATETIME2 NULL;
    PRINT 'Added ThoiGianCheckOut column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DiemDanhs') AND name = 'ThoiGianCheckIn')
BEGIN
    ALTER TABLE DiemDanhs ADD ThoiGianCheckIn DATETIME2 NOT NULL DEFAULT GETDATE();
    PRINT 'Added ThoiGianCheckIn column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DiemDanhs') AND name = 'LichLopId')
BEGIN
    ALTER TABLE DiemDanhs ADD LichLopId INT NULL;
    PRINT 'Added LichLopId column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DiemDanhs') AND name = 'TrangThai')
BEGIN
    ALTER TABLE DiemDanhs ADD TrangThai NVARCHAR(20) NOT NULL DEFAULT 'Present';
    PRINT 'Added TrangThai column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DiemDanhs') AND name = 'GhiChu')
BEGIN
    ALTER TABLE DiemDanhs ADD GhiChu NVARCHAR(500) NULL;
    PRINT 'Added GhiChu column';
END

-- Add foreign key constraint for LichLopId if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DiemDanhs_LichLops_LichLopId')
BEGIN
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'LichLops')
    BEGIN
        ALTER TABLE DiemDanhs ADD CONSTRAINT FK_DiemDanhs_LichLops_LichLopId 
        FOREIGN KEY (LichLopId) REFERENCES LichLops(LichLopId);
        PRINT 'Added foreign key constraint for LichLopId';
    END
END

-- Update existing records to have default values
UPDATE DiemDanhs 
SET LoaiCheckIn = 'Manual' 
WHERE LoaiCheckIn IS NULL;

UPDATE DiemDanhs 
SET ThoiGianCheckIn = ThoiGian 
WHERE ThoiGianCheckIn IS NULL OR ThoiGianCheckIn = '1900-01-01';
";

            await context.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
