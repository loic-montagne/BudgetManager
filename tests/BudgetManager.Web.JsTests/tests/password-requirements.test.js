import { beforeEach, describe, expect, it } from 'vitest';
import { loadScriptOnDomContentLoaded } from './test-utils.js';

function rule(name) {
    return document.querySelector(`[data-password-rule="${name}"]`);
}

describe('password-requirements.js', () => {
    beforeEach(() => {
        document.body.innerHTML = `
            <form>
                <input id="password" data-password-requirements value="">
                <ul class="password-requirements"
                    data-required-length="8"
                    data-required-unique-chars="4">
                    ${['length', 'uppercase', 'lowercase', 'digit', 'non-alphanumeric', 'unique']
                        .map(x => `<li data-password-rule="${x}"><i class="fa-regular fa-circle"></i></li>`)
                        .join('')}
                </ul>
            </form>`;
    });

    it('marks every requirement invalid initially', async () => {
        await loadScriptOnDomContentLoaded('password-requirements.js');

        document.querySelectorAll('[data-password-rule]').forEach(element => {
            expect(element.classList.contains('valid')).toBe(false);
            expect(element.querySelector('i').classList.contains('fa-circle')).toBe(true);
        });
    });

    it('updates all rules when the password becomes valid', async () => {
        await loadScriptOnDomContentLoaded('password-requirements.js');

        const input = document.getElementById('password');
        input.value = 'Abcd123!';
        input.dispatchEvent(new Event('input'));

        document.querySelectorAll('[data-password-rule]').forEach(element => {
            expect(element.classList.contains('valid')).toBe(true);
            const icon = element.querySelector('i');
            expect(icon.classList.contains('fa-solid')).toBe(true);
            expect(icon.classList.contains('fa-circle-check')).toBe(true);
            expect(icon.classList.contains('fa-regular')).toBe(false);
        });
    });

    it('evaluates the unique-character rule independently', async () => {
        await loadScriptOnDomContentLoaded('password-requirements.js');

        const input = document.getElementById('password');
        input.value = 'Aaaaaaa1!';
        input.dispatchEvent(new Event('input'));

        expect(rule('unique').classList.contains('valid')).toBe(true);
        expect(rule('length').classList.contains('valid')).toBe(true);
    });


    it('ignores an unknown password rule', async () => {
        document.querySelector('.password-requirements').insertAdjacentHTML(
            'beforeend',
            '<li data-password-rule="future-rule"><i class="fa-regular fa-circle"></i></li>'
        );

        await loadScriptOnDomContentLoaded('password-requirements.js');

        const unknown = rule('future-rule');
        expect(unknown.classList.contains('valid')).toBe(false);
        expect(unknown.querySelector('i').classList.contains('fa-circle')).toBe(true);
    });

    it('does nothing when the input is not inside a form', async () => {
        document.body.innerHTML = '<input data-password-requirements>';
        await expect(loadScriptOnDomContentLoaded('password-requirements.js')).resolves.toBeUndefined();
    });

    it('does nothing when the form has no requirements container', async () => {
        document.body.innerHTML = '<form><input data-password-requirements></form>';
        await expect(loadScriptOnDomContentLoaded('password-requirements.js')).resolves.toBeUndefined();
    });
});
