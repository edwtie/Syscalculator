# Syscalculator Feedback Endpoint

Upload `index.php` to a PHP-capable host, for example:

```text
https://www.tiedragon.com/api/syscalculator-feedback/index.php
```

Then configure Syscalculator by creating `feedback-endpoint.txt` next to `Syscalculator.exe`:

```text
https://www.tiedragon.com/api/syscalculator-feedback/index.php
```

The app posts JSON to this endpoint. If the endpoint is missing or fails, the app falls back to the existing mail/clipboard route.

## Host Notes

- Edit `FEEDBACK_TO` and `FEEDBACK_FROM` in `index.php` if needed.
- The endpoint accepts only `POST`.
- Payload limit is 128 KB.
- Basic per-IP rate limiting is enabled.
- A backup JSONL copy is saved in `inbox/` when the host allows writing.
- Make sure PHP `mail()` is configured by the hosting provider.
