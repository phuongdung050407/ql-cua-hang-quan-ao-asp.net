
document.addEventListener("DOMContentLoaded", async function () {

    const tinhSelect = document.getElementById("TinhThanh");
    const xaSelect = document.getElementById("XaPhuong");

    // Kiểm tra dropdown tỉnh/thành phố
    if (!tinhSelect) return;

    try {

        // 1. Tải danh sách tỉnh/thành phố
        const responseTinh = await fetch("/data/tinhthanh.json");

        if (!responseTinh.ok) {
            throw new Error("Không thể tải dữ liệu tỉnh/thành phố!");
        }

        const danhSachTinh = await responseTinh.json();

        // 2. Hiển thị danh sách tỉnh/thành phố
        danhSachTinh.forEach(function (tinh) {

            const option = document.createElement("option");

            option.value = tinh.ma_tinh;
            option.textContent = tinh.ten;

            tinhSelect.appendChild(option);

        });

        // Nếu trang chưa có dropdown xã/phường thì dừng tại đây
        if (!xaSelect) return;

        // 3. Tải danh sách xã/phường
        const responseXa = await fetch("/data/xaphuong.json");

        if (!responseXa.ok) {
            throw new Error("Không thể tải dữ liệu xã/phường!");
        }

        const danhSachXa = await responseXa.json();

        // 4. Khi người dùng chọn tỉnh/thành phố
        tinhSelect.addEventListener("change", function () {

            const maTinh = this.value;

            // Xóa danh sách xã/phường cũ
            xaSelect.innerHTML =
                '<option value="">-- Chọn xã/phường --</option>';

            // Chưa chọn tỉnh thì vô hiệu hóa dropdown xã/phường
            if (!maTinh) {
                xaSelect.disabled = true;
                return;
            }

            // Lọc xã/phường theo mã tỉnh
            const danhSachLoc = danhSachXa.filter(function (xa) {
                return xa.ma_tinh === maTinh;
            });

            // Hiển thị danh sách xã/phường
            danhSachLoc.forEach(function (xa) {

                const option = document.createElement("option");

                option.value = xa.ma_xa;
                option.textContent = xa.ten;

                xaSelect.appendChild(option);

            });

            xaSelect.disabled = danhSachLoc.length === 0;

        });

    }
    catch (error) {

        console.error("Lỗi tải dữ liệu địa chỉ:", error);

    }

});
