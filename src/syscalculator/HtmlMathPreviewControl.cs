#nullable enable
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syscalculator.UI.WinForms;

// Kleine HTML/WebView2-viewer voor formule en uitwerking in de NOD editor.
// Gebruikt dezelfde moderne engine als de NOD-help, zodat MathML/CSS visueel gelijker wordt.
public sealed class HtmlMathPreviewControl : UserControl
{
    private readonly WebView2 _browser;
    private readonly Label _fallback;
    private string _mathMarkup = "";
    private string? _pendingHtml;
    private bool _browserFailed;
    private TaskCompletionSource? _readySource;
    private CoreWebView2PreferredColorScheme _preferredColorScheme = CoreWebView2PreferredColorScheme.Light;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string MathMarkup
    {
        get => _mathMarkup;
        set
        {
            _mathMarkup = value ?? "";
            SetHtml(BuildHtml(_mathMarkup));
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CoreWebView2PreferredColorScheme PreferredColorScheme
    {
        get => _preferredColorScheme;
        set
        {
            if (_preferredColorScheme == value)
                return;

            _preferredColorScheme = value;
            if (_browser.CoreWebView2 is not null)
                _browser.CoreWebView2.Profile.PreferredColorScheme = value;
            if (!string.IsNullOrWhiteSpace(_mathMarkup))
                SetHtml(BuildHtml(_mathMarkup));
        }
    }

    public HtmlMathPreviewControl()
    {
        BackColor = Color.FromArgb(239, 246, 255);
        ForeColor = Color.FromArgb(0, 74, 173);

        _fallback = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0),
            ForeColor = ForeColor,
            BackColor = BackColor,
            Visible = false
        };

        _browser = new WebView2
        {
            Dock = DockStyle.Fill,
            AllowExternalDrop = false,
            DefaultBackgroundColor = BackColor,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = WebView2UserDataFolder.GetPath()
            }
        };

        _browser.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || _browser.CoreWebView2 is null)
            {
                ShowFallback();
                _readySource?.TrySetResult();
                return;
            }

            _browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _browser.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            _browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _browser.CoreWebView2.Profile.PreferredColorScheme = _preferredColorScheme;
            ShowPendingHtmlIfReady();
            _readySource?.TrySetResult();
        };

        Controls.Add(_browser);
        Controls.Add(_fallback);
        _ = InitializeBrowserAsync();
    }

    public async Task<byte[]?> CaptureJpegAsync()
    {
        if (_browserFailed)
            return null;

        _readySource ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_browser.CoreWebView2 is null)
            await _readySource.Task.ConfigureAwait(true);

        if (_browserFailed || _browser.CoreWebView2 is null)
            return null;

        await Task.Delay(160).ConfigureAwait(true);

        using var stream = new MemoryStream();
        await _browser.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, stream).ConfigureAwait(true);
        return stream.ToArray();
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (_fallback is null || _browser is null)
            return;

        _fallback.BackColor = BackColor;
        _browser.DefaultBackgroundColor = BackColor;
        if (!string.IsNullOrWhiteSpace(_mathMarkup))
            SetHtml(BuildHtml(_mathMarkup));
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        if (_fallback is null)
            return;

        _fallback.ForeColor = ForeColor;
        if (!string.IsNullOrWhiteSpace(_mathMarkup))
            SetHtml(BuildHtml(_mathMarkup));
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (!string.IsNullOrWhiteSpace(_mathMarkup))
            SetHtml(BuildHtml(_mathMarkup));
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            await _browser.EnsureCoreWebView2Async();
            ShowPendingHtmlIfReady();
        }
        catch (COMException)
        {
            ShowFallback();
        }
        catch (ObjectDisposedException)
        {
            _browserFailed = true;
        }
        catch (Exception)
        {
            ShowFallback();
        }
    }

    private void SetHtml(string html)
    {
        _pendingHtml = html;
        ShowPendingHtmlIfReady();
    }

    private void ShowPendingHtmlIfReady()
    {
        if (_browserFailed || IsDisposed || _browser.IsDisposed || _pendingHtml is null || _browser.CoreWebView2 is null)
            return;

        var html = _pendingHtml;
        _pendingHtml = null;

        try
        {
            _browser.NavigateToString(html);
            _fallback.Visible = false;
            _browser.Visible = true;
        }
        catch (COMException)
        {
            ShowFallback();
        }
        catch (ObjectDisposedException)
        {
            _browserFailed = true;
        }
    }

    private void ShowFallback()
    {
        _browserFailed = true;
        _browser.Visible = false;
        _fallback.Text = StripMarkup(_mathMarkup);
        _fallback.Visible = true;
    }

    private static string StripMarkup(string markup)
    {
        if (string.IsNullOrWhiteSpace(markup))
            return "";

        var text = System.Text.RegularExpressions.Regex.Replace(markup, "<[^>]+>", "");
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }

    private string BuildHtml(string mathMarkup)
    {
        var back = HtmlColor(BackColor);
        var fore = HtmlColor(ForeColor);
        var softBack = BackColor.GetBrightness() > 0.96f ? back : "#f8fbff";
        var softBorder = BackColor.GetBrightness() > 0.96f ? back : "#bfdbfe";
        var colorScheme = _preferredColorScheme == CoreWebView2PreferredColorScheme.Dark ? "dark" : "light";
        var baseFontSize = Math.Max(16f, Font.SizeInPoints);
        var mathFontSize = Math.Max(18f, baseFontSize + 3f);
        var baseSizeCss = baseFontSize.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var mathSizeCss = mathFontSize.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        return $$"""
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8">
        <style>
        :root { color-scheme: {{colorScheme}}; }
        * { box-sizing: border-box; }
        html, body {
            margin: 0;
            width: 100%;
            height: 100%;
            overflow: hidden;
            background: {{back}};
            color: {{fore}};
            font-family: "Cambria Math", "Segoe UI", Arial, sans-serif;
        }
        body {
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 0;
        }
        .formula-preview {
            display: flex;
            align-items: center;
            justify-content: center;
            text-align: center;
            min-height: 100%;
            width: 100%;
            max-width: 100%;
            overflow: hidden;
            color: {{fore}};
            font-size: {{baseSizeCss}}px;
            font-weight: 700;
            line-height: 1.5;
            white-space: normal;
        }
        .math-step-sequence {
            display: flex;
            flex-direction: column;
            align-items: stretch;
            justify-content: center;
            gap: 3px;
            max-width: 100%;
        }
        .math-step-row {
            display: flex;
            align-items: center;
            gap: 7px;
            min-width: 0;
            max-width: 100%;
        }
        .math-step-arrow {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            flex: 0 0 auto;
            color: {{fore}};
            font-family: "Cambria Math", "Segoe UI Symbol", sans-serif;
            font-weight: 700;
        }
        .formula-preview math {
            color: {{fore}};
            font-size: {{mathSizeCss}}px;
            line-height: 1.5;
        }
        .formula-preview span {
            color: {{fore}} !important;
        }
        .formula-preview .rule-card-formula {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100%;
            padding: 4px 8px;
            border-radius: 8px;
            background: {{softBack}};
            border: 1px solid {{softBorder}};
        }
        .formula-preview .rule-card-formula math {
            font-size: 21px;
        }
        .formula-preview .hot {
            color: #dc2626;
            font-weight: 800;
            animation: pulseHot 850ms ease-in-out infinite alternate;
        }
        .function-line-animation,
        .power-rule-animation,
        .derivative-notation-animation,
        .exponent-reduction-animation,
        .remove-one-animation,
        .power-simplify-animation,
        .derivative-result-animation,
        .zero-slope-animation,
        .solve-zero-animation,
        .root-zero-animation,
        .graph-ready-animation {
            width: 100%;
            max-width: 620px;
            height: 84px;
            overflow: hidden;
            color: {{fore}};
        }
        .function-line-animation,
        .derivative-notation-animation {
            max-width: 620px;
        }
        .exponent-reduction-animation {
            max-width: 420px;
        }
        .remove-one-animation {
            max-width: 430px;
        }
        .power-simplify-animation {
            max-width: 520px;
        }
        .derivative-result-animation,
        .graph-ready-animation {
            max-width: 620px;
        }
        .zero-slope-animation {
            max-width: 520px;
        }
        .solve-zero-animation {
            max-width: 560px;
        }
        .function-line-animation svg,
        .power-rule-animation svg,
        .derivative-notation-animation svg,
        .exponent-reduction-animation svg,
        .remove-one-animation svg,
        .power-simplify-animation svg,
        .derivative-result-animation svg,
        .zero-slope-animation svg,
        .solve-zero-animation svg,
        .root-zero-animation svg,
        .graph-ready-animation svg {
            display: block;
            width: 100%;
            height: 84px;
        }
        .function-line-animation text,
        .power-rule-animation text,
        .derivative-notation-animation text,
        .exponent-reduction-animation text,
        .remove-one-animation text,
        .power-simplify-animation text,
        .derivative-result-animation text,
        .zero-slope-animation text,
        .solve-zero-animation text,
        .root-zero-animation text,
        .graph-ready-animation text {
            font-family: "Cambria Math", "Segoe UI", Arial, sans-serif;
            dominant-baseline: alphabetic;
        }
        .function-line-animation .formula-base,
        .power-rule-animation .formula-base,
        .derivative-notation-animation .formula-base,
        .exponent-reduction-animation .formula-base,
        .remove-one-animation .formula-base,
        .power-simplify-animation .formula-base,
        .derivative-result-animation .formula-base,
        .zero-slope-animation .formula-base,
        .solve-zero-animation .formula-base,
        .root-zero-animation .formula-base,
        .graph-ready-animation .formula-base {
            fill: {{fore}};
            font-size: 28px;
            font-weight: 700;
        }
        .function-line-animation .formula-exp,
        .derivative-notation-animation .formula-exp,
        .power-simplify-animation .formula-exp,
        .solve-zero-animation .formula-exp {
            fill: {{fore}};
            font-size: 16px;
            font-weight: 700;
        }
        .function-line-animation .label,
        .power-rule-animation .label,
        .derivative-notation-animation .label,
        .exponent-reduction-animation .label,
        .remove-one-animation .label,
        .power-simplify-animation .label,
        .derivative-result-animation .label,
        .zero-slope-animation .label,
        .solve-zero-animation .label,
        .root-zero-animation .label,
        .graph-ready-animation .label {
            fill: {{fore}};
            font-size: 24px;
            font-weight: 700;
        }
        .function-line-animation .function-arg-x,
        .power-rule-animation .function-arg-x,
        .derivative-notation-animation .function-arg-x,
        .exponent-reduction-animation .function-arg-x,
        .remove-one-animation .function-arg-x,
        .power-simplify-animation .function-arg-x,
        .derivative-result-animation .function-arg-x,
        .zero-slope-animation .function-arg-x {
            font-size: 22px;
            font-style: italic;
            font-weight: 700;
        }
        .power-rule-animation .prime-mark,
        .derivative-notation-animation .prime-mark,
        .power-rule-animation .settled-prime {
            fill: #dc2626;
            font-size: 22px;
            font-weight: 900;
            opacity: 0;
            animation: showPrimeRed .45s ease-out 1.15s forwards, primeTurnsBlue .65s ease-out 2.05s forwards;
        }
        .power-rule-animation .settled-prime {
            fill: {{fore}};
            font-size: 22px;
            font-weight: 900;
            opacity: 1;
            animation: none;
        }
        .exponent-reduction-animation .settled-prime,
        .remove-one-animation .settled-prime,
        .power-simplify-animation .settled-prime,
        .derivative-result-animation .settled-prime,
        .zero-slope-animation .settled-prime {
            fill: {{fore}};
            font-size: 22px;
            font-weight: 900;
            opacity: 1;
        }
        .derivative-notation-animation .prime-mark {
            animation: showPrimeRed .45s ease-out .65s forwards, primeTurnsBlue .65s ease-out 1.35s forwards;
        }
        .derivative-notation-animation .derivative-caption-from {
            opacity: 1;
            animation: fadeLeftEquation .45s ease-out .85s forwards;
        }
        .derivative-notation-animation .derivative-caption-to {
            opacity: 0;
            animation: showFinalResult .55s ease-out 1.05s forwards;
        }
        .power-rule-animation .formula-exp {
            fill: {{fore}};
            font-size: 16px;
            font-weight: 700;
        }
        .power-rule-animation .moving-exp {
            fill: #dc2626;
            font-size: 18px;
            font-weight: 900;
            paint-order: stroke;
            stroke: #eff6ff;
            stroke-width: 3px;
            animation: movingExpGlow 1.9s ease-in-out 0.35s forwards;
        }
        .power-rule-animation .ghost-exp {
            fill: #93c5fd;
            filter: blur(.25px);
            animation: fadeGhostExp 1.9s ease-in-out 0.35s forwards;
        }
        .power-rule-animation .exponent-calc,
        .exponent-reduction-animation .exponent-calc {
            fill: #0f3f8f;
        }
        .power-rule-animation .exponent-result,
        .exponent-reduction-animation .exponent-result {
            fill: #dc2626;
            opacity: 0;
            animation: showExponentResult .55s ease-out 2s forwards;
        }
        .exponent-reduction-animation .exponent-calc {
            animation: fadeExponentCalc .8s ease-out 1.1s forwards;
        }
        .exponent-reduction-animation .coefficient-settle {
            fill: #dc2626;
            animation: coefficientTurnsBlue .6s ease-out .55s forwards;
        }
        .exponent-reduction-animation .exponent-result {
            animation: showExponentResult .65s ease-out 1.15s forwards;
            font-weight: 900;
        }
        .power-rule-animation .target-formula {
            opacity: 0;
            animation: showTargetFormula .55s ease-out 1.55s forwards;
        }
        .power-rule-animation .arrow-symbol,
        .power-rule-animation .dot,
        .exponent-reduction-animation .dot,
        .remove-one-animation .dot,
        .power-simplify-animation .dot,
        .solve-zero-animation .dot {
            fill: #2563eb;
            font-size: 21px;
            font-weight: 700;
        }
        .power-rule-animation .move-path {
            fill: none;
            stroke: #93c5fd;
            stroke-width: 2;
            stroke-dasharray: 4 5;
            marker-end: url(#arrowHead);
            opacity: .8;
        }
        .power-rule-animation .caption,
        .derivative-notation-animation .caption,
        .remove-one-animation .caption,
        .derivative-result-animation .caption,
        .zero-slope-animation .caption,
        .solve-zero-animation .caption,
        .graph-ready-animation .caption {
            fill: #475569;
            font-size: 14px;
            font-family: "Segoe UI", Arial, sans-serif;
            font-weight: 600;
        }
        .derivative-result-animation .answer-result {
            opacity: 1;
        }
        .remove-one-animation .one-exp {
            fill: #dc2626;
            font-size: 16px;
            font-weight: 900;
            animation: removeOneExp .85s ease-out .8s forwards;
        }
        .remove-one-animation .expanded-part {
            animation: fadeExpandedDerivative .55s ease-out 1.05s forwards;
        }
        .remove-one-animation .final-result {
            opacity: 0;
            animation: showFinalResult .65s ease-out 1.18s forwards;
        }
        .power-simplify-animation .expanded-part {
            animation: fadeExpandedDerivative .65s ease-out 1.05s forwards;
        }
        .power-simplify-animation .final-result {
            opacity: 0;
            animation: showFinalResult .75s ease-out 1.18s forwards;
        }
        .power-simplify-animation .simplify-caption {
            opacity: 0;
            fill: #475569;
            font-size: 14px;
            font-family: "Segoe UI", Arial, sans-serif;
            font-weight: 600;
            animation: showFinalResult .55s ease-out 1.4s forwards;
        }
        .remove-one-animation .compact-result {
            paint-order: stroke;
            stroke: #eff6ff;
            stroke-width: 3px;
        }
        .remove-one-animation .simplify-hint-base,
        .remove-one-animation .simplify-hint-exp,
        .remove-one-animation .simplify-hint-arrow,
        .remove-one-animation .simplify-hint-result {
            opacity: 0;
            animation: showFinalResult .55s ease-out 1.35s forwards;
        }
        .remove-one-animation .simplify-hint-exp {
            fill: #94a3b8;
        }
        .remove-one-animation .simplify-hint-arrow {
            fill: #64748b;
        }
        .zero-slope-animation .zero-arrow,
        .solve-zero-animation .zero-arrow {
            fill: #2563eb;
            font-size: 21px;
            font-weight: 800;
            opacity: 0;
            animation: showFinalResult .45s ease-out 1.45s forwards;
        }
        .zero-slope-animation .transition-arrow {
            animation: showThenHideArrow 1.25s ease-out 1.35s forwards;
        }
        .zero-slope-animation .zero-equation {
            opacity: 0;
            animation: showFinalResult .45s ease-out 1.85s forwards;
        }
        .zero-slope-animation .zero-rule-hint {
            opacity: 0;
            animation: showFinalResult .45s ease-out 2.15s forwards;
        }
        .zero-slope-animation .zero-left-label {
            animation: fadeLeftEquation .45s ease-out 1.75s forwards;
        }
        .zero-slope-animation .zero-number,
        .solve-zero-animation .zero-number,
        .root-zero-animation .zero-number {
            fill: #dc2626;
            font-size: 28px;
            font-weight: 900;
            opacity: 0;
            animation: showFinalResult .55s ease-out 2.05s forwards;
        }
        .zero-slope-animation .zero-number.derivative-zero {
            opacity: 0;
            animation: showThenHideDerivativeZero 1.15s ease-out .75s forwards;
        }
        .graph-ready-animation .zero-number {
            fill: #dc2626;
            font-size: 28px;
            font-weight: 900;
            opacity: 1;
        }
        .graph-ready-animation .graph-label {
            opacity: 0;
            animation: showFinalResult .45s ease-out .95s forwards;
        }
        .graph-ready-animation .graph-equation-part {
            paint-order: stroke;
            stroke: #eff6ff;
            stroke-width: 3px;
        }
        .graph-ready-animation .graph-caption {
            opacity: 0;
            animation: showFinalResult .55s ease-out 1.25s forwards;
        }
        .solve-zero-animation .zero-arrow {
            animation-delay: 1.9s;
        }
        .solve-zero-animation .zero-number {
            animation-delay: 2.75s;
        }
        .solve-zero-animation .solve-left-part {
            animation: fadeFractionPart .45s ease-out 2.85s forwards;
        }
        .solve-zero-animation .final-equation {
            opacity: 0;
            animation: showFinalResult .55s ease-out 2.95s forwards;
        }
        .solve-zero-animation .solve-caption {
            opacity: 0;
            fill: #64748b;
            font-size: 13px;
            animation: showFinalResult .55s ease-out 3.3s forwards;
        }
        .solve-zero-animation .fraction {
            fill: #004aad;
            font-size: 18px;
            font-weight: 800;
            opacity: 0;
            animation: showFinalResult .55s ease-out 1.7s forwards;
        }
        .solve-zero-animation .fraction-line {
            stroke: #004aad;
            stroke-width: 2;
            opacity: 0;
            animation: showFinalResult .55s ease-out 1.7s forwards;
        }
        .solve-zero-animation .fraction-part {
            animation: showFinalResult .55s ease-out 1.7s forwards, fadeFractionPart .45s ease-out 2.85s forwards;
        }
        .solve-zero-animation .moving-divisor {
            fill: #dc2626;
            font-size: 28px;
            font-weight: 900;
            paint-order: stroke;
            stroke: #eff6ff;
            stroke-width: 3px;
            animation: fadeMovingDivisor .55s ease-out 2.1s forwards;
        }
        .solve-zero-animation .moving-zero {
            fill: #004aad;
            paint-order: stroke;
            stroke: #eff6ff;
            stroke-width: 3px;
        }
        .solve-zero-animation .numerator-part {
            animation: fadeFractionPart .45s ease-out 2.85s forwards;
        }
        .solve-zero-animation .ghost-divisor {
            fill: #93c5fd;
            opacity: 0;
            animation: showGhostDivisor .25s ease-out 2s forwards, fadeFractionPart .45s ease-out 2.85s forwards;
        }
        .root-zero-animation .formula-exp {
            fill: #dc2626;
            font-size: 16px;
            font-weight: 900;
            animation: removeOneExp .85s ease-out .85s forwards;
        }
        .root-zero-animation .root-variable,
        .root-zero-animation .root-equals,
        .root-zero-animation .root-zero {
            opacity: 1;
            animation: fadeExpandedDerivative .45s ease-out 1.35s forwards;
        }
        .root-zero-animation .root-arrow {
            fill: #2563eb;
            font-size: 21px;
            font-weight: 800;
            opacity: 0;
            animation: showFinalResult .45s ease-out 1.35s forwards;
        }
        .root-zero-animation .final-root,
        .root-zero-animation .root-caption {
            opacity: 0;
            animation: showFinalResult .65s ease-out 1.75s forwards;
        }
        .root-zero-animation .root-caption {
            fill: #475569;
            font-size: 14px;
            font-family: "Segoe UI", Arial, sans-serif;
            font-weight: 600;
        }
        @keyframes pulseHot {
            from { opacity: .72; }
            to { opacity: 1; }
        }
        @keyframes movingExpGlow {
            from { opacity: .95; }
            to { opacity: 1; }
        }
        @keyframes fadeGhostExp {
            from { opacity: 1; }
            to { opacity: .25; }
        }
        @keyframes fadeExponentCalc {
            from { opacity: 1; }
            to { opacity: .2; }
        }
        @keyframes coefficientTurnsBlue {
            from { fill: #dc2626; }
            to { fill: #004aad; }
        }
        @keyframes showTargetFormula {
            from { opacity: 0; }
            to { opacity: 1; }
        }
        @keyframes showExponentResult {
            from { opacity: 0; transform: translateX(-5px); }
            to { opacity: 1; transform: translateX(0); }
        }
        @keyframes removeOneExp {
            from { opacity: 1; transform: translateY(0) scale(1); }
            to { opacity: 0; transform: translateY(-8px) scale(.55); }
        }
        @keyframes fadeExpandedDerivative {
            from { opacity: 1; }
            to { opacity: 0; }
        }
        @keyframes showFinalResult {
            from { opacity: 0; transform: translateX(-6px); }
            to { opacity: 1; transform: translateX(0); }
        }
        @keyframes showThenHideArrow {
            0% { opacity: 0; transform: translateX(-6px); }
            30% { opacity: 1; transform: translateX(0); }
            70% { opacity: 1; transform: translateX(0); }
            100% { opacity: 0; transform: translateX(6px); }
        }
        @keyframes showThenHideDerivativeZero {
            0% { opacity: 0; transform: translateX(-4px); }
            25% { opacity: 1; transform: translateX(0); }
            70% { opacity: 1; transform: translateX(0); }
            100% { opacity: 0; transform: translateX(6px); }
        }
        @keyframes showGhostDivisor {
            from { opacity: 0; }
            to { opacity: .25; }
        }
        @keyframes fadeMovingDivisor {
            from { opacity: 1; }
            to { opacity: 0; }
        }
        @keyframes fadeFractionPart {
            from { opacity: 1; }
            to { opacity: 0; }
        }
        @keyframes fadeLeftEquation {
            from { opacity: 1; }
            to { opacity: 0; }
        }
        @keyframes showPrimeRed {
            from { opacity: 0; transform: translateY(-4px); }
            to { opacity: 1; transform: translateY(0); }
        }
        @keyframes primeTurnsBlue {
            from { fill: #dc2626; }
            to { fill: #004aad; }
        }
        </style>
        </head>
        <body><div class="formula-preview">{{mathMarkup}}</div></body>
        </html>
        """;
    }

    private static string HtmlColor(Color color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
