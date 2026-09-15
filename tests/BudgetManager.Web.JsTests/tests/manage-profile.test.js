import { beforeEach, describe, expect, it, vi } from 'vitest';
import { loadScriptOnDomContentLoaded } from './test-utils.js';

describe('manage-profile.js', () => {
    beforeEach(() => {
        document.body.innerHTML = `
            <form id="profile-form">
                <input id="profile-picture-input" type="file">
                <input id="phone-number" type="tel">
            </form>`;
        window.PhoneNumber = { initialize: vi.fn() };
    });

    it('initializes the phone field', async () => {
        await loadScriptOnDomContentLoaded('manage-profile.js');

        expect(window.PhoneNumber.initialize)
            .toHaveBeenCalledWith(document.getElementById('phone-number'));
    });

    it('submits the form when a file is selected', async () => {
        const form = document.getElementById('profile-form');
        const submit = vi.spyOn(form, 'submit').mockImplementation(() => {});
        const input = document.getElementById('profile-picture-input');
        Object.defineProperty(input, 'files', {
            configurable: true,
            value: [new File(['x'], 'avatar.png', { type: 'image/png' })]
        });

        await loadScriptOnDomContentLoaded('manage-profile.js');
        input.dispatchEvent(new Event('change'));

        expect(submit).toHaveBeenCalledOnce();
    });

    it('does not submit when no file is selected', async () => {
        const form = document.getElementById('profile-form');
        const submit = vi.spyOn(form, 'submit').mockImplementation(() => {});
        const input = document.getElementById('profile-picture-input');
        Object.defineProperty(input, 'files', {
            configurable: true,
            value: []
        });

        await loadScriptOnDomContentLoaded('manage-profile.js');
        input.dispatchEvent(new Event('change'));

        expect(submit).not.toHaveBeenCalled();
    });

    it('still initializes PhoneNumber when the picture input is absent', async () => {
        document.body.innerHTML = '<input id="phone-number" type="tel">';

        await loadScriptOnDomContentLoaded('manage-profile.js');

        expect(window.PhoneNumber.initialize)
            .toHaveBeenCalledWith(document.getElementById('phone-number'));
    });
});
