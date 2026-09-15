import { beforeEach, describe, expect, it, vi } from 'vitest';
import { deferredAjax, installJQuery, loadScript } from './test-utils.js';

function markup({ bodyClass='authenticated theme-initializing', preferredTheme='light', preferredThemeNull=false, userId='u1', currentCulture='fr', preferredCulture='fr' }={}) { return `
<body class="${bodyClass}">
<input type="hidden" name="__RequestVerificationToken" value="token">
<aside class="sidebar" data-preferred-culture="${preferredCulture}" data-preferred-theme="${preferredTheme}" ${preferredThemeNull ? 'data-preferred-theme-null="true"' : ''} data-current-culture="${currentCulture}" data-user-id="${userId}" data-update-preferences-url="/prefs">
 <button class="toggle"></button><button class="sidebar-open-btn"></button><button class="sidebar-close-btn"></button>
 <ul><li class="menu"><span class="menu-target">One</span></li><li class="menu"><span class="menu-target-2">Two</span></li></ul>
 <button class="toggle-switch" data-dark-text="Dark" data-light-text="Light"></button><span class="mode-text"></span>
</aside>
<div class="preferences-save" hidden><button class="preferences-save-button"></button><button class="preferences-save-close"></button><div class="preferences-save-error" hidden></div></div>
</body>`; }

describe('sidebar.js', () => {
 beforeEach(() => {
   installJQuery(); document.body.outerHTML=markup().trim();
   localStorage.clear(); sessionStorage.clear();
   window.matchMedia=vi.fn(()=>({matches:false}));
   globalThis.requestAnimationFrame=window.requestAnimationFrame=cb=>{cb();return 1;};
 });

 it('initializes theme and sidebar controls', async () => {
   await loadScript('sidebar.js');
   expect(document.body.classList.contains('theme-initializing')).toBe(false);
   expect($('.mode-text').text()).toBe('Light');
   $('.toggle').trigger('click'); expect($('.sidebar').hasClass('close')).toBe(true);
   $('.sidebar').removeClass('close'); $('.menu-target').trigger('click');
   expect($('.menu').eq(0).hasClass('show')).toBe(true);
   $('.menu-target-2').trigger('click');
   expect($('.menu').eq(0).hasClass('show')).toBe(false);
   expect($('.menu').eq(1).hasClass('show')).toBe(true);
 });

 it('does not toggle menus while the sidebar is closed', async () => {
   await loadScript('sidebar.js'); $('.sidebar').addClass('close');
   $('.menu-target').trigger('click');
   expect($('.menu').eq(0).hasClass('show')).toBe(false);
 });

 it('opens and closes the responsive sidebar controls', async () => {
   await loadScript('sidebar.js');
   $('.sidebar-open-btn').trigger('click');
   expect($('.sidebar').hasClass('close')).toBe(true);
   expect($('.sidebar-open-btn').hasClass('btn-visible')).toBe(true);
   expect($('.sidebar-close-btn').hasClass('btn-visible')).toBe(true);
   $('.sidebar-close-btn').trigger('click');
   expect($('.sidebar').hasClass('close')).toBe(false);
   expect($('.sidebar-open-btn').hasClass('btn-visible')).toBe(false);
 });

 it('switches theme in both directions and exposes the preference save prompt', async () => {
   await loadScript('sidebar.js');
   $('.toggle-switch').trigger('click');
   expect(document.body.classList.contains('dark')).toBe(true);
   expect(localStorage.getItem('theme')).toBe('dark');
   expect($('.mode-text').text()).toBe('Dark');
   expect($('.preferences-save').prop('hidden')).toBe(false);
   $('.toggle-switch').trigger('click');
   expect(document.body.classList.contains('dark')).toBe(false);
   expect(localStorage.getItem('theme')).toBe('light');
   expect($('.mode-text').text()).toBe('Light');
   expect($('.preferences-save').prop('hidden')).toBe(true);
 });

 it('uses the system dark theme when no concrete preference exists', async () => {
   document.body.outerHTML=markup({preferredThemeNull:true}).trim();
   window.matchMedia=vi.fn(()=>({matches:true}));
   await loadScript('sidebar.js');
   expect(document.body.classList.contains('dark')).toBe(true);
   expect($('.mode-text').text()).toBe('Dark');
 });

 it('falls back to light when matchMedia is unavailable', async () => {
   document.body.outerHTML=markup({preferredThemeNull:true}).trim();
   window.matchMedia=undefined;
   await loadScript('sidebar.js');
   expect(document.body.classList.contains('dark')).toBe(false);
 });

 it('initializes local storage from an authenticated user preference only once', async () => {
   document.body.outerHTML=markup({preferredTheme:'dark'}).trim();
   await loadScript('sidebar.js');
   expect(localStorage.getItem('theme')).toBe('dark');
   expect(sessionStorage.getItem('ui-preferences-user-id')).toBe('u1');
 });

 it('clears remembered user id for unauthenticated pages', async () => {
   sessionStorage.setItem('ui-preferences-user-id','old');
   document.body.outerHTML=markup({bodyClass:'theme-initializing'}).trim();
   await loadScript('sidebar.js');
   expect(sessionStorage.getItem('ui-preferences-user-id')).toBeNull();
   expect($('.preferences-save').prop('hidden')).toBe(true);
 });

 it('does not overwrite local storage when the same user is already initialized', async () => {
   sessionStorage.setItem('ui-preferences-user-id','u1'); localStorage.setItem('theme','light');
   document.body.outerHTML=markup({preferredTheme:'dark'}).trim();
   await loadScript('sidebar.js');
   expect(localStorage.getItem('theme')).toBe('light');
 });

 it('removes local theme when the stored preference follows the system', async () => {
   localStorage.setItem('theme','dark'); document.body.outerHTML=markup({preferredThemeNull:true}).trim();
   await loadScript('sidebar.js');
   expect(localStorage.getItem('theme')).toBeNull();
 });

 it('saves preferences with antiforgery token', async () => {
   const chain=deferredAjax(); $.ajax=vi.fn(()=>chain.promise);
   await loadScript('sidebar.js');
   $('.toggle-switch').trigger('click'); $('.preferences-save-button').trigger('click');
   const request=$.ajax.mock.calls[0][0];
   expect(request.url).toBe('/prefs');
   expect(JSON.parse(request.data)).toEqual({preferredCulture:'fr',preferredTheme:'dark'});
   const xhr={setRequestHeader:vi.fn()}; request.beforeSend(xhr);
   expect(xhr.setRequestHeader).toHaveBeenCalledWith('XSRF-TOKEN','token');
   chain.resolve();
   expect($('.preferences-save-button').prop('disabled')).toBe(false);
   expect($('.preferences-save').prop('hidden')).toBe(true);
 });

 it('preserves a null/system preference when the concrete theme did not change', async () => {
   document.body.outerHTML=markup({preferredThemeNull:true}).trim();
   const chain=deferredAjax(); $.ajax=vi.fn(()=>chain.promise);
   await loadScript('sidebar.js'); $('.preferences-save-button').trigger('click');
   expect(JSON.parse($.ajax.mock.calls[0][0].data).preferredTheme).toBeNull();
 });

 it('shows save error and allows dismissing the prompt', async () => {
   const chain=deferredAjax({succeed:false}); $.ajax=vi.fn(()=>chain.promise);
   await loadScript('sidebar.js'); $('.toggle-switch').trigger('click'); $('.preferences-save-button').trigger('click'); chain.resolve();
   expect($('.preferences-save-error').prop('hidden')).toBe(false);
   $('.preferences-save-close').trigger('click');
   expect($('.preferences-save').prop('hidden')).toBe(true);
   expect($('.preferences-save-error').prop('hidden')).toBe(true);
 });
});
