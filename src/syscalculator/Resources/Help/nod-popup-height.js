function postHeight() {
    if (!window.chrome || !window.chrome.webview) {
        return;
    }

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
    window.chrome.webview.postMessage('nodhelp-height:' + height);
}

function observeBody() {
    if (window.ResizeObserver && document.body) {
        new ResizeObserver(postHeight).observe(document.body);
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        postHeight();
        observeBody();
    });
} else {
    postHeight();
    observeBody();
}
window.addEventListener('load', postHeight);
requestAnimationFrame(postHeight);
setTimeout(postHeight, 80);
