# JSON Resource File Format Reference

This document describes the JSON manifest format consumed by the `JsonResource` item. The manifest is a single JSON
**object**; each property becomes a resource entry.

## Sample

```json
{
  "SimpleKey": "SampleValue",
  "Number": 42,
  "Pi": 3.14159,
  "True": true,
  "Null": null,
  "SubKeys": {
    "Key1": "1",
    "SubKey2": {
      "Key2": "2"
    }
  },
  "$file:SimpleExternalRes1": "text_res.txt",
  "$file:SimpleExternalRes2": "bin_image.png",
  "$file:AdvExternalRes": {
    "file": "ExternalFile.txt",
    "mode": "text"
  }
}
```

## Key naming

- Property names are used as resource names verbatim.
- Nested objects are flattened into dotted keys: `{ "a": { "b": { "c": 1 } } }` produces the resource key
  `a.b.c`.
- Keys that start with the `FilePrefix` (default `$file:`) load their value from an external file instead of embedding
  the value directly. The prefix itself is stripped, and the remainder becomes the resource name (so
  `"$file:SimpleExternalRes1"` produces the resource name `SimpleExternalRes1`). `FilePrefix` can be changed per
  `JsonResource` item.

## Supported value types

| JSON Type | Stored .NET Type                      | Notes                                   |
|-----------|---------------------------------------|-----------------------------------------|
| `object`  | Nested resource keys (`parent.child`) | Recursively flattened                   |
| `string`  | `string`                              |                                         |
| `number`  | `long` / `decimal` / `double`         | See [Number handling](#number-handling) |
| `true`    | `bool`                                |                                         |
| `false`   | `bool`                                |                                         |
| `null`    | `null`                                |                                         |
| `array`   | *(not supported)*                     | Throws an error with a clear message    |

The root element **must** be a JSON object; an array or primitive root is rejected with an error.

### Number handling

To avoid precision loss, JSON numbers are stored with the most precise .NET type available:

1. Integral values within `long` range are stored as `long` (exact).
2. Otherwise, values within `decimal` range are stored as `decimal` (exact decimal arithmetic, up to 28-29 significant
   digits).
3. Values exceeding `decimal` range (e.g. `1e100`) fall back to `double`.

## External File Resource Specification

External-file keys (`FilePrefix` keys) accept two value shapes:

- A **string** — the file path. The resource type (load mode) is determined automatically by the file extension (`text`
  for known text extensions, otherwise `binary`).
- An **object** — advanced settings with `file` and `mode` properties:

| Property Name | Description                                        | Supported Values                             |
|---------------|----------------------------------------------------|----------------------------------------------|
| `file`        | The external resource file to be loaded            | Absolute/Relative File Path                  |
| `mode`        | The resource type (load mode) of the external file | `auto` (default), `text`, `binary`, `stream` |

### Load modes

| Mode     | Stored / Retrieved As | Description                                                                                                     |
|----------|-----------------------|-----------------------------------------------------------------------------------------------------------------|
| `auto`   | text or binary        | Default when `mode` is omitted. `text` if the extension is in the known text-extension set, otherwise `binary`. |
| `text`   | `string`              | The file content is read as UTF-8 text. (`txt` is accepted as an alias.)                                        |
| `binary` | `byte[]`              | The file content is read as raw bytes.                                                                          |
| `stream` | `Stream`              | The file stream is kept open and read by `Generate()`.                                                          |

Known text extensions (for `auto` mode): `.txt`, `.log`, `.ini`, `.xml`, `.json`, `.jsonc`, `.cs`, `.vb`,
`.css`, `.htm`, `.html`, `.js`, `.ts`, `.c`, `.cpp`, `.h`, `.go`, `.rs`, `.py`, `.rtf`, `.odt`, `.sql`, `.sh`.

### Path resolution

Relative file paths are resolved against the item's `ContextPath` (default: project directory; `FileDir`
means the directory containing the JSON manifest). Absolute paths are used as-is.

### `stream` mode semantics

The `stream` mode keeps the file stream open until `Generate()` is called, at which point the content is read into
memory. It is not a true streaming pipeline, so it does not reduce peak memory usage.

## Duplicate detection

Resource keys are compared case-insensitively (matching `ResourceManager` lookup semantics). A JSON structure like
`{ "a.b": 1, "a": { "b": 2 } }` produces the duplicated key `a.b`
and throws an `ArgumentException` during conversion.

## Authoring conveniences

Trailing commas and comments are allowed in the manifest (`//`, `/* ... */`), so it is convenient to maintain by hand.
