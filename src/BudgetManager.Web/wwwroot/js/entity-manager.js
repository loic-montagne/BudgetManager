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
                icon: 'error',
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

                var title = column.title
                    ? '<strong>' + column.title + ' : </strong>'
                    : '';

                return '<div class="text-start" style="margin-bottom:0;padding-top:7.5px;padding-bottom:7.5px;padding-right:8px;" ' +
                    'data-dt-row="' + column.rowIndex + '" data-dt-column="' + column.columnIndex + '">' +
                    title + '<p>' + column.data + '</p></div>';
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
            "order": options.defaultSorting,
            "processing": true,
            "serverSide": true,
            "ajax": {
                "url": page.data('list-url'),
                "data": function (data) {
                    if (options.ajaxData)
                        options.ajaxData(data);
                }
            },
            "ordering": true,
            "responsive": {
                details: {
                    display: $.fn.dataTable.Responsive.display.childRow,
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        return buildResponsiveDetails(columns);
                    }
                }
            },
            "columns": options.columns,
            "columnDefs": [
                ...(options.columnDefs || []),
                {
                    "targets": "_all",
                    "orderSequence": ["asc", "desc"]
                }
            ],
            "lengthMenu": [10, 20, 30, 40, 50, 60, 70, 80, 90, 100],
            "language": {
                "info": page.data('datatable-info'),
                "infoFiltered": page.data('datatable-info-filtered'),
                "infoEmpty": page.data('datatable-empty'),
                "lengthMenu": page.data('datatable-lengthmenu-show') +
                    ' _MENU_ ' +
                    page.data('datatable-lengthmenu-label'),
                "search": '',
                "emptyTable": page.data('datatable-empty'),
                "zeroRecords": page.data('datatable-zerorecord'),
                "paginate": {
                    "previous": page.data('datatable-previous'),
                    "next": page.data('datatable-next'),
                    "first": page.data('datatable-first'),
                    "last": page.data('datatable-last')
                },
                "processing": page.data('datatable-processing')
            }
        });

        var tableWrapper = tableElement.closest('.dt-container');
        var filter = tableWrapper.find('.dt-search');
        var length = tableWrapper.find('.dt-length');

        $('.search-zone').append(filter);
        filter.find('input').attr('placeholder', page.data('datatable-search-placeholder'));
        length.find('select').addClass('form-select form-select-sm input-inline');
        $('.length-zone').append(length);

        tableWrapper
            .children('.row.mt-2.justify-content-between')
            .not('.dt-layout-table')
            .filter(function () {
                return $(this).find('.dt-layout-start, .dt-layout-end')
                    .filter(function () {
                        return $(this).children().length > 0;
                    })
                    .length === 0;
            })
            .first()
            .remove();

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
                                icon: 'error',
                                title: errors.join('<br/>')
                            });
                        }
                        return;
                    }

                    table.draw();
                    Toast.fire({
                        icon: 'success',
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
                            icon: 'error',
                            title: errors.join('<br/>')
                        });
                        confirmModal.modal('hide');
                        return;
                    }

                    table.draw();

                    Toast.fire({
                        icon: 'success',
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
