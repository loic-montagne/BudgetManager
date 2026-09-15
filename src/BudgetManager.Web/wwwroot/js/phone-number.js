(function (window) {
    'use strict';

    function format(value) {
        var trimmedValue = value.trim();

        if (!trimmedValue)
            return '';

        var compactValue = trimmedValue.replace(/[\s.\-()]/g, '');

        if (/^0[1-9]\d{8}$/.test(compactValue)) {
            return compactValue.replace(
                /^(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})$/,
                '$1 $2 $3 $4 $5'
            );
        }

        if (/^\+33[1-9]\d{8}$/.test(compactValue)) {
            return compactValue.replace(
                /^\+33(\d)(\d{2})(\d{2})(\d{2})(\d{2})$/,
                '+33 $1 $2 $3 $4 $5'
            );
        }

        if (/^0033[1-9]\d{8}$/.test(compactValue)) {
            return compactValue.replace(
                /^0033(\d)(\d{2})(\d{2})(\d{2})(\d{2})$/,
                '0033 $1 $2 $3 $4 $5'
            );
        }

        return trimmedValue;
    }

    function initialize(element) {
        if (!element)
            return;

        // Évite d'enregistrer plusieurs fois le même événement,
        // notamment si une modale est réutilisée.
        if (element.dataset.phoneNumberInitialized === 'true')
            return;

        element.dataset.phoneNumberInitialized = 'true';

        element.addEventListener('blur', function () {
            element.value = format(element.value);
        });

        if (element.value)
            element.value = format(element.value);
    }

    window.PhoneNumber = {
        format: format,
        initialize: initialize
    };
})(window);