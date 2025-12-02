USE master;
GO

IF EXISTS (SELECT * FROM sys.databases WHERE name = 'PhotoBookingTH')
BEGIN
    ALTER DATABASE PhotoBookingTH SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PhotoBookingTH;
END
GO

CREATE DATABASE PhotoBookingTH;
GO

USE PhotoBookingTH;
GO


CREATE TABLE DiaDiem (
    MaDiaDiem INT IDENTITY(1,1) PRIMARY KEY,
    TenThanhPho NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100)
);
GO

CREATE TABLE NguoiDung (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    MatKhau NVARCHAR(100) NOT NULL,
    HoVaTen NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE,
    SoDienThoai NVARCHAR(20),
    VaiTro NVARCHAR(20) NOT NULL, 
    AnhDaiDien NVARCHAR(MAX),
    AnhBia NVARCHAR(MAX),
    GioiThieu NVARCHAR(MAX),
    MaDiaDiem INT, 
    NgayTao DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (MaDiaDiem) REFERENCES DiaDiem(MaDiaDiem)
);
GO


CREATE TABLE DanhMuc (
    MaDanhMuc INT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL, 
    MoTa NVARCHAR(500),
    AnhDaiDien NVARCHAR(MAX)
);
GO

CREATE TABLE GoiDichVu (
    MaGoi INT IDENTITY(1,1) PRIMARY KEY,
    TenGoi NVARCHAR(200) NOT NULL, 
    GiaTien DECIMAL(18, 2) NOT NULL,
    GiaCoc DECIMAL(18, 2) DEFAULT 0, 
    ThoiLuong INT NOT NULL,
    SoNguoiToiDa INT DEFAULT 1,
    MoTaChiTiet NVARCHAR(MAX),
    SanPhamBanGiao NVARCHAR(MAX),
    AnhDaiDien NVARCHAR(MAX), 
    MaDanhMuc INT NOT NULL,
    MaNhiepAnhGia INT NOT NULL,
    FOREIGN KEY (MaDanhMuc) REFERENCES DanhMuc(MaDanhMuc),
    FOREIGN KEY (MaNhiepAnhGia) REFERENCES NguoiDung(MaNguoiDung)
);
GO


CREATE TABLE DonDatLich (
    MaDon INT IDENTITY(1,1) PRIMARY KEY,
    NgayChup DATETIME2 NOT NULL,
    TongTien DECIMAL(18, 2), 
    TienDaCoc DECIMAL(18, 2),
    GhiChu NVARCHAR(500),
    DiaChiChup NVARCHAR(200),
    TrangThai INT NOT NULL DEFAULT 0, 
    TrangThaiThanhToan INT NOT NULL DEFAULT 0, 
    NgayTao DATETIME2 DEFAULT GETDATE(),
    MaKhachHang INT NOT NULL, 
    MaGoi INT NOT NULL, 
    FOREIGN KEY (MaGoi) REFERENCES GoiDichVu(MaGoi),
    FOREIGN KEY (MaKhachHang) REFERENCES NguoiDung(MaNguoiDung)
);
GO


CREATE TABLE DanhGia (
    MaDanhGia INT IDENTITY(1,1) PRIMARY KEY,
    SoSao INT NOT NULL CHECK (SoSao >= 1 AND SoSao <= 5),
    BinhLuan NVARCHAR(1000),
    PhanHoi NVARCHAR(1000),
    NgayDanhGia DATETIME2 DEFAULT GETDATE(),
    MaDon INT NOT NULL UNIQUE,
    FOREIGN KEY (MaDon) REFERENCES DonDatLich(MaDon)
);
GO


CREATE TABLE AlbumAnh (
    MaAlbum INT IDENTITY(1,1) PRIMARY KEY,
    TieuDe NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(MAX),
    MaNhiepAnhGia INT NOT NULL,
    FOREIGN KEY (MaNhiepAnhGia) REFERENCES NguoiDung(MaNguoiDung)
);
GO


CREATE TABLE AnhChiTiet (
    MaAnh INT IDENTITY(1,1) PRIMARY KEY,
    DuongDanAnh NVARCHAR(MAX) NOT NULL,
    MaAlbum INT NOT NULL,
    FOREIGN KEY (MaAlbum) REFERENCES AlbumAnh(MaAlbum) ON DELETE CASCADE
);
GO

-- A. DIA DIEM
INSERT INTO DiaDiem (TenThanhPho, Slug) VALUES 
(N'Hà Nội', 'ha-noi'), (N'TP. Hồ Chí Minh', 'tp-hcm'), (N'Đà Nẵng', 'da-nang'),
(N'Đà Lạt', 'da-lat'), (N'Nha Trang', 'nha-trang'), (N'An Giang', 'an-giang');
-- B. DANH MUC
INSERT INTO DanhMuc (TenDanhMuc, MoTa, AnhDaiDien) VALUES 
(N'Chụp Ảnh Cưới', N'Lưu giữ khoảnh khắc', 'wedding.jpg'),
(N'Chụp Kỷ Yếu', N'Lưu giữ thanh xuân', 'kyyeu.jpg'),
(N'Chụp Lookbook', N'Hình ảnh thời trang', 'fashion.jpg'),
(N'Chụp Gia Đình', N'Khoảnh khắc ấm áp', 'family.jpg');

-- C. NGUOI DUNG
-- 1. Admin
INSERT INTO NguoiDung (TenDangNhap, MatKhau, HoVaTen, Email, SoDienThoai, VaiTro, MaDiaDiem) VALUES 
('admin', '123456', N'Quản Trị Viên', 'admin@potobooking.com', '0901234567', 'Admin', 1);

-- 2. Nhiep anh gia
INSERT INTO NguoiDung (TenDangNhap, MatKhau, HoVaTen, Email, SoDienThoai, VaiTro, GioiThieu, MaDiaDiem) VALUES 
('lee_minh', '123456', N'Lê Minh Studio', 'leeminh@gmail.com', '0988111222', 'Photographer', N'Chuyên chụp ảnh cưới.', 1),
('sarah_tran', '123456', N'Sarah Trần', 'sarah@gmail.com', '0977888999', 'Photographer', N'Chuyên chụp Lookbook.', 2),
('tung_nui', '123456', N'Tùng Núi Foto', 'tungnui@gmail.com', '0912341234', 'Photographer', N'Chuyên săn mây.', 4);

-- 3. Khách hàng
INSERT INTO NguoiDung (TenDangNhap, MatKhau, HoVaTen, Email, SoDienThoai, VaiTro, MaDiaDiem) VALUES 
('khachhang1', '123456', N'Nguyễn Văn An', 'an.nguyen@gmail.com', '0900000001', 'Customer', 1),
('khachhang2', '123456', N'Trần Thị Bích', 'bich.tran@gmail.com', '0900000002', 'Customer', 2),
('khachhang3', '123456', N'Lê Hoàng Cường', 'cuong.le@gmail.com', '0900000003', 'Customer', 1);

-- D. GÓI DỊCH VỤ (Sử dụng biến để lấy ID động, tránh lỗi khóa ngoại)
DECLARE @IdLeMinh INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'lee_minh');
DECLARE @IdSarah INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'sarah_tran');
DECLARE @IdTungNui INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'tung_nui');

-- Lấy ID Danh mục
DECLARE @IdDMCuoi INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc = N'Chụp Ảnh Cưới');
DECLARE @IdDMLookbook INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc = N'Chụp Lookbook');
DECLARE @IdDMKyYeu INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc = N'Chụp Kỷ Yếu');

INSERT INTO GoiDichVu (TenGoi, GiaTien, GiaCoc, ThoiLuong, SoNguoiToiDa, MoTaChiTiet, SanPhamBanGiao, MaDanhMuc, MaNhiepAnhGia, AnhDaiDien) VALUES 
(N'Cưới Studio Premium', 5000000, 1000000, 180, 2, N'Chụp tại Studio với 3 concept. Đã bao gồm trang điểm và váy cưới.', N'20 ảnh chỉnh sửa, 1 ảnh cổng ép gỗ', @IdDMCuoi, @IdLeMinh, 'https://www.pinterest.com/pin/126593439522022455/'),
(N'Cưới Ngoại Cảnh Phố Cổ', 8000000, 2000000, 300, 2, N'Chụp ngoại cảnh quanh Hồ Gươm và Phố Cổ Hà Nội.', N'Toàn bộ file gốc, 40 ảnh chỉnh sửa', @IdDMCuoi, @IdLeMinh, 'https://www.pinterest.com/pin/126593439522022455/'),
(N'Lookbook Thời Trang', 3000000, 500000, 120, 1, N'Chụp lookbook cho shop quần áo hoặc cá nhân.', N'15 ảnh retouch da và dáng', @IdDMLookbook, @IdSarah, 'https://www.pinterest.com/pin/126593439522022455/'),
(N'Profile Doanh Nhân', 2000000, 500000, 60, 1, N'Chụp ảnh profile chuyên nghiệp tại văn phòng hoặc studio.', N'5 ảnh CV chất lượng cao', @IdDMLookbook, @IdSarah, 'https://www.pinterest.com/pin/126593439522022455/'),
(N'Săn Mây Đà Lạt', 4500000, 1000000, 240, 2, N'Khởi hành từ 4h sáng để săn mây tại đồi chè Cầu Đất.', N'Video ngắn 1 phút + Toàn bộ ảnh gốc', @IdDMKyYeu, @IdTungNui, 'https://www.pinterest.com/pin/126593439522022455/');

-- E. ÐON Ð?T L?CH (S? D?NG BI?N Ð? L?Y ID Ð?NG -> KH?C PH?C L?I 547)
-- L?y ID Khách hàng th?c t?
DECLARE @IdKhach1 INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'khachhang1');
DECLARE @IdKhach2 INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'khachhang2');
DECLARE @IdKhach3 INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'khachhang3');

-- L?y ID Gói th?c t?
DECLARE @IdGoiCuoi INT = (SELECT TOP 1 MaGoi FROM GoiDichVu WHERE TenGoi LIKE N'%Cưới Studio%');
DECLARE @IdGoiLookbook INT = (SELECT TOP 1 MaGoi FROM GoiDichVu WHERE TenGoi LIKE N'%Lookbook%');
DECLARE @IdGoiSanMay INT = (SELECT TOP 1 MaGoi FROM GoiDichVu WHERE TenGoi LIKE N'%Săn Mây%');
DECLARE @IdGoiNgoaiCanh INT = (SELECT TOP 1 MaGoi FROM GoiDichVu WHERE TenGoi LIKE N'%Phố Cổ%');

INSERT INTO DonDatLich (NgayChup, TongTien, TienDaCoc, GhiChu, DiaChiChup, TrangThai, TrangThaiThanhToan, MaKhachHang, MaGoi) VALUES 
('2023-10-20 08:00:00', 5000000, 1000000, N'Muốn chụp phong cách vintage', N'Studio Lê Minh', 2, 2, @IdKhach1, @IdGoiCuoi);

-- 2. Đơn đã cọc
INSERT INTO DonDatLich (NgayChup, TongTien, TienDaCoc, GhiChu, DiaChiChup, TrangThai, TrangThaiThanhToan, MaKhachHang, MaGoi) VALUES 
('2024-12-25 09:00:00', 3000000, 500000, N'Chụp concept giáng sinh', N'Quận 1, TP.HCM', 1, 1, @IdKhach2, @IdGoiLookbook);

-- 3. Đơn mới
INSERT INTO DonDatLich (NgayChup, TongTien, TienDaCoc, GhiChu, DiaChiChup, TrangThai, TrangThaiThanhToan, MaKhachHang, MaGoi) VALUES 
('2024-11-30 04:00:00', 4500000, 0, N'Hy vọng hôm đó có nhiều mây', N'Đồi chè Cầu Đất', 0, 0, @IdKhach3, @IdGoiSanMay);

-- 4. Đơn hủy
INSERT INTO DonDatLich (NgayChup, TongTien, TienDaCoc, GhiChu, DiaChiChup, TrangThai, TrangThaiThanhToan, MaKhachHang, MaGoi) VALUES 
('2023-09-15 14:00:00', 8000000, 0, N'Bận đột xuất', N'Hồ Gươm', 3, 0, @IdKhach1, @IdGoiNgoaiCanh);

-- F. ĐÁNH GIÁ (Lấy ID đơn đã hoàn thành)
DECLARE @IdDonHoanThanh INT = (SELECT TOP 1 MaDon FROM DonDatLich WHERE TrangThai = 2);

INSERT INTO DanhGia (SoSao, BinhLuan, PhanHoi, MaDon) VALUES 
(5, N'Anh Minh chụp rất có tâm!', N'Cảm ơn bạn.', @IdDonHoanThanh);

-- G. ALBUM ẢNH
INSERT INTO AlbumAnh (TieuDe, MoTa, MaNhiepAnhGia) VALUES 
(N'Ảnh Cưới Studio 2023', N'Tổng hợp ảnh cưới', @IdLeMinh),
(N'Street Style Saigon', N'Phong cách đường phố', @IdSarah);
GO

-- ==================================================
-- TRUY VẤN DỮ LIỆU TẤT CẢ CÁC BẢNG
-- ==================================================

PRINT N'--- 1. Bảng Địa Điểm (DiaDiem) ---';
SELECT * FROM DiaDiem;

PRINT N'--- 2. Bảng Người Dùng (NguoiDung) ---';
-- Lưu ý: Cột MatKhau đang lưu dạng thô (plaintext), chưa mã hóa.
SELECT * FROM NguoiDung;

PRINT N'--- 3. Bảng Danh Mục (DanhMuc) ---';
SELECT * FROM DanhMuc;

PRINT N'--- 4. Bảng Gói Dịch Vụ (GoiDichVu) ---';
-- Bảng này liên kết với DanhMuc và NguoiDung (Photographer)
SELECT * FROM GoiDichVu;

PRINT N'--- 5. Bảng Đặt Lịch (DonDatLich) ---';
-- Bảng này liên kết với NguoiDung (KhachHang) và GoiDichVu
-- TrangThai: 0:ChoDuyet, 1:DaDuyet, 2:HoanThanh, 3:Huy
-- TrangThaiThanhToan: 0:ChuaTT, 1:DaCoc, 2:DaTTHet
SELECT * FROM DonDatLich;

PRINT N'--- 6. Bảng Đánh Giá (DanhGia) ---';
-- Bảng này liên kết 1-1 với DonDatLich
SELECT * FROM DanhGia;

PRINT N'--- 7. Bảng Album Ảnh (AlbumAnh) ---';
-- Bảng này liên kết với NguoiDung (Photographer)
SELECT * FROM AlbumAnh;

PRINT N'--- 8. Bảng Chi Tiết Ảnh (AnhChiTiet) ---';
-- Bảng này chứa danh sách ảnh thuộc về các AlbumAnh
SELECT * FROM AnhChiTiet;

PRINT N'Bắt đầu quá trình xóa dữ liệu mẫu...';

-- =======================================================
-- XÓA DỮ LIỆU THEO THỨ TỰ NGƯỢC LẠI (TRÁNH LỖI KHÓA NGOẠI)
-- =======================================================

-- 1. Xóa bảng Đánh Giá (DanhGia) - Phụ thuộc vào DonDatLich
-- (Bảng AnhChiTiet có ON DELETE CASCADE theo AlbumAnh nên có thể không cần xóa riêng, nhưng cứ xóa cho chắc chắn)
DELETE FROM AnhChiTiet;
DBCC CHECKIDENT ('AnhChiTiet', RESEED, 0);

DELETE FROM DanhGia;
DBCC CHECKIDENT ('DanhGia', RESEED, 0);
PRINT N'- Đã xóa DanhGia và AnhChiTiet';


-- 2. Xóa bảng Đơn Đặt Lịch (DonDatLich) - Phụ thuộc vào GoiDichVu, NguoiDung
DELETE FROM DonDatLich;
DBCC CHECKIDENT ('DonDatLich', RESEED, 0);
PRINT N'- Đã xóa DonDatLich';


-- 3. Xóa bảng Album Ảnh (AlbumAnh) - Phụ thuộc vào NguoiDung (Photographer)
DELETE FROM AlbumAnh;
DBCC CHECKIDENT ('AlbumAnh', RESEED, 0);
PRINT N'- Đã xóa AlbumAnh';


-- 4. Xóa bảng Gói Dịch Vụ (GoiDichVu) - Phụ thuộc vào DanhMuc, NguoiDung
DELETE FROM GoiDichVu;
DBCC CHECKIDENT ('GoiDichVu', RESEED, 0);
PRINT N'- Đã xóa GoiDichVu';


-- 5. Xóa bảng Người Dùng (NguoiDung) - Phụ thuộc vào DiaDiem
DELETE FROM NguoiDung;
DBCC CHECKIDENT ('NguoiDung', RESEED, 0);
PRINT N'- Đã xóa NguoiDung';


-- 6. Xóa các bảng độc lập (Danh mục, Địa điểm)
DELETE FROM DanhMuc;
DBCC CHECKIDENT ('DanhMuc', RESEED, 0);

DELETE FROM DiaDiem;
DBCC CHECKIDENT ('DiaDiem', RESEED, 0);
PRINT N'- Đã xóa DanhMuc và DiaDiem';


PRINT N'--------------------------------------------------';
PRINT N'ĐÃ XÓA TOÀN BỘ DỮ LIỆU MẪU THÀNH CÔNG!';
PRINT N'Cơ sở dữ liệu PhotoBookingTH đã trốn rỗng.';
GO

-- Kiểm tra lại (Tất cả sẽ trả về 0 dòng)
SELECT * FROM DonDatLich;
-- SELECT * FROM NguoiDung;
select * from GoiDichVu;