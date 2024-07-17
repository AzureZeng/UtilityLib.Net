# JSON Resource File Format Reference

## Sample

```json
{
  "SimpleKey": "SampleValue",
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

## External File Resource Specification

The key of resources which load from external files should start with `$file:` (This can be changed by `FilePrefix`
property in `JsonResource` item).

If the value type of the property that represents external resources in JSON Resource file is `string`, then the
resource type (load mode) will be determined automatically by the task (`text` for generic text files, `binary` for
other file types).

You can use advanced settings to set the external resource file path and the resource type (load mode).

| Property Name | Description                                        | Supported Values            |
|---------------|----------------------------------------------------|-----------------------------|
| `file`        | The external resource file to be loaded            | Absolute/Relative File Path |
| `mode`        | The resource type (load mode) of the external file | `text`, `binary`, `stream`  | 
