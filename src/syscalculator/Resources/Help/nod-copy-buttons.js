(() => {
    const copyLabel = '[menu.edit.copy]';
    const copiedLabel = '[help.copy.copied]';

    function copyText(text) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(text);
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
    }

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
                if (!copyText(text)) {
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

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', addCopyButtons);
    } else {
        addCopyButtons();
    }
})();
