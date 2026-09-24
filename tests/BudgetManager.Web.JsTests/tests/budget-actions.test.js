import { beforeEach, describe, expect, it, vi } from 'vitest';
import { loadScript } from './test-utils.js';

function createMarkup() {
    return `
<div class="budget-actions" data-budget-actions>
    <div class="budget-action" data-action-priority="5">
        <button type="button" class="btn btn-primary btn-sm">
            <i class="fa-solid fa-plus"></i>
            <span class="budget-action-label">Ajouter une catégorie</span>
        </button>
    </div>

    <div class="budget-action" data-action-priority="4">
        <button type="button" class="btn btn-outline-primary btn-sm">
            <i class="fa-solid fa-lock"></i>
            <span class="budget-action-label">Verrouiller</span>
        </button>
    </div>

    <div class="budget-action" data-action-priority="3">
        <button type="button" class="btn btn-outline-primary btn-sm">
            <i class="fa-solid fa-users"></i>
            <span class="budget-action-label">Gérer les permissions</span>
        </button>
    </div>

    <div class="budget-action" data-action-priority="2">
        <button type="button" class="btn btn-outline-primary btn-sm">
            <i class="fa-solid fa-pen"></i>
            <span class="budget-action-label">Renommer</span>
        </button>
    </div>

    <div class="budget-action" data-action-priority="1">
        <button type="button" class="btn btn-danger btn-sm">
            <i class="fa-solid fa-trash"></i>
            <span class="budget-action-label">Supprimer</span>
        </button>
    </div>

    <div class="budget-actions-overflow dropdown" data-budget-actions-overflow hidden>
        <button type="button"
                class="btn btn-outline-primary btn-sm dropdown-toggle"
                data-bs-toggle="dropdown"
                aria-expanded="false">
            <i class="fa-solid fa-ellipsis-vertical"></i>
        </button>

        <ul class="dropdown-menu dropdown-menu-end"
            data-budget-actions-menu>
        </ul>
    </div>
</div>`;
}

function setDimensions(container, clientWidth) {
    let width = clientWidth;

    container.style.columnGap = '8px';

    Object.defineProperty(container, 'clientWidth', {
        configurable: true,
        get: () => width
    });

    container
        .querySelectorAll(':scope > .budget-action')
        .forEach(action => {
            Object.defineProperty(action, 'getBoundingClientRect', {
                configurable: true,
                value: () => ({
                    width: container.classList.contains(
                        'budget-actions-icons-only'
                    )
                        ? 40
                        : 100
                })
            });
        });

    const overflow = container.querySelector(
        '[data-budget-actions-overflow]'
    );

    if (overflow) {
        const button = overflow.querySelector('button');

        if (button) {
            Object.defineProperty(button, 'getBoundingClientRect', {
                configurable: true,
                value: () => ({
                    width: 40
                })
            });
        }
    }

    return {
        setWidth(value) {
            width = value;
        }
    };
}

describe('budget-actions.js', () => {
    beforeEach(() => {
        document.body.innerHTML = createMarkup();

        globalThis.requestAnimationFrame = window.requestAnimationFrame =
            callback => {
                callback();
                return 1;
            };
    });

    it('does nothing when no budget actions container exists', async () => {
        document.body.innerHTML = '';

        await loadScript('budget-actions.js');

        expect(document.body.innerHTML).toBe('');
    });

    it('keeps all actions with their labels when they fit', async () => {
        const container = document.querySelector('[data-budget-actions]');

        setDimensions(container, 600);

        await loadScript('budget-actions.js');

        expect(
            container.classList.contains('budget-actions-icons-only')
        ).toBe(false);

        expect(
            container.querySelectorAll(':scope > .budget-action')
        ).toHaveLength(5);

        expect(
            container.querySelector('[data-budget-actions-overflow]').hidden
        ).toBe(true);

        expect(
            container.querySelectorAll('.budget-action-label')
        ).toHaveLength(5);
    });

    it('switches to icon-only mode before using the overflow menu', async () => {
        const container = document.querySelector('[data-budget-actions]');

        // 5 icon buttons + gaps fit, but the labelled buttons do not.
        setDimensions(container, 240);

        await loadScript('budget-actions.js');

        expect(
            container.classList.contains('budget-actions-icons-only')
        ).toBe(true);

        expect(
            container.querySelectorAll(':scope > .budget-action')
        ).toHaveLength(5);

        expect(
            container.querySelector('[data-budget-actions-overflow]').hidden
        ).toBe(true);

        expect(
            container.querySelectorAll('[data-budget-actions-menu] > li')
        ).toHaveLength(0);
    });

    it('moves the lowest-priority actions to the overflow menu when icon-only mode is not enough', async () => {
        const container = document.querySelector('[data-budget-actions]');

        // 4 visible icons + overflow do not fit.
        // 3 visible icons + overflow do fit.
        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        const menuActions = Array.from(
            container.querySelectorAll(
                '[data-budget-actions-menu] .budget-action'
            )
        );

        expect(
            container.classList.contains('budget-actions-icons-only')
        ).toBe(true);

        expect(
            container.querySelector('[data-budget-actions-overflow]').hidden
        ).toBe(false);

        expect(
            menuActions.map(action => action.dataset.actionPriority)
        ).toEqual(['1', '2']);

        expect(
            Array.from(
                container.querySelectorAll(':scope > .budget-action')
            ).map(action => action.dataset.actionPriority)
        ).toEqual(['5', '4', '3']);
    });

    it('keeps the real action wrappers and buttons when moving them to overflow', async () => {
        const container = document.querySelector('[data-budget-actions]');
        const deleteAction = container.querySelector(
            '[data-action-priority="1"]'
        );
        const deleteButton = deleteAction.querySelector('button');
        const originalClassName = deleteButton.className;

        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        const menuAction = container.querySelector(
            '[data-budget-actions-menu] .budget-action[data-action-priority="1"]'
        );

        expect(menuAction).toBe(deleteAction);
        expect(menuAction.querySelector('button')).toBe(deleteButton);

        expect(deleteButton.className).toBe('dropdown-item');

        expect(
            container.querySelector(
                ':scope > .budget-action[data-action-priority="1"]'
            )
        ).toBeNull();

        expect(
            container.querySelector(
                '[data-budget-actions-menu] .budget-action[data-action-priority="1"]'
            )
        ).toBe(deleteAction);

        // The original button classes must be restored when the action
        // comes back to the toolbar.
        setDimensions(container, 600);
        window.dispatchEvent(new Event('resize'));

        expect(
            container.querySelector(
                ':scope > .budget-action[data-action-priority="1"]'
            )
        ).toBe(deleteAction);

        expect(deleteAction.querySelector('button')).toBe(deleteButton);
        expect(deleteButton.className).toBe(originalClassName);
    });

    it('preserves the original action order after restoring from overflow', async () => {
        const container = document.querySelector('[data-budget-actions]');

        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        expect(
            Array.from(
                container.querySelectorAll(
                    '[data-budget-actions-menu] .budget-action'
                )
            ).map(action => action.dataset.actionPriority)
        ).toEqual(['1', '2']);

        setDimensions(container, 600);
        window.dispatchEvent(new Event('resize'));

        expect(
            Array.from(
                container.querySelectorAll(':scope > .budget-action')
            ).map(action => action.dataset.actionPriority)
        ).toEqual(['5', '4', '3', '2', '1']);

        expect(
            container.querySelector('[data-budget-actions-overflow]').hidden
        ).toBe(true);

        expect(
            container.querySelectorAll('[data-budget-actions-menu] > li')
        ).toHaveLength(0);
    });

    it('recalculates the layout when the window is resized', async () => {
        const container = document.querySelector('[data-budget-actions]');

        const dimensions = setDimensions(container, 200);

        await loadScript('budget-actions.js');

        expect(
            container.querySelectorAll(
                '[data-budget-actions-menu] .budget-action'
            )
        ).toHaveLength(2);

        dimensions.setWidth(600);

        window.dispatchEvent(new Event('resize'));

        expect(
            container.querySelectorAll(
                '[data-budget-actions-menu] .budget-action'
            )
        ).toHaveLength(0);

        expect(
            container.querySelectorAll(':scope > .budget-action')
        ).toHaveLength(5);

        expect(
            container.classList.contains('budget-actions-icons-only')
        ).toBe(false);
    });

    it('does nothing when there are no actions', async () => {
        const container = document.querySelector('[data-budget-actions]');

        container
            .querySelectorAll(':scope > .budget-action')
            .forEach(action => action.remove());

        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        expect(
            container.classList.contains('budget-actions-icons-only')
        ).toBe(false);

        expect(
            container.querySelectorAll(':scope > .budget-action')
        ).toHaveLength(0);
    });

    it('handles an overflow container without a button', async () => {
        const container = document.querySelector('[data-budget-actions]');

        container
            .querySelector('[data-budget-actions-overflow] button')
            .remove();

        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        expect(
            container.querySelectorAll(':scope > .budget-action')
        ).toHaveLength(4);
    });

    it('handles a missing overflow container', async () => {
        const container = document.querySelector('[data-budget-actions]');

        container
            .querySelector('[data-budget-actions-overflow]')
            .remove();

        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        expect(
            container.querySelectorAll(':scope > .budget-action')
        ).toHaveLength(5);
    });

    it('handles a missing overflow menu', async () => {
        const container = document.querySelector('[data-budget-actions]');

        container
            .querySelector('[data-budget-actions-menu]')
            .remove();

        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        expect(
            container.querySelectorAll(':scope > .budget-action')
        ).toHaveLength(5);
    });

    it('ignores an action without a button when moving actions to overflow', async () => {
        const container = document.querySelector('[data-budget-actions]');
        const actionWithoutButton = container.querySelector(
            '[data-action-priority="1"]'
        );

        actionWithoutButton.querySelector('button').remove();

        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        expect(
            container.querySelector(
                '[data-budget-actions-menu] .budget-action[data-action-priority="1"]'
            )
        ).toBeNull();

        expect(
            container.querySelector(
                ':scope > .budget-action[data-action-priority="1"]'
            )
        ).toBe(actionWithoutButton);

        expect(
            container.querySelectorAll(
                '[data-budget-actions-menu] .budget-action'
            )
        ).toHaveLength(2);
    });

    it('removes invalid menu items when restoring actions', async () => {
        const container = document.querySelector('[data-budget-actions]');

        setDimensions(container, 200);

        await loadScript('budget-actions.js');

        const menu = container.querySelector('[data-budget-actions-menu]');

        const invalidItem = document.createElement('li');
        menu.appendChild(invalidItem);

        setDimensions(container, 600);
        window.dispatchEvent(new Event('resize'));

        expect(invalidItem.parentElement).toBeNull();

        expect(
            container.querySelectorAll(':scope > .budget-action')
        ).toHaveLength(5);
    });

    it('ignores a resize event while another resize is pending', async () => {
        const container = document.querySelector('[data-budget-actions]');

        setDimensions(container, 200);

        let animationFrameCallback;

        globalThis.requestAnimationFrame =
            window.requestAnimationFrame = callback => {
                animationFrameCallback = callback;
                return 1;
            };

        await loadScript('budget-actions.js');

        window.dispatchEvent(new Event('resize'));
        window.dispatchEvent(new Event('resize'));

        expect(animationFrameCallback).toBeDefined();

        animationFrameCallback();

        expect(
            container.querySelectorAll(
                '[data-budget-actions-menu] .budget-action'
            )
        ).toHaveLength(2);
    });

    it('keeps button event handlers when actions are moved to and restored from overflow', async () => {
        const container = document.querySelector('[data-budget-actions]');
        const deleteButton = container.querySelector(
            '[data-action-priority="1"] button'
        );

        const handler = vi.fn();

        deleteButton.addEventListener('click', handler);

        const dimensions = setDimensions(container, 200);

        await loadScript('budget-actions.js');

        deleteButton.click();

        expect(handler).toHaveBeenCalledTimes(1);

        dimensions.setWidth(600);

        window.dispatchEvent(new Event('resize'));

        deleteButton.click();

        expect(handler).toHaveBeenCalledTimes(2);
    });
});