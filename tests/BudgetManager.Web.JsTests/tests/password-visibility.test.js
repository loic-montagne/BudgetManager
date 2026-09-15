import { beforeEach, describe, expect, it } from 'vitest';
import { loadScriptOnDomContentLoaded } from './test-utils.js';

describe('password-visibility.js', () => {
    beforeEach(async () => {
        document.body.dataset.passwordShow = 'Show password';
        document.body.dataset.passwordHide = 'Hide password';
        document.body.innerHTML = `
            <div class="form-floating">
                <input id="password" type="password">
            </div>
            <div>
                <input id="outside" type="password">
            </div>`;
        await loadScriptOnDomContentLoaded('password-visibility.js');
    });

    it('adds a visibility button only to password fields in form-floating', async () => {
        const input = document.getElementById('password');
        const button = input.parentElement.querySelector('.password-visibility-toggle');

        expect(input.classList.contains('password-control')).toBe(true);
        expect(button).not.toBeNull();
        expect(button.type).toBe('button');
        expect(button.getAttribute('aria-label')).toBe('Show password');
        expect(button.getAttribute('title')).toBe('Show password');
        expect(document.getElementById('outside').parentElement.querySelector('button')).toBeNull();
    });

    it('toggles the password, icon and accessible text', async () => {
        const input = document.getElementById('password');
        const button = input.parentElement.querySelector('.password-visibility-toggle');
        const icon = button.querySelector('i');

        button.click();

        expect(input.type).toBe('text');
        expect(icon.classList.contains('fa-eye-slash')).toBe(true);
        expect(icon.classList.contains('fa-eye')).toBe(false);
        expect(button.getAttribute('aria-label')).toBe('Hide password');
        expect(button.getAttribute('title')).toBe('Hide password');

        button.click();

        expect(input.type).toBe('password');
        expect(icon.classList.contains('fa-eye')).toBe(true);
        expect(button.getAttribute('aria-label')).toBe('Show password');
    });
});
