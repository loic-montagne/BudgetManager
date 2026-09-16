import { beforeEach, describe, expect, it, vi } from 'vitest';
import { installJQuery, loadScript } from './test-utils.js';

describe('user-manager.js', () => {
    let options, table;
    beforeEach(async () => {
        installJQuery();
        document.body.innerHTML = `
          <div class="js-entity-manager" data-send-activation-email-url="/activate" data-send-activation-email-title="Title" data-send-activation-email-confirmation="Confirm" data-activation-email-sent="Sent" data-edit-action="Edit" data-delete-action="Delete" data-send-activation-email-action="Send">
            <table class="js-entity-table"><tbody><tr><td><button class="js-entity-send-activation-email" data-id="a b"></button></td></tr></tbody></table>
          </div>
          <select class="js-user-state-filter"><option value="yes" selected>yes</option></select>
          <div class="js-entity-edit-modal"></div><input id="phone-number">`;
        table = { draw: vi.fn(), showConfirmation: vi.fn() };
        window.EntityManager = { initialize: vi.fn(o => { options=o; return table; }) };
        window.PhoneNumber = { initialize: vi.fn() };
        await loadScript('user-manager.js');
    });

    it('adds activation state to ajax data and redraws when filter changes', () => {
        const data={}; options.ajaxData(data);
        expect(data).toEqual({isActivated:'yes'});
        $('.js-user-state-filter').trigger('change');
        expect(table.draw).toHaveBeenCalledOnce();
    });

    it('renders every activation-state branch', () => {
        const active=options.columns[5].render;
        expect(active('')).toBe('');
        expect(active(null)).toBe('');
        expect(active('1')).toContain('fa-check');
        expect(active('x')).toBe('');
        expect(active('0¤Pending¤Tomorrow')).toBe('Pending<br/>Tomorrow');
        expect(active('2¤Other¤Date')).toBe('');
    });

    it('renders every action-button branch', () => {
        const buttons=options.columns[6].render;
        expect(buttons('')).toBe('');
        expect(buttons(null)).toBe('');
        expect(buttons('bad')).toBe('');
        expect(buttons('7¤0')).toContain('js-entity-send-activation-email');
        expect(buttons('7¤0')).toContain('js-entity-edit');
        expect(buttons('7¤0')).toContain('js-entity-delete');
        expect(buttons('7¤1')).not.toContain('js-entity-send-activation-email');
        expect(buttons('7¤1')).toContain('js-entity-edit');
        expect(buttons('7¤1')).toContain('js-entity-delete');
    });

    it('initializes phone number on modal display and confirms activation email', () => {
        $('.js-entity-edit-modal').trigger('shown');
        expect(window.PhoneNumber.initialize).toHaveBeenCalledWith(document.getElementById('phone-number'));
        $('.js-entity-send-activation-email').trigger('click');
        expect(table.showConfirmation).toHaveBeenCalledWith('/activate?id=a%20b','Title','Confirm','Sent');
    });
});
