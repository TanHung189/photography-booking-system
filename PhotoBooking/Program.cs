using Microsoft.AspNetCore.Authentication.Cookies; // 1. Thư viện xác thực Cookie
using Microsoft.EntityFrameworkCore;               // 2. Thư viện kết nối SQL
using PhotoBooking.Models;                         // 3. Namespace chứa DbContext và Models
// using PhotoBooking.Services;                    // 4. (Mở comment dòng này nếu bạn đã tạo file PhotoService.cs)

var builder = WebApplication.CreateBuilder(args);

// ====================================================
// PHẦN 1: ĐĂNG KÝ DỊCH VỤ (ADD SERVICES)
// ====================================================

// 1. Kết nối SQL Server
// "PhotoBookingConn" phải khớp với tên trong appsettings.json
builder.Services.AddDbContext<PhotoBookingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PhotoBookingConn")));

// 2. Cấu hình Đăng nhập (Cookie Authentication) - Vì không dùng Identity
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "PhotoBookingCookie"; // Tên Cookie
        options.LoginPath = "/Account/Login";       // Đường dẫn trang đăng nhập
        options.AccessDeniedPath = "/Account/Forbidden"; // Đường dẫn khi không có quyền
        options.ExpireTimeSpan = TimeSpan.FromDays(7);   // Duy trì đăng nhập 7 ngày
    });

// 3. Đăng ký Session (Để lưu thông tin tạm)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Hết hạn sau 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Đăng ký Service Upload ảnh (Cloudinary)
// Nếu bạn chưa tạo file Services/PhotoService.cs thì tạm thời comment dòng dưới lại để không lỗi
// builder.Services.AddScoped<PhotoService>(); 

// 5. Thêm MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ====================================================
// PHẦN 2: CẤU HÌNH PIPELINE (MIDDLEWARE)
// ====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ⚠️ QUAN TRỌNG: Thứ tự phải đúng như sau:
app.UseAuthentication(); // 1. Kiểm tra danh tính (Bạn là ai?)
app.UseAuthorization();  // 2. Kiểm tra quyền hạn (Bạn được làm gì?)

app.UseSession(); // Kích hoạt Session

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();