import { beforeEach, describe, expect, it, vi } from 'vitest';
import { installJQuery, loadScript } from './test-utils.js';

describe('account-manager.js', () => {
    let $, options, table, multiselectOptions;
    beforeEach(async () => {
        $ = installJQuery();
        document.body.innerHTML = `
          <div class="js-entity-manager" data-bank-filter-select-all-text="All" data-bank-filter-non-selected-text="None" data-bank-filter-selected-text="selected" data-bank-filter-all-selected-text="All selected" data-select-all-value="*" data-separator-char="," data-edit-action="Edit" data-close-action="Close" data-delete-action="Delete" data-close-url="/close" data-close-title="Close title" data-close-confirmation="Sure?" data-closed="Closed">
            <table class="js-entity-table"><tbody><tr><td><button class="js-entity-close" data-id="a b"></button></td></tr></tbody></table>
          </div>
          <select class="js-account-state-filter"><option value="open" selected>Open</option></select>
          <select class="js-account-bank-filter" multiple><option value="1" selected>One</option><option value="2" selected>Two</option><option value="3">Three</option></select>`;
        $.fn.multiselect = function(arg) {
            if (typeof arg === 'object') {
                multiselectOptions=arg;
                this.data('multiselect', {options:arg});
                if (!this.next('div.btn-group').length)
                    this.after('<div class="btn-group"><div class="multiselect-container"><button class="multiselect-all"></button></div></div>');
            }
            return this;
        };
        globalThis.applyMultiSelectWidth = window.applyMultiSelectWidth = vi.fn();
        table={draw:vi.fn(), showConfirmation:vi.fn()};
        window.EntityManager={initialize:vi.fn(o=>{options=o;return table;})};
        await loadScript('account-manager.js');
    });

    it('configures every multiselect label case', () => {
        const bankSelect = document.querySelector('.js-account-bank-filter');
        const opts = [...bankSelect.querySelectorAll('option')];
        expect(multiselectOptions.buttonText([], bankSelect)).toBe('None');
        expect(multiselectOptions.buttonText([opts[0]], bankSelect)).toBe('One');
        expect(multiselectOptions.buttonText(opts.slice(0, 2), bankSelect)).toBe('2 selected');
        expect(multiselectOptions.buttonText(opts, bankSelect)).toBe('All selected');
        expect(applyMultiSelectWidth).toHaveBeenCalledWith($('.js-account-bank-filter'));
        expect($('.dropdown-divider')).toHaveLength(1);
    });

    it('adds all-bank and selected-bank ajax filters', () => {
        $('.js-account-bank-filter').val(['1','2','3']);
        let data=[]; options.ajaxData(data);
        expect(data).toEqual([{name:'isClosed',value:'open'},{name:'banksIds',value:'*'}]);

        $('.js-account-bank-filter').val(['1','3']);
        data=[]; options.ajaxData(data);
        expect(data[1]).toEqual({name:'banksIds',value:'1,3'});

        $('.js-account-bank-filter').val([]);
        data=[]; options.ajaxData(data);
        expect(data[1]).toEqual({name:'banksIds',value:''});
    });

    it('renders account state and every button state', () => {
        expect(options.columns[1].mRender('1')).toContain('fa-check');
        expect(options.columns[1].mRender('0')).toBe('');
        const render=options.columns[6].mRender;
        expect(render('')).toBe('');
        expect(render(null)).toBe('');
        expect(render('bad')).toBe('');
        expect(render('7¤0')).toContain('js-entity-close');
        expect(render('7¤0')).toContain('js-entity-edit');
        expect(render('7¤1')).not.toContain('js-entity-close');
        expect(render('7¤1')).not.toContain('js-entity-edit');
        expect(render('7¤1')).toContain('js-entity-delete');
    });

    it('redraws on filter changes and delegates close confirmation', () => {
        $('.js-account-state-filter').trigger('change');
        $('.js-account-bank-filter').trigger('change');
        expect(table.draw).toHaveBeenCalledTimes(2);
        $('.js-entity-close').trigger('click');
        expect(table.showConfirmation).toHaveBeenCalledWith('/close?id=a%20b','Close title','Sure?','Closed');
    });
});
