(function () {
    const containers = document.querySelectorAll('[data-budget-actions]');

    if (containers.length === 0) {
        return;
    }

    const states = new WeakMap();

    function postBudgetAction(container, action) {
        const budgetId = container.dataset.budgetId;
        const url = container.dataset[`${action}Url`];

        if (!budgetId || !url) {
            return;
        }

        axios.post(url + '?id=' + encodeURIComponent(budgetId))
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

                    return;
                }

                location.reload();
            })
            .catch(function () {
                location.reload();
            });
    }

    function initializeBudgetActions(container) {
        container.addEventListener('click', function (event) {
            const button = event.target.closest('[data-budget-action]');

            if (!button) {
                return;
            }

            event.preventDefault();

            postBudgetAction(container, button.dataset.budgetAction);
        });
    }

    function getActions(container) {
        return Array.from(container.querySelectorAll(':scope > .budget-action'));
    }

    function getOverflow(container) {
        return container.querySelector('[data-budget-actions-overflow]');
    }

    function getOverflowWidth(container) {
        const overflow = getOverflow(container);

        if (!overflow) {
            return 0;
        }

        const button = overflow.querySelector('button');

        if (!button) {
            return 0;
        }

        const wasHidden = overflow.hidden;

        if (wasHidden) {
            overflow.hidden = false;
        }

        const width = button.getBoundingClientRect().width;

        if (wasHidden) {
            overflow.hidden = true;
        }

        return width;
    }

    function getMenu(container) {
        return container.querySelector('[data-budget-actions-menu]');
    }

    function getPriority(action) {
        return Number.parseInt(action.dataset.actionPriority ?? '0', 10);
    }

    function isOverflowing(container, includeOverflow = false) {
        const actions = getActions(container);

        if (actions.length === 0) {
            return false;
        }

        const gap = Number.parseFloat(getComputedStyle(container).columnGap) || 0;

        const actionsWidth = actions.reduce((total, action) => {
            return total + action.getBoundingClientRect().width;
        }, 0);

        const gaps = Math.max(actions.length - 1, 0) * gap;

        let requiredWidth = actionsWidth + gaps;

        if (includeOverflow) {
            const overflowWidth = getOverflowWidth(container);

            if (overflowWidth > 0) {
                requiredWidth += gap + overflowWidth;
            }
        }

        return requiredWidth > container.clientWidth;
    }

    function getState(container) {
        let state = states.get(container);

        if (!state) {
            state = {
                actions: new Map(),
                buttonClasses: new WeakMap()
            };

            states.set(container, state);
        }

        return state;
    }

    function rememberActions(container) {
        const state = getState(container);

        getActions(container).forEach((action, index) => {
            if (!state.actions.has(action)) {
                state.actions.set(action, index);
            }

            const button = action.querySelector('button');

            if (button && !state.buttonClasses.has(button)) {
                state.buttonClasses.set(button, button.className);
            }
        });
    }

    function restoreActions(container) {
        const state = getState(container);
        const menu = getMenu(container);
        const overflow = getOverflow(container);

        if (!menu || !overflow) {
            return;
        }

        const menuItems = Array.from(menu.querySelectorAll(':scope > li'));

        menuItems.forEach(item => {
            const action = item.querySelector(':scope > .budget-action');

            if (!action) {
                item.remove();
                return;
            }

            const button = action.querySelector('button');

            if (button) {
                const originalClassName = state.buttonClasses.get(button);

                if (originalClassName !== undefined) {
                    button.className = originalClassName;
                }
            }

            item.remove();
        });

        const actions = Array.from(state.actions.keys())
            .sort((a, b) => state.actions.get(a) - state.actions.get(b));

        actions.forEach(action => {
            if (action.parentElement !== container) {
                container.insertBefore(action, overflow);
            }
        });
    }

    function resetState(container) {
        restoreActions(container);

        container.classList.remove('budget-actions-icons-only');

        getActions(container).forEach(action => {
            const label = action.querySelector('.budget-action-label');

            if (label) {
                label.style.removeProperty('display');
            }
        });

        const overflow = getOverflow(container);

        if (overflow) {
            overflow.hidden = true;
        }
    }

    function moveToOverflow(container, action) {
        const state = getState(container);
        const menu = getMenu(container);
        const overflow = getOverflow(container);
        const button = action.querySelector('button');

        if (!menu || !overflow || !button) {
            return;
        }

        const item = document.createElement('li');
        item.appendChild(action);

        const originalClassName = state.buttonClasses.get(button);

        if (originalClassName !== undefined) {
            button.className = 'dropdown-item';
        }

        menu.appendChild(item);
        overflow.hidden = false;
    }

    function apply(container) {
        rememberActions(container);
        resetState(container);

        const actions = getActions(container);

        if (actions.length === 0 || !isOverflowing(container)) {
            return;
        }

        container.classList.add('budget-actions-icons-only');

        if (!isOverflowing(container)) {
            return;
        }

        const overflow = getOverflow(container);
        const menu = getMenu(container);

        if (!overflow || !menu) {
            return;
        }

        const sortedActions = [...actions]
            .sort((a, b) => getPriority(a) - getPriority(b));

        for (const action of sortedActions) {
            moveToOverflow(container, action);

            if (!isOverflowing(container, true)) {
                break;
            }
        }

        if (menu.children.length === 0) {
            overflow.hidden = true;
        }
    }

    function scheduleApply(container) {
        window.requestAnimationFrame(() => {
            apply(container);
        });
    }

    containers.forEach(container => {
        initializeBudgetActions(container);
        scheduleApply(container);
    });

    let resizePending = false;

    window.addEventListener('resize', () => {
        if (resizePending) {
            return;
        }

        resizePending = true;
        window.requestAnimationFrame(() => {
            resizePending = false;
            containers.forEach(container => {
                apply(container);
            });
        });
    });
})();