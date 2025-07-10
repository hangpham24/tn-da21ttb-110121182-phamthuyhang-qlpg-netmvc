-- Tạo bảng quyền
CREATE TABLE Quyen (
    MaQuyen INT PRIMARY KEY IDENTITY,
    TenQuyen NVARCHAR(50) NOT NULL,
    MoTa NVARCHAR(255)
);

-- Thêm dữ liệu mẫu cho bảng quyền
INSERT INTO Quyen (TenQuyen, MoTa)
VALUES 
('Admin', 'Quyền quản trị hệ thống'),
('Huấn luyện viên', 'Quyền dành cho huấn luyện viên'),
('Thành viên', 'Quyền dành cho thành viên tập gym');

-- Tạo bảng tài khoản
CREATE TABLE TaiKhoan (
    MaTK INT PRIMARY KEY IDENTITY,
    TenDangNhap NVARCHAR(50) NOT NULL,
    MatKhau NVARCHAR(255) NOT NULL,
    VaiTro NVARCHAR(50),
    TrangThai NVARCHAR(20),
    MaQuyen INT FOREIGN KEY REFERENCES Quyen(MaQuyen)
);

-- Tạo bảng thành viên
CREATE TABLE ThanhVien (
    MaTV INT PRIMARY KEY IDENTITY,
    HoTen NVARCHAR(100),
    NgaySinh DATE,
    GioiTinh NVARCHAR(10),
    SoDienThoai NVARCHAR(15),
    Email NVARCHAR(100),
    DiaChi NVARCHAR(200),
    NgayDangKy DATE,
    AnhDaiDien VARBINARY(MAX),
    MaTK INT FOREIGN KEY REFERENCES TaiKhoan(MaTK),
    TrangThai NVARCHAR(20)
);

-- Tạo bảng khách vãng lai
CREATE TABLE KhachVangLai (
    MaKVL INT PRIMARY KEY IDENTITY,
    HoTen NVARCHAR(100),
    SoDienThoai NVARCHAR(15),
    Email NVARCHAR(100),
    NgayGhiNhan DATE DEFAULT GETDATE(),
    GiaTien DECIMAL(18,2),
    GhiChu NVARCHAR(500),
    
);

-- Tạo bảng khuyến mãi
CREATE TABLE KhuyenMai (
    MaKM INT PRIMARY KEY IDENTITY,
    TenKM NVARCHAR(100),
    MoTa NVARCHAR(500),
    PhanTramGiam INT,
    NgayBatDau DATE,
    NgayKetThuc DATE,
    TrangThai NVARCHAR(20)
);

-- Tạo bảng gói tập
CREATE TABLE GoiTap (
    MaGoi INT PRIMARY KEY IDENTITY,
    TenGoi NVARCHAR(100),
    MoTa NVARCHAR(500),
    ThoiHanThang INT,
    GiaTien DECIMAL(18,2),
    SoLanTapToiDa INT,
    MaKM INT FOREIGN KEY REFERENCES KhuyenMai(MaKM)
);

-- Tạo bảng lớp học
CREATE TABLE LopHoc (
    MaLop INT PRIMARY KEY IDENTITY,
    TenLop NVARCHAR(100) NOT NULL,
    MaPT INT FOREIGN KEY REFERENCES HuanLuyenVien(MaPT),
    ThoiGianBatDau TIME NOT NULL,
    ThoiGianKetThuc TIME NOT NULL,
    NgayTrongTuan NVARCHAR(20) NOT NULL,
    SoLuongToiDa INT NOT NULL,
    SoLuongHienTai INT DEFAULT 0,
    TrangThai NVARCHAR(20) DEFAULT 'DangMo',
    GhiChu NVARCHAR(500)
);

-- Tạo bảng đăng ký thống nhất
CREATE TABLE DangKy (
    MaDangKy INT PRIMARY KEY IDENTITY(1,1),
    MaThanhVien INT NULL,
    MaKhachVangLai INT NULL,
    MaGoiTap INT NULL, -- Nullable: nếu là đăng ký lớp học thì không cần
    MaLopHoc INT NULL, -- Nullable: nếu là đăng ký gói tập thì không cần
    NgayBatDau DATE NOT NULL,
    NgayKetThuc DATE NOT NULL,
    LoaiDangKy NVARCHAR(20) NOT NULL CHECK (LoaiDangKy IN (N'GoiTap', N'LopHoc')),
    SoBuoi INT,
    TrangThai NVARCHAR(20) DEFAULT N'ConHieuLuc',
    GhiChu NVARCHAR(500),
    FOREIGN KEY (MaThanhVien) REFERENCES ThanhVien(MaTV),
    FOREIGN KEY (MaKhachVangLai) REFERENCES KhachVangLai(MaKVL),
    FOREIGN KEY (MaGoiTap) REFERENCES GoiTap(MaGoi),
    FOREIGN KEY (MaLopHoc) REFERENCES LopHoc(MaLop),
    -- Đảm bảo hoặc là thành viên hoặc là khách vãng lai
    CHECK ((MaThanhVien IS NULL AND MaKhachVangLai IS NOT NULL) OR (MaThanhVien IS NOT NULL AND MaKhachVangLai IS NULL))
);

-- Tạo bảng lịch sử đăng ký (phiên bản mới, liên kết với DangKy)
DROP TABLE IF EXISTS LichSuDangKy;
CREATE TABLE LichSuDangKy (
    MaLichSu INT PRIMARY KEY IDENTITY,
    MaDangKy INT NOT NULL FOREIGN KEY REFERENCES DangKy(MaDangKy), -- Liên kết chính đến bảng Đăng Ký
    ThoiGianSuKien DATETIME DEFAULT GETDATE(), -- Thời điểm xảy ra sự kiện lịch sử
    LoaiSuKien NVARCHAR(100) NOT NULL, -- Ví dụ: 'Tạo mới', 'Gia hạn', 'Hủy', 'Thay đổi trạng thái', 'Cập nhật thông tin'
    MoTaSuKien NVARCHAR(500) NULL, -- Mô tả chi tiết hơn về sự kiện nếu cần
    DuLieuTruoc NVARCHAR(MAX) NULL, -- Lưu trữ dữ liệu (ví dụ: dạng JSON) của bản ghi Đăng Ký TRƯỚC khi thay đổi
    DuLieuSau NVARCHAR(MAX) NULL,   -- Lưu trữ dữ liệu (ví dụ: dạng JSON) của bản ghi Đăng Ký SAU khi thay đổi
    MaTKNguoiThucHien INT FOREIGN KEY REFERENCES TaiKhoan(MaTK) NULL, -- Tài khoản người thực hiện hành động
    GhiChu NVARCHAR(500) -- Ghi chú thêm nếu có
);

-- Tạo bảng huấn luyện viên
CREATE TABLE HuanLuyenVien (
    MaPT INT PRIMARY KEY IDENTITY,
    HoTen NVARCHAR(100),
    NgaySinh DATE,
    GioiTinh NVARCHAR(10),
    SDT NVARCHAR(15),
    Email NVARCHAR(100),
    ChuyenMon NVARCHAR(200),
    AnhDaiDien VARBINARY(MAX),
    TrangThai NVARCHAR(20),
    MaTK INT FOREIGN KEY REFERENCES TaiKhoan(MaTK)
);

-- Tạo bảng lịch sử check-in
CREATE TABLE LichSuCheckIn (
    MaCheckIn INT PRIMARY KEY IDENTITY,
    MaTK INT NULL,
    MaKVL INT NULL,
    ThoiGian DATETIME,
    KetQuaNhanDien BIT,
    AnhNhanDien VARBINARY(MAX),
    FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK),
    FOREIGN KEY (MaKVL) REFERENCES KhachVangLai(MaKVL),
    -- Đảm bảo hoặc là thành viên hoặc là khách vãng lai
    CHECK ((MaThanhVien IS NULL AND MaKhachVangLai IS NOT NULL) OR (MaThanhVien IS NOT NULL AND MaKhachVangLai IS NULL))
);

-- Tạo bảng thông báo
CREATE TABLE ThongBao (
    MaThongBao INT PRIMARY KEY IDENTITY,
    TieuDe NVARCHAR(100),
    NoiDung NVARCHAR(MAX),
    NgayGui DATETIME,
    MaTK INT FOREIGN KEY REFERENCES TaiKhoan(MaTK),
    DaDoc BIT DEFAULT 0
);

-- Tạo bảng báo cáo tài chính
CREATE TABLE BaoCaoTaiChinh (
    MaBaoCao INT PRIMARY KEY IDENTITY,
    Thang INT NOT NULL,
    Nam INT NOT NULL,
    TongDoanhThu DECIMAL(18,2) DEFAULT 0,
    NgayLapBaoCao DATETIME DEFAULT GETDATE(),
    NguoiLap INT FOREIGN KEY REFERENCES TaiKhoan(MaTK),
    TrangThai NVARCHAR(20) DEFAULT 'ChuaDuyet',
    GhiChu NVARCHAR(500)
);

-- Tạo bảng gia hạn đăng ký
CREATE TABLE GiaHanDangKy (
    MaGiaHan INT PRIMARY KEY IDENTITY,
    MaDangKy INT NOT NULL,
    NgayKetThucMoi DATE NOT NULL,
    SoBuoiThem INT NULL,
    SoTien DECIMAL(18,2) NOT NULL,
    NgayGiaHan DATETIME NOT NULL DEFAULT GETDATE(),
    MaTK INT NULL,
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'DaDong',
    GhiChu NVARCHAR(500),
    FOREIGN KEY (MaDangKy) REFERENCES DangKy(MaDangKy),
    FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK)
);

-- Tạo bảng thanh toán (ĐÃ CẬP NHẬT ĐỂ GOM BẢNG DOANHTHU VÀ LÀM RÕ NGƯỜI MUA)
DROP TABLE IF EXISTS ThanhToan;

CREATE TABLE ThanhToan (
    MaThanhToan INT PRIMARY KEY IDENTITY,
    LoaiThanhToan NVARCHAR(50) NOT NULL, -- Đăng ký, Gia hạn, Dịch vụ, Khách vãng lai, v.v.
    MaDangKy INT NULL, -- FK đến DangKy
    MaGiaHan INT NULL, -- FK đến GiaHanDangKy
    MaTK_NguoiDung INT NULL, -- FK đến TaiKhoan (người nộp tiền)
    MaKVL_NguoiDung INT NULL, -- FK đến KhachVangLai (nếu là khách vãng lai)
    SoTien DECIMAL(18,2) NOT NULL,
    PhuongThucThanhToan NVARCHAR(50) NOT NULL DEFAULT N'TienMat',
    NgayThanhToan DATETIME DEFAULT GETDATE(),
    MaTKNguoiThu INT NULL, -- FK đến TaiKhoan (nhân viên thu)
    TrangThai NVARCHAR(20) DEFAULT N'ThanhCong',
    GhiChu NVARCHAR(500),
    MaGiaoDich NVARCHAR(100),
    DonViThanhToan NVARCHAR(100),
    TaiKhoanThanhToan NVARCHAR(100),
    HoaDonDienTuUrl NVARCHAR(255),
    DaXuatHoaDon BIT DEFAULT 0,
    FOREIGN KEY (MaDangKy) REFERENCES DangKy(MaDangKy),
    FOREIGN KEY (MaGiaHan) REFERENCES GiaHanDangKy(MaGiaHan),
    FOREIGN KEY (MaTK_NguoiDung) REFERENCES TaiKhoan(MaTK),
    FOREIGN KEY (MaKVL_NguoiDung) REFERENCES KhachVangLai(MaKVL),
    FOREIGN KEY (MaTKNguoiThu) REFERENCES TaiKhoan(MaTK),
    CONSTRAINT CK_ThanhToan_XacDinhNguoiMua CHECK (
        (CASE WHEN MaDangKy IS NOT NULL THEN 1 ELSE 0 END +
         CASE WHEN MaGiaHan IS NOT NULL THEN 1 ELSE 0 END +
         CASE WHEN MaTK_NguoiDung IS NOT NULL THEN 1 ELSE 0 END +
         CASE WHEN MaKVL_NguoiDung IS NOT NULL THEN 1 ELSE 0 END) <= 1
    )
);

-- Tạo bảng dịch vụ
CREATE TABLE DichVu (
    MaDichVu INT PRIMARY KEY IDENTITY,
    TenDichVu NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(500),
    LoaiDichVu NVARCHAR(20) NOT NULL, -- 'GoiTap' hoặc 'LopHoc'
    GiaBatDau DECIMAL(18,2) NOT NULL,
    HinhAnhURL NVARCHAR(255),
    MaGoiTap INT FOREIGN KEY REFERENCES GoiTap(MaGoi),
    MaLopHoc INT FOREIGN KEY REFERENCES LopHoc(MaLop)
);

-- Xóa bảng PT_GoiTap và PT_LopHoc để thay thế bằng PT_PhanCongHoaHong
DROP TABLE IF EXISTS PT_GoiTap;
DROP TABLE IF EXISTS PT_LopHoc;

-- Tạo bảng PT_PhanCongHoaHong (Hợp nhất từ PT_GoiTap và PT_LopHoc)
CREATE TABLE PT_PhanCongHoaHong (
    MaPhanCong INT PRIMARY KEY IDENTITY,
    MaPT INT NOT NULL FOREIGN KEY REFERENCES HuanLuyenVien(MaPT),
    MaGoiTap INT NULL FOREIGN KEY REFERENCES GoiTap(MaGoi),
    MaLopHoc INT NULL FOREIGN KEY REFERENCES LopHoc(MaLop),
    PhanTramHoaHong DECIMAL(5,2) NOT NULL,
    CONSTRAINT CK_PT_PhanCong_Loai CHECK ((MaGoiTap IS NOT NULL AND MaLopHoc IS NULL) OR (MaGoiTap IS NULL AND MaLopHoc IS NOT NULL))
);

-- Tạo các chỉ mục duy nhất có điều kiện để đảm bảo tính duy nhất như khóa chính ban đầu
-- Điều này đảm bảo một PT chỉ có một mức hoa hồng cho một gói tập cụ thể (nếu MaGoiTap không NULL)
CREATE UNIQUE NONCLUSTERED INDEX UQ_PT_GoiTap_HoaHong ON PT_PhanCongHoaHong(MaPT, MaGoiTap) WHERE MaGoiTap IS NOT NULL;
-- Và một PT chỉ có một mức hoa hồng cho một lớp học cụ thể (nếu MaLopHoc không NULL)
CREATE UNIQUE NONCLUSTERED INDEX UQ_PT_LopHoc_HoaHong ON PT_PhanCongHoaHong(MaPT, MaLopHoc) WHERE MaLopHoc IS NOT NULL;

-- Tạo bảng phiên dạy
CREATE TABLE PhienDay (
    MaPhien INT PRIMARY KEY IDENTITY,
    MaPT INT FOREIGN KEY REFERENCES HuanLuyenVien(MaPT),
    MaLopHoc INT FOREIGN KEY REFERENCES LopHoc(MaLop),
    MaGoiTap INT FOREIGN KEY REFERENCES GoiTap(MaGoi),
    NgayDay DATETIME NOT NULL,
    SoTienPhatSinh DECIMAL(18,2) NOT NULL,
    TrangThai NVARCHAR(20)
);

-- Tạo bảng bảng lương PT
CREATE TABLE BangLuongPT (
    MaLuong INT PRIMARY KEY IDENTITY,
    MaPT INT FOREIGN KEY REFERENCES HuanLuyenVien(MaPT),
    ThangNam DATE NOT NULL,
    TongDoanhThu DECIMAL(18,2) DEFAULT 0,
    TongHoaHong DECIMAL(18,2) DEFAULT 0,
    LuongCoBan DECIMAL(18,2) NOT NULL,
    TongThanhToan DECIMAL(18,2) DEFAULT 0,
    TrangThai NVARCHAR(20) DEFAULT 'ChuaThanhToan'
);

-- Tạo bảng tin tức
CREATE TABLE TinTuc (
    MaTinTuc INT PRIMARY KEY IDENTITY,
    TieuDe NVARCHAR(200) NOT NULL,
    NoiDung NVARCHAR(MAX) NOT NULL,
    HinhAnhURL NVARCHAR(255),
    NgayDang DATETIME DEFAULT GETDATE(),
    NguoiDang INT FOREIGN KEY REFERENCES TaiKhoan(MaTK),
    
);

-- Tạo bảng phiên tập
CREATE TABLE PhienTap (
    MaPhien INT PRIMARY KEY IDENTITY,
    MaThanhVien INT NULL,
    MaKhachVangLai INT NULL,
    MaPT INT FOREIGN KEY REFERENCES HuanLuyenVien(MaPT),
    NgayTap DATETIME NOT NULL,
    GhiChu NVARCHAR(500),
    TinhTrang NVARCHAR(50),
    FOREIGN KEY (MaThanhVien) REFERENCES ThanhVien(MaTV),
    FOREIGN KEY (MaKhachVangLai) REFERENCES KhachVangLai(MaKVL),
    -- Đảm bảo hoặc là thành viên hoặc là khách vãng lai
    CHECK ((MaThanhVien IS NULL AND MaKhachVangLai IS NOT NULL) OR (MaThanhVien IS NOT NULL AND MaKhachVangLai IS NULL))
);

CREATE TABLE CapNhatAnhNhanDien (
    MaCapNhat INT PRIMARY KEY IDENTITY,
    MaTK INT NOT NULL,
    AnhCu VARBINARY(MAX) NULL,
    AnhMoi VARBINARY(MAX) NOT NULL,
    ThoiGianCapNhat DATETIME DEFAULT GETDATE(),
    NguoiCapNhat INT NULL,
    LyDo NVARCHAR(255),
    FOREIGN KEY (MaTK) REFERENCES TaiKhoan(MaTK),
    FOREIGN KEY (NguoiCapNhat) REFERENCES TaiKhoan(MaTK)
);

-- Tạo bảng DoanhThu chi tiết theo từng giao dịch
CREATE TABLE DoanhThu (
    MaDoanhThu INT PRIMARY KEY IDENTITY,
    MaThanhToan INT FOREIGN KEY REFERENCES ThanhToan(MaThanhToan),
    SoTien DECIMAL(18,2) NOT NULL,
    Ngay DATE NOT NULL,
    GhiChu NVARCHAR(500),
    NgayTao DATETIME DEFAULT GETDATE(),
    NguoiTao INT FOREIGN KEY REFERENCES TaiKhoan(MaTK)
);

