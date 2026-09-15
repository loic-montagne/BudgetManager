(function () {
    const container = document.querySelector('.user-profile-menu-container');
    if (!container) {
        return;
    }

    const toggle = container.querySelector('.user-profile-menu-toggle');
    const menu = container.querySelector('.user-profile-menu');

    if (!toggle || !menu) {
        return;
    }

    function closeMenu(restoreFocus) {
        if (menu.hidden) {
            return;
        }

        menu.hidden = true;
        toggle.setAttribute('aria-expanded', 'false');

        if (restoreFocus) {
            toggle.focus();
        }
    }

    toggle.addEventListener('click', function () {
        const willOpen = menu.hidden;
        menu.hidden = !willOpen;
        toggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
    });

    document.addEventListener('click', function (event) {
        if (!container.contains(event.target)) {
            closeMenu(false);
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeMenu(true);
        }
    });
})();
