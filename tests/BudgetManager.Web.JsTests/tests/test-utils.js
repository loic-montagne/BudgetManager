import $ from 'jquery';
import { vi } from 'vitest';

const scripts = import.meta.glob('../../../src/BudgetManager.Web/wwwroot/js/*.js');

function getScriptLoader(fileName) {
    const modulePath = `../../../src/BudgetManager.Web/wwwroot/js/${fileName}`;
    const loader = scripts[modulePath];

    if (!loader) {
        throw new Error(`Unknown JavaScript production file: ${fileName}`);
    }

    return loader;
}

export async function loadScript(fileName) {
    vi.resetModules();
    return await getScriptLoader(fileName)();
}

export async function loadScriptOnDomContentLoaded(fileName) {
    const originalAddEventListener = document.addEventListener.bind(document);

    document.addEventListener = function (type, listener, options) {
        if (type === 'DOMContentLoaded') {
            listener.call(document, new Event('DOMContentLoaded'));
            return;
        }

        return originalAddEventListener(type, listener, options);
    };

    try {
        await loadScript(fileName);
    } finally {
        document.addEventListener = originalAddEventListener;
    }
}

export function installJQuery() {
    // Les scripts de production initialisent leur comportement avec
    // $(document).ready(...). Sous jsdom, jQuery peut déclencher ready
    // de manière asynchrone après le retour de loadScript(), ce qui
    // peut faire déborder l'initialisation sur le test suivant.
    //
    // Seul ce hook de cycle de vie est rendu synchrone. Tout le reste
    // (sélecteurs, événements, data, traversal, Deferred, etc.) reste
    // fourni par le vrai jQuery.
    $.fn.ready = function (handler) {
        handler.call(document, $);
        return this;
    };

    window.$ = $;
    window.jQuery = $;
    globalThis.$ = $;
    globalThis.jQuery = $;

    return $;
}

export function deferredAjax({ succeed = true } = {}) {
    const deferred = $.Deferred();

    return {
        promise: deferred.promise(),
        resolve(value) {
            if (succeed)
                deferred.resolve(value);
            else
                deferred.reject(value);
        }
    };
}
