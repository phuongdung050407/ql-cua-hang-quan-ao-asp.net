// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
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
            const existing = cart.find(item => item.id === id);
            if (existing) existing.quantity = Number(existing.quantity || 1) + 1;
            else cart.push({
                id,
                name: button.dataset.name || 'Sản phẩm',
                price: Number(button.dataset.price || 0),
                image: button.dataset.image || '',
                category: button.dataset.category || '',
                quantity: 1
            });
            write(cartKey, cart);
            refreshCartBadges();
            fetch(`/Shop/RecordInteraction?id=${encodeURIComponent(id)}&type=cart`, { method: 'POST' }).catch(() => {});
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
            if (index >= 0) {
                values.splice(index, 1);
                button.classList.remove('active');
                button.textContent = '♡';
                toast('Đã bỏ khỏi yêu thích');
                fetch(`/Shop/RecordInteraction?id=${encodeURIComponent(id)}&type=wishlist_remove`, { method: 'POST' }).catch(() => {});
                const wishlistItem = button.closest('[data-wishlist-item]');
                if (wishlistItem) {
                    wishlistItem.remove();
                    const remaining = document.querySelectorAll('[data-wishlist-item]');
                    if (!remaining.length) window.location.reload();
                }
            }
            else {
                values.push(id);
                button.classList.add('active');
                button.textContent = '♥';
                toast('Đã thêm vào yêu thích');
                fetch(`/Shop/RecordInteraction?id=${encodeURIComponent(id)}&type=wishlist`, { method: 'POST' }).catch(() => {});
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
        const addButton = document.querySelector('.detail-actions [data-add-cart]');
        addButton?.addEventListener('click', () => {
            const cart = read(cartKey);
            const id = addButton.dataset.addCart;
            const quantity = Number(detailQuantity.textContent);
            const existing = cart.find(item => item.id === id);
            if (existing) existing.quantity = Number(existing.quantity || 1) + quantity;
            else cart.push({ id, name:addButton.dataset.name, price:Number(addButton.dataset.price), image:addButton.dataset.image, category:addButton.dataset.category, quantity });
            write(cartKey, cart); refreshCartBadges();
            fetch(`/Shop/RecordInteraction?id=${encodeURIComponent(id)}&type=cart`, { method: 'POST' }).catch(() => {});
        });
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
                row.innerHTML = `<img src="${item.image}" alt="${item.name}"><div class="cart-item-info"><small>${item.category}</small><h3>${item.name}</h3><div class="cart-options"><label>Kích thước<select><option>S</option><option selected>M</option><option>L</option><option>XL</option></select></label><label>Màu sắc<select><option>Trắng</option><option>Đen</option><option>Be</option></select></label></div></div><div class="quantity"><button type="button" aria-label="Giảm số lượng">−</button><strong>${item.quantity}</strong><button type="button" aria-label="Tăng số lượng">＋</button></div><strong class="line-price">${money(item.price * item.quantity)}</strong><button type="button" class="remove" aria-label="Xóa sản phẩm">×</button>`;
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
            const checkoutLink = document.querySelector('.order-summary > .checkout-button');
            if (checkoutLink) checkoutLink.classList.toggle('disabled', cart.length === 0);
            if (!cart.length) {
                cartContainer.innerHTML = '<div class="empty-cart"><div>♡</div><h2>Giỏ hàng đang trống</h2><p>Hãy chọn một sản phẩm phù hợp với phong cách của bạn.</p><a class="checkout-button" href="/Shop">Khám phá sản phẩm</a></div>';
            }
        };

        renderCart();

        document.querySelector('.coupon button')?.addEventListener('click', () => {
            const input = document.querySelector('.coupon input');
            if (input?.value.trim().toUpperCase() === 'NEWYEAR20' || input?.value.trim().toUpperCase() === 'FASHION20') {
                sessionStorage.setItem('fashionhub-coupon', 'NEWYEAR20'); toast('Áp dụng mã giảm 150.000 đ'); renderCart();
            } else { sessionStorage.removeItem('fashionhub-coupon'); toast('Mã khuyến mãi không hợp lệ'); renderCart(); }
        });
    }

    document.querySelectorAll('.payment-option').forEach(option => option.addEventListener('click', () => {
        document.querySelectorAll('.payment-option').forEach(item => item.classList.remove('selected'));
        option.classList.add('selected');
        const radio = option.querySelector('input[type="radio"]'); if (radio) radio.checked = true;
    }));

    document.querySelectorAll('.table-panel tbody tr').forEach(row => {
        row.querySelector('.icon-button')?.addEventListener('click', () => toast('Đã mở thao tác cho mục đã chọn'));
    });

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
            if (!status) { toast('Đã mở menu thao tác'); return; }
            const active = status.classList.contains('badge-danger');
            status.textContent = active ? 'Hoạt động' : (status.textContent.trim() === 'Hoạt động' ? 'Tạm khóa' : status.textContent.trim());
            status.className = active ? 'badge badge-success' : (status.textContent.trim() === 'Tạm khóa' ? 'badge badge-danger' : status.className);
            toast(status.textContent.trim() === 'Tạm khóa' ? 'Đã khóa tài khoản' : 'Đã cập nhật trạng thái');
        });
    });

    document.querySelectorAll('.promotion-card button, .promotion-detail').forEach(button => {
        button.addEventListener('click', () => toast(`Khuyến mãi ${button.dataset.code || ''}: ${button.dataset.status || 'Đang chạy'}`));
    });

    const categoryFilter = document.querySelector('.category-filter');
    categoryFilter?.addEventListener('change', () => {
        document.querySelectorAll('.table-panel tbody tr[data-category]').forEach(row => {
            row.hidden = categoryFilter.value !== '' && row.dataset.category !== categoryFilter.value;
        });
    });

    document.querySelectorAll('.category-card').forEach(card => {
        card.style.cursor = 'pointer';
        card.addEventListener('click', () => toast(`Đang mở danh mục: ${card.querySelector('strong')?.textContent || ''}`));
    });

    document.querySelectorAll('.order-status-save').forEach(button => {
        button.addEventListener('click', () => {
            const row = button.closest('tr');
            const select = row?.querySelector('.order-status');
            if (!select) return;
            const form = document.createElement('form');
            form.method = 'post'; form.action = '/Management/UpdateOrderStatus';
            [{name:'code',value:select.dataset.code},{name:'status',value:select.value}].forEach(field => {
                const input = document.createElement('input'); input.type='hidden'; input.name=field.name; input.value=field.value; form.appendChild(input);
            });
            document.body.appendChild(form); form.submit();
        });
    });

    document.querySelectorAll('.status-save').forEach(button => {
        button.addEventListener('click', () => {
            const row = button.closest('tr');
            const select = row?.querySelector('.status-select');
            if (!select) return;
            const form = document.createElement('form');
            form.method = 'post';
            form.action = '/Management/UpdateStatus';
            const fields = { type: button.dataset.type, id: button.dataset.id, status: select.value };
            Object.entries(fields).forEach(([name, value]) => {
                const input = document.createElement('input');
                input.type = 'hidden'; input.name = name; input.value = value || '';
                form.appendChild(input);
            });
            document.body.appendChild(form);
            form.submit();
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

    const passwordInput = document.querySelector('#register-password');
    const confirmInput = document.querySelector('#confirm-password');
    const termsInput = document.querySelector('#register-terms');
    const registerSubmit = document.querySelector('#register-submit');

    const updatePasswordStrength = () => {
        if (!passwordInput) return;
        const value = passwordInput.value;
        const checks = [value.length >= 8, /[A-Z]/.test(value), /[0-9]/.test(value), /[^A-Za-z0-9]/.test(value)].filter(Boolean).length;
        const bar = document.querySelector('.password-strength');
        const label = bar?.querySelector('small');
        if (bar) bar.dataset.level = String(checks);
        if (label) label.textContent = ['Chưa nhập', 'Mật khẩu yếu', 'Mật khẩu khá', 'Mật khẩu mạnh', 'Mật khẩu rất mạnh'][checks];
        updatePasswordMatch();
    };

    const updatePasswordMatch = () => {
        if (!confirmInput) return;
        const match = document.querySelector('#password-match');
        if (!confirmInput.value) { if (match) match.textContent = ''; return; }
        const valid = confirmInput.value === passwordInput?.value;
        if (match) { match.textContent = valid ? '✓ Mật khẩu khớp' : 'Mật khẩu xác nhận chưa khớp'; match.className = `password-match ${valid ? 'match' : 'not-match'}`; }
    };

    passwordInput?.addEventListener('input', updatePasswordStrength);
    confirmInput?.addEventListener('input', updatePasswordMatch);

    registerSubmit?.addEventListener('click', event => {
        if (!termsInput?.checked) {
            event.preventEvent?.();
            event.preventDefault();
            toast('Vui lòng đồng ý với điều khoản sử dụng');
            termsInput?.focus();
        }
    });

    document.querySelectorAll('.action-select').forEach(select => {
        select.addEventListener('change', () => {
            if (!select.value) return;
            const labels = {
                view:'Đã mở chi tiết', edit:'Đang mở form sửa', delete:'Đã xóa mục đã chọn',
                adjust:'Đang mở điều chỉnh tồn kho', export:'Đã tạo phiếu xuất kho', approve:'Đã duyệt đánh giá',
                reply:'Đã mở hộp trả lời', hide:'Đã ẩn đánh giá'
            };
            toast(labels[select.value] || 'Đã thực hiện thao tác');
            if (select.classList.contains('review-action')) {
                const badge = select.closest('.review-row')?.querySelector('.badge');
                if (select.value === 'approve' && badge) { badge.textContent='Đã duyệt'; badge.className='badge badge-success'; }
                if (select.value === 'hide' && badge) { badge.textContent='Đã ẩn'; badge.className='badge badge-danger'; }
            }
            if (select.value === 'delete') {
                const row = select.closest('tr');
                if (row && confirm('Bạn có chắc muốn xóa mục này?')) row.remove();
            }
            select.value = '';
        });
    });

    refreshCartBadges();
});
