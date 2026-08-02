# Json2Resource

This NuGet package converts JSON resource file(s) to standard .NET resource (`.resources`) files and embeds
them into the target assembly automatically during build.

Currently, this package is enough for normal usage. Maybe more features are needed to be implemented in the future.

**WARNING**: This package may have some breaking changes in the future.

## Usage

After installing this package, add JSON resource file(s) as `JsonResource` item type:

```xml
<ItemGroup>
    <JsonResource Include="Test.json"/>
</ItemGroup>
```

The JSON manifest is converted to a `.resources` file and embedded into the assembly automatically.
The intermediate `.resources` files are generated under the `IntermediateOutputPath` and are removed when the
project is cleaned.

## How it works

1. `JsonResourcePropEval` evaluates each `JsonResource` item and computes its `LogicalName`
   (`RootNamespace` + directory-as-namespace + file name, with a `.resources` suffix) and `IntermediatePath`
   (`obj/{LogicalName}.resources`). It also scans every manifest for external files referenced through the
   `FilePrefix` keys.
2. `CompileJsonResources` converts each manifest into a `.resources` file. See
   [JSON_RES_FORMAT.md](JSON_RES_FORMAT.md) for the manifest format.
3. `CompileAndAddJsonResources` adds the generated files as `EmbeddedResource` items, so they are compiled
   into the assembly.

## Supported properties

| Property Name | Description                                                                    | Accepted Values                | Default Value           |
|---------------|--------------------------------------------------------------------------------|--------------------------------|-------------------------|
| FilePrefix    | The prefix of resource keys whose value is loaded from an external file        | Any string                     | `$file:`                |
| LogicalName   | The logical resource name in the assembly                                      | Any                            | Auto-determined by task |
| ContextPath   | Base directory for resolving relative external file paths in the manifest      | `FileDir`, or any directory    | Project directory       |

Notes:

- `LogicalName` defaults to `RootNamespace` + the manifest's relative path (directories become namespace
  segments, e.g. `Data/Test.json` under root namespace `MyApp` yields `MyApp.Data.Test`). Setting
  `LogicalName` explicitly overrides the whole name (the `.resources` suffix is appended automatically).
- `ContextPath` is used only when resolving *relative* paths in external-file (`$file:`) values. If it is set
  to `FileDir`, the base directory becomes the directory that contains the JSON manifest file; otherwise it is
  interpreted as a directory, defaulting to the project directory.

## Reading the resources at runtime

The resources are embedded with their logical names and can be loaded with `ResourceManager`:

```csharp
var rm = new ResourceManager("MyApp.Data.Test", typeof(SomeTypeInTheAssembly).Assembly);

string   text   = rm.GetString("SubKeys.Key1");   // nested keys are flattened with dots
byte[]   binary = (byte[])rm.GetObject("Image");  // external file, binary mode (prefix is stripped)
Stream   stream = rm.GetStream("Log");            // external file, stream mode (prefix is stripped)
```

## Incremental builds

The `CompileJsonResources` target tracks both the JSON resource files and the external files referenced by
`FilePrefix` keys as incremental-build inputs. Changes to either trigger regeneration, so the embedded
resources never go stale.
