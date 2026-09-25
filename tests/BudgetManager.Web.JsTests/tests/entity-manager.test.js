import { beforeEach, describe, expect, it, vi } from 'vitest';
import { installJQuery, loadScript } from './test-utils.js';

const flush = () => new Promise(resolve => setTimeout(resolve, 0));

describe('entity-manager.js', () => {
    let $, table, config;

    beforeEach(async () => {
        $ = await installJQuery();

        document.body.innerHTML = `
        <div class="js-entity-manager"
             data-list-url="/list"
             data-add-get-url="/add"
             data-add-post-url="/add-post"
             data-edit-get-url="/edit"
             data-edit-post-url="/edit-post"
             data-delete-url="/delete"
             data-add-title="Add"
             data-edit-title="Edit"
             data-delete-title="Delete"
             data-delete-confirmation="Sure"
             data-deleted="Deleted"
             data-saved="Saved"
             data-error="Error"
             data-datatable-search-placeholder="Search"
             data-datatable-lengthmenu-show="Show"
             data-datatable-lengthmenu-label="items">
          <div class="dt-container">
            <div class="row mt-2 justify-content-between js-controls-row">
              <div class="dt-layout-start">
                <div class="dt-length"><select></select></div>
              </div>
              <div class="dt-layout-end">
                <div class="dt-search"><input></div>
              </div>
            </div>
            <div class="row mt-2 justify-content-between dt-layout-table">
              <table class="js-entity-table">
                <tbody>
                  <tr>
                    <td>
                      <button class="js-entity-edit" data-id="a b"></button>
                      <button class="js-entity-delete" data-id="9"></button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="row mt-2 justify-content-between js-footer-row">
              <div class="dt-info">Info</div>
              <div class="dt-paging">Paging</div>
            </div>
          </div>
        </div>
        <button class="js-entity-add"></button>
        <div class="search-zone"></div>
        <div class="length-zone"></div>
        <div class="js-entity-edit-modal">
          <h5 class="modal-title"></h5>
          <div class="js-entity-edit-content"></div>
          <form class="js-entity-form">
            <input name="Name">
            <input name="Locked" disabled>
            <textarea name="Notes"></textarea>
            <div class="invalid-feedback"></div>
          </form>
          <button class="js-entity-save"></button>
          <button class="js-entity-cancel"></button>
        </div>
        <div class="js-entity-confirm-modal">
          <h5 class="modal-title"></h5>
          <div class="modal-body"></div>
          <button class="js-entity-confirm"></button>
          <button class="js-entity-confirm-cancel" data-bs-dismiss="modal"></button>
        </div>`;

        table = { draw: vi.fn() };

        $.fn.DataTable = function (c) {
            config = c;
            return table;
        };

        $.fn.modal = function (action) {
            this.data('modalAction', action);
            return this;
        };

        $.fn.dataTable = {
            Responsive: {
                display: {
                    childRow: vi.fn()
                }
            }
        };

        globalThis.Toast = window.Toast = { fire: vi.fn() };
        globalThis.validateForm = window.validateForm = vi.fn(() => []);
        globalThis.textToHtml = window.textToHtml = s => `html:${s}`;
        globalThis.axios = window.axios = {
            get: vi.fn(),
            post: vi.fn()
        };

        await loadScript('entity-manager.js');
    });

    function initialize(overrides = {}) {
        return window.EntityManager.initialize({
            defaultSorting: [[1, 'asc']],
            columns: [{ name: 'Name' }],
            columnDefs: [],
            ...overrides
        });
    }

    it('initializes DataTables, moves controls and exposes confirmation', () => {
        const result = initialize();

        expect(result).toBe(table);
        expect(config.order).toEqual([[1, 'asc']]);
        expect(config.ajax.url).toBe('/list');

        expect(config.lengthMenu).toEqual([
            10, 20, 30, 40, 50, 60, 70, 80, 90, 100
        ]);

        expect(config.language.lengthMenu).toBe('Show _MENU_ items');

        expect(config.columnDefs).toEqual([
            {
                targets: '_all',
                orderSequence: ['asc', 'desc']
            }
        ]);

        expect($('.search-zone input').attr('placeholder')).toBe('Search');
        expect($('.length-zone select').hasClass('form-select')).toBe(true);
        expect(typeof table.showConfirmation).toBe('function');
    });

    it('removes the empty DataTables controls row after moving search and length controls', () => {
        expect($('.js-controls-row')).toHaveLength(1);
        expect($('.dt-layout-table')).toHaveLength(1);
        expect($('.js-footer-row')).toHaveLength(1);

        initialize();

        expect($('.js-controls-row')).toHaveLength(0);

        expect($('.search-zone .dt-search')).toHaveLength(1);
        expect($('.length-zone .dt-length')).toHaveLength(1);

        expect($('.dt-layout-table')).toHaveLength(1);
        expect($('.js-footer-row')).toHaveLength(1);
    });

    it('preserves custom column definitions and adds the two-state order sequence', () => {
        const customColumnDef = {
            targets: 0,
            responsivePriority: 1
        };

        initialize({
            columnDefs: [customColumnDef]
        });

        expect(config.columnDefs).toEqual([
            customColumnDef,
            {
                targets: '_all',
                orderSequence: ['asc', 'desc']
            }
        ]);
    });

    it('forwards optional ajax data and renders only meaningful responsive details', () => {
        const ajaxData = vi.fn();

        initialize({ ajaxData });

        const data = {};
        config.ajax.data(data);

        expect(ajaxData).toHaveBeenCalledWith(data);

        const renderer = config.responsive.details.renderer;

        const details = renderer(null, 0, [
            {
                hidden: true,
                data: 'Value',
                rowIndex: 1,
                columnIndex: 2,
                title: 'Label'
            },
            {
                hidden: false,
                data: 'Visible'
            },
            {
                hidden: true,
                data: null
            },
            {
                hidden: true,
                data: ''
            }
        ]);

        expect(details.html()).toContain('Label');
        expect(details.html()).toContain('Value');

        expect(
            renderer(null, 0, [{ hidden: false, data: 'Value' }])
        ).toBe(false);
    });

    it('does not render a colon when a hidden responsive column has no title', () => {
        initialize();

        const renderer = config.responsive.details.renderer;

        const details = renderer(null, 0, [
            {
                hidden: true,
                data: '<button>Edit</button>',
                rowIndex: 1,
                columnIndex: 4,
                title: ''
            }
        ]);

        expect(details).not.toBe(false);
        expect(details.html()).toContain('<button>Edit</button>');
        expect(details.html()).not.toContain('<strong>');
        expect(details.html()).not.toContain(' : ');
    });

    it('loads add and edit forms and opens delete confirmation', async () => {
        axios.get.mockResolvedValueOnce({
            data: '<input name="Added">'
        });

        initialize();

        $('.js-entity-add').trigger('click');
        await flush();

        expect(axios.get).toHaveBeenCalledWith('/add');
        expect($('.js-entity-edit-modal .modal-title').html()).toBe('Add');
        expect($('.js-entity-edit-modal').data('modalAction')).toBe('show');

        axios.get.mockResolvedValueOnce({
            data: '<input name="Edited">'
        });

        $('.js-entity-edit').trigger('click');
        await flush();

        expect(axios.get).toHaveBeenLastCalledWith('/edit?id=a%20b');
        expect($('.js-entity-edit-modal .modal-title').html()).toBe('Edit');

        $('.js-entity-delete').trigger('click');

        expect($('.js-entity-confirm-modal .modal-title').html()).toBe('Delete');
        expect($('.js-entity-confirm-modal .modal-body').html()).toBe('Sure');
    });

    it('reports add and edit loading errors', async () => {
        const error = new Error('load');
        const log = vi.spyOn(console, 'log').mockImplementation(() => { });

        axios.get.mockRejectedValue(error);

        initialize();

        $('.js-entity-add').trigger('click');
        await flush();

        $('.js-entity-edit').trigger('click');
        await flush();

        expect(Toast.fire).toHaveBeenCalledTimes(2);
        expect(Toast.fire).toHaveBeenLastCalledWith({
            icon: 'error',
            title: 'Error'
        });

        expect(log).toHaveBeenCalledWith(error);

        log.mockRestore();
    });

    it('saves successfully, restores disabled inputs and redraws the table', async () => {
        axios.post.mockResolvedValue({
            data: { success: true }
        });

        initialize();

        $('.js-entity-save').trigger('click');

        expect($('.js-entity-save').prop('disabled')).toBe(true);

        await flush();

        expect(axios.post).toHaveBeenCalledWith(
            '/add-post',
            expect.stringContaining('Name=')
        );

        expect($('[name="Locked"]').prop('disabled')).toBe(true);
        expect(table.draw).toHaveBeenCalledOnce();

        expect(Toast.fire).toHaveBeenCalledWith({
            icon: 'success',
            title: 'Saved'
        });

        expect($('.js-entity-edit-modal').data('modalAction')).toBe('hide');
    });

    it('uses the edit post url after an entity has been selected', async () => {
        axios.get.mockResolvedValue({
            data: 'Edited'
        });

        axios.post.mockResolvedValue({
            data: { success: true }
        });

        initialize();

        $('.js-entity-edit').trigger('click');
        await flush();

        $('.js-entity-save').trigger('click');
        await flush();

        expect(axios.post.mock.calls[0][0]).toBe('/edit-post');
    });

    it('shows server and unmatched validation errors without closing the editor', async () => {
        validateForm.mockReturnValue(['Missing']);

        axios.post.mockResolvedValue({
            data: {
                success: false,
                error: 'No',
                invalidControls: [
                    { name: 'X', text: 'Bad' }
                ]
            }
        });

        initialize();

        $('.js-entity-save').trigger('click');
        await flush();

        expect(validateForm).toHaveBeenCalled();

        expect(Toast.fire).toHaveBeenCalledWith({
            icon: 'error',
            title: 'No<br/>html:Missing'
        });

        expect(table.draw).not.toHaveBeenCalled();
    });

    it('does not show an empty validation toast', async () => {
        axios.post.mockResolvedValue({
            data: {
                success: false,
                invalidControls: []
            }
        });

        initialize();

        $('.js-entity-save').trigger('click');
        await flush();

        expect(Toast.fire).not.toHaveBeenCalled();
    });

    it('restores the form and shows a generic error when saving fails', async () => {
        const error = new Error('save');
        const log = vi.spyOn(console, 'log').mockImplementation(() => { });

        axios.post.mockRejectedValue(error);

        initialize();

        $('.js-entity-save').trigger('click');
        await flush();

        expect($('.js-entity-save').prop('disabled')).toBe(false);
        expect($('[name="Locked"]').prop('disabled')).toBe(true);

        expect(Toast.fire).toHaveBeenCalledWith({
            icon: 'error',
            title: 'Error'
        });

        log.mockRestore();
    });

    it('submits on Enter except for textarea, disabled save and other keys', () => {
        axios.post.mockResolvedValue({
            data: { success: true }
        });

        initialize();

        $('[name="Name"]').trigger(
            $.Event('keydown', { key: 'x' })
        );

        $('[name="Notes"]').trigger(
            $.Event('keydown', { key: 'Enter' })
        );

        $('.js-entity-save').prop('disabled', true);

        $('[name="Name"]').trigger(
            $.Event('keydown', { key: 'Enter' })
        );

        expect(axios.post).not.toHaveBeenCalled();

        $('.js-entity-save').prop('disabled', false);

        $('[name="Name"]').trigger(
            $.Event('keydown', { key: 'Enter' })
        );

        expect(axios.post).toHaveBeenCalledOnce();
    });

    it('cancels editing and clears the modal content', () => {
        initialize();

        $('.js-entity-edit-content').html('content');
        $('.js-entity-cancel').trigger('click');

        expect($('.js-entity-edit-content').html()).toBe('');
        expect($('.js-entity-edit-modal').data('modalAction')).toBe('hide');
    });

    it('hides confirmation immediately when no confirmation is pending', () => {
        initialize();

        $('.js-entity-confirm').trigger('click');

        expect(axios.post).not.toHaveBeenCalled();
        expect($('.js-entity-confirm-modal').data('modalAction')).toBe('hide');
    });

    it('posts successful confirmations, redraws and resets confirmation state', async () => {
        axios.post.mockResolvedValue({
            data: { success: true }
        });

        initialize();

        table.showConfirmation('/custom', 'T', 'C', 'Done');

        $('.js-entity-confirm').trigger('click');
        await flush();

        expect(axios.post).toHaveBeenCalledWith('/custom');
        expect(table.draw).toHaveBeenCalledOnce();

        expect(Toast.fire).toHaveBeenCalledWith({
            icon: 'success',
            title: 'Done'
        });

        expect($('.js-entity-confirm').prop('disabled')).toBe(false);
        expect($('.js-entity-confirm-cancel').prop('disabled')).toBe(false);

        axios.post.mockClear();

        $('.js-entity-confirm').trigger('click');

        expect(axios.post).not.toHaveBeenCalled();
    });

    it('reports all confirmation validation errors', async () => {
        axios.post.mockResolvedValue({
            data: {
                success: false,
                error: 'No',
                invalidControls: [
                    { name: 'X', text: 'Bad' },
                    { name: 'Y', text: 'Worse' }
                ]
            }
        });

        initialize();

        table.showConfirmation('/custom', 'T', 'C', 'Done');

        $('.js-entity-confirm').trigger('click');
        await flush();

        expect(Toast.fire).toHaveBeenCalledWith({
            icon: 'error',
            title: 'No<br/>html:Bad<br/>html:Worse'
        });

        expect($('.js-entity-confirm-modal').data('modalAction')).toBe('hide');
    });

    it('reports confirmation transport errors and always re-enables buttons', async () => {
        const error = new Error('confirm');
        const log = vi.spyOn(console, 'log').mockImplementation(() => { });

        axios.post.mockRejectedValue(error);

        initialize();

        table.showConfirmation('/custom', 'T', 'C', 'Done');

        $('.js-entity-confirm').trigger('click');
        await flush();

        expect(Toast.fire).toHaveBeenCalledWith({
            icon: 'error',
            title: 'Error'
        });

        expect($('.js-entity-confirm').prop('disabled')).toBe(false);
        expect($('.js-entity-confirm-cancel').prop('disabled')).toBe(false);

        log.mockRestore();
    });

    it('clears a pending confirmation when cancel is clicked', () => {
        initialize();

        table.showConfirmation('/custom', 'T', 'C', 'Done');

        $('.js-entity-confirm-cancel').trigger('click');
        $('.js-entity-confirm').trigger('click');

        expect(axios.post).not.toHaveBeenCalled();
        expect($('.js-entity-confirm-modal').data('modalAction')).toBe('hide');
    });

    it('focuses the first visible enabled input when the edit modal is shown', () => {
        initialize();

        const originalVisible = $.expr.pseudos.visible;
        $.expr.pseudos.visible = () => true;

        const focus = vi.spyOn($('[name="Name"]')[0], 'focus');

        $('.js-entity-edit-modal').trigger('shown');

        expect(focus).toHaveBeenCalledOnce();

        $.expr.pseudos.visible = originalVisible;
    });
});