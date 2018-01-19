#### How to create Nuget package

commands:

    > nuget spec
    > nuget pack nuget/RafaelSoft.TsCodeGen.nuspec

Notes:

 - `nuget spec` will generate `Package.nuspec` file.
 - Fill it in, rename to `RafaelSoft.TsCodeGen.nuspec`, etc
 - Build the release, otherwise `..\RafaelSoft.TsCodeGen.WebApi\bin\Release\` will not have the latest dlls
 - `nuget pack nuget/RafaelSoft.TsCodeGen.nuspec` will use it to generate the `.nupkg`
 - `target` dlls should go in the `lib\` folder, nuget gives you a lot of helpful warnings
 - `src="..\` is relative to location of `Package.nuspec`

sample nuspec file:

    <?xml version="1.0"?>
    <package >
      <metadata>
        <id>RafaelSoft.TsCodeGen.WebApi</id>
        ...
        <dependencies>
          <dependency id="Newtonsoft.Json" version="10.0.3" />
          ...
        </dependencies>
      </metadata>
      <files>
        <file src="..\RafaelSoft.TsCodeGen.WebApi\bin\Release\RafaelSoft.TsCodeGen.WebApi.dll" target="lib\net461" />
      </files>
    </package>
