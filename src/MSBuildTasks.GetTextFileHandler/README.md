# GetTextFileHandler

This NuGet package can help you compile GetText PO file automatically when you build your .NET apps using MSBuild.

Currently this package is enough for normal usage. Maybe more features are needed to be implemented in the future.

## Requirements

Tool `msgfmt` must be installed and configured properly in your `PATH` environment variable.

## Usage

After installing this package, add PO files as `PoSrcFile` in your project.

```xml
<ItemGroup>
    <PoSrcFile Include="Test.po" />
</ItemGroup>
```

And these PO files will get processed to MO files in the build phase.
