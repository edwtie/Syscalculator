#nullable enable
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Syscalculator.UI.WinForms;

internal sealed class FeedbackForm : Form
{
    private const string SupportAddress = "info@tiedragon.com";
    private const string FeedbackEndpointEnvironmentVariable = "SYSCALCULATOR_FEEDBACK_ENDPOINT";
    private const string FeedbackEndpointFileName = "feedback-endpoint.txt";
    private const string DefaultFeedbackEndpoint = "https://www.tiedragon.com/api/syscalculator-feedback/index.php";
    private static readonly HttpClient FeedbackHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };
    private static readonly JsonSerializerOptions WebMessageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly LanguageCatalog _language;
    private readonly string _supportInfo;
    private readonly string _converterName;
    private readonly WebView2 _browser;

    public FeedbackForm(LanguageCatalog language, string supportInfo, string converterName)
    {
        _language = language;
        _supportInfo = supportInfo;
        _converterName = converterName;

        Text = T("feedback.title", "Feedback");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(760, 660);
        BackColor = Color.FromArgb(246, 248, 252);
        AppWindowIcon.ApplyTo(this);

        _browser = new WebView2
        {
            Dock = DockStyle.Fill,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = WebView2UserDataFolder.GetPath()
            }
        };
        _browser.CoreWebView2InitializationCompleted += Browser_CoreWebView2InitializationCompleted;
        Controls.Add(_browser);

        Shown += async (_, _) => await InitializeBrowserAsync();
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            await _browser.EnsureCoreWebView2Async();
        }
        catch (Exception ex) when (ex is InvalidOperationException or COMException)
        {
            ShowFallback(ex.Message);
        }
    }

    private void Browser_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess || _browser.CoreWebView2 is null)
        {
            ShowFallback(e.InitializationException?.Message ?? T("feedback.webview_error", "WebView2 kon niet starten."));
            return;
        }

        _browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        _browser.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
        _browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _browser.CoreWebView2.WebMessageReceived += Browser_WebMessageReceived;
        _browser.NavigateToString(BuildHtml());
    }

    private async void Browser_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        FeedbackPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<FeedbackPayload>(e.WebMessageAsJson, WebMessageJsonOptions);
        }
        catch
        {
            return;
        }

        if (payload is null)
        {
            return;
        }

        if (payload.Action.Equals("close", StringComparison.OrdinalIgnoreCase))
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        if (payload.Action.Equals("mail", StringComparison.OrdinalIgnoreCase))
        {
            await SendFeedbackAsync(payload);
        }
    }

    private string BuildHtml()
    {
        var title = T("feedback.heading", "Feedback sturen");
        var subtitle = T("feedback.subtitle", "Schrijf wat er gebeurt, wat u verwachtte, of welk idee u hebt.");
        var defaultSubject = string.IsNullOrWhiteSpace(_converterName)
            ? T("feedback.default_subject", "Feedback over Syscalculator")
            : string.Format(T("feedback.default_subject_converter", "Feedback over {0}"), _converterName);
        var messagePlaceholder = NormalizeLanguageNewLines(T("feedback.message_placeholder", "Wat gebeurde er?\r\n\r\nWat had u verwacht?\r\n\r\nStappen om het te herhalen:"));
        var background = LoadFeedbackBackgroundDataUri();
        var deliveryHint = GetFeedbackEndpoint() is null
            ? string.Format(T("feedback.copy_hint", "Open uw mailprogramma voor {0}."), SupportAddress)
            : T("feedback.send_hint", "Feedback wordt veilig naar Tiedragon gestuurd.");
        var emailInvalid = T("feedback.email_invalid", "Vul een geldig e-mailadres in.");

        return $$"""
<!doctype html>
<html lang="nl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
:root {
  --accent: #111827;
  --ink: #10213a;
  --muted: #465970;
  --paper: rgba(255, 255, 255, 0.03);
  --paper-strong: rgba(255, 255, 255, 0.18);
  --line: rgba(55, 66, 82, 0.26);
}
* { box-sizing: border-box; }
html, body {
  width: 100%;
  height: 100%;
  margin: 0;
  overflow: hidden;
  font: 13px "Segoe UI", Arial, sans-serif;
  color: var(--ink);
}
body {
  background-image: url('{{background}}');
  background-size: cover;
  background-position: center;
}
.sheet {
  position: absolute;
  left: 205px;
  top: 92px;
  width: 410px;
  height: 552px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
h1 {
  margin: 0;
  color: var(--accent);
  font-size: 20px;
  line-height: 1.15;
}
.subtitle {
  margin-top: 3px;
  color: var(--muted);
}
.form {
  display: grid;
  grid-template-columns: 104px 1fr;
  grid-template-rows: 36px 36px 36px 36px 1fr 34px 34px;
  column-gap: 8px;
  row-gap: 0;
  flex: 1;
  min-height: 0;
}
label {
  align-self: center;
  font-weight: 700;
  color: #23364f;
}
input, textarea {
  appearance: none;
  -webkit-appearance: none;
  width: 100%;
  border: 1px solid transparent;
  border-bottom-color: var(--line);
  background: transparent !important;
  background-color: transparent !important;
  color: #0f172a;
  border-radius: 3px;
  padding: 4px 6px;
  outline: none;
  font: inherit;
  box-shadow: none;
}
input:focus {
  border-color: transparent;
  border-bottom-color: rgba(17, 24, 39, 0.58);
  background: transparent !important;
  background-color: transparent !important;
  box-shadow: 0 1px 0 rgba(17, 24, 39, 0.30);
}
input.invalid {
  border-bottom-color: rgba(17, 24, 39, 0.78);
  box-shadow: 0 1px 0 rgba(17, 24, 39, 0.42);
}
textarea:focus {
  border-color: rgba(17, 24, 39, 0.42);
  background: transparent !important;
  background-color: transparent !important;
  box-shadow: 0 0 0 1px rgba(17, 24, 39, 0.10);
}
input:hover, textarea:hover {
  background: transparent !important;
  background-color: transparent !important;
  box-shadow: none;
}
input:hover {
  border-bottom-color: rgba(17, 24, 39, 0.38);
}
textarea:hover {
  box-shadow: 0 0 0 1px rgba(65, 78, 96, 0.10);
}
.kind-picker {
  position: relative;
  width: 100%;
}
.kind-button {
  width: 100%;
  height: 28px;
  border: 1px solid transparent;
  border-bottom-color: var(--line);
  border-radius: 3px;
  background: transparent;
  background-color: transparent;
  color: #0f172a;
  padding: 4px 28px 4px 6px;
  text-align: left;
  font: inherit;
}
.kind-button:hover,
.kind-picker.open .kind-button {
  background: transparent;
  background-color: transparent;
  border-bottom-color: rgba(17, 24, 39, 0.38);
  box-shadow: none;
}
.kind-button:focus {
  outline: none;
  border-color: transparent;
  border-bottom-color: rgba(17, 24, 39, 0.58);
  box-shadow: 0 1px 0 rgba(17, 24, 39, 0.30);
}
.kind-button:after {
  content: "";
  position: absolute;
  right: 12px;
  top: 12px;
  border-left: 4px solid transparent;
  border-right: 4px solid transparent;
  border-top: 5px solid rgba(35,54,79,0.78);
}
.kind-list {
  display: none;
  position: absolute;
  z-index: 10;
  top: 30px;
  left: 0;
  right: 0;
  padding: 4px;
  margin: 0;
  list-style: none;
  border-radius: 6px;
  border: 1px solid rgba(17, 24, 39, 0.12);
  background: rgba(245, 242, 238, 0.28);
  background-color: rgba(245, 242, 238, 0.28);
  backdrop-filter: blur(10px) saturate(0.82);
  box-shadow: 0 7px 16px rgba(44, 36, 28, 0.08);
}
.kind-picker.open .kind-list {
  display: block;
}
.kind-list li {
  padding: 5px 7px;
  border-radius: 4px;
  cursor: default;
}
.kind-list li:hover,
.kind-list li.active {
  background: rgba(17, 24, 39, 0.04);
  background-color: rgba(17, 24, 39, 0.04);
  font-weight: 600;
}
textarea {
  resize: none;
  min-height: 0;
  line-height: 1.45;
  background: transparent !important;
  background-color: transparent !important;
  border-color: rgba(55, 66, 82, 0.18);
}
textarea:focus {
  background: transparent !important;
  background-color: transparent !important;
}
textarea::-webkit-scrollbar {
  width: 10px;
}
textarea::-webkit-scrollbar-track {
  background: rgba(255,255,255,0.12);
}
textarea::-webkit-scrollbar-thumb {
  background: rgba(70,89,112,0.32);
  border-radius: 8px;
}
.check-row {
  grid-column: 2;
  display: flex;
  align-items: center;
  gap: 6px;
  color: #23364f;
  cursor: pointer;
}
.check-row input {
  appearance: none;
  -webkit-appearance: none;
  width: 14px;
  height: 14px;
  margin: 0;
  flex: 0 0 auto;
  display: inline-grid;
  place-content: center;
  position: relative;
  border: 1.2px solid rgba(17, 24, 39, 0.70);
  border-radius: 3px;
  background: transparent !important;
  background-color: transparent !important;
  box-shadow: none;
}
.check-row input:focus {
  border-color: rgba(17, 24, 39, 0.92);
  background: transparent !important;
  background-color: transparent !important;
  box-shadow: 0 0 0 2px rgba(17, 24, 39, 0.10);
}
.check-row input:hover {
  background: rgba(17, 24, 39, 0.05) !important;
  background-color: rgba(17, 24, 39, 0.05) !important;
}
.check-row input:checked:before,
.check-row input:checked:after {
  content: "";
  position: absolute;
  top: 50%;
  left: 50%;
  width: 8px;
  height: 1.25px;
  border-radius: 999px;
  background: rgba(24, 30, 39, 0.92);
  transform-origin: center;
}
.check-row input:checked:before {
  transform: translate(-53%, -50%) rotate(42deg);
}
.check-row input:checked:after {
  width: 8.6px;
  transform: translate(-48%, -48%) rotate(-49deg);
}
.hint {
  grid-column: 2;
  align-self: center;
  color: var(--muted);
}
.hint.error {
  color: #111827;
  font-weight: 600;
}
.buttons {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding-top: 2px;
}
button {
  min-width: 104px;
  height: 30px;
  border: 1px solid rgba(17, 24, 39, 0.34);
  border-radius: 4px;
  background: transparent;
  background-color: transparent;
  color: #111827;
  font: inherit;
  outline: none;
}
button.secondary {
  border-color: rgba(17, 24, 39, 0.28);
  color: #111827;
}
button:hover {
  background: rgba(17, 24, 39, 0.05);
  background-color: rgba(17, 24, 39, 0.05);
  border-color: rgba(17, 24, 39, 0.52);
  box-shadow: 0 0 0 2px rgba(17, 24, 39, 0.08);
}
button:focus-visible {
  border-color: rgba(17, 24, 39, 0.70);
  box-shadow: 0 0 0 2px rgba(17, 24, 39, 0.14);
}
button.icon-only {
  min-width: 44px;
  width: 44px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: transparent;
  background-color: transparent;
}
button.icon-only:hover,
button.icon-only:focus-visible {
  background: rgba(17, 24, 39, 0.07);
  background-color: rgba(17, 24, 39, 0.07);
}
.mail-icon {
  display: inline-block;
  width: 15px;
  height: 11px;
  border: 1.5px solid #111827;
  border-radius: 2px;
  position: relative;
  top: 1px;
}
.mail-icon:before,
.mail-icon:after {
  content: "";
  position: absolute;
  top: 1px;
  width: 9px;
  border-top: 1.5px solid #111827;
}
.mail-icon:before { left: 0; transform: rotate(35deg); transform-origin: left top; }
.mail-icon:after { right: 0; transform: rotate(-35deg); transform-origin: right top; }
</style>
</head>
<body>
  <main class="sheet">
    <header>
      <h1>{{H(title)}}</h1>
      <div class="subtitle">{{H(subtitle)}}</div>
    </header>
    <section class="form">
      <label for="kind">{{H(T("feedback.kind", "Soort"))}}</label>
      <div class="kind-picker" id="kindPicker">
        <button class="kind-button" id="kindButton" type="button">{{H(T("feedback.kind.problem", "Probleem"))}}</button>
        <ul class="kind-list" id="kindList">
          <li class="active" data-value="{{H(T("feedback.kind.problem", "Probleem"))}}">{{H(T("feedback.kind.problem", "Probleem"))}}</li>
          <li data-value="{{H(T("feedback.kind.idea", "Idee"))}}">{{H(T("feedback.kind.idea", "Idee"))}}</li>
          <li data-value="{{H(T("feedback.kind.question", "Vraag"))}}">{{H(T("feedback.kind.question", "Vraag"))}}</li>
          <li data-value="{{H(T("feedback.kind.other", "Overig"))}}">{{H(T("feedback.kind.other", "Overig"))}}</li>
        </ul>
      </div>

      <label for="name">{{H(T("feedback.name", "Naam"))}}</label>
      <input id="name" autocomplete="name">

      <label for="email">{{H(T("feedback.email", "E-mail"))}}</label>
      <input id="email" type="email" autocomplete="email">

      <label for="subject">{{H(T("feedback.subject", "Onderwerp"))}}</label>
      <input id="subject" value="{{H(defaultSubject)}}">

      <label for="message">{{H(T("feedback.message", "Bericht"))}}</label>
      <textarea id="message">{{H(messagePlaceholder)}}</textarea>

      <label class="check-row"><input id="includeSupport" type="checkbox" checked> {{H(T("feedback.include_support_info", "Supportinformatie meesturen"))}}</label>
      <div class="hint">{{H(deliveryHint)}}</div>
    </section>
    <footer class="buttons">
      <button class="icon-only" id="mail" title="{{H(T("feedback.open_mail", "Mail"))}}" aria-label="{{H(T("feedback.open_mail", "Mail"))}}"><span class="mail-icon"></span></button>
      <button class="secondary" id="ok">{{H("OK")}}</button>
    </footer>
  </main>
<script>
const supportInfo = {{JsonSerializer.Serialize(_supportInfo)}};
const deliveryHintText = {{JsonSerializer.Serialize(deliveryHint)}};
const emailInvalidText = {{JsonSerializer.Serialize(emailInvalid)}};
let selectedKind = document.getElementById('kindButton').textContent;
function value(id) { return document.getElementById(id).value; }
function isValidEmail(text) {
  const value = text.trim();
  return value.length > 0 && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}
function setEmailError(message) {
  const email = document.getElementById('email');
  const hint = document.querySelector('.hint');
  email.classList.toggle('invalid', Boolean(message));
  hint.textContent = message || deliveryHintText;
  hint.classList.toggle('error', Boolean(message));
}
function validateEmail() {
  const email = document.getElementById('email');
  const ok = isValidEmail(email.value);
  setEmailError(ok ? '' : emailInvalidText);
  if (!ok) {
    email.focus();
  }

  return ok;
}
function payload(action) {
  return {
    action,
    kind: selectedKind,
    name: value('name'),
    email: value('email'),
    subject: value('subject'),
    message: value('message'),
    includeSupportInfo: document.getElementById('includeSupport').checked,
    supportInfo
  };
}
const kindPicker = document.getElementById('kindPicker');
const kindButton = document.getElementById('kindButton');
const kindList = document.getElementById('kindList');
kindButton.addEventListener('click', () => kindPicker.classList.toggle('open'));
kindList.querySelectorAll('li').forEach(item => {
  item.addEventListener('click', () => {
    kindList.querySelectorAll('li').forEach(li => li.classList.remove('active'));
    item.classList.add('active');
    selectedKind = item.dataset.value;
    kindButton.textContent = selectedKind;
    kindPicker.classList.remove('open');
  });
});
document.addEventListener('click', event => {
  if (!kindPicker.contains(event.target)) {
    kindPicker.classList.remove('open');
  }
});
document.getElementById('mail').addEventListener('click', () => {
  if (!validateEmail()) {
    return;
  }

  chrome.webview.postMessage(payload('mail'));
});
document.getElementById('email').addEventListener('input', () => setEmailError(''));
document.getElementById('ok').addEventListener('click', () => chrome.webview.postMessage(payload('close')));
document.getElementById('message').focus();
</script>
</body>
</html>
""";
    }

    private async Task SendFeedbackAsync(FeedbackPayload payload)
    {
        if (!ValidateFeedback(payload))
            return;

        var endpoint = GetFeedbackEndpoint();
        if (endpoint is not null && await TryPostFeedbackAsync(endpoint, payload))
        {
            MessageBox.Show(this,
                T("feedback.sent", "Feedback is verstuurd. Dank u."),
                T("feedback.title", "Feedback"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        OpenMail(payload);
    }

    private bool ValidateFeedback(FeedbackPayload payload)
    {
        if (IsValidEmailAddress(payload.Email))
            return true;

        MessageBox.Show(this,
            T("feedback.email_invalid", "Vul een geldig e-mailadres in."),
            T("feedback.title", "Feedback"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        return false;
    }

    private static bool IsValidEmailAddress(string? email)
    {
        var value = email?.Trim();
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@', StringComparison.Ordinal))
            return false;

        try
        {
            var address = new MailAddress(value);
            return address.Address.Equals(value, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryPostFeedbackAsync(Uri endpoint, FeedbackPayload payload)
    {
        try
        {
            var body = BuildFeedbackText(payload);
            var request = new
            {
                product = AppVersionInfo.ProductName,
                version = AppVersionInfo.DisplayVersion,
                releaseChannel = AppVersionInfo.ReleaseChannel,
                releaseDate = AppVersionInfo.ReleaseDate,
                submittedAtUtc = DateTimeOffset.UtcNow,
                kind = payload.Kind,
                name = payload.Name,
                email = payload.Email,
                subject = payload.Subject,
                message = payload.Message,
                includeSupportInfo = payload.IncludeSupportInfo,
                supportInfo = payload.IncludeSupportInfo ? payload.SupportInfo : "",
                body
            };

            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await FeedbackHttpClient.PostAsync(endpoint, content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void OpenMail(FeedbackPayload payload)
    {
        var subject = string.IsNullOrWhiteSpace(payload.Subject)
            ? T("feedback.default_subject", "Feedback over Syscalculator")
            : payload.Subject.Trim();
        var body = BuildFeedbackText(payload);
        var uri = "mailto:" + SupportAddress +
                  "?subject=" + Uri.EscapeDataString(subject) +
                  "&body=" + Uri.EscapeDataString(body);

        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            Clipboard.SetText(body);
            MessageBox.Show(this,
                T("feedback.mail_failed", "Mail openen lukte niet. Het feedbackbericht is naar het klembord gekopieerd."),
                T("feedback.title", "Feedback"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private static string BuildFeedbackText(FeedbackPayload payload)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Soort: {EmptyAsDash(payload.Kind)}");
        sb.AppendLine($"Naam: {EmptyAsDash(payload.Name)}");
        sb.AppendLine($"E-mail: {EmptyAsDash(payload.Email)}");
        sb.AppendLine($"Onderwerp: {EmptyAsDash(payload.Subject)}");
        sb.AppendLine();
        sb.AppendLine((payload.Message ?? string.Empty).Trim());

        if (payload.IncludeSupportInfo)
        {
            sb.AppendLine();
            sb.AppendLine((payload.SupportInfo ?? string.Empty).TrimEnd());
        }

        return sb.ToString();
    }

    private void ShowFallback(string message)
    {
        Controls.Clear();
        Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            Text = T("feedback.webview_error", "WebView2 kon niet starten.") + Environment.NewLine + message,
            TextAlign = ContentAlignment.MiddleCenter
        });
    }

    private static string LoadFeedbackBackgroundDataUri()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "FeedbackBackground.png");
        if (!File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "FeedbackBackground.png");
        }

        if (!File.Exists(path))
        {
            return "";
        }

        return "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
    }

    private static string EmptyAsDash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string H(string value) => WebUtility.HtmlEncode(value);

    private static Uri? GetFeedbackEndpoint()
    {
        var configured = Environment.GetEnvironmentVariable(FeedbackEndpointEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            var path = Path.Combine(AppContext.BaseDirectory, FeedbackEndpointFileName);
            if (File.Exists(path))
            {
                configured = File.ReadLines(path).FirstOrDefault();
            }
        }

        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = DefaultFeedbackEndpoint;
        }

        if (!Uri.TryCreate(configured?.Trim(), UriKind.Absolute, out var endpoint))
        {
            return null;
        }

        return endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               endpoint.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            ? endpoint
            : null;
    }

    private static string NormalizeLanguageNewLines(string value)
    {
        return value
            .Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private string T(string key, string fallback) => _language.Text(key, fallback);

    private sealed class FeedbackPayload
    {
        public string Action { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Message { get; set; } = "";
        public bool IncludeSupportInfo { get; set; }
        public string SupportInfo { get; set; } = "";
    }
}
