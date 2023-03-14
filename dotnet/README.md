# ObjectBasin
.NET library to stream updates to an object.
It is called Basin because streams flow into a basin.

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