# PulseAPK Smali Detection Rules

The rules below are designed for line-by-line scanning of apktool decompiled Smali. Each rule includes a category, short description, regex patterns (apply to the current line), optional context hints, and a rough confidence level. Apply library filtering first so only non-library code is highlighted by default; library matches can still be recorded with `package_type = "library"` if needed.

## Library Filtering
Treat these class prefixes as libraries by default and suppress or tag hits under them:
```
Landroidx/,
Lkotlin/,
Lkotlinx/,
Lcom/google/,
Lcom/squareup/,
Lokhttp3/,
Lokio/,
Lretrofit2/
```

## Rules (JSON)
```json
{
  "library_prefixes": [
    "Landroidx/",
    "Lkotlin/",
    "Lkotlinx/",
    "Lcom/google/",
    "Lcom/squareup/",
    "Lokhttp3/",
    "Lokio/",
    "Lretrofit2/"
  ],
  "rules": [
    {
      "category": "root_check",
      "description": "Exec calls to su/busybox/magisk/xposed binaries",
      "regex_patterns": [
        "(?i)invoke-static .*Runtime;->getRuntime\(\).*->exec\(.*\"(su|magisk|busybox|superuser|xposed)\"",
        "(?i)const-string [vp0-9, ]+\"(/system/xbin/su|/system/bin/su|/sbin/su|/system/bin/magisk|/system/bin/busybox)\""
      ],
      "context_hint": "Skip if class name starts with a library prefix.",
      "confidence": "high"
    },
    {
      "category": "root_check",
      "description": "Checks for known root packages or file paths",
      "regex_patterns": [
        "(?i)const-string [vp0-9, ]+\"(com\.topjohnwu\.magisk|eu\.chainfire\.supersu|com\.noshufou\.android\.su|com\.koushikdutta\.superuser|org\.meowcat\.edxposed|de\.robv\.android\.xposed\.installer)\"",
        "(?i)const-string [vp0-9, ]+\"(/data/local/tmp|/data/local|/system/app/Superuser|/system/app/SuperSU|/system/app/Magisk|/system/xbin/which)\""
      ],
      "context_hint": "Skip library classes; optionally require path/package string to be compared or passed into file/package checks in adjacent lines (look for invoke-virtual {.*} Ljava/io/File;->exists or Landroid/content/pm/PackageManager;->getPackageInfo).",
      "confidence": "medium"
    },
    {
      "category": "root_check",
      "description": "Build.* inspection for root/emulator tags",
      "regex_patterns": [
        "(?i)const-string [vp0-9, ]+\"(test-keys|test_keys|dev-keys|test-keys|userdebug|eng)\"",
        "(?i)invoke-static .*Landroid/os/Build;->get(Tags|Fingerprint|Brand|Model|Manufacturer|Product)\(\)"
      ],
      "context_hint": "Trigger when a suspicious tag string literal is within 3 lines of a Build.* getter or equals/contains check (look for ->equals or ->contains).",
      "confidence": "medium"
    },
    {
      "category": "root_check",
      "description": "Busybox or su presence via which or file existence",
      "regex_patterns": [
        "(?i)const-string [vp0-9, ]+\"which\"",
        "(?i)const-string [vp0-9, ]+\"su\""
      ],
      "context_hint": "Only trigger when the same method also calls File;->exists, File;->canExecute, or Runtime;->exec in nearby lines.",
      "confidence": "medium"
    },
    {
      "category": "emulator_check",
      "description": "System property checks for emulator indicators",
      "regex_patterns": [
        "(?i)const-string [vp0-9, ]+\"ro\\.kernel\\.qemu\"",
        "(?i)const-string [vp0-9, ]+\"(ro\\.product\\.(manufacturer|brand|model|device|name)|ro\\.hardware|ro\\.serial|gsm\\.operator\\.numeric)\""
      ],
      "context_hint": "Increase confidence if paired with substring checks for sdk, emulator, generic, google_sdk, vbox, genymotion within 3 lines.",
      "confidence": "high"
    },
    {
      "category": "emulator_check",
      "description": "Comparison against emulator fingerprints, models, or brands",
      "regex_patterns": [
        "(?i)const-string [vp0-9, ]+\"(generic|sdk|google_sdk|vbox|test-keys|emulator|goldfish|ranchu|sdk_gphone|Genymotion|BlueStacks|Nox|MuMu|LDPlayer)\"",
        "(?i)invoke-static .*Landroid/os/Build;->(FINGERPRINT|MODEL|BRAND|PRODUCT|HARDWARE|DEVICE)"
      ],
      "context_hint": "Trigger when suspicious string is compared to Build.* values using equals/contains/startsWith in nearby lines. Skip library classes.",
      "confidence": "medium"
    },
    {
      "category": "emulator_check",
      "description": "Fake device ID/phone number checks",
      "regex_patterns": [
        "(?i)const-string [vp0-9, ]+\"(15555215554|15555215556|15555215558|000000000000000|eMulator|android-emulator)\""
      ],
      "context_hint": "Look for usage with TelephonyManager methods (getDeviceId|getLine1Number) in the same method.",
      "confidence": "medium"
    },
    {
      "category": "hardcoded_creds",
      "description": "Identifier names suggesting credentials paired with string constants",
      "regex_patterns": [
        "(?i)(password|passwd|pwd|secret|token|api[_-]?key|auth|login|user(name)?|email)[^\n]*=\s*\"[^\"]{4,}\"",
        "(?i)const-string [vp0-9, ]+\"[^\"]{4,}\""
      ],
      "context_hint": "Only report when the const-string line is within 2 lines of a .field or move-result referencing an identifier containing credential keywords. Ignore enums/UI hints containing AutofillType|InputType|EditorInfo.",
      "confidence": "high"
    },
    {
      "category": "hardcoded_creds",
      "description": "Authorization headers with embedded tokens",
      "regex_patterns": [
        "(?i)const-string [vp0-9, ]+\"Authorization: (Bearer|Basic|Token) [^\"]+\"",
        "(?i)const-string [vp0-9, ]+\"(Bearer|Basic) [A-Za-z0-9\\-_.:+/]{8,}\""
      ],
      "context_hint": "Skip library classes; treat as high-confidence when header string is concatenated with http requests (look for okhttp/retrofit request builders or HttpURLConnection).",
      "confidence": "high"
    },
    {
      "category": "hardcoded_creds",
      "description": "Long random-looking tokens/keys",
      "regex_patterns": [
        "const-string [vp0-9, ]+\"[A-Za-z0-9+/]{16,}=*\"",
        "const-string [vp0-9, ]+\"[A-Fa-f0-9]{32,}\""
      ],
      "context_hint": "Exclude matches when preceding identifier contains words like layout, theme, color, icon, font. Prefer when nearby identifiers include key/token/secret.",
      "confidence": "medium"
    },
    {
      "category": "sql_query",
      "description": "Raw SQL statements in strings",
      "regex_patterns": [
        "(?i)const-string [vp0-9, ]+\"\s*(SELECT|INSERT|UPDATE|DELETE|CREATE TABLE|DROP TABLE|ALTER TABLE)\b[\s\S]*\"",
        "(?i)const-string [vp0-9, ]+\"\s*(FROM|WHERE|VALUES|SET)\b[\s\S]*\""
      ],
      "context_hint": "Merge adjacent string fragments within same method to catch concatenated queries. Suppress when class is under library prefixes or file path contains /database/.*(migration|schema)/ from known ORMs unless package_type is app.",
      "confidence": "high"
    },
    {
      "category": "sql_query",
      "description": "Exec/compile of SQL APIs with raw strings",
      "regex_patterns": [
        "invoke-virtual .*Landroid/database/sqlite/SQLiteDatabase;->(execSQL|rawQuery|compileStatement)\(Ljava/lang/String;",
        "invoke-interface .*Landroid/database/Cursor;->(rawQuery|execSQL)"
      ],
      "context_hint": "Only elevate when a const-string in the same method contains SQL keywords. Skip common migration helpers under library prefixes.",
      "confidence": "medium"
    },
    {
      "category": "http_url",
      "description": "Capture HTTP/HTTPS URLs",
      "regex_patterns": [
        "const-string [vp0-9, ]+\"https?://[^\"\\s]+\""
      ],
      "context_hint": "Classify package_type using class prefix. Flag non-HTTPS (http://) as potential security issue. Extract hostname/path; highlight if path contains /api/|/v1|/v2|/auth|/login|/user|/token|/payment|/admin.",
      "confidence": "high"
    }
  ]
}
```
```

## Application Notes
- Apply regexes after removing trailing comments where possible to avoid hits in comment-only lines.
- When evaluating proximity (e.g., "within 2-3 lines"), operate inside the same method block to reduce cross-method noise.
- Emit both `package_type` and rule metadata so library hits can be shown separately instead of hidden.
- For concatenated strings split across lines, combine sequential `const-string` + `invoke-virtual {..} Ljava/lang/StringBuilder;->append` chunks before evaluating keyword patterns.
```
