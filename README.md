# 📸 Photography Booking System (Hệ thống Website Đặt lịch Chụp ảnh)

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-6.0%2F8.0-purple) ![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red) ![Status](https://img.shields.io/badge/Status-In%20Development-yellow)

## 📖 Giới thiệu (Introduction)
Đây là đồ án **Website Đặt lịch Chụp ảnh**, một nền tảng kết nối giữa Khách hàng và các Nhiếp ảnh gia chuyên nghiệp. Dự án được thiết kế để giải quyết vấn đề quản lý lịch hẹn thủ công, giúp tối ưu hóa quy trình làm việc cho các Studio và mang lại trải nghiệm đặt lịch minh bạch, nhanh chóng cho khách hàng.

Dự án được xây dựng dựa trên mô hình **MVC**, không sử dụng Identity có sẵn mà tự xây dựng hệ thống xác thực (Custom Authentication) để tùy biến sâu theo nghiệp vụ.

## 🚀 Tính năng chính (Key Features)

### 👤 Khách hàng (Customer)
* **Tìm kiếm thông minh:** Tìm nhiếp ảnh gia theo Tỉnh/Thành phố (Location) và Danh mục (Cưới, Kỷ yếu, Sự kiện...).
* **Đặt lịch trực tuyến:** Xem lịch trống (Calendar), chọn gói dịch vụ và đặt cọc (Deposit).
* **Quản lý đơn hàng:** Theo dõi trạng thái đơn (Chờ duyệt, Đã cọc, Hoàn thành) và lịch sử thanh toán.
* **Đánh giá:** Gửi feedback và chấm điểm sao cho dịch vụ đã sử dụng.

### 📷 Nhiếp ảnh gia (Photographer)
* **Profile chuyên nghiệp:** Tự quản lý trang cá nhân với Ảnh bìa, Bio, và Portfolio (Album ảnh mẫu).
* **Quản lý Gói chụp:** Thiết lập giá, số tiền cọc, thời lượng và sản phẩm bàn giao.
* **Quản lý Lịch:** Xác nhận hoặc từ chối lịch hẹn mới.

### 🛡️ Quản trị viên (Admin)
* Quản lý người dùng (User Management).
* Quản lý danh mục dịch vụ (Categories).
* Thống kê báo cáo doanh thu và số lượng booking.

## 🛠 Công nghệ sử dụng (Tech Stack)

* **Backend:** ASP.NET Core MVC (C#)
* **ORM:** Entity Framework Core (Code-First)
* **Database:** SQL Server
* **Frontend:** HTML5, CSS3, Bootstrap 5, JavaScript (jQuery)
* **Tools:** Visual Studio 2022, SSMS

## 🗄️ Thiết kế Cơ sở dữ liệu (Database Design)
Hệ thống bao gồm các bảng chính:
* `Users` (Custom Auth: Admin, Photographer, Customer)
* `Locations` (Quản lý địa điểm)
* `ServicePackages` (Gói dịch vụ & Giá cọc)
* `Bookings` (Quản lý lịch hẹn & Trạng thái thanh toán)
* `Portfolios` & `PortfolioPhotos` (Thư viện ảnh)

## ⚙️ Cài đặt & Chạy (Installation)

1. Clone dự án về máy:
   ```bash
   git clone [https://github.com/TanHung189/photography-booking-system.git](https://github.com/TanHung189/photography-booking-system.git)
