nuget pack nuget/RafaelSoft.TsCodeGen.nuspec
nuget pack nuget/RafaelSoft.TsCodeGen.WebApi.nuspec
start nuget push RafaelSoft.TsCodeGen.1.0.1.nupkg -source https://www.nuget.org
start nuget push RafaelSoft.TsCodeGen.WebApi.1.0.1.nupkg -source https://www.nuget.org