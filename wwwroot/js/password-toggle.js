(function() {
    'use strict';

    function initPasswordToggle() {
        // Находим все кнопки переключения пароля
        const toggleButtons = document.querySelectorAll('.toggle-btn');
        
        toggleButtons.forEach(button => {
            // Удаляем старые обработчики, чтобы избежать дублирования
            button.removeEventListener('click', handleToggleClick);
            button.addEventListener('click', handleToggleClick);
        });
    }

    function handleToggleClick(event) {
        event.preventDefault();
        
        const button = event.currentTarget;
        const passwordContainer = button.closest('.password-toggle');
        
        if (!passwordContainer) return;
        
        const passwordInput = passwordContainer.querySelector('input');
        const icon = button.querySelector('i');
        
        if (!passwordInput || !icon) return;
        
        // Переключаем тип поля
        const isPassword = passwordInput.type === 'password';
        passwordInput.type = isPassword ? 'text' : 'password';
        
        // Меняем иконку
        if (isPassword) {
            icon.classList.remove('bi-eye-slash');
            icon.classList.add('bi-eye');
        } else {
            icon.classList.remove('bi-eye');
            icon.classList.add('bi-eye-slash');
        }
    }

    // Запускаем после загрузки DOM
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPasswordToggle);
    } else {
        initPasswordToggle();
    }

    // Также запускаем после AJAX загрузки (если используется)
    document.addEventListener('ajaxComplete', initPasswordToggle);
})();