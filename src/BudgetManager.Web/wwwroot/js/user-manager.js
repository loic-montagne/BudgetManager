$(document).ready(function () {

    var page = $('.js-entity-manager');
    var tableElement = page.find('.js-entity-table');
    var stateFilter = $('.js-user-state-filter');
    var editModal = $('.js-entity-edit-modal');

    editModal.on('shown.bs.modal', function () {
        PhoneNumber.initialize(
            document.getElementById('phone-number')
        );
    });

    var table = EntityManager.initialize({
        defaultSorting: [[1, 'asc'], [2, 'asc']],
        ajaxData: function (data) {
            data.isActivated = stateFilter.val();
        },
        columns: [
            {
                "name": "Responsive",
                "className": "dtr-control align-middle",
                "searchable": false,
                "orderable": false
            },
            { "name": "LastName", "className": "align-middle text-start" },
            { "name": "FirstName", "className": "align-middle text-start" },
            { "name": "Email", "className": "align-middle text-start" },
            { "name": "Roles", "className": "align-middle text-start" },
            {
                "name": "Active",
                "className": "align-middle text-center",
                "render": function (data) {
                    if (data == '' || data == null)
                        return "";
                    if (data == '1')
                        return '<i class="fa-solid fa-check"></i>';
                    if (!data.includes('¤'))
                        return "";
                    var dataArray = data.split('¤');
                    if (dataArray[0] == '0')
                        return dataArray[1] + '<br/>' + dataArray[2];
                    return "";
                }
            },
            {
                "name": "Buttons",
                "className": "align-middle text-end text-nowrap",
                "searchable": false,
                "orderable": false,
                "width": "0",
                "render": function (data) {
                    if (data == '' || data == null || !data.includes('¤'))
                        return "";

                    var dataArray = data.split('¤');

                    var btns = '';
                    if (dataArray[1] == '0') {
                        btns += '<button type="button" title="' + page.data('send-activation-email-action') + '" class="btn btn-warning btn-sm js-entity-send-activation-email" style="margin-right:5px;" data-id="' + dataArray[0] + '"><i class="fa-regular fa-paper-plane"></i></button>';
                    }
                    btns += '<button type="button" title="' + page.data('edit-action') + '" class="btn btn-secondary btn-sm js-entity-edit" style="margin-right:5px;" data-id="' + dataArray[0] + '"><i class="fa-solid fa-pen-to-square"></i></button>';
                    btns += '<button type="button" title="' + page.data('delete-action') + '" class="btn btn-danger btn-sm js-entity-delete" data-id="' + dataArray[0] + '"><i class="fa-solid fa-trash-can"></i></button>';
                    return btns;
                }
            }
        ],
        columnDefs: [
            // responsivePriority : Plus le chiffre est petit, plus la colonne est prioritaire (elle disparait le plus tard)
            { "responsivePriority": 0, "targets": 0 },  // Responsive
            { "responsivePriority": 2, "targets": 1 },  // LastName
            { "responsivePriority": 3, "targets": 2 },  // FirstName
            { "responsivePriority": 0, "targets": 3 },  // Email
            { "responsivePriority": 4, "targets": 4 },  // Roles
            { "responsivePriority": 1, "targets": 5 },  // Active
            { "responsivePriority": 0, "targets": 6 },  // Buttons
        ]
    });

    tableElement.on('click', '.js-entity-send-activation-email', function (e) {
        e.preventDefault();

        var id = $(this).data('id');

        table.showConfirmation(
            page.data('send-activation-email-url') + '?id=' + encodeURIComponent(id),
            page.data('send-activation-email-title'),
            page.data('send-activation-email-confirmation'),
            page.data('activation-email-sent')
        );
    });

    stateFilter.on('change', function () {
        table.draw();
    });
});
