/*====================================================
  QuanLyGym v6 – 19 bảng, index & ràng buộc mở rộng
====================================================*/
IF DB_ID('QLPG') IS NULL
    CREATE DATABASE QLPG;
GO
USE QLPG;
GO
/********* 1. Bảo mật ****************************************/
CREATE TABLE VaiTro(
    VaiTroId  INT IDENTITY PRIMARY KEY,
    TenVaiTro NVARCHAR(50) UNIQUE NOT NULL,
    MoTa      NVARCHAR(200)
);
GO

CREATE TABLE NguoiDung(
    NguoiDungId   INT IDENTITY PRIMARY KEY,
    LoaiNguoiDung NVARCHAR(20) CHECK (LoaiNguoiDung IN ('THANHVIEN','HLV','ADMIN','VANGLAI')) NOT NULL,
    Ho            NVARCHAR(50) NOT NULL,
    Ten           NVARCHAR(50),
    GioiTinh      NVARCHAR(10),
    NgaySinh      DATE,
    SoDienThoai   NVARCHAR(15),
    Email         NVARCHAR(100),
    NgayThamGia   DATE DEFAULT GETDATE(),
    TrangThai     NVARCHAR(20) DEFAULT 'ACTIVE'
);
GO

CREATE TABLE TaiKhoan(
    TaiKhoanId  INT IDENTITY PRIMARY KEY,
    TenDangNhap NVARCHAR(50) UNIQUE NOT NULL,
    MatKhauHash NVARCHAR(255) NOT NULL,
    VaiTroId    INT REFERENCES VaiTro(VaiTroId),
    NguoiDungId INT REFERENCES NguoiDung(NguoiDungId) ON DELETE CASCADE,
    KichHoat    BIT DEFAULT 1
);
GO
/********* 2. Sản phẩm – khuyến mãi **************************/
CREATE TABLE GoiTap(
    GoiTapId       INT IDENTITY PRIMARY KEY,
    TenGoi         NVARCHAR(100) NOT NULL,
    ThoiHanThang   INT NOT NULL,
    SoBuoiToiDa    INT,
    Gia            DECIMAL(12,2) NOT NULL,
    MoTa           NVARCHAR(500)
);
GO

CREATE TABLE LopHoc(
    LopHocId     INT IDENTITY PRIMARY KEY,
    TenLop       NVARCHAR(100) NOT NULL,
    HlvId        INT REFERENCES NguoiDung(NguoiDungId),
    SucChua      INT NOT NULL,
    GioBatDau    TIME NOT NULL,
    GioKetThuc   TIME NOT NULL,
    ThuTrongTuan NVARCHAR(50) NOT NULL,
    GiaTuyChinh  DECIMAL(12,2),
    TrangThai    NVARCHAR(20) DEFAULT 'OPEN'
);
GO

/* Bảng liệt kê từng buổi lớp (nếu lớp lặp theo tuần) */
CREATE TABLE LichLop(
    LichLopId    INT IDENTITY PRIMARY KEY,
    LopHocId     INT REFERENCES LopHoc(LopHocId) ON DELETE CASCADE,
    Ngay         DATE NOT NULL,
    GioBatDau    TIME NOT NULL,
    GioKetThuc   TIME NOT NULL,
    TrangThai    NVARCHAR(20) DEFAULT 'SCHEDULED'   -- OPEN/CANCELED/FINISHED
);
GO

CREATE TABLE KhuyenMai(
    KhuyenMaiId  INT IDENTITY PRIMARY KEY,
    MaCode       NVARCHAR(50) UNIQUE NOT NULL,
    MoTa         NVARCHAR(300),
    PhanTramGiam INT CHECK (PhanTramGiam BETWEEN 0 AND 100),
    NgayBatDau   DATE NOT NULL,
    NgayKetThuc  DATE NOT NULL,
    KichHoat     BIT DEFAULT 1
);
GO
/********* 3. Đăng ký – Thanh toán ***************************/
CREATE TABLE DangKy(
    DangKyId    INT IDENTITY PRIMARY KEY,
    NguoiDungId INT REFERENCES NguoiDung(NguoiDungId) ON DELETE CASCADE,
    GoiTapId    INT NULL REFERENCES GoiTap(GoiTapId),
    LopHocId    INT NULL REFERENCES LopHoc(LopHocId),
    NgayBatDau  DATE NOT NULL,
    NgayKetThuc DATE NOT NULL,
    TrangThai   NVARCHAR(20) DEFAULT 'ACTIVE',
    NgayTao     DATETIME2 DEFAULT SYSDATETIME(),
    CHECK (NgayKetThuc >= NgayBatDau)              -- Ràng buộc nghiệp vụ
);
GO

CREATE TABLE ThanhToan(
    ThanhToanId   INT IDENTITY PRIMARY KEY,
    DangKyId      INT REFERENCES DangKy(DangKyId) ON DELETE CASCADE,
    SoTien        DECIMAL(12,2) NOT NULL,
    NgayThanhToan DATETIME2 DEFAULT SYSDATETIME(),
    PhuongThuc    NVARCHAR(20) CHECK (PhuongThuc IN ('CASH','CARD','BANK','WALLET','VNPAY')),
    TrangThai     NVARCHAR(20) DEFAULT 'PENDING',  -- PENDING/SUCCESS/FAILED/REFUND
    GhiChu        NVARCHAR(200),
    CHECK (SoTien >= 0)                            -- Không cho âm tiền
);
GO

CREATE TABLE ThanhToanGateway(
    GatewayId        INT IDENTITY PRIMARY KEY,
    ThanhToanId      INT UNIQUE REFERENCES ThanhToan(ThanhToanId) ON DELETE CASCADE,
    GatewayTen       NVARCHAR(30) DEFAULT 'VNPAY',
    GatewayTransId   NVARCHAR(100),
    GatewayOrderId   NVARCHAR(100),
    GatewayAmount    DECIMAL(12,2),
    GatewayRespCode  NVARCHAR(10),
    GatewayMessage   NVARCHAR(255),
    ThoiGianCallback DATETIME2
);
GO
/********* 4. Đặt chỗ online (tuỳ chọn) **********************/
CREATE TABLE Booking(
    BookingId    INT IDENTITY PRIMARY KEY,
    ThanhVienId  INT REFERENCES NguoiDung(NguoiDungId),
    LopHocId     INT NULL REFERENCES LopHoc(LopHocId),
    LichLopId    INT NULL REFERENCES LichLop(LichLopId),
    Ngay         DATE NOT NULL,
    TrangThai    NVARCHAR(20) DEFAULT 'BOOKED'      -- BOOKED/CANCELED/ATTENDED
);
GO
/********* 5. Hoạt động – Check-in ***************************/
CREATE TABLE BuoiHlv(
    BuoiHlvId        INT IDENTITY PRIMARY KEY,
    HlvId            INT REFERENCES NguoiDung(NguoiDungId),
    ThanhVienId      INT REFERENCES NguoiDung(NguoiDungId),
    LopHocId         INT NULL REFERENCES LopHoc(LopHocId),
    NgayTap          DATE NOT NULL,
    ThoiLuongPhut    INT NOT NULL,
    GhiChu           NVARCHAR(300)
);
GO

CREATE TABLE BuoiTap(
    BuoiTapId        INT IDENTITY PRIMARY KEY,
    ThanhVienId      INT REFERENCES NguoiDung(NguoiDungId),
    LopHocId         INT NULL REFERENCES LopHoc(LopHocId),
    ThoiGianVao      DATETIME2 NOT NULL,
    ThoiGianRa       DATETIME2,
    GhiChu           NVARCHAR(200)
);
GO

CREATE TABLE MauMat(
    MauMatId     INT IDENTITY PRIMARY KEY,
    NguoiDungId  INT UNIQUE REFERENCES NguoiDung(NguoiDungId) ON DELETE CASCADE,
    Embedding    VARBINARY(MAX) NOT NULL,
    NgayTao      DATETIME2 DEFAULT SYSDATETIME(),
    ThuatToan    NVARCHAR(50) DEFAULT 'ArcFace'
);
GO

CREATE TABLE DiemDanh(
    DiemDanhId       INT IDENTITY PRIMARY KEY,
    ThanhVienId      INT REFERENCES NguoiDung(NguoiDungId),
    ThoiGian         DATETIME2 DEFAULT SYSDATETIME(),
    KetQuaNhanDang   BIT,
    AnhMinhChung     NVARCHAR(255)
);
GO
/********* 6. Lương & Hoa hồng *******************************/
CREATE TABLE CauHinhHoaHong(
    CauHinhHoaHongId INT IDENTITY PRIMARY KEY,
    GoiTapId         INT NULL REFERENCES GoiTap(GoiTapId),
    PhanTramHoaHong  INT CHECK (PhanTramHoaHong BETWEEN 0 AND 100) NOT NULL
);
GO

CREATE TABLE BangLuong(
    BangLuongId   INT IDENTITY PRIMARY KEY,
    HlvId         INT REFERENCES NguoiDung(NguoiDungId),
    Thang         CHAR(7) NOT NULL, -- 'YYYY-MM'
    LuongCoBan    DECIMAL(12,2) NOT NULL,
    TienHoaHong   DECIMAL(12,2) DEFAULT 0,
    TongThanhToan AS CAST(LuongCoBan + TienHoaHong AS DECIMAL(12,2)) PERSISTED,
    NgayThanhToan DATE
);
GO
/********* 7. Hệ thống ****************************************/
CREATE TABLE ThongBao(
    ThongBaoId   INT IDENTITY PRIMARY KEY,
    TieuDe       NVARCHAR(100),
    NoiDung      NVARCHAR(1000),
    NgayTao      DATETIME2 DEFAULT SYSDATETIME(),
    NguoiDungId  INT NULL REFERENCES NguoiDung(NguoiDungId),
    Kenh         NVARCHAR(10) CHECK (Kenh IN ('EMAIL','SMS','APP')),
    DaDoc        BIT DEFAULT 0
);
GO

CREATE TABLE LichSuAnh(
    LichSuAnhId INT IDENTITY PRIMARY KEY,
    NguoiDungId INT REFERENCES NguoiDung(NguoiDungId),
    AnhCu       NVARCHAR(255),
    AnhMoi      NVARCHAR(255),
    NgayCapNhat DATETIME2 DEFAULT SYSDATETIME(),
    LyDo        NVARCHAR(200)
);
GO
/********* 8. Chỉ mục hiệu năng ******************************/
CREATE UNIQUE INDEX UX_MauMat_NguoiDung        ON MauMat(NguoiDungId);
CREATE INDEX IX_BuoiTap_ThanhVien_Ngay         ON BuoiTap(ThanhVienId, ThoiGianVao);
CREATE INDEX IX_DiemDanh_ThanhVien_Ngay        ON DiemDanh(ThanhVienId, ThoiGian);
CREATE INDEX IX_DangKy_TrangThai               ON DangKy(TrangThai);
CREATE INDEX IX_ThanhToan_DangKy               ON ThanhToan(DangKyId);
CREATE INDEX IX_ThanhToan_TrangThai            ON ThanhToan(TrangThai);
CREATE INDEX IX_BangLuong_Thang                ON BangLuong(HlvId, Thang);
GO
/********* 9. Trigger tự động hết hạn ************************/
CREATE OR ALTER TRIGGER TRG_DangKy_AutoExpire
ON DangKy
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dk
      SET TrangThai = 'EXPIRED'
    FROM DangKy dk
    WHERE dk.TrangThai = 'ACTIVE'
      AND dk.NgayKetThuc < CAST(GETDATE() AS DATE);
END;
GO
