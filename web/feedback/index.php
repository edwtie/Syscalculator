<?php
declare(strict_types=1);

/*
 * Syscalculator feedback endpoint.
 *
 * Upload this folder to your PHP host and point Syscalculator to this URL
 * with feedback-endpoint.txt or SYSCALCULATOR_FEEDBACK_ENDPOINT.
 */

const FEEDBACK_TO = 'info@tiedragon.com';
const FEEDBACK_FROM = 'no-reply@tiedragon.com';
const MAX_BODY_BYTES = 131072;
const RATE_LIMIT_SECONDS = 20;
const SAVE_COPY = true;
const COPY_DIR = __DIR__ . '/inbox';

header('Content-Type: application/json; charset=utf-8');
header('X-Content-Type-Options: nosniff');

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    respond(405, ['ok' => false, 'error' => 'method_not_allowed']);
}

$clientIp = $_SERVER['REMOTE_ADDR'] ?? 'unknown';
if (!rateLimit($clientIp)) {
    respond(429, ['ok' => false, 'error' => 'rate_limited']);
}

$contentLength = (int)($_SERVER['CONTENT_LENGTH'] ?? 0);
if ($contentLength <= 0 || $contentLength > MAX_BODY_BYTES) {
    respond(413, ['ok' => false, 'error' => 'invalid_size']);
}

$raw = file_get_contents('php://input', false, null, 0, MAX_BODY_BYTES + 1);
if ($raw === false || strlen($raw) > MAX_BODY_BYTES) {
    respond(413, ['ok' => false, 'error' => 'invalid_size']);
}

$payload = json_decode($raw, true);
if (!is_array($payload)) {
    respond(400, ['ok' => false, 'error' => 'invalid_json']);
}

$subject = cleanLine((string)($payload['subject'] ?? 'Syscalculator feedback'));
$kind = cleanLine((string)($payload['kind'] ?? 'Feedback'));
$name = cleanLine((string)($payload['name'] ?? ''));
$email = cleanLine((string)($payload['email'] ?? ''));
$message = trim((string)($payload['message'] ?? ''));
$supportInfo = trim((string)($payload['supportInfo'] ?? ''));
$version = cleanLine((string)($payload['version'] ?? ''));
$channel = cleanLine((string)($payload['releaseChannel'] ?? ''));
$submittedAt = cleanLine((string)($payload['submittedAtUtc'] ?? gmdate(DATE_ATOM)));

if ($message === '' && $subject === '') {
    respond(400, ['ok' => false, 'error' => 'empty_feedback']);
}

$mailSubject = '[Syscalculator feedback] ' . ($subject !== '' ? $subject : $kind);
$body = buildMailBody([
    'submittedAt' => $submittedAt,
    'kind' => $kind,
    'name' => $name,
    'email' => $email,
    'subject' => $subject,
    'version' => $version,
    'channel' => $channel,
    'message' => $message,
    'supportInfo' => $supportInfo,
    'ip' => $clientIp,
]);

$stored = SAVE_COPY ? saveCopy($payload, $body) : false;
$mailed = sendMail($mailSubject, $body, $email);

if (!$mailed && !$stored) {
    respond(500, ['ok' => false, 'error' => 'delivery_failed']);
}

respond(200, ['ok' => true, 'mailed' => $mailed, 'stored' => $stored]);

function buildMailBody(array $data): string
{
    return implode("\n", [
        'Syscalculator feedback',
        '======================',
        '',
        'Submitted: ' . dash($data['submittedAt']),
        'Kind: ' . dash($data['kind']),
        'Name: ' . dash($data['name']),
        'E-mail: ' . dash($data['email']),
        'Subject: ' . dash($data['subject']),
        'Version: ' . dash($data['version']),
        'Channel: ' . dash($data['channel']),
        'IP: ' . dash($data['ip']),
        '',
        'Message',
        '-------',
        trim((string)$data['message']),
        '',
        'Support information',
        '-------------------',
        trim((string)$data['supportInfo']),
        '',
    ]);
}

function sendMail(string $subject, string $body, string $replyTo): bool
{
    $headers = [
        'From: Syscalculator Feedback <' . FEEDBACK_FROM . '>',
        'MIME-Version: 1.0',
        'Content-Type: text/plain; charset=UTF-8',
        'X-Mailer: PHP/' . phpversion(),
    ];

    if ($replyTo !== '' && filter_var($replyTo, FILTER_VALIDATE_EMAIL)) {
        $headers[] = 'Reply-To: ' . $replyTo;
    }

    return @mail(FEEDBACK_TO, encodedSubject($subject), $body, implode("\r\n", $headers));
}

function saveCopy(array $payload, string $body): bool
{
    if (!is_dir(COPY_DIR) && !@mkdir(COPY_DIR, 0750, true) && !is_dir(COPY_DIR)) {
        return false;
    }

    protectCopyDir();

    $record = [
        'receivedAtUtc' => gmdate(DATE_ATOM),
        'payload' => $payload,
        'mailBody' => $body,
    ];

    $file = COPY_DIR . '/feedback-' . gmdate('Y-m') . '.jsonl';
    $line = json_encode($record, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE) . "\n";
    return @file_put_contents($file, $line, FILE_APPEND | LOCK_EX) !== false;
}

function rateLimit(string $clientIp): bool
{
    $dir = sys_get_temp_dir() . '/syscalculator-feedback-rate';
    if (!is_dir($dir) && !@mkdir($dir, 0700, true) && !is_dir($dir)) {
        return true;
    }

    $key = preg_replace('/[^a-zA-Z0-9_.-]/', '_', $clientIp);
    $file = $dir . '/' . $key;
    $now = time();
    $last = is_file($file) ? (int)@file_get_contents($file) : 0;
    if ($last > 0 && ($now - $last) < RATE_LIMIT_SECONDS) {
        return false;
    }

    @file_put_contents($file, (string)$now, LOCK_EX);
    return true;
}

function cleanLine(string $value): string
{
    return trim(str_replace(["\r", "\n"], ' ', $value));
}

function dash(string $value): string
{
    $value = trim($value);
    return $value === '' ? '-' : $value;
}

function encodedSubject(string $subject): string
{
    $subject = cleanLine($subject);
    return '=?UTF-8?B?' . base64_encode($subject) . '?=';
}

function protectCopyDir(): void
{
    $htaccess = COPY_DIR . '/.htaccess';
    if (!is_file($htaccess)) {
        @file_put_contents($htaccess, "Require all denied\nDeny from all\n", LOCK_EX);
    }

    $index = COPY_DIR . '/index.html';
    if (!is_file($index)) {
        @file_put_contents($index, "", LOCK_EX);
    }
}

function respond(int $status, array $body)
{
    http_response_code($status);
    echo json_encode($body, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE);
    exit;
}
