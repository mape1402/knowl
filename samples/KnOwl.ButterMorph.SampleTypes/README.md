# KnOwl ButterMorph Sample Types

This sample library exposes C# custom language types that can be uploaded in KnOwl through **Async Contracts > Type Bundles**.

Build the project and upload the generated DLL:

```powershell
dotnet build .\samples\KnOwl.ButterMorph.SampleTypes\KnOwl.ButterMorph.SampleTypes.csproj -c Release -f net9.0
```

Upload this file in the ButterMorph Type Bundle designer:

```text
samples\KnOwl.ButterMorph.SampleTypes\bin\Release\net9.0\KnOwl.ButterMorph.SampleTypes.dll
```

The bundle key is `knowl-sample-contract-types`, the language is `csharp`, and the discovered types can be used by the Type Mappings designer.
