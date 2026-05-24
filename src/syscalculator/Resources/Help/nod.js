// Bundled NOD help script for packaged help content.
window.syscalNodHelp = window.syscalNodHelp || {};

window.syscalNodHelp.installCopyButtons = function () {
    window.syscalculatorHelp.installPreCopyButtons({
        copyLabel: '[menu.edit.copy]',
        copiedLabel: '[help.copy.copied]'
    });
};

window.syscalNodHelp.installPopupHeight = function () {
    window.syscalculatorHelp.installHeightReporter('nodhelp-height:');
};
