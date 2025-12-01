using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace PhotoBooking.Web.Services
{
    public class PhotoService
    {
        private readonly Cloudinary _cloudinary;

        // Constructor: Lấy thông tin cấu hình từ appsettings.json
        public PhotoService(IConfiguration config)
        {
            var cloudName = config["CloudinarySettings:CloudName"];
            var apiKey = config["CloudinarySettings:ApiKey"];
            var apiSecret = config["CloudinarySettings:ApiSecret"];

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        // Hàm Upload ảnh: Nhận file từ form -> Trả về đường link URL
        public async Task<string> UploadPhotoAsync(IFormFile file)
        {
            // 1. Kiểm tra file có tồn tại không
            if (file == null || file.Length == 0) return null;

            var uploadResult = new ImageUploadResult();

            // 2. Mở luồng đọc file
            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    // (Tùy chọn) Tự động crop ảnh nếu cần
                    // Transformation = new Transformation().Height(500).Width(500).Crop("fill")
                };

                // 3. Đẩy lên Cloudinary
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            // 4. Trả về đường dẫn ảnh online (https://...)
            return uploadResult.SecureUrl.ToString();
        }
    }
}
