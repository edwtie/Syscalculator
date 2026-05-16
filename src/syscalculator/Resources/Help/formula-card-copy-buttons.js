document.addEventListener('click', event => {
    const target = event.target instanceof Element ? event.target : event.target.parentElement;
    const button = target ? target.closest('[data-copy]') : null;
    if (!button || !window.chrome || !window.chrome.webview) {
        return;
    }

    event.preventDefault();
    window.chrome.webview.postMessage('copy:' + button.getAttribute('data-copy'));
});
