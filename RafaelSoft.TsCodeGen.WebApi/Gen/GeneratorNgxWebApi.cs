using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Generator;
using RafaelSoft.TsCodeGen.Generator.NgxGenerators;
using RafaelSoft.TsCodeGen.Models;
using RafaelSoft.TsCodeGen.WebApi.Models;

namespace RafaelSoft.TsCodeGen.WebApi.Gen
{
    public class GeneratorNgxWebApi
    {
        public const string Ts_HttpParamsBuilder = @"/**
 * This is a utility class to avoid adding 'null' or 'undefined' to HttpParams,
 * which unfortunatelly happens when calling e.g. HttpParams.set('key', null)
 */
class HttpParamsBuilder {
  private p:HttpParams;
  constructor() {
    this.p = new HttpParams();
  }
  set(key: string, value: string):HttpParamsBuilder {
    if (value)
      this.p = this.p.set(key, value);
    return this;
  }
  setAsStr(key: string, value: any):HttpParamsBuilder {
    if (value)
      this.p = this.p.set(key, `${value}`);
    return this;
  }
  toHttpParams():HttpParams {
    return this.p;
  }
}";
        public EndpointMethodCollection EndpointMethods { get; set; }
        public string AngularClassName { get; set; } = "RestService";
        public GeneratorTsClasses TsClassesGen { get; set; }

        public GeneratorNgxWebApi(TsImportsManager importsManager)
        {
            importsManager.AddImportStatements(new[]
            {
                TsCodeImports.Injectable,
                TsCodeImports.InjectionTokenImports,
                TsCodeImports.HttpImports,
                TsCodeImports.Lodash,
                TsCodeImports.Subject,
                TsCodeImports.toPromise,
            });
        }

        public string Generate()
        {
            var code_methods = EndpointMethods.Methods
                .Select(m => Generate_webApiCall(m).Trim().RemoveEmptyLines())
                .StringJoin("\n\n");
            var code_makeRootProviders = NgxTsSnippets.Service_makeRootProviders(AngularClassName);

            return $@"
{code_makeRootProviders}

@Injectable()
export class {AngularClassName} {{
  private headers = new HttpHeaders({{'Content-Type': 'application/json'}});

  constructor(
    @Inject({AngularClassName}_apiUrl)
    private apiUrlPrefix: string,
    private http: HttpClient
  ) {{}}

  {code_methods.Trim().IndentEveryLine("  ", skipFirst: true)}
  
  public readonly onError: Subject<any> = new Subject<any>();
  
  private handleError(error: any): Promise<any> {{
    this.onError.next(error.error);
    //console.error('An error occurred', error);
    return Promise.reject(error.error);
  }}
}}
";
        }

        private string Generate_webApiCall(EndpointMethodSpec endpointMethod)
        {
            var inputParams = string.Join(", ", endpointMethod.AllInputParams.Select(p => p.Name + p.IsOptional.ToString("?", "") + ":" + p.ParamTypeTsString));
            var bodyParamPassed = (endpointMethod.HttpMethod == "get" || endpointMethod.HttpMethod == "delete")
                ? ""
                : (endpointMethod.BodyParams.Any()
                    ? endpointMethod.BodyParams[0].Name
                    : "null");
            var warningMessageMoreThan1BodyParam = (endpointMethod.BodyParams.Count() > 1)
                ? " // WARNING: cannot pass more than 1 body parameter to http." + endpointMethod.HttpMethod + ", however more than 1 was given: " + string.Join(", ", endpointMethod.BodyParams.Select(p => p.Name))
                : "";
            var thenTsReviverCode = (endpointMethod.BuildTsReviverString() != null)
                ? $".then(response => {endpointMethod.BuildTsReviverString("response")})"
                : "";
            return $@"
/**
 * {endpointMethod.DocDescription.TrimEveryLine().IndentEveryLine(" * ", skipFirst: true)}
 * URL: {endpointMethod.DocHttpMethod} {endpointMethod.DocUrlFull}
 * {endpointMethod.DebugString.TrimEveryLine().IndentEveryLine(" * ", skipFirst: true)}
 */
public {endpointMethod.EndpointMethodName}({inputParams})
  : Promise<{endpointMethod.ResponseTypeSpec.ToTsString()}>
{{
  let options = {BuildOptionsTs(endpointMethod).IndentEveryLine("  ", skipFirst:true)};
  return this.http
    .{endpointMethod.HttpMethod}<{endpointMethod.ResponseTypeSpec.ToTsString()}>(`${{this.apiUrlPrefix}}/{endpointMethod.UrlTsFriendly}`{bodyParamPassed.PrefixIfNotEmpty(", ")}, options){warningMessageMoreThan1BodyParam}
    .toPromise()
    {thenTsReviverCode}
    .catch(error => this.handleError(error));
}}
";
        }

        public string BuildOptionsTs(EndpointMethodSpec endpointMethod)
        {
            var optionsTsList = new List<string>();
            optionsTsList.Add("headers: this.headers");
            var optionalUriParams = endpointMethod.UriParams; //.Where(p => p.IsOptional);
            if (optionalUriParams.Any())
            {
                var optionalParamsTs = optionalUriParams
                    .Select(p => new EndpointRequestHttpParamParamSpec
                    {
                        Key = p.Name,
                        VarName = (endpointMethod.UriParamsWrapperClass != null)
                            ? $"{endpointMethod.UriParamsWrapperClass.Name}.{TsClassesGen?.GetPropertyName(endpointMethod.UriParamsWrapperClass.ParamType, p.Name) ?? p.Name}"
                            : p.Name,
                        IsString = (p.ParamTypeTsString == "string"),
                    })
                    .ToList();
                optionsTsList.Add("params: " + TsHttpUtils.BuildHttpParamsFromTheseParams(optionalParamsTs).IndentEveryLine("  ", skipFirst: true));
            }
            if ((endpointMethod.HttpMethod == "get" || endpointMethod.HttpMethod == "delete") && endpointMethod.BodyParams.Any())
                optionsTsList.Add("body: " + endpointMethod.BodyParams.FirstOrDefault().Name);
            return TsHttpUtils.BuildReadableJsonStructWithProperties(optionsTsList);
        }
    }
}
