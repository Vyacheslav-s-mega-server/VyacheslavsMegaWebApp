(function () {
    function initBurger() {
        const burger = document.querySelector('.header__burger');
        const menu = document.querySelector('.header__menu');
        const body = document.body;

        if (!burger || !menu) return;

        function closeMenu() {
            menu.classList.remove('active');
            burger.classList.remove('active');
            body.classList.remove('lock');
        }

        function toggleMenu(e) {
            e.stopPropagation();
            const isOpen = menu.classList.toggle('active');
            burger.classList.toggle('active', isOpen);
            body.classList.toggle('lock', isOpen);
        }

        burger.addEventListener('click', toggleMenu);

        menu.querySelectorAll('a').forEach(function (link) {
            link.addEventListener('click', function () {
                closeMenu();
            });
        });

        document.addEventListener('click', function (e) {
            if (menu.classList.contains('active') && !menu.contains(e.target) && !burger.contains(e.target)) {
                closeMenu();
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && menu.classList.contains('active')) {
                closeMenu();
            }
        });

        window.addEventListener('resize', function () {
            if (window.innerWidth > 992 && menu.classList.contains('active')) {
                closeMenu();
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initBurger);
    } else {
        initBurger();
    }
})();
