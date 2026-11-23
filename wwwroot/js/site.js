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

    document.querySelectorAll('[data-wishlist]').forEach(button => {
        const id = button.dataset.wishlist;
        const saved = read(wishlistKey);
        if (saved.includes(id)) { button.classList.add('active'); button.textContent = '♥'; }
        button.addEventListener('click', event => {
            event.preventDefault();
            const values = read(wishlistKey);
            const index = values.indexOf(id);
            if (index >= 0) {
                values.splice(index, 1);
                button.classList.remove('active');
                button.textContent = '♡';
                toast('Đã bỏ khỏi yêu thích');
            } else {
                values.push(id);
                button.classList.add('active');
                button.textContent = '♥';
                toast('Đã thêm vào yêu thích');
            }
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

    document.querySelectorAll('.payment-option').forEach(option => option.addEventListener('click', () => {
        document.querySelectorAll('.payment-option').forEach(item => item.classList.remove('selected'));
        option.classList.add('selected');
        const radio = option.querySelector('input[type="radio"]');
        if (radio) radio.checked = true;
    }));

    const productSearch = document.querySelector('.management-toolbar .filter-search input');
    productSearch?.addEventListener('input', () => {
        const keyword = productSearch.value.toLowerCase();
        document.querySelectorAll('.table-panel tbody tr').forEach(row => row.hidden = !row.textContent.toLowerCase().includes(keyword));
    });

    const placeOrderButton = document.querySelector('#place-order-button');
    placeOrderButton?.addEventListener('click', event => {
        const checkoutForm = document.querySelector('#checkout-form');
        if (checkoutForm && !checkoutForm.checkValidity()) {
            event.preventDefault();
            checkoutForm.reportValidity();
            toast('Vui lòng nhập đầy đủ thông tin giao hàng');
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

    document.querySelectorAll('.status-select').forEach(select => {
        const current = select.dataset.current;
        if (current) {
            Array.from(select.options).forEach(option => option.selected = option.textContent.trim() === current);
        }
        select.addEventListener('change', () => {
            select.classList.toggle('status-danger', select.value === 'Tạm khóa' || select.value === 'Đã hủy' || select.value === 'Khoa' || select.value === 'Da_huy');
            select.classList.toggle('status-warning', select.value === 'Nghỉ phép' || select.value === 'Chờ xử lý' || select.value === 'Cho_xac_nhan');
            select.classList.toggle('status-success', select.value === 'Hoạt động' || select.value === 'Hoàn tất' || select.value === 'Hoat_dong' || select.value === 'Da_giao');
        });
        select.dispatchEvent(new Event('change'));
    });

    refreshCartBadges();
});