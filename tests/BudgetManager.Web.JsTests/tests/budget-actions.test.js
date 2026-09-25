import { beforeEach, describe, expect, it, vi } from 'vitest';
import { installJQuery, loadScript } from './test-utils.js';

function createMarkup() {
    return `
<div class="budget-actions"
     data-budget-actions
     data-budget-id="42"
     data-lock-url="/Budget/Lock"
     data-unlock-url="/Budget/Unlock">
    <div class="budget-action" data-action-priority="5">
        <button type="button" class="btn btn-primary btn-sm">
            <i class="fa-solid fa-plus"></i>
            <span class="budget-action-label">Ajouter une catégorie</span>
        </button>
    </div>

    <div class="budget-action" data-action-priority="4">
        <button type="button"
                class="btn btn-outline-primary btn-sm"
                data-budget-action="lock"
                data-budget-action-confirm
                data-budget-action-confirm-title="Verrouiller le budget"
                data-budget-action-confirmation="Êtes-vous sûr de vouloir verrouiller ce budget&nbsp;?">
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
</div>

<div class="modal fade js-budget-action-confirm-modal"
     aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header primary">
                <h5 class="modal-title"></h5>
                <button type="button"
                        class="btn-close"
                        data-bs-dismiss="modal"
                        aria-label="Close"></button>
            </div>

            <div class="modal-body"></div>

            <div class="modal-footer">
                <button type="button"
                        class="btn btn-danger js-budget-action-confirm">
                    Oui
                </button>

                <button type="button"
                        class="btn btn-secondary js-budget-action-confirm-cancel">
                    Non
                </button>
            </div>
        </div>
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
    beforeEach(async () => {
        document.body.innerHTML = createMarkup();

        await installJQuery();

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

    it('opens the confirmation modal for a confirmable budget action', async () => {
        const container = document.querySelector('[data-budget-actions]');
        const lockButton = container.querySelector(
            '[data-budget-action="lock"]'
        );

        setDimensions(container, 600);

        await loadScript('budget-actions.js');

        const modal = document.querySelector(
            '.js-budget-action-confirm-modal'
        );

        const shown = new Promise(resolve => {
            modal.addEventListener('shown.bs.modal', resolve, {
                once: true
            });
        });

        lockButton.click();

        await shown;

        expect(modal.classList.contains('show')).toBe(true);

        expect(
            modal.querySelector('.modal-title').textContent
        ).toBe('Verrouiller le budget');

        expect(
            modal.querySelector('.modal-body').textContent
        ).toBe('Êtes-vous sûr de vouloir verrouiller ce budget\u00A0?');
    });

    it('closes the confirmation modal without posting when the user cancels', async () => {
        const container = document.querySelector('[data-budget-actions]');
        const lockButton = container.querySelector(
            '[data-budget-action="lock"]'
        );
        const modal = document.querySelector(
            '.js-budget-action-confirm-modal'
        );
        const cancelButton = modal.querySelector(
            '.js-budget-action-confirm-cancel'
        );

        globalThis.axios = {
            post: vi.fn()
        };

        setDimensions(container, 600);

        await loadScript('budget-actions.js');

        const shown = new Promise(resolve => {
            modal.addEventListener('shown.bs.modal', resolve, {
                once: true
            });
        });

        lockButton.click();

        await shown;

        const hidden = new Promise(resolve => {
            modal.addEventListener('hidden.bs.modal', resolve, {
                once: true
            });
        });

        cancelButton.click();

        await hidden;

        expect(globalThis.axios.post).not.toHaveBeenCalled();
        expect(modal.classList.contains('show')).toBe(false);
    });

    it('posts the lock action when the user confirms', async () => {
        globalThis.axios = {
            post: vi.fn(() => {
                return Promise.resolve({
                    data: {
                        success: false
                    }
                });
            })
        };

        globalThis.Toast = {
            fire: vi.fn()
        };

        const container = document.querySelector('[data-budget-actions]');
        const lockButton = container.querySelector(
            '[data-budget-action="lock"]'
        );

        setDimensions(container, 600);

        await loadScript('budget-actions.js');

        const modal = document.querySelector(
            '.js-budget-action-confirm-modal'
        );

        const shown = new Promise(resolve => {
            modal.addEventListener('shown.bs.modal', resolve, {
                once: true
            });
        });

        lockButton.click();

        await shown;

        const confirmButton = modal.querySelector(
            '.js-budget-action-confirm'
        );
        const cancelButton = modal.querySelector(
            '.js-budget-action-confirm-cancel'
        );

        confirmButton.click();

        expect(confirmButton.disabled).toBe(true);
        expect(cancelButton.disabled).toBe(true);

        expect(globalThis.axios.post).toHaveBeenCalledTimes(1);
        expect(globalThis.axios.post).toHaveBeenCalledWith(
            '/Budget/Lock?id=42'
        );

        const hidden = new Promise(resolve => {
            modal.addEventListener('hidden.bs.modal', resolve, {
                once: true
            });
        });

        await hidden;

        expect(modal.classList.contains('show')).toBe(false);
    });

    it('re-enables the confirmation buttons after the modal is closed', async () => {
        const container = document.querySelector('[data-budget-actions]');
        const lockButton = container.querySelector(
            '[data-budget-action="lock"]'
        );
        const modal = document.querySelector(
            '.js-budget-action-confirm-modal'
        );
        const confirmButton = modal.querySelector(
            '.js-budget-action-confirm'
        );
        const cancelButton = modal.querySelector(
            '.js-budget-action-confirm-cancel'
        );

        setDimensions(container, 600);

        await loadScript('budget-actions.js');

        const shown = new Promise(resolve => {
            modal.addEventListener('shown.bs.modal', resolve, {
                once: true
            });
        });

        lockButton.click();

        await shown;

        confirmButton.disabled = true;
        cancelButton.disabled = true;

        const hidden = new Promise(resolve => {
            modal.addEventListener('hidden.bs.modal', resolve, {
                once: true
            });
        });

        $(modal).modal('hide');

        await hidden;

        expect(confirmButton.disabled).toBe(false);
        expect(cancelButton.disabled).toBe(false);
    });

    it('opens the confirmation modal for the unlock action', async () => {
        const container = document.querySelector('[data-budget-actions]');
        const unlockButton = container.querySelector(
            '[data-budget-action="lock"]'
        );

        unlockButton.dataset.budgetAction = 'unlock';
        unlockButton.dataset.budgetActionConfirmTitle =
            'Déverrouiller le budget';
        unlockButton.dataset.budgetActionConfirmation =
            'Êtes-vous sûr de vouloir déverrouiller ce budget&nbsp;?';

        setDimensions(container, 600);

        await loadScript('budget-actions.js');

        const modal = document.querySelector(
            '.js-budget-action-confirm-modal'
        );

        const shown = new Promise(resolve => {
            modal.addEventListener('shown.bs.modal', resolve, {
                once: true
            });
        });

        unlockButton.click();

        await shown;

        expect(modal.classList.contains('show')).toBe(true);

        expect(
            modal.querySelector('.modal-title').textContent
        ).toBe('Déverrouiller le budget');

        expect(
            modal.querySelector('.modal-body').textContent
        ).toBe('Êtes-vous sûr de vouloir déverrouiller ce budget\u00A0?');
    });

    it('posts the unlock action when the user confirms', async () => {
        globalThis.axios = {
            post: vi.fn(() => {
                return Promise.resolve({
                    data: {
                        success: false
                    }
                });
            })
        };

        globalThis.Toast = {
            fire: vi.fn()
        };

        const container = document.querySelector('[data-budget-actions]');
        const unlockButton = container.querySelector(
            '[data-budget-action="lock"]'
        );

        unlockButton.dataset.budgetAction = 'unlock';
        unlockButton.dataset.budgetActionConfirmTitle =
            'Déverrouiller le budget';
        unlockButton.dataset.budgetActionConfirmation =
            'Êtes-vous sûr de vouloir déverrouiller ce budget&nbsp;?';

        setDimensions(container, 600);

        await loadScript('budget-actions.js');

        const modal = document.querySelector(
            '.js-budget-action-confirm-modal'
        );

        const shown = new Promise(resolve => {
            modal.addEventListener('shown.bs.modal', resolve, {
                once: true
            });
        });

        unlockButton.click();

        await shown;

        const confirmButton = modal.querySelector(
            '.js-budget-action-confirm'
        );
        const cancelButton = modal.querySelector(
            '.js-budget-action-confirm-cancel'
        );

        confirmButton.click();

        expect(confirmButton.disabled).toBe(true);
        expect(cancelButton.disabled).toBe(true);

        expect(globalThis.axios.post).toHaveBeenCalledTimes(1);
        expect(globalThis.axios.post).toHaveBeenCalledWith(
            '/Budget/Unlock?id=42'
        );

        const hidden = new Promise(resolve => {
            modal.addEventListener('hidden.bs.modal', resolve, {
                once: true
            });
        });

        await hidden;

        expect(modal.classList.contains('show')).toBe(false);
    });

});