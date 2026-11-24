// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const cartKey = 'fashionhub_cart';
const wishlistKey = 'fashionhub-wishlist';
const money = value => `${Math.max(0, Number(value) || 0).toLocaleString('vi-VN')} đ`;

function readCart() {
    let value;
    try {
        value = JSON.parse(localStorage.getItem(cartKey)
            || localStorage.getItem('fashionhub-cart')
            || localStorage.getItem('cart')
            || '[]');
    } catch {
        value = [];
    }

    if (!Array.isArray(value)) return [];
    return value.filter(item => item && String(item.id || '').trim()
        && Number.isFinite(Number(item.price)) && Number(item.price) >= 0)
        .map(item => ({
            id: String(item.id),
            name: String(item.name || 'Sản phẩm'),
            category: String(item.category || ''),
            image: String(item.image || ''),
            variantId: Number(item.variantId) > 0 ? Number(item.variantId) : null,
            variantLabel: String(item.variantLabel || ''),
            stock: Number.isFinite(Number(item.stock)) ? Math.max(0, Math.floor(Number(item.stock))) : 99,
            price: Number(item.price),
            quantity: Math.min(99, Math.max(1, Math.floor(Number(item.quantity) || 1)))
        }));
}

function saveCart(cart) {
    localStorage.setItem(cartKey, JSON.stringify(cart));
    localStorage.removeItem('fashionhub-cart');
    localStorage.removeItem('cart');
}

function showToast(message) {
    let el = document.querySelector('.app-toast');
    if (!el) {
        el = document.createElement('div');
        el.className = 'app-toast';
        document.body.appendChild(el);
    }
    el.textContent = message;
    el.classList.add('show');
    clearTimeout(window.__toastTimer);
    window.__toastTimer = setTimeout(() => el.classList.remove('show'), 2200);
}

function updateCartBadge() {
    const count = readCart().reduce((sum, item) => sum + item.quantity, 0);
    const badge = document.getElementById('headerCartCount');
    if (badge) badge.textContent = String(count);
    document.querySelectorAll('.cart-link b').forEach(item => item.textContent = String(count));
}

let cartSyncQueue = Promise.resolve();

function syncCartToServer(cart) {
    if (!window.shopCartSyncEnabled) return Promise.resolve(true);

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        showToast('Không thể xác thực thao tác giỏ hàng. Vui lòng tải lại trang.');
        return Promise.resolve(false);
    }

    const snapshot = cart.map(item => ({
        id: item.id,
        variantId: item.variantId || null,
        quantity: item.quantity
    }));
    cartSyncQueue = cartSyncQueue.then(async () => {
        const body = new URLSearchParams({
            cartJson: JSON.stringify(snapshot),
            __RequestVerificationToken: token
        });
        const response = await fetch(window.shopCartSyncUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8' },
            body
        });
        const result = await response.json();
        if (!response.ok || !result.success) throw new Error(result.message || 'Không thể cập nhật giỏ hàng.');
        return true;
    }).catch(error => {
        showToast(error.message || 'Không thể đồng bộ giỏ hàng.');
        return false;
    });
    return cartSyncQueue;
}

window.updateHeaderCartBadge = updateCartBadge;
window.addProductToCart = (id, variantId, name, price, image, category, variantLabel, stock, quantity) => {
    const productId = String(id || '').trim();
    const skuId = Number(variantId);
    const parsedPrice = Number(price);
    const availableStock = Math.max(0, Math.floor(Number(stock) || 0));
    const amount = Math.floor(Number(quantity) || 1);
    if (!productId || !Number.isSafeInteger(skuId) || skuId < 1
        || !Number.isFinite(parsedPrice) || parsedPrice < 0 || amount < 1 || amount > 99) {
        showToast('Thông tin sản phẩm không hợp lệ.');
        return;
    }

    const cart = readCart();
    const existing = cart.find(item => item.id === productId && item.variantId === skuId);
    if (existing && existing.quantity + amount > availableStock) {
        showToast('Số lượng trong giỏ vượt quá tồn kho hiện tại.');
        return;
    }
    if (!existing && amount > availableStock) {
        showToast('Số lượng vượt quá tồn kho hiện tại.');
        return;
    }
    if (existing) {
        existing.quantity = Math.min(99, existing.quantity + amount);
        existing.stock = availableStock;
        existing.price = parsedPrice;
        existing.variantLabel = String(variantLabel || '');
    }
    else cart.push({
        id: productId,
        variantId: skuId,
        name: String(name || 'Sản phẩm'),
        category: String(category || ''),
        variantLabel: String(variantLabel || ''),
        stock: availableStock,
        price: parsedPrice,
        image: String(image || ''),
        quantity: amount
    });

    try {
        saveCart(cart);
        updateCartBadge();
        showToast(`Đã thêm ${amount} sản phẩm vào giỏ hàng.`);
    } catch {
        showToast('Không thể lưu giỏ hàng trên trình duyệt này.');
    }
};

document.addEventListener('DOMContentLoaded', () => {
    const read = key => {
        try { return JSON.parse(localStorage.getItem(key) || '[]'); }
        catch { return []; }
    };
    const write = (key, value) => localStorage.setItem(key, JSON.stringify(value));
    const toast = showToast;

    const persistenceReady = (async () => {
        if (!window.shopCartSyncEnabled) return;
        try {
            await cartSyncQueue;
            const response = await fetch(window.shopPersistentCartUrl);
            if (!response.ok) throw new Error('Không tải được giỏ hàng đã lưu.');
            const persistentCart = await response.json();
            const localCart = readCart();
            if (persistentCart.length > 0) {
                const merged = new Map();
                [...persistentCart, ...localCart].forEach(item => {
                    const key = `${item.id}:${item.variantId || ''}`;
                    const previous = merged.get(key);
                    merged.set(key, previous
                        ? { ...previous, quantity: Math.max(previous.quantity, item.quantity) }
                        : item);
                });
                saveCart([...merged.values()]);
                await syncCartToServer(readCart());
            } else if (localCart.length > 0) {
                await syncCartToServer(localCart);
            }
        } catch (error) {
            toast(error.message || 'Không thể tải giỏ hàng đã lưu.');
        }
    })();

    updateCartBadge();
    document.querySelectorAll('[data-add-cart]').forEach(button => {
        button.addEventListener('click', async () => {
            await persistenceReady;
            const detailQuantity = button.closest('.detail-actions')?.querySelector('#detailQty');
            window.addProductToCart(
                button.dataset.addCart,
                button.dataset.variantId,
                button.dataset.name,
                button.dataset.price,
                button.dataset.image,
                button.dataset.category,
                button.dataset.variantLabel,
                button.dataset.stock,
                detailQuantity?.textContent || 1
            );
            await syncCartToServer(readCart());
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
        const maxQuantity = Number(document.querySelector('.detail-actions [data-add-cart]')?.dataset.stock) || 1;
        buttons[0]?.addEventListener('click', () => detailQuantity.textContent = String(Math.max(1, Number(detailQuantity.textContent) - 1)));
        buttons[1]?.addEventListener('click', () => detailQuantity.textContent = String(Math.min(maxQuantity, 99, Number(detailQuantity.textContent) + 1)));
    }

    const cartContainer = document.querySelector('.cart-items');
    const emptyMessage = () => {
        const panel = document.createElement('div');
        panel.className = 'cart-empty-state';
        const icon = document.createElement('span');
        icon.textContent = '🛍';
        const title = document.createElement('h2');
        title.textContent = 'Giỏ hàng đang trống';
        const copy = document.createElement('p');
        copy.textContent = 'Hãy chọn những thiết kế bạn yêu thích và quay lại đây nhé.';
        const link = document.createElement('a');
        link.href = '/Shop/Index';
        link.className = 'checkout-button';
        link.textContent = 'Khám phá sản phẩm';
        panel.append(icon, title, copy, link);
        return panel;
    };

    const drawCart = () => {
        if (!cartContainer) return;
        const cart = readCart();
        cartContainer.replaceChildren();
        if (cart.length === 0) cartContainer.appendChild(emptyMessage());

        cart.forEach(item => {
            const row = document.createElement('article');
            row.className = 'cart-item';
            const image = document.createElement('img');
            image.src = item.image;
            image.alt = item.name;
            image.loading = 'lazy';

            const info = document.createElement('div');
            info.className = 'cart-item-info';
            const category = document.createElement('small');
            category.textContent = item.category;
            const name = document.createElement('h3');
            name.textContent = item.name;
            const detail = document.createElement('span');
            detail.textContent = item.variantLabel || 'Phiên bản được xác nhận khi thanh toán';
            info.append(category, name, detail);

            const quantity = document.createElement('div');
            quantity.className = 'quantity';
            const decrease = document.createElement('button');
            decrease.type = 'button';
            decrease.setAttribute('aria-label', 'Giảm số lượng');
            decrease.textContent = '−';
            const count = document.createElement('strong');
            count.textContent = String(item.quantity);
            const increase = document.createElement('button');
            increase.type = 'button';
            increase.setAttribute('aria-label', 'Tăng số lượng');
            increase.textContent = '＋';
            quantity.append(decrease, count, increase);

            const lineTotal = document.createElement('strong');
            lineTotal.className = 'cart-line-total';
            lineTotal.textContent = money(item.price * item.quantity);
            const remove = document.createElement('button');
            remove.type = 'button';
            remove.className = 'remove';
            remove.setAttribute('aria-label', `Xóa ${item.name} khỏi giỏ hàng`);
            remove.textContent = '×';
            row.append(image, info, quantity, lineTotal, remove);

            decrease.addEventListener('click', () => changeQuantity(item.id, item.variantId, -1));
            increase.addEventListener('click', () => changeQuantity(item.id, item.variantId, 1));
            remove.addEventListener('click', () => {
                const updatedCart = readCart().filter(cartItem => cartItem.id !== item.id || cartItem.variantId !== item.variantId);
                saveCart(updatedCart);
                syncCartToServer(updatedCart);
                drawCart();
                toast('Đã xóa sản phẩm khỏi giỏ hàng.');
            });
            cartContainer.appendChild(row);
        });

        const subtotal = cart.reduce((sum, item) => sum + item.price * item.quantity, 0);
        const itemCount = cart.reduce((sum, item) => sum + item.quantity, 0);
        const coupon = sessionStorage.getItem('fashionhub-coupon') === 'NEWYEAR20' ? Math.min(150000, subtotal) : 0;
        const itemCountText = document.getElementById('cartItemCount');
        const summaryItemCount = document.getElementById('summaryItemCount');
        if (itemCountText) itemCountText.textContent = String(itemCount).padStart(2, '0');
        if (summaryItemCount) summaryItemCount.textContent = `${itemCount} sản phẩm`;
        const subtotalText = document.getElementById('cartSubtotal');
        const totalText = document.getElementById('cartTotal');
        if (subtotalText) subtotalText.textContent = money(subtotal);
        if (totalText) totalText.textContent = money(subtotal - coupon);
        const couponMessage = document.querySelector('.coupon-message');
        if (couponMessage) couponMessage.textContent = coupon ? 'Đã áp dụng ưu đãi 150.000 đ.' : '';
        const checkoutLink = document.querySelector('.order-summary .checkout-button');
        if (checkoutLink) checkoutLink.classList.toggle('is-disabled', cart.length === 0);
        updateCartBadge();
    };

    function changeQuantity(id, variantId, difference) {
        const cart = readCart();
        const item = cart.find(cartItem => cartItem.id === id && cartItem.variantId === variantId);
        if (!item) return;
        if (item.quantity + difference > item.stock) {
            toast('Đã đạt số lượng tồn kho hiện tại.');
            return;
        }
        item.quantity = Math.min(99, Math.max(1, item.quantity + difference));
        saveCart(cart);
        syncCartToServer(cart);
        drawCart();
    }

    if (cartContainer) drawCart();
    persistenceReady.then(() => {
        if (cartContainer) drawCart();
        if (checkoutPage) drawCheckoutSummary();
    });

    document.querySelector('.order-summary .checkout-button')?.addEventListener('click', event => {
        if (readCart().length === 0) {
            event.preventDefault();
            toast('Hãy thêm sản phẩm trước khi thanh toán.');
        }
    });

    document.querySelector('.coupon button')?.addEventListener('click', () => {
        const input = document.querySelector('.coupon input');
        const couponMessage = document.querySelector('.coupon-message');
        if (input?.value.trim().toUpperCase() === 'NEWYEAR20') {
            sessionStorage.setItem('fashionhub-coupon', 'NEWYEAR20');
            if (couponMessage) couponMessage.textContent = 'Đã áp dụng ưu đãi 150.000 đ.';
            toast('Áp dụng mã giảm 150.000 đ.');
        } else {
            sessionStorage.removeItem('fashionhub-coupon');
            if (couponMessage) couponMessage.textContent = 'Mã khuyến mãi không hợp lệ.';
            toast('Mã khuyến mãi không hợp lệ.');
        }
        drawCart();
    });

    const checkoutPage = document.querySelector('.checkout-page');
    function drawCheckoutSummary() {
        if (!checkoutPage) return;
        const cart = readCart();
        const products = document.getElementById('checkoutProducts');
        products.replaceChildren();
        let subtotal = 0;
        cart.forEach(item => {
            subtotal += item.price * item.quantity;
            const row = document.createElement('div');
            row.className = 'summary-product';
            const image = document.createElement('img');
            image.src = item.image;
            image.alt = item.name;
            const info = document.createElement('div');
            const name = document.createElement('strong');
            name.textContent = item.name;
            const quantity = document.createElement('small');
            quantity.textContent = `Số lượng: ${item.quantity}`;
            info.append(name, quantity);
            const total = document.createElement('b');
            total.textContent = money(item.price * item.quantity);
            row.append(image, info, total);
            products.appendChild(row);
        });
        if (cart.length === 0) {
            const empty = document.createElement('p');
            empty.className = 'checkout-empty-message';
            empty.textContent = 'Giỏ hàng đang trống. Hãy quay lại cửa hàng để chọn sản phẩm.';
            products.appendChild(empty);
        }
        const discount = sessionStorage.getItem('fashionhub-coupon') === 'NEWYEAR20' ? Math.min(150000, subtotal) : 0;
        document.getElementById('checkoutSubtotal').textContent = money(subtotal);
        document.getElementById('checkoutDiscount').textContent = money(discount);
        document.getElementById('checkoutTotal').textContent = money(subtotal - discount);
    }

    if (checkoutPage) {
        drawCheckoutSummary();
    }

    const placeForm = document.querySelector('#place-order-form');
    placeForm?.addEventListener('submit', event => {
        const cart = readCart();
        if (!cart.length) {
            event.preventDefault();
            toast('Giỏ hàng của bạn đang trống.');
            return;
        }

        placeForm.querySelectorAll('.dynamic-order-field').forEach(el => el.remove());
        const appendField = (name, value) => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = name;
            input.value = value ?? '';
            input.className = 'dynamic-order-field';
            placeForm.appendChild(input);
        };
        appendField('cartJson', JSON.stringify(cart.map(item => ({
            id: item.id,
            variantId: item.variantId || null,
            quantity: item.quantity
        }))));
        appendField('couponCode', sessionStorage.getItem('fashionhub-coupon') || '');
    });

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

    updateCartBadge();
});
