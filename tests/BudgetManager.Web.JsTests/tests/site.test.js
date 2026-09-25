import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { installJQuery, loadScript } from './test-utils.js';

describe('site.js', () => {
 let $, originalOuterWidth, originalOuterHeight;
 beforeEach(async () => {
   vi.useFakeTimers();
   $ = await installJQuery();
   originalOuterWidth=$.fn.outerWidth; originalOuterHeight=$.fn.outerHeight;
   document.body.innerHTML='<div class="main-content"></div><footer class="footer"></footer>';
   document.documentElement.style.setProperty('--scrollbar-size','17px');
   globalThis.Swal=window.Swal={mixin:vi.fn(()=>({fire:vi.fn()}))};
 });
 afterEach(()=>{ $.fn.outerWidth=originalOuterWidth; $.fn.outerHeight=originalOuterHeight; vi.useRealTimers(); });

 function dimensions({width=100,scrollWidth=100,height=100,scrollHeight=100}={}) {
   const main=document.querySelector('.main-content');
   Object.defineProperty(main,'scrollWidth',{configurable:true,get:()=>scrollWidth});
   Object.defineProperty(main,'clientWidth',{configurable:true,get:()=>width});
   Object.defineProperty(main,'scrollHeight',{configurable:true,get:()=>scrollHeight});
   Object.defineProperty(main,'clientHeight',{configurable:true,get:()=>height});
   return main;
 }

 it('defines scrollbar helpers and initializes Toast without scrollbars', async () => {
   dimensions(); await loadScript('site.js');
   expect($('.main-content').hasHorizontalScrollBar()).toBe(false);
   expect($('.main-content').hasVerticalScrollBar()).toBe(false);
   expect($('.footer').css('bottom')).toBe('0px');
   expect($('.footer').css('width')).toBe('100%');
   expect(Swal.mixin).toHaveBeenCalledWith(expect.objectContaining({toast:true,position:'top-end',timer:2000}));
 });

 it('positions the footer for horizontal and vertical scrollbars', async () => {
   dimensions({scrollWidth:200,scrollHeight:200}); await loadScript('site.js');
   expect($('.footer').css('bottom')).toBe('17px');
   expect($('.footer')[0].style.width).toBe('calc(100% - 17px)');
 });

 it('notifies only callable handlers when horizontal scrollbar state changes', async () => {
   let width=100; const main=document.querySelector('.main-content');
   Object.defineProperty(main,'scrollWidth',{configurable:true,get:()=>width});
   Object.defineProperty(main,'clientWidth',{configurable:true,value:100});
   Object.defineProperty(main,'scrollHeight',{configurable:true,value:100});
   Object.defineProperty(main,'clientHeight',{configurable:true,value:100});
   await loadScript('site.js');
   const changed=vi.fn(); $('.main-content').hasHorizontalScrollBarChanged(changed); $('.main-content').hasHorizontalScrollBarChanged('not-a-function');
   vi.advanceTimersByTime(100); expect(changed).not.toHaveBeenCalled();
   width=200; vi.advanceTimersByTime(100); expect(changed).toHaveBeenCalledOnce();
   vi.advanceTimersByTime(100); expect(changed).toHaveBeenCalledOnce();
 });

 it('notifies when vertical scrollbar state changes', async () => {
   let height=100; const main=document.querySelector('.main-content');
   Object.defineProperty(main,'scrollWidth',{configurable:true,value:100});
   Object.defineProperty(main,'clientWidth',{configurable:true,value:100});
   Object.defineProperty(main,'scrollHeight',{configurable:true,get:()=>height});
   Object.defineProperty(main,'clientHeight',{configurable:true,value:100});
   await loadScript('site.js');
   const changed=vi.fn(); $('.main-content').hasVerticalScrollBarChanged(changed);
   height=200; vi.advanceTimersByTime(100);
   expect(changed).toHaveBeenCalledOnce();
   expect($('.footer')[0].style.width).toBe('calc(100% - 17px)');
 });

 it('positions multi-row fixed table headers and row headers', async () => {
   document.body.innerHTML=`<div class="main-content" style="padding-top:10px">
     <table class="table-with-fixed-header">
       <thead><tr><th id="h1">H1</th><th id="h2">H2</th></tr><tr><th id="h3">H3</th></tr></thead>
       <tbody><tr><th data-w="30" rowspan="2">R1</th><th data-w="40">R2</th><td>A</td></tr><tr><th data-w="50">R3</th><td>B</td></tr></tbody>
     </table></div><footer class="footer"></footer>`;
   dimensions();
   $.fn.outerWidth=function(){return Number(this.attr('data-w')) || 20;};
   $.fn.outerHeight=function(){return 25;};
   await loadScript('site.js');
   expect($('#h1').css('left')).toBe('-10px');
   expect($('#h2').css('left')).toBe('20px');
   expect($('#h1').css('z-index')).toBe('11');
   expect($('#h3').css('top')).toBe('15px');
   expect($('.table-with-fixed-header tbody tr:first th').eq(1).css('left')).toBe('20px');
 });

 it('converts plain text to safe HTML while preserving line breaks and double spaces', async () => {
   dimensions();
   const { textToHtml } = await loadScript('site.js');
   expect(textToHtml('')).toBe('');
   expect(textToHtml(null)).toBe('');
   expect(textToHtml('<b>A</b>  B\r\nC\nD\rE')).toBe('&lt;b&gt;A&lt;/b&gt;&nbsp;&nbsp;B<br/>\r\nC<br/>\r\nD<br/>\r\nE');
 });

 it('clears previous validation errors and maps server errors to matching controls', async () => {
   document.body.innerHTML=`<div class="main-content"></div><footer class="footer"></footer>
     <form id="form"><input name="Name" class="is-invalid"><div class="invalid-feedback">Old</div>
     <input name="Description"><div class="invalid-feedback"></div></form>`;
   dimensions();
   const { validateForm } = await loadScript('site.js');
   const form=$('#form');
   expect(validateForm(form, null)).toEqual([]);
   expect(form.find('[name="Name"]').hasClass('is-invalid')).toBe(false);
   expect(form.find('[name="Name"] + div.invalid-feedback').html()).toBe('');
   const unmatched=validateForm(form,[{name:'Description',text:'Line 1\nLine 2'},{name:'Missing',text:'Missing <value>'}]);
   expect(unmatched).toEqual(['Missing <value>']);
   expect(form.find('[name="Description"]').hasClass('is-invalid')).toBe(true);
   expect(form.find('[name="Description"] + div.invalid-feedback').html()).toBe('Line 1<br>\nLine 2');
 });

 it('sizes a multiselect from plugin labels and option texts', async () => {
   document.body.innerHTML=`<div class="main-content"></div><footer class="footer"></footer>
     <select id="banks"><option>Short</option><option>Longest option</option></select>
     <div class="btn-group"><button class="multiselect"></button><div class="multiselect-container"></div></div>`;
   dimensions();
   const { applyMultiSelectWidth } = await loadScript('site.js');
   const select=$('#banks');
   select.data('multiselect',{options:{selectAllText:'All',nonSelectedText:'None',nSelectedText:null,allSelectedText:'Everything'}});
   $.fn.outerWidth=function(){ return this.is('span') ? this.text().length * 10 : originalOuterWidth.apply(this,arguments); };
   applyMultiSelectWidth(select);
   expect(select.next('.btn-group').find('> button.multiselect').css('width')).toBe('195px');
   expect(select.next('.btn-group').find('.multiselect-container').css('min-width')).toBe('195px');
   expect(document.body.querySelectorAll('span').length).toBe(0);
 });

});
