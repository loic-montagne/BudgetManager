import { beforeEach, describe, expect, it, vi } from 'vitest';
import { loadScript } from './test-utils.js';

describe('phone-number.js', () => {
    beforeEach(async () => {
        document.body.innerHTML = '';
        delete window.PhoneNumber;
        await loadScript('phone-number.js');
    });

    it.each([
        ['0612345678', '06 12 34 56 78'],
        ['06.12-34 (56) 78', '06 12 34 56 78'],
        ['+33612345678', '+33 6 12 34 56 78'],
        ['0033612345678', '0033 6 12 34 56 78'],
        ['  0612345678  ', '06 12 34 56 78'],
        ['', ''],
        ['   ', '']
    ])('formats %s as %s', async (input, expected) => {
        expect(window.PhoneNumber.format(input)).toBe(expected);
    });

    it('keeps an unsupported value trimmed instead of altering it', async () => {
        expect(window.PhoneNumber.format('  +441234567890  ')).toBe('+441234567890');
        expect(window.PhoneNumber.format('0123')).toBe('0123');
    });

    it('formats an existing value when initialized', async () => {
        const input = document.createElement('input');
        input.value = '0612345678';

        window.PhoneNumber.initialize(input);

        expect(input.value).toBe('06 12 34 56 78');
        expect(input.dataset.phoneNumberInitialized).toBe('true');
    });

    it('formats the value on blur', async () => {
        const input = document.createElement('input');
        window.PhoneNumber.initialize(input);
        input.value = '+33612345678';

        input.dispatchEvent(new Event('blur'));

        expect(input.value).toBe('+33 6 12 34 56 78');
    });

    it('initializes an element only once', async () => {
        const input = document.createElement('input');
        const addEventListener = vi.spyOn(input, 'addEventListener');

        window.PhoneNumber.initialize(input);
        window.PhoneNumber.initialize(input);

        expect(addEventListener).toHaveBeenCalledTimes(1);
    });

    it('ignores a missing element', async () => {
        expect(() => window.PhoneNumber.initialize(null)).not.toThrow();
    });
});
