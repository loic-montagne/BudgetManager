$(document).ready(function () {

    function enableMultiSelect(page, select) {
        var multiselectOptions = {
            buttonClass: 'form-select form-select-sm input-inline',
            includeSelectAllOption: true,
            includeSelectAllIfMoreThan: 0,
            selectAllText: page.data('bank-filter-select-all-text'),
            selectAllNumber: false,
            nonSelectedText: page.data('bank-filter-non-selected-text'),
            nSelectedText: page.data('bank-filter-selected-text'),
            allSelectedText: page.data('bank-filter-all-selected-text'),
            disableIfEmpty: true,
            selectAllValue: page.data('select-all-value'),
            buttonTextAlignment: 'left',

            buttonText: function (options, select) {
                var totalOptions = $('option', select).length;

                if (options.length === 0)
                    return multiselectOptions.nonSelectedText;

                if (options.length === totalOptions)
                    return multiselectOptions.allSelectedText;

                if (options.length > 1)
                    return options.length + ' ' + multiselectOptions.nSelectedText;

                return $(options[0]).text();
            }
        };

        select.multiselect(multiselectOptions);
        select.multiselect('selectAll', false);
        select.next('div.btn-group').children('div.multiselect-container').eq(0).children('button.multiselect-all').eq(0).after($('<div class="dropdown-divider"></div>'));
        applyMultiSelectWidth(select);

        return multiselectOptions;
    }

    var page = $('.js-entity-manager');
    var tableElement = page.find('.js-entity-table');
    var stateFilter = $('.js-account-state-filter');
    var bankFilter = $('.js-account-bank-filter');
    var bankMultiselectOptions = enableMultiSelect(page, bankFilter);

    var table = EntityManager.initialize({
        defaultSorting: [[1, 'desc'], [2, 'asc']],
        ajaxData: function (data) {
            var selectedBanks = bankFilter.val() ?? [];
            var totalBanks = bankFilter.find('option').length;

            data.push({
                name: 'isClosed',
                value: stateFilter.val()
            });

            data.push({
                name: 'banksIds',
                value: selectedBanks.length === totalBanks && totalBanks > 0
                    ? bankMultiselectOptions.selectAllValue
                    : selectedBanks.join(page.data('separator-char'))
            });
        },
        columns: [
            {
                "sName": "Responsive",
                "className": "dtr-control align-middle",
                "bSearchable": false,
                "bSortable": false
            },
            {
                "sName": "Active",
                "className": "align-middle text-center",
                "mRender": function (data) {
                    if (data == '1')
                        return '<i class="fa-solid fa-check"></i>';
                    return '';
                }
            },
            { "sName": "Name", "className": "align-middle text-start" },
            { "sName": "Bank", "className": "align-middle text-start" },
            { "sName": "Iban", "className": "align-middle text-start" },
            { "sName": "Bic", "className": "align-middle text-center" },
            {
                "sName": "Buttons",
                "className": "align-middle text-end text-nowrap",
                "bSearchable": false,
                "bSortable": false,
                "sWidth": "0",
                "mRender": function (data) {
                    if (data == '' || data == null || !data.includes('¤'))
                        return "";

                    var dataArray = data.split('¤');

                    var btns = '';
                    if (dataArray[1] == '0') {
                        btns += '<button type="button" title="' + page.data('edit-action') + '" class="btn btn-secondary btn-sm js-entity-edit" style="margin-right:5px;" data-id="' + dataArray[0] + '"><i class="fa-solid fa-pen-to-square"></i></button>';
                        btns += '<button type="button" title="' + page.data('close-action') + '" class="btn btn-warning btn-sm js-entity-close" style="margin-right:5px;" data-id="' + dataArray[0] + '"><i class="fa-solid fa-square-xmark"></i></button>';
                    }
                    btns += '<button type="button" title="' + page.data('delete-action') + '" class="btn btn-danger btn-sm js-entity-delete" data-id="' + dataArray[0] + '"><i class="fa-solid fa-trash-can"></i></button>';
                    return btns;
                }
            }
        ],
        columnDefs: [
            // responsivePriority : Plus le chiffre est petit, plus la colonne est prioritaire (elle disparait le plus tard)
            { "responsivePriority": 0, "targets": 0 },  // Responsive
            { "responsivePriority": 0, "targets": 1 },  // Active
            { "responsivePriority": 0, "targets": 2 },  // Name
            { "responsivePriority": 1, "targets": 3 },  // Bank
            { "responsivePriority": 2, "targets": 4 },  // Iban
            { "responsivePriority": 3, "targets": 5 },  // Bic
            { "responsivePriority": 0, "targets": 6 },  // Buttons
        ]
    });

    tableElement.on('click', '.js-entity-close', function (e) {
        e.preventDefault();

        var id = $(this).data('id');

        table.showConfirmation(
            page.data('close-url') + '?id=' + encodeURIComponent(id),
            page.data('close-title'),
            page.data('close-confirmation'),
            page.data('closed')
        );
    });

    stateFilter.on('change', function () {
        table.draw();
    });

    bankFilter.on('change', function () {
        table.draw();
    });
});
