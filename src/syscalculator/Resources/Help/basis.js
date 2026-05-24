// Shared HelpApi basis script for packaged help content.
// Keep common WebView, DOM-ready and copy helpers here so focused help scripts
// stay small and package validation can keep a strict JavaScript allowlist.
window.syscalculatorHelp = window.syscalculatorHelp || {};
window.syscalculatorHelp.version = window.syscalculatorHelp.version || 2;

window.syscalculatorHelp.onReady = function (callback) {
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', callback);
        return;
    }

    callback();
};

window.syscalculatorHelp.eventTarget = function (event) {
    if (!event || !event.target) {
        return null;
    }

    return event.target instanceof Element ? event.target : event.target.parentElement;
};

window.syscalculatorHelp.closestFromEvent = function (event, selector) {
    const target = window.syscalculatorHelp.eventTarget(event);
    return target ? target.closest(selector) : null;
};

window.syscalculatorHelp.postWebView = function (message) {
    if (!window.chrome || !window.chrome.webview) {
        return false;
    }

    window.chrome.webview.postMessage(message);
    return true;
};

window.syscalculatorHelp.copyText = function (text) {
    if (window.syscalculatorHelp.postWebView(text)) {
        return true;
    }

    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text);
        return true;
    }

    const area = document.createElement('textarea');
    area.value = text;
    area.setAttribute('readonly', '');
    area.style.position = 'fixed';
    area.style.left = '-9999px';
    document.body.appendChild(area);
    area.select();
    const ok = document.execCommand('copy');
    area.remove();
    return ok;
};

window.syscalculatorHelp.installPreCopyButtons = function (options) {
    const copyLabel = options && options.copyLabel ? options.copyLabel : 'Copy';
    const copiedLabel = options && options.copiedLabel ? options.copiedLabel : 'Copied';

    function addCopyButtons() {
        document.querySelectorAll('pre').forEach(pre => {
            if (pre.closest('.code-copy-wrap')) {
                return;
            }

            const wrapper = document.createElement('div');
            wrapper.className = 'code-copy-wrap';
            pre.parentNode.insertBefore(wrapper, pre);
            wrapper.appendChild(pre);

            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'code-copy-button';
            button.title = copyLabel;
            button.setAttribute('aria-label', copyLabel);
            wrapper.appendChild(button);

            button.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                const text = pre.innerText.replace(/\n+$/g, '');
                if (!window.syscalculatorHelp.copyText(text)) {
                    return;
                }

                button.classList.add('copied');
                button.title = copiedLabel;
                window.setTimeout(() => {
                    button.classList.remove('copied');
                    button.title = copyLabel;
                }, 1200);
            });
        });
    }

    window.syscalculatorHelp.onReady(addCopyButtons);
};

window.syscalculatorHelp.installHeightReporter = function (messagePrefix) {
    function postHeight() {
        const body = document.body;
        const html = document.documentElement;
        const bodyTop = body ? body.getBoundingClientRect().top : 0;
        const childBottom = body
            ? Array.from(body.children).reduce((bottom, child) => {
                const rect = child.getBoundingClientRect();
                return Math.max(bottom, rect.bottom - bodyTop);
              }, 0)
            : 0;
        const height = Math.ceil(Math.max(
            body ? body.getBoundingClientRect().height : 0,
            body ? body.scrollHeight : 0,
            html ? html.scrollHeight : 0,
            childBottom
        ));
        window.syscalculatorHelp.postWebView(messagePrefix + height);
    }

    function observeBody() {
        if (window.ResizeObserver && document.body) {
            new ResizeObserver(postHeight).observe(document.body);
        }
    }

    window.syscalculatorHelp.onReady(() => {
        postHeight();
        observeBody();
    });
    window.addEventListener('load', postHeight);
    requestAnimationFrame(postHeight);
    setTimeout(postHeight, 80);
};
