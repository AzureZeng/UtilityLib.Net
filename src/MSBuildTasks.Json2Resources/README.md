# Json2Resource

This NuGet package helps you convert JSON resource file(s) to .NET resource files.

Currently, this package is enough for normal usage. Maybe more features are needed to be implemented in the future.

**WARNING**: This package may have some break changes in the future.

## Usage

After installing this package, add JSON resource file(s) as `JsonResource` item type.

```xml

<ItemGroup>
    <JsonResource Include="Test.json"/>
</ItemGroup>
```

It will be compiled and embedded to assembly automatically.

## Supported properties

| Property Name | Description                                                              | Default Value           |
|---------------|--------------------------------------------------------------------------|-------------------------|
| FilePrefix    | The prefix of resource keys which resources are load from external files | `$file:`                |
| LogicalName   | The logical resource name in assembly                                    | Auto-determined by task |
