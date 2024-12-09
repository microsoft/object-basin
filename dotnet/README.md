# ObjectBasin
.NET library to stream updates to an object using JSONPaths and JSON Patch Pointers.
It is called Basin because streams flow into a basin.

See in [NuGet](https://www.nuget.org/packages/ObjectBasin).

This library supports various ways to update objects or their contents:
* appending to the end of a string
* inserting anywhere in a string
* removing characters from a string
* appending to the end of a list
* inserting anywhere in a list
* overwriting an item in a list
* deleting items in a list
* setting a value in an object

Learn more at https://github.com/microsoft/object-basin.

# Unsupported Operations
Unlike the JavaScript/TyperScript version which relies on simpler objects, this .NET version does not yet support operations such as adding or modifying all items within `JsonElement`s or other complex structures.
String manipulation is supporting within `JsonElement`s, but not other operations such as adding a list or replacing an item with something that is not a string.

To be clear, deep manipulation within .NET objects (POCOs) is fine. The limitation is with some complex objects that require their own complex parsers.

# Install
```bash
dotnet add package ObjectBasin
```

# Examples
See examples in the [tests](tests/Tests/BasinTests.cs).

# Development
## Code Formatting
CI enforces:
```bash
dotnet format --verify-no-changes --severity info --no-restore *.sln
```

To automatically format code, run:
```bash
dotnet format --severity info --no-restore *.sln
```

## Publishing
From the dotnet folder in the root of the repo, run:
```bash
$api_key=<your NuGet API key>
dotnet pack --configuration Release
dotnet nuget push src/bin/Release/ObjectBasin.*.nupkg --source https://api.nuget.org/v3/index.json -k $api_key --skip-duplicate
```
