document.addEventListener('DOMContentLoaded', () => {
    const dropdowns = document.querySelectorAll('.header-capsule .dropdown');

    dropdowns.forEach(dropdown => {
        const toggleBtn = dropdown.querySelector('.dropdown__content');
        const menu = dropdown.querySelector('.dropdown__menu');
        if (!toggleBtn || !menu) return;

        // Toggle on click
        toggleBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            const isActive = dropdown.classList.contains('is-active');
            
            // Close other dropdowns
            dropdowns.forEach(d => d.classList.remove('is-active'));

            if (!isActive) {
                dropdown.classList.add('is-active');
            }
        });

        // Smart screen edge positioning
        const updatePosition = () => {
            menu.style.right = '0';
            const rect = menu.getBoundingClientRect();
            if (rect.left < 0) {
                menu.style.right = `${rect.left}px`;
            }
        };

        dropdown.addEventListener('mouseenter', updatePosition);
    });

    // Close when clicking outside
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.header-capsule .dropdown')) {
            dropdowns.forEach(d => d.classList.remove('is-active'));
        }
    });
});