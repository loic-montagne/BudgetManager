/* Defining jquery functions for project */
(function ($) {
    $.fn.hasHorizontalScrollBar = function () {
        return this.get(0).scrollWidth > this.get(0).clientWidth;
    }
})(jQuery);
(function ($) {
    $.fn.hasHorizontalScrollBarChanged = function (handleFunction) {
        var $element = $(this);
        var last = $element.hasHorizontalScrollBar();
        setInterval(function () {
            if (last === $element.hasHorizontalScrollBar())
                return;
            if (typeof (handleFunction) == 'function') {
                handleFunction();
                last = $element.hasHorizontalScrollBar();
            }
        }, 100);
        return $element;
    }
})(jQuery);
(function ($) {
    $.fn.hasVerticalScrollBar = function () {
        return this.get(0).scrollHeight > this.get(0).clientHeight;
    }
})(jQuery);
(function ($) {
    $.fn.hasVerticalScrollBarChanged = function (handleFunction) {
        var $element = $(this);
        var last = $element.hasVerticalScrollBar();
        setInterval(function () {
            if (last === $element.hasVerticalScrollBar())
                return;
            if (typeof (handleFunction) == 'function') {
                handleFunction();
                last = $element.hasVerticalScrollBar();
            }
        }, 100);
        return $element;
    }
})(jQuery);

/* Changing footer position if horizontal scrollbar is visible */
function changeFooterPosition() {
    if ($('.main-content').hasHorizontalScrollBar()) {
        var scrollbarHeight = getComputedStyle(document.body).getPropertyValue("--scrollbar-size");
        $('footer.footer').css('bottom', scrollbarHeight);
    }
    else {
        $('footer.footer').css('bottom', '0px');
    }
}
changeFooterPosition();
$('.main-content').hasHorizontalScrollBarChanged(function () {
    changeFooterPosition();
});
/* Changing footer width if vertical scrollbar is visible */
function changeFooterWidth() {
    if ($('.main-content').hasVerticalScrollBar()) {
        var scrollbarWidth = getComputedStyle(document.body).getPropertyValue("--scrollbar-size");
        $('footer.footer').css('width', 'calc(100% - ' + scrollbarWidth + ')');
    }
    else {
        $('footer.footer').css('width', '100%');
    }
}
changeFooterWidth();
$('.main-content').hasVerticalScrollBarChanged(function () {
    changeFooterWidth();
});

/* Manage table with fixed header which contains several lines */
$(document).ready(function () {
    var mainContentPadding = parseInt($('div.main-content').css('padding-top'));
    $('.table-with-fixed-header').each(function () {

        // Row headers management
        var $rowTH = $(this).find('tbody').find('tr:first').find('th');
        $(this).find('tbody').find('tr').each(function () {
            var $firstTH = $(this).find('th:first');
            var $otherTH = $(this).find('th:not(:first)');
            $otherTH.each(function (index) {
                var lastTHWidth = $firstTH.outerWidth();
                for (var i = 0; i < index; i++) {
                    lastTHWidth += $($otherTH[i]).outerWidth();
                }
                $(this).css({
                    'left': (lastTHWidth - mainContentPadding) + 'px'
                });
            });
        });

        // Column headers management
        var $firstTR = $(this).find('thead').find('tr:first');
        if ($rowTH && $rowTH.length > 0) {
            $firstTR.find('th').each(function (index) {
                if (index < $rowTH.length) {
                    var lastTHWidth = 0;
                    for (var i = 0; i < index; i++) {
                        lastTHWidth += $($rowTH[i]).outerWidth();
                    }
                    $(this).css({
                        'left': (lastTHWidth - mainContentPadding) + 'px',
                        'z-index': '11'
                    });
                }
            });
        }

        var $otherTR = $(this).find('thead').find('tr:not(:first)');
        $otherTR.each(function (index) {
            var lastTRheight = $firstTR.outerHeight();
            for (var i = 1; i < index; i++) {
                lastTRheight += $($otherTR[i]).outerHeight();
            }
            $(this).find('th').each(function (thIndex) {
                if ($rowTH && $rowTH.length > 0) {
                    for (var i = 0; i < $rowTH.length; i++) {
                        if ($rowTH[i].rowSpan >= index - 1) {
                            thIndex++;
                        }
                    }
                    if (thIndex < $rowTH.length) {
                        var lastTHWidth = 0;
                        for (var i = 0; i < thIndex; i++) {
                            lastTHWidth += $($rowTH[i]).outerWidth();
                        }
                        $(this).css({
                            'left': (lastTHWidth - mainContentPadding) + 'px',
                            'z-index': '11'
                        });
                    }
                }

                $(this).css({
                    'top': (lastTRheight - mainContentPadding) + 'px'
                });
            });
        });
    });
});

/* Defining toast const for project */
const Toast = Swal.mixin({
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 2000,
    animation: false,
    customClass: {
        popup: 'animated pulse'
    }
});

/* Defining functions for project */
function textToHtml(text)
{
    if (text == '' || text == null)
        return "";
    text = $('<textarea/>').text(text).html();
    text = text.replaceAll("\r\n", "\r");
    text = text.replaceAll("\n", "\r");
    text = text.replaceAll("\r", "<br/>\r\n");
    text = text.replaceAll("  ", "&nbsp;&nbsp;");
    return text;
}

function validateForm(form, invalidControls) {
    form.find(':input').removeClass('is-invalid');
    form.find(':input + div.invalid-feedback').html('');

    var unmatchedErrors = [];

    if (!invalidControls)
        return unmatchedErrors;

    invalidControls.forEach(function (invalidControl) {
        var input = form.find('[name="' + invalidControl.name + '"]');

        if (input.length === 0) {
            unmatchedErrors.push(invalidControl.text);
            return;
        }

        input.addClass('is-invalid');
        input.next('div.invalid-feedback').html(
            textToHtml(invalidControl.text)
        );
    });

    return unmatchedErrors;
}

function applyMultiSelectWidth(select) {
    var multiselectGroup = select.next('.btn-group');
    var multiselectButton = multiselectGroup.find('> button.multiselect');
    var multiselectMenu = multiselectGroup.find('.multiselect-container');

    var measure = $('<span>')
        .css({
            position: 'absolute',
            visibility: 'hidden',
            whiteSpace: 'nowrap'
        })
        .appendTo('body');

    var multiselect = select.data('multiselect');
    var texts = [
        multiselect.options.selectAllText,
        multiselect.options.nonSelectedText,
        multiselect.options.nSelectedText,
        multiselect.options.allSelectedText
    ];

    select.find('option').each(function () {
        texts.push($(this).text());
    });

    var maxWidth = 0;

    texts.forEach(function (text) {
        measure.text(text ?? '');
        maxWidth = Math.max(maxWidth, measure.outerWidth());
    });

    measure.remove();

    maxWidth += 55;

    multiselectButton.css('width', maxWidth);
    multiselectMenu.css('min-width', maxWidth);
}