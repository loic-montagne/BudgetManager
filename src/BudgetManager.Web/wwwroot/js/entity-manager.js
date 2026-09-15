(function (window, $) {
    'use strict';

    function initialize(options) {
        var page = $('.js-entity-manager');
        var tableElement = page.find('.js-entity-table');
        var addButton = $('.js-entity-add');
        var editModal = $('.js-entity-edit-modal');
        var editModalContent = editModal.find('.js-entity-edit-content');
        var form = editModal.find('.js-entity-form');
        var saveButton = editModal.find('.js-entity-save');
        var cancelButton = editModal.find('.js-entity-cancel');
        var confirmModal = $('.js-entity-confirm-modal');
        var confirmButton = confirmModal.find('.js-entity-confirm');
        var confirmCancelButton = confirmModal.find('.js-entity-confirm-cancel');
        var confirmUrl = '';
        var confirmSuccessMessage = '';
        var currentId = '';

        function showGenericError(error) {
            if (error)
                console.log(error);

            Toast.fire({
                type: 'error',
                title: page.data('error')
            });
        }

        function setFormEnabled(enabled, disabledInputs) {
            form.find(':input').prop('disabled', !enabled);

            if (enabled && disabledInputs)
                disabledInputs.prop('disabled', true);

            saveButton.prop('disabled', !enabled);
            cancelButton.prop('disabled', !enabled);
        }

        function clearEditModal() {
            currentId = '';
            editModalContent.empty();
        }

        function buildResponsiveDetails(columns) {
            var data = $.map(columns, function (column) {
                if (!column.hidden || column.data == null || column.data === '')
                    return '';

                return '<div class="text-start" style="margin-bottom:0;padding-top:7.5px;padding-bottom:7.5px;padding-right:8px;" ' +
                    'data-dt-row="' + column.rowIndex + '" data-dt-column="' + column.columnIndex + '">' +
                    '<strong>' + column.title + ' : </strong><p>' + column.data + '</p></div>';
            });

            var content = data.join('');

            return content
                ? $('<div style="height:100%;width:100%;"/>').append(content)
                : false;
        }

        function showConfirmation(url, title, confirmation, successMessage) {
            confirmUrl = url;
            confirmSuccessMessage = successMessage;

            confirmModal.find('.modal-title').html(title);
            confirmModal.find('.modal-body').html(confirmation);
            confirmModal.modal('show');
        }

        var table = tableElement.DataTable({
            "aaSorting": options.defaultSorting,
            "bProcessing": true,
            "bServerSide": true,
            "sAjaxSource": page.data('list-url'),
            "fnServerParams": function (aoData) {
                if (options.ajaxData)
                    options.ajaxData(aoData);
            },
            "bSortable": true,
            "responsive": {
                details: {
                    display: $.fn.dataTable.Responsive.display.childRow,
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        return buildResponsiveDetails(columns);
                    }
                }
            },
            "aoColumns": options.columns,
            "columnDefs": options.columnDefs,
            "oLanguage": {
                "sInfo": page.data('datatable-info'),
                "sInfoFiltered": page.data('datatable-info-filtered'),
                "sInfoEmpty": page.data('datatable-empty'),
                "sLengthMenu": page.data('datatable-lengthmenu-show') + ' <select>' +
                    '<option value="10">10</option>' +
                    '<option value="20">20</option>' +
                    '<option value="30">30</option>' +
                    '<option value="40">40</option>' +
                    '<option value="50">50</option>' +
                    '<option value="60">60</option>' +
                    '<option value="70">70</option>' +
                    '<option value="80">80</option>' +
                    '<option value="90">90</option>' +
                    '<option value="100">100</option>' +
                    '</select> ' + page.data('datatable-lengthmenu-label'),
                "sSearch": '',
                "sEmptyTable": page.data('datatable-empty'),
                "sZeroRecords": page.data('datatable-zerorecord'),
                "oPaginate": {
                    "sPrevious": page.data('datatable-previous'),
                    "sNext": page.data('datatable-next'),
                    "sFirst": page.data('datatable-first'),
                    "sLast": page.data('datatable-last')
                },
                "sProcessing": page.data('datatable-processing')
            }
        });

        var tableWrapper = tableElement.closest('.dataTables_wrapper');
        var filter = tableWrapper.find('.dataTables_filter');
        var length = tableWrapper.find('.dataTables_length');

        $('.search-zone').append(filter);
        filter.find('input').attr('placeholder', page.data('datatable-search-placeholder'));
        length.find('select').addClass('form-select form-select-sm input-inline');
        $('.length-zone').append(length);

        addButton.on('click', function (e) {
            e.preventDefault();

            axios.get(page.data('add-get-url'))
                .then(function (response) {
                    currentId = '';
                    editModal.find('.modal-title').html(page.data('add-title'));
                    editModalContent.empty().html(response.data);
                    editModal.modal('show');
                })
                .catch(showGenericError);
        });

        saveButton.on('click', function (e) {
            e.preventDefault();

            var formValues = form.serialize();
            var disabledInputs = form.find(':input:disabled');
            var postUrl = currentId
                ? page.data('edit-post-url')
                : page.data('add-post-url');

            setFormEnabled(false);

            axios.post(postUrl, formValues)
                .then(function (response) {
                    setFormEnabled(true, disabledInputs);

                    if (response.data.success === false) {
                        var unmatchedErrors = validateForm(form, response.data.invalidControls);
                        var errors = [];
                        if (response.data.error)
                            errors.push(response.data.error);
                        if (unmatchedErrors.length > 0)
                            errors.push(...unmatchedErrors.map(textToHtml));
                        if (errors.length > 0) {
                            Toast.fire({
                                type: 'error',
                                title: errors.join('<br/>')
                            });
                        }
                        return;
                    }

                    table.draw();
                    Toast.fire({
                        type: 'success',
                        title: page.data('saved')
                    });
                    editModal.modal('hide');
                    clearEditModal();
                })
                .catch(function (error) {
                    setFormEnabled(true, disabledInputs);
                    showGenericError(error);
                });
        });

        form.on('keydown', ':input', function (e) {
            if (e.key !== 'Enter')
                return;

            if ($(this).is('textarea'))
                return;

            e.preventDefault();

            if (!saveButton.prop('disabled'))
                saveButton.trigger('click');
        });

        cancelButton.on('click', function (e) {
            e.preventDefault();
            clearEditModal();
            editModal.modal('hide');
        });

        tableElement.on('click', '.js-entity-edit', function (e) {
            e.preventDefault();

            currentId = $(this).data('id');

            axios.get(page.data('edit-get-url') + '?id=' + encodeURIComponent(currentId))
                .then(function (response) {
                    editModal.find('.modal-title').html(page.data('edit-title'));
                    editModalContent.empty().html(response.data);
                    editModal.modal('show');
                })
                .catch(showGenericError);
        });

        tableElement.on('click', '.js-entity-delete', function (e) {
            e.preventDefault();

            var id = $(this).data('id');

            showConfirmation(
                page.data('delete-url') + '?id=' + encodeURIComponent(id),
                page.data('delete-title'),
                page.data('delete-confirmation'),
                page.data('deleted')
            );
        });

        confirmButton.on('click', function (e) {
            e.preventDefault();

            if (!confirmUrl) {
                confirmModal.modal('hide');
                return;
            }

            var cancelButton = confirmModal.find('[data-bs-dismiss="modal"]');

            confirmButton.prop('disabled', true);
            cancelButton.prop('disabled', true);

            axios.post(confirmUrl)
                .then(function (response) {
                    if (response.data.success === false) {
                        var errors = [];
                        if (response.data.error)
                            errors.push(response.data.error);
                        if (response.data.invalidControls) {
                            response.data.invalidControls.forEach(function (invalidControl) {
                                errors.push(textToHtml(invalidControl.text));
                            });
                        }
                        Toast.fire({
                            type: 'error',
                            title: errors.join('<br/>')
                        });
                        confirmModal.modal('hide');
                        return;
                    }

                    table.draw();

                    Toast.fire({
                        type: 'success',
                        title: confirmSuccessMessage
                    });

                    confirmUrl = '';
                    confirmSuccessMessage = '';
                    confirmModal.modal('hide');
                })
                .catch(showGenericError)
                .finally(function () {
                    confirmButton.prop('disabled', false);
                    cancelButton.prop('disabled', false);
                });
        });

        confirmCancelButton.on('click', function (e) {
            e.preventDefault();

            confirmUrl = '';
            confirmSuccessMessage = '';
            confirmModal.modal('hide');
        });

        editModal.on('shown.bs.modal', function () {
            $('input:visible:enabled:first', this).focus();
        });

        table.showConfirmation = showConfirmation;

        return table;
    }

    window.EntityManager = {
        initialize: initialize
    };
})(window, jQuery);
