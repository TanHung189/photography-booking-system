-- SQL Migration: Thêm cột DiaChiVi vào bảng NguoiDung
-- Chạy script này trên SQL Server Express trước khi khởi động ứng dụng

-- Kiểm tra và thêm cột DiaChiVi nếu chưa tồn tại
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'NguoiDung' AND COLUMN_NAME = 'DiaChiVi'
)
BEGIN
    ALTER TABLE NguoiDung
    ADD DiaChiVi NVARCHAR(100) NULL;

    PRINT 'Đã thêm cột DiaChiVi vào bảng NguoiDung thành công.';
END
ELSE
BEGIN
    PRINT 'Cột DiaChiVi đã tồn tại, bỏ qua.';
END

-- Tùy chọn: Gán địa chỉ ví mẫu cho nhiếp ảnh gia để test
-- UPDATE NguoiDung
-- SET DiaChiVi = '0xYourPhotographerWalletAddressHere'
-- WHERE VaiTro = 'NhiepAnh' AND MaNguoiDung = <MaNguoiDung>;
