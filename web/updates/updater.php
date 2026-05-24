<?php
declare(strict_types=1);

/*
 * Syscalculator update manifest endpoint.
 *
 * Upload this file together with syscalculator.json to:
 *   /api/syscalculator-updates/
 *
 * The updater must not contain hardcoded daily metadata. The daily manifest is
 * maintained in syscalculator.json so packageId and sha256 stay in one place.
 */

const MANIFEST_FILE = __DIR__ . '/syscalculator.json';

header('Content-Type: application/json; charset=utf-8');
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');
header('Pragma: no-cache');
header('X-Content-Type-Options: nosniff');

if (!is_file(MANIFEST_FILE) || !is_readable(MANIFEST_FILE)) {
    http_response_code(500);
    echo json_encode([
        'error' => 'update_manifest_missing',
        'message' => 'Syscalculator update manifest is not available.'
    ], JSON_UNESCAPED_SLASHES);
    exit;
}

$manifest = file_get_contents(MANIFEST_FILE);
if ($manifest === false || trim($manifest) === '') {
    http_response_code(500);
    echo json_encode([
        'error' => 'update_manifest_empty',
        'message' => 'Syscalculator update manifest is empty.'
    ], JSON_UNESCAPED_SLASHES);
    exit;
}

try {
    json_decode($manifest, true, 512, JSON_THROW_ON_ERROR);
} catch (JsonException $exception) {
    error_log('Syscalculator updater manifest JSON error: ' . $exception->getMessage());
    http_response_code(500);
    echo json_encode([
        'error' => 'update_manifest_invalid',
        'message' => 'Syscalculator update manifest is invalid.'
    ], JSON_UNESCAPED_SLASHES);
    exit;
}

echo $manifest;

