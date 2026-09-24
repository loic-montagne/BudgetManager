const $body = $('body'),
      $sidebar = $('.sidebar'),
      $toggle = $('.toggle'),
      $openBtn = $('.sidebar-open-btn'),
      $closeBtn = $('.sidebar-close-btn'),
      $menus = $('.menu'),
      $modeSwitch = $('.toggle-switch'),
      $modeText = $('.mode-text'),
      $preferencesSave = $('.preferences-save'),
      $preferencesSaveButton = $('.preferences-save-button'),
      $preferencesSaveClose = $('.preferences-save-close'),
      $preferencesSaveError = $('.preferences-save-error'),
      darkModeText = $modeSwitch.data('dark-text'),
      lightModeText = $modeSwitch.data('light-text');

let preferredCulture = $sidebar.data('preferred-culture'),
    preferredTheme = $sidebar.data('preferred-theme-null') ? null : $sidebar.data('preferred-theme'),
    preferencesSaveDismissed = false;

const sidebarStateStorageKey = 'sidebar-state';
const sidebarMenuStorageKey = 'sidebar-menu';

const storedSidebarState = localStorage.getItem(sidebarStateStorageKey);

if (window.matchMedia && window.matchMedia('(max-width: 500px)').matches) {
    $sidebar.addClass('close');
    $openBtn.addClass('btn-visible');
    $closeBtn.removeClass('btn-visible');
}
else if (storedSidebarState === 'open') {
    $sidebar.removeClass('close');
    $openBtn.removeClass('btn-visible');
    $closeBtn.addClass('btn-visible');
}

function restoreOpenMenu() {
    const menuId = localStorage.getItem(sidebarMenuStorageKey);

    if (!menuId || $sidebar.hasClass('close')) {
        return;
    }

    const $menu = $menus.filter(function () {
        return $(this).data('menu-id') === menuId;
    });

    if ($menu.length > 0) {
        openCloseMenu($menu, true);
    }
}

$toggle.on('click', () => {
    $sidebar.toggleClass('close');

    localStorage.setItem(
        sidebarStateStorageKey,
        $sidebar.hasClass('close') ? 'closed' : 'open');

    if (!$sidebar.hasClass('close')) {
        restoreOpenMenu();
    }
});
$openBtn.on('click', () => {
    $sidebar.toggleClass('close');
    $openBtn.toggleClass('btn-visible');
    $closeBtn.toggleClass('btn-visible');

    localStorage.setItem(
        sidebarStateStorageKey,
        $sidebar.hasClass('close') ? 'closed' : 'open');

    if (!$sidebar.hasClass('close')) {
        restoreOpenMenu();
    }
});
$closeBtn.on('click', () => {
    $sidebar.toggleClass('close');
    $openBtn.toggleClass('btn-visible');
    $closeBtn.toggleClass('btn-visible');

    localStorage.setItem(
        sidebarStateStorageKey,
        $sidebar.hasClass('close') ? 'closed' : 'open');
});

function toggleMenu($element) {
    if (!$element.is('li')) {
        $element = $element.parents('li');
    }
    $element.toggleClass('show');
}
function openCloseMenu($element, open) {
    if (!$element.is('li')) {
        $element = $element.parents('li');
    }
    if (open && !$element.hasClass('show')) {
        $element.addClass('show');
    }
    else if (!open && $element.hasClass('show')) {
        $element.removeClass('show');
    }
}

$menus.on('click', function (e) {
    if ($sidebar.hasClass('close')) {
        return;
    }

    if ($(e.target).closest('.budget-add-link').length > 0) {
        return;
    }

    if ($(e.target).closest('.sub-menu').length > 0) {
        return;
    }

    const $menu = $(this);

    toggleMenu($menu);

    $menus.not($menu).each(function () {
        openCloseMenu($(this), false);
    });

    if ($menu.hasClass('show')) {
        localStorage.setItem(
            sidebarMenuStorageKey,
            $menu.data('menu-id'));
    }
    else {
        localStorage.removeItem(sidebarMenuStorageKey);
    }

    return;
});

restoreOpenMenu();

function resolvePreferredTheme(theme) {
    var normalizedTheme = typeof theme === 'string' ? theme.toLowerCase() : theme;

    if (normalizedTheme === 'dark' || normalizedTheme === 'light') {
        return normalizedTheme;
    }

    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return 'dark';
    }

    return 'light';
}

function initializeAuthenticatedTheme() {
    if (!$body.hasClass('authenticated')) {
        sessionStorage.removeItem('ui-preferences-user-id');
        return;
    }

    var userId = $sidebar.data('user-id');
    if (!userId || sessionStorage.getItem('ui-preferences-user-id') === userId) {
        return;
    }

    var normalizedTheme = typeof preferredTheme === 'string' ? preferredTheme.toLowerCase() : preferredTheme;
    if (normalizedTheme === 'dark' || normalizedTheme === 'light') {
        localStorage.setItem('theme', normalizedTheme);
    } else {
        localStorage.removeItem('theme');
    }

    sessionStorage.setItem('ui-preferences-user-id', userId);
}

function detectColorScheme() {
    var theme = 'light';

    if (localStorage.getItem('theme')) {
        if (localStorage.getItem('theme') == 'dark') {
            theme = 'dark';
        }
    } else if (!window.matchMedia) {
        return false;
    } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        theme = 'dark';
    }

    if (theme == 'dark') {
        $body.addClass('dark');
        $modeText.text(darkModeText);
    }
    else {
        $body.removeClass('dark');
        $modeText.text(lightModeText);
    }
}

function getCurrentTheme() {
    return $body.hasClass('dark') ? 'dark' : 'light';
}

function updatePreferencesSaveVisibility() {
    if (!$body.hasClass('authenticated')) {
        return;
    }

    var currentCulture = $sidebar.data('current-culture'),
        currentTheme = getCurrentTheme(),
        preferredConcreteTheme = resolvePreferredTheme(preferredTheme),
        hasChanges = currentCulture !== preferredCulture || currentTheme !== preferredConcreteTheme;

    $preferencesSave.prop('hidden', !hasChanges || preferencesSaveDismissed);

    if (!hasChanges) {
        $preferencesSaveError.prop('hidden', true);
    }
}

initializeAuthenticatedTheme();
detectColorScheme();
updatePreferencesSaveVisibility();

requestAnimationFrame(() => {
    $body.removeClass('theme-initializing');
});

function switchTheme() {
    preferencesSaveDismissed = false;
    $body.toggleClass("dark");
    if ($body.hasClass('dark')) {
        localStorage.setItem('theme', 'dark');
        $modeText.text(darkModeText);
    } else {
        localStorage.setItem('theme', 'light');
        $modeText.text(lightModeText);
    }

    updatePreferencesSaveVisibility();
}
$modeSwitch.on('click', () => {
    switchTheme();
});

function saveUiPreferences() {
    var currentCulture = $sidebar.data('current-culture'),
        currentTheme = getCurrentTheme(),
        preferredConcreteTheme = resolvePreferredTheme(preferredTheme),
        themeToSave = currentTheme === preferredConcreteTheme ? preferredTheme : currentTheme;

    $preferencesSaveButton.prop('disabled', true);
    $preferencesSaveError.prop('hidden', true);

    $.ajax({
        type: 'POST',
        url: $sidebar.data('update-preferences-url'),
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({
            preferredCulture: currentCulture,
            preferredTheme: themeToSave ?? null
        }),
        beforeSend: function (xhr) {
            xhr.setRequestHeader('XSRF-TOKEN', $('input:hidden[name="__RequestVerificationToken"]').val());
        }
    })
    .done(function () {
        preferredCulture = currentCulture;
        preferredTheme = themeToSave;
        $sidebar.data('preferred-culture', preferredCulture);
        $sidebar.data('preferred-theme', preferredTheme);
        updatePreferencesSaveVisibility();
    })
    .fail(function () {
        $preferencesSaveError.prop('hidden', false);
    })
    .always(function () {
        $preferencesSaveButton.prop('disabled', false);
    });
}
$preferencesSaveButton.on('click', () => {
    saveUiPreferences();
});
$preferencesSaveClose.on('click', () => {
    preferencesSaveDismissed = true;
    $preferencesSave.prop('hidden', true);
    $preferencesSaveError.prop('hidden', true);
});
