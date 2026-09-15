import { beforeEach, describe, expect, it, vi } from 'vitest';
import { loadScript } from './test-utils.js';

describe('user-profile-menu.js', () => {
    beforeEach(async () => {
        document.body.innerHTML = `
            <div class="user-profile-menu-container">
                <button class="user-profile-menu-toggle" aria-expanded="false">Profile</button>
                <div class="user-profile-menu" hidden><a href="#">Item</a></div>
            </div>
            <button id="outside">Outside</button>`;
        await loadScript('user-profile-menu.js');
    });

    it('opens and closes the menu from the toggle', async () => {
        const toggle = document.querySelector('.user-profile-menu-toggle');
        const menu = document.querySelector('.user-profile-menu');

        toggle.click();
        expect(menu.hidden).toBe(false);
        expect(toggle.getAttribute('aria-expanded')).toBe('true');

        toggle.click();
        expect(menu.hidden).toBe(true);
        expect(toggle.getAttribute('aria-expanded')).toBe('false');
    });

    it('closes the menu on an outside click without restoring focus', async () => {
        const toggle = document.querySelector('.user-profile-menu-toggle');
        const menu = document.querySelector('.user-profile-menu');
        const focus = vi.spyOn(toggle, 'focus');
        toggle.click();

        document.getElementById('outside').click();

        expect(menu.hidden).toBe(true);
        expect(toggle.getAttribute('aria-expanded')).toBe('false');
        expect(focus).not.toHaveBeenCalled();
    });

    it('closes the menu on Escape and restores focus', async () => {
        const toggle = document.querySelector('.user-profile-menu-toggle');
        const menu = document.querySelector('.user-profile-menu');
        const focus = vi.spyOn(toggle, 'focus');
        toggle.click();

        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

        expect(menu.hidden).toBe(true);
        expect(focus).toHaveBeenCalledOnce();
    });

    it('does not close on a click inside the menu container', async () => {
        const toggle = document.querySelector('.user-profile-menu-toggle');
        const menu = document.querySelector('.user-profile-menu');
        toggle.click();

        menu.querySelector('a').click();

        expect(menu.hidden).toBe(false);
    });


    it('does nothing when toggle or menu is missing', async () => {
        document.body.innerHTML = '<div class="user-profile-menu-container"><div class="user-profile-menu"></div></div>';
        await loadScript('user-profile-menu.js');

        document.body.innerHTML = '<div class="user-profile-menu-container"><button class="user-profile-menu-toggle"></button></div>';
        await loadScript('user-profile-menu.js');
    });

    it('does nothing when the menu container is absent', async () => {
        document.body.innerHTML = '';
        await loadScript('user-profile-menu.js');
    });
});
