import { beforeEach, describe, expect, it, vi } from 'vitest';
import { installJQuery, loadScript } from './test-utils.js';

const cases = [
    ['bank-manager.js', ['Responsive','Name','Bic','AccountsCount','Buttons']],
    ['budget-category-manager.js', ['Responsive','Name','Description','BudgetsCount','Buttons']]
];

describe.each(cases)('%s', (fileName, names) => {
    beforeEach(async () => {
        await installJQuery();
        document.body.innerHTML = `<div class="js-entity-manager" data-edit-action="Edit" data-delete-action="Delete"></div>`;
        window.EntityManager = { initialize: vi.fn(() => ({})) };
    });

    it('initializes EntityManager with the expected columns and renders action buttons', async () => {
        await loadScript(fileName);
        const options = window.EntityManager.initialize.mock.calls[0][0];
        expect(options.defaultSorting).toEqual([[1, 'asc']]);
        expect(options.columns.map(c => c.name)).toEqual(names);
        const render = options.columns.at(-1).render;
        expect(render(null)).toBe('');
        expect(render('42')).toContain('js-entity-edit');
        expect(render('42')).toContain('js-entity-delete');
        expect(render('42')).toContain('data-id="42"');
    });
});
