// File: wwwroot/js/provinces.js

/**
 * Hàm load danh sách tỉnh thành vào thẻ select
 * @param {string} selectId - ID của thẻ select cần đổ dữ liệu (VD: 'province-select')
 * @param {string} hiddenInputId - (Tùy chọn) ID của thẻ input ẩn để lưu tên tỉnh
 * @param {string} selectedValue - (Tùy chọn) Tên tỉnh cần chọn sẵn (dùng cho trang Edit)
 */
function loadProvinces(selectId, hiddenInputId = null, selectedValue = null) {
    var select = document.getElementById(selectId);

    // Nếu không tìm thấy thẻ select thì dừng lại
    if (!select) return;

    fetch('https://provinces.open-api.vn/api/?depth=1')
        .then(response => response.json())
        .then(data => {
            // Xóa các option cũ (trừ option đầu tiên "Chọn tỉnh...")
            while (select.options.length > 1) {
                select.remove(1);
            }

            data.forEach(item => {
                // Làm sạch tên (bỏ chữ Tỉnh/Thành phố)
                var cleanName = item.name.replace('Tỉnh ', '').replace('Thành phố ', '');

                var option = document.createElement('option');
                option.value = cleanName;
                option.text = cleanName;

                // Nếu có giá trị cần chọn trước (cho trang Edit/Search)
                if (selectedValue && cleanName === selectedValue) {
                    option.selected = true;
                }

                select.appendChild(option);
            });

            // Nếu có input ẩn, gán sự kiện change để cập nhật giá trị
            if (hiddenInputId) {
                var hiddenInput = document.getElementById(hiddenInputId);
                if (hiddenInput) {
                    select.addEventListener('change', function () {
                        hiddenInput.value = this.value;
                    });
                }
            }
        })
        .catch(error => console.error('Lỗi gọi API Tỉnh/Thành:', error));
}