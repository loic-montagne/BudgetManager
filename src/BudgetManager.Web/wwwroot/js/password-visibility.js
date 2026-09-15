document.addEventListener('DOMContentLoaded', function () {
    var showText = document.body.dataset.passwordShow;
    var hideText = document.body.dataset.passwordHide;

    document.querySelectorAll('input[type="password"]').forEach(function (input) {
        var container = input.closest('.form-floating');

        if (!container) {
            return;
        }

        input.classList.add('password-control');

        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'password-visibility-toggle';
        button.setAttribute('aria-label', showText);
        button.setAttribute('title', showText);

        var icon = document.createElement('i');
        icon.className = 'fa-solid fa-eye';
        icon.setAttribute('aria-hidden', 'true');

        button.appendChild(icon);
        container.appendChild(button);

        button.addEventListener('click', function () {
            var showPassword = input.type === 'password';

            input.type = showPassword ? 'text' : 'password';

            icon.classList.toggle('fa-eye', !showPassword);
            icon.classList.toggle('fa-eye-slash', showPassword);

            var text = showPassword ? hideText : showText;

            button.setAttribute('aria-label', text);
            button.setAttribute('title', text);
        });
    });
});