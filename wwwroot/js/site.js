// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', () => {
    const money = value => `${Math.max(0, Number(value)).toLocaleString('vi-VN')} đ`;
    const cartKey = 'fashionhub-cart';
    const wishlistKey = 'fashionhub-wishlist';

    const read = key => { try { return JSON.parse(localStorage.getItem(key) || '[]'); } catch { return []; } };
    const write = (key, value) => localStorage.setItem(key, JSON.stringify(value));

    function toast(message) {
        let el = document.querySelector('.app-toast');
        if (!el) { el = document.createElement('div'); el.className = 'app-toast'; document.body.appendChild(el); }
        el.textContent = message;
        el.classList.add('show');
        clearTimeout(window.__toastTimer);
        window.__toastTimer = setTimeout(() => el.classList.remove('show'), 2200);
    }

    function refreshCartBadges() {
        const count = read(cartKey).reduce((sum, item) => sum + Number(item.quantity || 1), 0);
        document.querySelectorAll('.cart-link b').forEach(badge => badge.textContent = String(count));
    }

    document.querySelectorAll('[data-add-cart]').forEach(button => {
        button.addEventListener('click', event => {
            event.preventDefault();
            const cart = read(cartKey);
            const id = button.dataset.addCart;
            const detailQtyEl = document.querySelector('.detail-quantity strong');
            const qtyToAdd = detailQtyEl && button.closest('.detail-actions') ? Number(detailQtyEl.textContent || 1) : 1;

            const existing = cart.find(item => item.id === id);
            if (existing) existing.quantity = Number(existing.quantity || 1) + qtyToAdd;
            else cart.push({
                id,
                name: button.dataset.name || 'Sản phẩm',
                price: Number(button.dataset.price || 0),
                image: button.dataset.image || '',
                category: button.dataset.category || '',
                quantity: qtyToAdd
            });
            write(cartKey, cart);
            refreshCartBadges();
            toast('Đã thêm sản phẩm vào giỏ hàng');
        });
    });

    document.querySelectorAll('[data-wishlist]').forEach(button => {
        const id = button.dataset.wishlist;
        const saved = read(wishlistKey);
        if (saved.includes(id)) { button.classList.add('active'); button.textContent = '♥'; }
        button.addEventListener('click', event => {
            event.preventDefault();
            const values = read(wishlistKey);
            const index = values.indexOf(id);
            if (index >= 0) { values.splice(index, 1); button.classList.remove('active'); button.textContent = '♡'; toast('Đã bỏ khỏi yêu thích'); }
            else { values.push(id); button.classList.add('active'); button.textContent = '♥'; toast('Đã thêm vào yêu thích'); }
            write(wishlistKey, values);
        });
    });

    document.querySelectorAll('.size-options button').forEach(button => button.addEventListener('click', () => {
        document.querySelectorAll('.size-options button').forEach(item => item.classList.remove('selected'));
        button.classList.add('selected');
    }));

    document.querySelectorAll('.color-options button').forEach(button => button.addEventListener('click', () => {
        document.querySelectorAll('.color-options button').forEach(item => item.classList.remove('selected'));
        button.classList.add('selected');
    }));

    const detailQuantity = document.querySelector('.detail-quantity strong');
    if (detailQuantity) {
        const buttons = document.querySelectorAll('.detail-quantity button');
        buttons[0]?.addEventListener('click', () => detailQuantity.textContent = String(Math.max(1, Number(detailQuantity.textContent) - 1)));
        buttons[1]?.addEventListener('click', () => detailQuantity.textContent = String(Number(detailQuantity.textContent) + 1));
    }

    const cartContainer = document.querySelector('.cart-items');
    if (cartContainer) {
        let cart = read(cartKey);
        if (!cart.length) {
            cart = Array.from(document.querySelectorAll('.cart-item')).map(row => ({
                id: row.dataset.id,
                name: row.querySelector('h3')?.textContent || '',
                category: row.querySelector('small')?.textContent || '',
                image: row.querySelector('img')?.getAttribute('src') || '',
                price: Number(row.dataset.price || 0),
                quantity: 1
            }));
        }

        const renderCart = () => {
            cartContainer.innerHTML = '';
            cart.forEach((item, index) => {
                const row = document.createElement('article');
                row.className = 'cart-item';
                row.dataset.price = item.price;
                row.innerHTML = `<img src="${item.image}" alt="${item.name}"><div class="cart-item-info"><small>${item.category}</small><h3>${item.name}</h3><span>Kích thước: M &nbsp; Màu: Trắng</span></div><div class="quantity"><button type="button">−</button><strong>${item.quantity}</strong><button type="button">＋</button></div><strong>${money(item.price * item.quantity)}</strong><button type="button" class="remove">×</button>`;
                const quantityButtons = row.querySelectorAll('.quantity button');
                quantityButtons[0].addEventListener('click', () => { item.quantity = Math.max(1, item.quantity - 1); write(cartKey, cart); renderCart(); });
                quantityButtons[1].addEventListener('click', () => { item.quantity += 1; write(cartKey, cart); renderCart(); });
                row.querySelector('.remove').addEventListener('click', () => { cart.splice(index, 1); write(cartKey, cart); renderCart(); toast('Đã xóa khỏi giỏ hàng'); });
                cartContainer.appendChild(row);
            });
            const subtotal = cart.reduce((sum, item) => sum + item.price * item.quantity, 0);
            const coupon = sessionStorage.getItem('fashionhub-coupon') === 'NEWYEAR20' ? 150000 : 0;
            document.querySelectorAll('.order-summary > div:first-of-type strong').forEach(el => el.textContent = money(subtotal));
            document.querySelectorAll('.order-summary .total strong').forEach(el => el.textContent = money(subtotal - coupon));
            document.querySelectorAll('.cart-link b').forEach(badge => badge.textContent = String(cart.reduce((sum, item) => sum + item.quantity, 0)));
        };

        renderCart();

        document.querySelector('.coupon button')?.addEventListener('click', () => {
            const input = document.querySelector('.coupon input');
            if (input?.value.trim().toUpperCase() === 'NEWYEAR20' || input?.value.trim().toUpperCase() === 'FASHION20') {
                sessionStorage.setItem('fashionhub-coupon', 'NEWYEAR20'); toast('Áp dụng mã giảm 150.000 đ'); renderCart();
            } else { sessionStorage.removeItem('fashionhub-coupon'); toast('Mã khuyến mãi không hợp lệ'); renderCart(); }
        });
    }

    // Hiển thị đúng các sản phẩm đã chọn ở trang Thanh toán (Checkout)
    const checkoutPage = document.querySelector('.checkout-page');
    if (checkoutPage) {
        const cart = read(cartKey);
        const summaryBox = checkoutPage.querySelector('.order-summary');
        if (summaryBox && cart.length) {
            summaryBox.querySelectorAll('.summary-product').forEach(el => el.remove());
            const totalDiv = summaryBox.querySelector('.total');
            let subtotal = 0;
            cart.forEach(item => {
                subtotal += item.price * item.quantity;
                const prodDiv = document.createElement('div');
                prodDiv.className = 'summary-product';
                prodDiv.innerHTML = `<img src="${item.image}"><div><strong>${item.name}</strong><small>Số lượng: ${item.quantity}</small></div><b>${money(item.price * item.quantity)}</b>`;
                summaryBox.insertBefore(prodDiv, totalDiv);
            });
            const coupon = sessionStorage.getItem('fashionhub-coupon') === 'NEWYEAR20' ? 150000 : 0;
            const totalStrong = totalDiv?.querySelector('strong');
            if (totalStrong) totalStrong.textContent = money(subtotal - coupon);
        }
    }

    document.querySelectorAll('.payment-option').forEach(option => option.addEventListener('click', () => {
        document.querySelectorAll('.payment-option').forEach(item => item.classList.remove('selected'));
        option.classList.add('selected');
        const radio = option.querySelector('input[type="radio"]'); if (radio) radio.checked = true;
    }));

    const placeOrderButton = document.querySelector('#place-order-button');
    placeOrderButton?.addEventListener('click', event => {
        const checkoutForm = document.querySelector('#checkout-form');
        const placeForm = document.querySelector('#place-order-form');
        const cart = read(cartKey);

        if (!cart.length) {
            event.preventDefault();
            toast('Giỏ hàng của bạn đang trống!');
            return;
        }

        if (checkoutForm && !checkoutForm.checkValidity()) {
            event.preventDefault();
            checkoutForm.reportValidity();
            toast('Vui lòng nhập đầy đủ thông tin giao hàng');
            return;
        }

        if (placeForm && checkoutForm) {
            placeForm.querySelectorAll('.dynamic-order-field').forEach(el => el.remove());
            const appendField = (name, val) => {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = name;
                input.value = val ?? '';
                input.className = 'dynamic-order-field';
                placeForm.appendChild(input);
            };

            const fd = new FormData(checkoutForm);
            fd.forEach((value, key) => appendField(key, value));
            appendField('cartJson', JSON.stringify(cart));
            appendField('couponCode', sessionStorage.getItem('fashionhub-coupon') || '');
            localStorage.removeItem(cartKey);
            sessionStorage.removeItem('fashionhub-coupon');
        }
    });

    document.querySelectorAll('.toggle-password').forEach(button => {
        button.addEventListener('click', () => {
            const input = button.parentElement.querySelector('input');
            if (!input) return;
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            button.textContent = show ? '◉' : '👁';
            button.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
            input.focus();
        });
    });

    document.querySelectorAll('.review-row').forEach(row => {
        row.querySelector('.icon-button')?.addEventListener('click', () => {
            const badge = row.querySelector('.badge');
            if (!badge) return;
            if (badge.textContent.trim() === 'Chờ duyệt') {
                badge.textContent = 'Đã duyệt'; badge.className = 'badge badge-success'; toast('Đã duyệt đánh giá');
            } else {
                badge.textContent = 'Chờ duyệt'; badge.className = 'badge badge-warning'; toast('Đã chuyển đánh giá về chờ duyệt');
            }
        });
    });

    document.querySelectorAll('.table-panel tbody tr').forEach(row => {
        const status = row.querySelector('.badge');
        row.querySelector('.icon-button')?.addEventListener('click', () => {
            if (!status) { toast('Đã mở thao tác cho mục đã chọn'); return; }
            const active = status.classList.contains('badge-danger');
            status.textContent = active ? 'Hoạt động' : (status.textContent.trim() === 'Hoạt động' ? 'Tạm khóa' : status.textContent.trim());
            status.className = active ? 'badge badge-success' : (status.textContent.trim() === 'Tạm khóa' ? 'badge badge-danger' : status.className);
            toast(status.textContent.trim() === 'Tạm khóa' ? 'Đã khóa tài khoản' : 'Đã cập nhật trạng thái');
        });
    });

    const productSearch = document.querySelector('.management-toolbar .filter-search input');
    productSearch?.addEventListener('input', () => {
        const keyword = productSearch.value.toLowerCase();
        document.querySelectorAll('.table-panel tbody tr').forEach(row => row.hidden = !row.textContent.toLowerCase().includes(keyword));
    });

    document.querySelectorAll('.promotion-card button').forEach(button => {
        button.addEventListener('click', () => toast('Đã mở chi tiết khuyến mãi'));
    });

    document.querySelectorAll('.category-card').forEach(card => {
        card.style.cursor = 'pointer';
        card.addEventListener('click', () => toast(`Đang mở danh mục: ${card.querySelector('strong')?.textContent || ''}`));
    });

    document.querySelectorAll('.status-save').forEach(button => {
        button.addEventListener('click', () => {
            const select = button.closest('tr')?.querySelector('.status-select');
            toast(select ? `Đã lưu trạng thái: ${select.value}` : 'Đã lưu trạng thái');
        });
    });

    document.querySelectorAll('.status-select').forEach(select => {
        const current = select.dataset.current;
        if (current) {
            Array.from(select.options).forEach(option => option.selected = option.textContent.trim() === current);
        }
        select.addEventListener('change', () => {
            select.classList.toggle('status-danger', select.value === 'Tạm khóa' || select.value === 'Đã hủy');
            select.classList.toggle('status-warning', select.value === 'Nghỉ phép' || select.value === 'Chờ xử lý');
            select.classList.toggle('status-success', select.value === 'Hoạt động' || select.value === 'Hoàn tất');
        });
        select.dispatchEvent(new Event('change'));
    });

    document.querySelectorAll('.action-select').forEach(select => {
        select.addEventListener('change', () => {
            if (!select.value) return;
            const labels = {
                view: 'Đã mở chi tiết', edit: 'Đang mở form sửa', delete: 'Đã xóa mục đã chọn',
                adjust: 'Đang mở điều chỉnh tồn kho', export: 'Đã tạo phiếu xuất kho', approve: 'Đã duyệt đánh giá',
                reply: 'Đã mở hộp trả lời', hide: 'Đã ẩn đánh giá'
            };
            toast(labels[select.value] || 'Đã thực hiện thao tác');
            if (select.value === 'delete') {
                const row = select.closest('tr');
                if (row && confirm('Bạn có chắc muốn xóa mục này?')) row.remove();
            }
            select.value = '';
        });
    });

    refreshCartBadges();
});