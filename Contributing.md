# Contributing

Contributions are very welcome!

## Prereqs

1. Install .NET  6: https://get.dot.net
2. Install VSCode
3. Run (F5)

## Launch Config

The .vscode launch config has several parameters configured. When new flags are added, its best to add them to the launch config for easier testing.

## Build

~~~terminal
dotnet build
~~~

This will build a debug version to the `bin` folder of the project. The output
will be in the `bin\Debug\net6.0` folder by default.

## Testing

~~~terminal
dotnet test .\AzureDevOps.WikiPDFExport.Test\
~~~
