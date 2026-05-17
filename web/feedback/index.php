<?php

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
    respond(405, array('ok' => false, 'error' => 'method_not_allowed'));
}

$clientIp = isset($_SERVER['REMOTE_ADDR']) ? $_SERVER['REMOTE_ADDR'] : 'unknown';
if (!rateLimit($clientIp)) {
    respond(429, array('ok' => false, 'error' => 'rate_limited'));
}

$contentLength = (int)(isset($_SERVER['CONTENT_LENGTH']) ? $_SERVER['CONTENT_LENGTH'] : 0);
if ($contentLength <= 0 || $contentLength > MAX_BODY_BYTES) {
    respond(413, array('ok' => false, 'error' => 'invalid_size'));
}

$raw = file_get_contents('php://input', false, null, 0, MAX_BODY_BYTES + 1);
if ($raw === false || strlen($raw) > MAX_BODY_BYTES) {
    respond(413, array('ok' => false, 'error' => 'invalid_size'));
}

$payload = json_decode($raw, true);
if (!is_array($payload)) {
    respond(400, array('ok' => false, 'error' => 'invalid_json'));
}

$subject = cleanLine((string)getValue($payload, 'subject', 'Syscalculator feedback'));
$kind = cleanLine((string)getValue($payload, 'kind', 'Feedback'));
$name = cleanLine((string)getValue($payload, 'name', ''));
$email = cleanLine((string)getValue($payload, 'email', ''));
$message = trim((string)getValue($payload, 'message', ''));
$supportInfo = trim((string)getValue($payload, 'supportInfo', ''));
$version = cleanLine((string)getValue($payload, 'version', ''));
$channel = cleanLine((string)getValue($payload, 'releaseChannel', ''));
$submittedAt = cleanLine((string)getValue($payload, 'submittedAtUtc', gmdate(DATE_ATOM)));

if ($message === '' && $subject === '') {
    respond(400, array('ok' => false, 'error' => 'empty_feedback'));
}

$mailSubject = '[Syscalculator feedback] ' . ($subject !== '' ? $subject : $kind);
$body = buildMailBody(array(
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
));

$stored = SAVE_COPY ? saveCopy($payload, $body) : false;
$mailed = sendMail($mailSubject, $body, $email);

if (!$mailed && !$stored) {
    respond(500, array('ok' => false, 'error' => 'delivery_failed'));
}

respond(200, array('ok' => true, 'mailed' => $mailed, 'stored' => $stored));

function buildMailBody($data)
{
    return implode("\n", array(
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
    ));
}

function sendMail($subject, $body, $replyTo)
{
    $headers = array(
        'From: Syscalculator Feedback <' . FEEDBACK_FROM . '>',
        'MIME-Version: 1.0',
        'Content-Type: text/plain; charset=UTF-8',
        'X-Mailer: PHP/' . phpversion(),
    );

    if ($replyTo !== '' && filter_var($replyTo, FILTER_VALIDATE_EMAIL)) {
        $headers[] = 'Reply-To: ' . $replyTo;
    }

    return @mail(FEEDBACK_TO, encodedSubject($subject), $body, implode("\r\n", $headers));
}

function saveCopy($payload, $body)
{
    if (!is_dir(COPY_DIR) && !@mkdir(COPY_DIR, 0750, true) && !is_dir(COPY_DIR)) {
        return false;
    }

    protectCopyDir();

    $record = array(
        'receivedAtUtc' => gmdate(DATE_ATOM),
        'payload' => $payload,
        'mailBody' => $body,
    );

    $file = COPY_DIR . '/feedback-' . gmdate('Y-m') . '.jsonl';
    $line = json_encode_compat($record) . "\n";
    return @file_put_contents($file, $line, FILE_APPEND | LOCK_EX) !== false;
}

function rateLimit($clientIp)
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

function cleanLine($value)
{
    return trim(str_replace(array("\r", "\n"), ' ', $value));
}

function dash($value)
{
    $value = trim($value);
    return $value === '' ? '-' : $value;
}

function encodedSubject($subject)
{
    $subject = cleanLine($subject);
    return '=?UTF-8?B?' . base64_encode($subject) . '?=';
}

function protectCopyDir()
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

function respond($status, $body)
{
    if (function_exists('http_response_code')) {
        http_response_code($status);
    } else {
        header(statusHeader($status));
    }

    echo json_encode_compat($body);
    exit;
}

function getValue($array, $key, $default)
{
    return isset($array[$key]) ? $array[$key] : $default;
}

function json_encode_compat($value)
{
    $flags = 0;
    if (defined('JSON_UNESCAPED_SLASHES')) {
        $flags |= JSON_UNESCAPED_SLASHES;
    }
    if (defined('JSON_UNESCAPED_UNICODE')) {
        $flags |= JSON_UNESCAPED_UNICODE;
    }

    return $flags === 0 ? json_encode($value) : json_encode($value, $flags);
}

function statusHeader($status)
{
    $texts = array(
        200 => 'OK',
        400 => 'Bad Request',
        405 => 'Method Not Allowed',
        413 => 'Payload Too Large',
        429 => 'Too Many Requests',
        500 => 'Internal Server Error',
    );
    $text = isset($texts[$status]) ? $texts[$status] : 'Status';
    return 'HTTP/1.1 ' . $status . ' ' . $text;
}
