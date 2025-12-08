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
	SoNamKinhNghiem INT DEFAULT 0,
    NgayTao DATETIME2 DEFAULT GETDATE(),
	MaXacNhan NVARCHAR(50) NULL,
	HanMaXacNhan DATETIME2 NULL,
	TinhThanh NVARCHAR(100)
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

-- Tạo bảng Yêu Cầu (Job Requests)
CREATE TABLE YeuCau (
    MaYeuCau INT IDENTITY(1,1) PRIMARY KEY,
    MaKhachHang INT NOT NULL,  
    TieuDe NVARCHAR(200) NOT NULL,      -- VD: Cần thợ chụp sinh nhật bé
    MoTa NVARCHAR(MAX),                 -- VD: Yêu cầu thợ nhiệt tình, trả ảnh nhanh...
    DiaChi NVARCHAR(200),               -- VD: Quận 1, TP.HCM
    NganSach DECIMAL(18, 2),            -- VD: 2.000.000
    NgayCanChup DATETIME2,              -- Khách muốn chụp ngày nào
    -- Trạng thái tin đăng
    -- 0: Đang tìm (Mở), 1: Đã chốt thợ (Đóng), 2: Đã hủy
    TrangThai INT DEFAULT 0,
    NgayTao DATETIME2 DEFAULT GETDATE(),
    
    FOREIGN KEY (MaKhachHang) REFERENCES NguoiDung(MaNguoiDung) ON DELETE CASCADE
);
GO

CREATE TABLE UngTuyen (
    MaUngTuyen INT IDENTITY(1,1) PRIMARY KEY,
    MaYeuCau INT NOT NULL,
    MaNhiepAnhGia INT NOT NULL,
    
    GiaBao DECIMAL(18, 2) NOT NULL, -- Giá thợ muốn nhận
    LoiNhan NVARCHAR(MAX),          -- Lời chào hàng
    
    TrangThai INT DEFAULT 0,        -- 0: Chờ duyệt, 1: Được chọn, 2: Bị từ chối
    NgayUngTuyen DATETIME2 DEFAULT GETDATE(),
    
    FOREIGN KEY (MaYeuCau) REFERENCES YeuCau(MaYeuCau) ON DELETE CASCADE, -- Xóa tin thì xóa luôn ứng tuyển
    FOREIGN KEY (MaNhiepAnhGia) REFERENCES NguoiDung(MaNguoiDung)
);
GO

CREATE TABLE TinNhan (
    MaTinNhan INT IDENTITY(1,1) PRIMARY KEY,
    NguoiGuiId INT NOT NULL,
    NguoiNhanId INT NOT NULL,
    NoiDung NVARCHAR(MAX) NOT NULL,
    ThoiGianGui DATETIME2 DEFAULT GETDATE(),
    DaXem BIT DEFAULT 0,
    
    -- Tạo liên kết khóa ngoại với bảng NguoiDung
    CONSTRAINT FK_TinNhan_NguoiGui FOREIGN KEY (NguoiGuiId) REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT FK_TinNhan_NguoiNhan FOREIGN KEY (NguoiNhanId) REFERENCES NguoiDung(MaNguoiDung)
);
GO

-- Cập nhật dữ liệu mẫu cho có số liệu đẹp
UPDATE NguoiDung SET SoNamKinhNghiem = 5 WHERE TenDangNhap = 'lee_minh';
UPDATE NguoiDung SET SoNamKinhNghiem = 3 WHERE TenDangNhap = 'sarah_tran';
UPDATE NguoiDung SET SoNamKinhNghiem = 2 WHERE TenDangNhap = 'tung_nui';

-- 1. Thêm cột MaNhiepAnhGia vào bảng DonDatLich (Để biết đặt ai nếu không chọn gói)
ALTER TABLE DonDatLich
ADD MaNhiepAnhGia INT;
GO

-- 2. Cập nhật dữ liệu cũ: Lấy MaNhiepAnhGia từ bảng GoiDichVu đổ sang
-- (Bước này quan trọng để không bị mất dữ liệu liên kết cũ)
UPDATE d
SET d.MaNhiepAnhGia = g.MaNhiepAnhGia
FROM DonDatLich d
JOIN GoiDichVu g ON d.MaGoi = g.MaGoi;
GO

-- 3. Tạo khóa ngoại cho cột MaNhiepAnhGia mới
ALTER TABLE DonDatLich
ADD CONSTRAINT FK_DonDatLich_NguoiDung_Photographer
FOREIGN KEY (MaNhiepAnhGia) REFERENCES NguoiDung(MaNguoiDung);
GO

-- 4. Cho phép cột MaGoi được phép NULL (Vì đặt trực tiếp thì không có gói)
ALTER TABLE DonDatLich
ALTER COLUMN MaGoi INT NULL;
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
USE PhotoBookingTH;
GO

-- =======================================================
-- 1. THÊM DANH MỤC MỚI (CATEGORIES)
-- =======================================================
INSERT INTO DanhMuc (TenDanhMuc, MoTa, AnhDaiDien) VALUES 
(N'Chụp Đồ Ăn (Food)', N'Hình ảnh quảng cáo món ăn, menu nhà hàng chuyên nghiệp', 'https://images.unsplash.com/photo-1476224203421-9ac39bcb3327?auto=format&fit=crop&w=800&q=80'),
(N'Chụp Sự Kiện (Event)', N'Ghi lại khoảnh khắc hội nghị, tiệc tùng, khai trương', 'https://images.unsplash.com/photo-1511578314322-379afb476865?auto=format&fit=crop&w=800&q=80'),
(N'Chụp Kiến Trúc & Nội Thất', N'Quảng bá không gian sống, khách sạn, resort', 'https://images.unsplash.com/photo-1618221195710-dd6b41faaea6?auto=format&fit=crop&w=800&q=80'),
(N'Chụp Thú Cưng (Pet)', N'Lưu giữ khoảnh khắc đáng yêu của boss và sen', 'https://images.unsplash.com/photo-1583511655857-d19b40a7a54e?auto=format&fit=crop&w=800&q=80');
GO

-- =======================================================
-- 2. THÊM NHIẾP ẢNH GIA MỚI (PHOTOGRAPHERS)
-- =======================================================

-- Lấy ID Địa điểm (Để gán cho user)
DECLARE @HN INT = (SELECT TOP 1 MaDiaDiem FROM DiaDiem WHERE Slug = 'ha-noi');
DECLARE @HCM INT = (SELECT TOP 1 MaDiaDiem FROM DiaDiem WHERE Slug = 'tp-hcm');
DECLARE @DN INT = (SELECT TOP 1 MaDiaDiem FROM DiaDiem WHERE Slug = 'da-nang');

-- Hash mật khẩu "123456" (Dùng chung cho nhanh)
DECLARE @Pass NVARCHAR(MAX) = '$2a$11$Z9/bXX/y.X/y.X/y.X/y.e1.1.1.1.1.1.1.1.1.1.1.1.1'; 

INSERT INTO NguoiDung (TenDangNhap, MatKhau, HoVaTen, Email, SoDienThoai, VaiTro, MaDiaDiem, SoNamKinhNghiem, GioiThieu, AnhDaiDien) VALUES
-- Chuyên Food - HCM
('foodie_tuan', @Pass, N'Tuấn Foodie', 'tuan.food@gmail.com', '0909111222', 'Photographer', @HCM, 4, N'Chuyên gia Food Stylist & Photography. Đã hợp tác với Golden Gate, The Coffee House.', 'https://ui-avatars.com/api/?name=Tuan+Foodie&background=FF9800&color=fff'),

-- Chuyên Sự kiện - Hà Nội
('event_pro_hn', @Pass, N'Hà Nội Event Media', 'hnevent@gmail.com', '0909333444', 'Photographer', @HN, 8, N'Đội ngũ quay chụp sự kiện chuyên nghiệp. Thiết bị hiện đại 4K, Flycam.', 'https://ui-avatars.com/api/?name=Event+HN&background=2196F3&color=fff'),

-- Chuyên Nội thất - Đà Nẵng
('arch_design_dn', @Pass, N'Không Gian Việt', 'kgv.dn@gmail.com', '0909555666', 'Photographer', @DN, 5, N'Chuyên chụp ảnh căn hộ Airbnb, Resort, Villa tại Đà Nẵng - Hội An.', 'https://ui-avatars.com/api/?name=Khong+Gian&background=607D8B&color=fff'),

-- Chuyên Thú cưng - HCM
('pet_lover_saigon', @Pass, N'Pet Studio Sài Gòn', 'pet.sg@gmail.com', '0909777888', 'Photographer', @HCM, 3, N'Studio thân thiện với thú cưng. Có sẵn phụ kiện và đồ chơi cho các bé.', 'https://ui-avatars.com/api/?name=Pet+Studio&background=E91E63&color=fff'),

-- Chuyên Chân dung - Hà Nội
('portrait_art_hn', @Pass, N'Thanh Xuân Portrait', 'thanhxuan@gmail.com', '0909999000', 'Photographer', @HN, 2, N'Lưu giữ thanh xuân qua những bức ảnh chân dung đầy cảm xúc.', 'https://ui-avatars.com/api/?name=Thanh+Xuan&background=9C27B0&color=fff'),

-- Chuyên Cưới - Đà Nẵng
('wedding_danang', @Pass, N'Nắng Wedding', 'nang.wd@gmail.com', '0909123123', 'Photographer', @DN, 6, N'Chụp ảnh cưới phong cách tự nhiên, bắt trọn khoảnh khắc hạnh phúc.', 'https://ui-avatars.com/api/?name=Nang+Wedding&background=FF5722&color=fff');
GO

USE PhotoBookingTH;
GO

-- =======================================================
-- KHAI BÁO BIẾN (BẮT BUỘC PHẢI CHẠY CÙNG LÚC VỚI LỆNH INSERT DƯỚI)
-- =======================================================

-- 1. Lấy ID Nhiếp ảnh gia
DECLARE @IdFoodie INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'foodie_tuan');
DECLARE @IdEventHN INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'event_pro_hn');
DECLARE @IdArchDN INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'arch_design_dn');
DECLARE @IdPetSG INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'pet_lover_saigon');
DECLARE @IdPortraitHN INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'portrait_art_hn');
DECLARE @IdWeddingDN INT = (SELECT MaNguoiDung FROM NguoiDung WHERE TenDangNhap = 'wedding_danang');

-- 2. Lấy ID Danh mục (Chắc chắn danh mục đã được tạo trước đó)
DECLARE @CatFood INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc LIKE N'%Đồ Ăn%');
DECLARE @CatEvent INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc LIKE N'%Sự Kiện%');
DECLARE @CatArch INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc LIKE N'%Kiến Trúc%');
DECLARE @CatPet INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc LIKE N'%Thú Cưng%');
DECLARE @CatLookbook INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc LIKE N'%Lookbook%');
DECLARE @CatWedding INT = (SELECT MaDanhMuc FROM DanhMuc WHERE TenDanhMuc LIKE N'%Cưới%');

-- =======================================================
-- THỰC HIỆN INSERT (Chạy liền mạch với phần khai báo trên)
-- =======================================================

-- 1. Gói Đồ Ăn (Foodie Tuấn)
IF @IdFoodie IS NOT NULL AND @CatFood IS NOT NULL
BEGIN
    INSERT INTO GoiDichVu (TenGoi, GiaTien, GiaCoc, ThoiLuong, SoNguoiToiDa, MoTaChiTiet, SanPhamBanGiao, MaDanhMuc, MaNhiepAnhGia, AnhDaiDien) VALUES 
    (N'Chụp Menu Nhà Hàng (Cơ bản)', 3000000, 1000000, 180, 1, N'Chụp món ăn trên nền trắng hoặc setup đơn giản. Tối đa 10 món.', N'10 ảnh retouch kỹ, toàn bộ file gốc', @CatFood, @IdFoodie, 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80'),
    (N'Concept Food Art Creative', 5000000, 2000000, 240, 1, N'Setup concept nghệ thuật, có Food Stylist hỗ trợ.', N'15 ảnh chất lượng cao dùng chạy quảng cáo', @CatFood, @IdFoodie, 'https://images.unsplash.com/photo-1476224203421-9ac39bcb3327?auto=format&fit=crop&w=800&q=80');
END

-- 2. Gói Sự Kiện (Event HN)
IF @IdEventHN IS NOT NULL AND @CatEvent IS NOT NULL
BEGIN
    INSERT INTO GoiDichVu (TenGoi, GiaTien, GiaCoc, ThoiLuong, SoNguoiToiDa, MoTaChiTiet, SanPhamBanGiao, MaDanhMuc, MaNhiepAnhGia, AnhDaiDien) VALUES 
    (N'Chụp Hội Thảo/Workshop (Nửa ngày)', 2500000, 500000, 240, 100, N'Chụp toàn cảnh, diễn giả, khách mời check-in.', N'200+ file đã chỉnh sáng, trả ảnh sau 24h', @CatEvent, @IdEventHN, 'https://images.unsplash.com/photo-1515187029135-18ee286d815b?auto=format&fit=crop&w=800&q=80'),
    (N'Quay Phim Highlight Sự Kiện', 4500000, 1500000, 240, 100, N'Quay và dựng video highlight 3-5 phút.', N'01 Video Full HD', @CatEvent, @IdEventHN, 'https://images.unsplash.com/photo-1492684223066-81342ee5ff30?auto=format&fit=crop&w=800&q=80');
END

-- 3. Gói Nội Thất (Arch DN)
IF @IdArchDN IS NOT NULL AND @CatArch IS NOT NULL
BEGIN
    INSERT INTO GoiDichVu (TenGoi, GiaTien, GiaCoc, ThoiLuong, SoNguoiToiDa, MoTaChiTiet, SanPhamBanGiao, MaDanhMuc, MaNhiepAnhGia, AnhDaiDien) VALUES 
    (N'Chụp Căn Hộ Airbnb/Homestay', 2000000, 500000, 120, 1, N'Chụp góc rộng, chi tiết decor để đăng bán phòng.', N'20 ảnh HDR chất lượng cao', @CatArch, @IdArchDN, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=800&q=80'),
    (N'Chụp Biệt Thự/Resort Cao Cấp', 8000000, 3000000, 480, 1, N'Chụp kiến trúc ngoại thất và nội thất. Sử dụng ánh sáng nhân tạo.', N'30 ảnh retouch cao cấp', @CatArch, @IdArchDN, 'https://images.unsplash.com/photo-1600596542815-22b845069566?auto=format&fit=crop&w=800&q=80');
END

-- 4. Gói Thú Cưng (Pet SG)
IF @IdPetSG IS NOT NULL AND @CatPet IS NOT NULL
BEGIN
    INSERT INTO GoiDichVu (TenGoi, GiaTien, GiaCoc, ThoiLuong, SoNguoiToiDa, MoTaChiTiet, SanPhamBanGiao, MaDanhMuc, MaNhiepAnhGia, AnhDaiDien) VALUES 
    (N'Chụp Boss tại Studio', 1200000, 300000, 90, 1, N'Phông nền màu sắc, có sẵn phụ kiện nón, kính cho bé.', N'10 ảnh chỉnh sửa dễ thương', @CatPet, @IdPetSG, 'https://images.unsplash.com/photo-1548199973-03cce0bbc87b?auto=format&fit=crop&w=800&q=80'),
    (N'Dã Ngoại Công Viên Cùng Boss', 1800000, 500000, 120, 2, N'Chụp khoảnh khắc bé chạy nhảy tự nhiên ngoài trời.', N'Toàn bộ file gốc + 15 ảnh chỉnh sửa', @CatPet, @IdPetSG, 'https://images.unsplash.com/photo-1587300003388-59208cc962cb?auto=format&fit=crop&w=800&q=80');
END

-- 5. Gói Chân Dung (Portrait HN)
IF @IdPortraitHN IS NOT NULL AND @CatLookbook IS NOT NULL
BEGIN
    INSERT INTO GoiDichVu (TenGoi, GiaTien, GiaCoc, ThoiLuong, SoNguoiToiDa, MoTaChiTiet, SanPhamBanGiao, MaDanhMuc, MaNhiepAnhGia, AnhDaiDien) VALUES 
    (N'Nàng Thơ Hà Nội', 1500000, 0, 90, 1, N'Chụp áo dài trắng tinh khôi tại Phan Đình Phùng hoặc Hồ Tây.', N'15 ảnh blend màu film', @CatLookbook, @IdPortraitHN, 'https://images.unsplash.com/photo-1529626455594-4ff0802cfb7e?auto=format&fit=crop&w=800&q=80');
END

-- 6. Gói Cưới (Wedding DN)
IF @IdWeddingDN IS NOT NULL AND @CatWedding IS NOT NULL
BEGIN
    INSERT INTO GoiDichVu (TenGoi, GiaTien, GiaCoc, ThoiLuong, SoNguoiToiDa, MoTaChiTiet, SanPhamBanGiao, MaDanhMuc, MaNhiepAnhGia, AnhDaiDien) VALUES 
    (N'Pre-wedding Hội An Cổ Kính', 6000000, 2000000, 240, 2, N'Chụp phố cổ Hội An buổi sáng và thả đèn hoa đăng buổi tối.', N'1 Album 30x30, 2 ảnh lớn', @CatWedding, @IdWeddingDN, 'https://images.unsplash.com/photo-1519741497674-611481863552?auto=format&fit=crop&w=800&q=80'),
    (N'Bình Minh Biển Mỹ Khê', 4000000, 1000000, 120, 2, N'Đón bình minh trên biển Đà Nẵng. Concept lãng mạn, nhẹ nhàng.', N'20 ảnh chỉnh sửa, toàn bộ file gốc', @CatWedding, @IdWeddingDN, 'https://images.unsplash.com/photo-1510076857177-be9321a29160?auto=format&fit=crop&w=800&q=80');
END
GO

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

DECLARE @IdKhach INT = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'Customer');
INSERT INTO YeuCau (MaKhachHang, TieuDe, MoTa, DiaChi, NganSach, NgayCanChup)
VALUES (@IdKhach, N'Tìm thợ chụp Lookbook gấp', N'Cần chụp 5 bộ đồ cho shop thời trang', N'Studio tại Cầu Giấy', 1500000, GETDATE()+2);
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
/*
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
GO*/

-- Kiểm tra lại (Tất cả sẽ trả về 0 dòng)
SELECT * FROM DonDatLich;
Select * from DanhGia;
SELECT * FROM NguoiDung;
select * from GoiDichVu;
Select * from YeuCau;
select * from DanhMuc;

