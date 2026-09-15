$(document).ready(function () {
    var page = $('.js-entity-manager');

    EntityManager.initialize({
        defaultSorting: [[1, 'asc']],
        columns: [
            {
                "sName": "Responsive",
                "className": "dtr-control align-middle",
                "bSearchable": false,
                "bSortable": false
            },
            { "sName": "Name", "className": "align-middle text-start" },
            { "sName": "Description", "className": "align-middle text-start" },
            { "sName": "BudgetsCount", "className": "align-middle text-center" },
            {
                "sName": "Buttons",
                "className": "align-middle text-end text-nowrap",
                "bSearchable": false,
                "bSortable": false,
                "sWidth": "0",
                "mRender": function (data) {
                    if (!data)
                        return '';

                    return '<button type="button" title="' + page.data('edit-action') + '" class="btn btn-secondary btn-sm js-entity-edit" style="margin-right:5px;" data-id="' + data + '"><i class="fa-solid fa-pen-to-square"></i></button>' +
                           '<button type="button" title="' + page.data('delete-action') + '" class="btn btn-danger btn-sm js-entity-delete" data-id="' + data + '"><i class="fa-solid fa-trash-can"></i></button>';
                }
            }
        ],
        columnDefs: [
            // responsivePriority : Plus le chiffre est petit, plus la colonne est prioritaire (elle disparait le plus tard)
            { "responsivePriority": 0, "targets": 0 },  // Responsive
            { "responsivePriority": 0, "targets": 1 },  // Name
            { "responsivePriority": 3, "targets": 2 },  // Description
            { "responsivePriority": 1, "targets": 3 },  // BudgetsCount
            { "responsivePriority": 0, "targets": 4 }   // Buttons
        ]
    });
});
