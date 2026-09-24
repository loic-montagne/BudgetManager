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

$toggle.on('click', () => {
    $sidebar.toggleClass('close');
});
$openBtn.on('click', () => {
    $sidebar.toggleClass('close');
    $openBtn.toggleClass('btn-visible');
    $closeBtn.toggleClass('btn-visible');
});
$closeBtn.on('click', () => {
    $sidebar.toggleClass('close');
    $openBtn.toggleClass('btn-visible');
    $closeBtn.toggleClass('btn-visible');
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
function getIndex($element) {
    if (!$element.is('li')) {
        $element = $element.parents('li');
    }
    return $element.data('index');
}

for (var i = 0; i < $menus.length; i++) {
    $menus[i].dataset.index = i;
}
$menus.on('click', function (e) {
    if ($sidebar.hasClass('close')) {
        return;
    }

    if ($(e.target).closest('.sub-menu').length > 0) {
        return;
    }

    toggleMenu($(e.target));

    // Fermeture des autres menus
    var index = getIndex($(e.target));
    for (var j = 0; j < $menus.length; j++) {
        if (index != j) {
            openCloseMenu($($menus[j]), false);
        }
    }
    return;
});

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
