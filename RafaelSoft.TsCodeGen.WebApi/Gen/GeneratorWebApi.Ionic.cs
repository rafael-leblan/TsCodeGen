using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Common;
using RafaelSoft.TsCodeGen.Generator;
using RafaelSoft.TsCodeGen.Generator.NgxGenerators;
using RafaelSoft.TsCodeGen.Models;
using RafaelSoft.TsCodeGen.WebApi.Models;

namespace RafaelSoft.TsCodeGen.WebApi.Gen
{
    public class GeneratorWebApiIonic
    {
        /// <summary>
        /// NOTE: this is a mandatory code snippet that is required for NgxWebApi service class to function
        /// </summary>
        public const string Ts_WebApiUtilityClasses = @"
/**
 * This is a utility class to avoid adding 'null' or 'undefined' to http params in ionic's HTTP service,
 */
class HttpParamsBuilder {
  private params: any;
  constructor() {
    this.params = {};
  }
  set(key: string, value: string): HttpParamsBuilder {
    if (value != undefined && value != null)
      this.params[key] = encodeURIComponent(value);
    return this;
  }
  setAsStr(key: string, value: any): HttpParamsBuilder {
    if (value != undefined && value != null)
      this.params[key] = encodeURIComponent(value);
    return this;
  }
  toHttpParams(): any {
    return this.params;
  }
}

/**
 * An extension of the standard Response class, adding an error field
 */
export interface HttpResponse extends Response {
  error: any;
}";

        public EndpointMethodCollection EndpointMethods { get; set; }
        public string AngularClassName { get; set; } = "RestService";
        public ITsClassGenerationConfig TsGenConfig { get; set; }

        public GeneratorWebApiIonic(TsImportsManager importsManager)
        {
            importsManager.AddImportStatements(new[]
            {
                TsCodeImports.Injectable,
                TsCodeImports.InjectionTokenImports,
                //TsCodeImports.HttpImports,
                TsCodeImports.Lodash,
                TsCodeImports.Subject,
                TsCodeImports.rxjs_take,
                TsCodeImports.ionicNativeHttp,
            });
        }

        public string Generate()
        {
            var code_methods = EndpointMethods.Methods
                .ConditionallyIf(TsGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.EndpointMethodName))
                .Select(m => Generate_webApiCall(m).Trim().RemoveEmptyLines())
                .StringJoin("\n\n");
            var code_makeRootProviders = NgxTsSnippets.Service_makeRootProviders(AngularClassName);

            return $@"
{code_makeRootProviders}

@Injectable()
export class {AngularClassName} {{
  private headersRaw = {{ 'Content-Type': 'application/json' }};

  constructor(
    @Inject({AngularClassName}_apiUrl)
    private apiUrlPrefix: string,
    private httpIonic: HTTP
  ) {{
    this.httpIonic.setDataSerializer('json');
  }}

  private getHttpHeaders(): any {{
    let token = sessionStorage.getItem('currentUser.token'); // TODO: 7d4100d0: use StorageKeys.Token in codegen
    let headers = token
      ? Object.assign({{}}, this.headersRaw, {{'Authorization': 'Session ' + token}})
      : this.headersRaw;
  }}

  {code_methods.Trim().IndentEveryLine("  ", skipFirst: true)}
  
  public readonly onError: Subject<HttpResponse> = new Subject<HttpResponse>();
  
  private handleError(error: HttpResponse): Promise<any> {{
    this.onError.next(error);
    //console.error('An error occurred', error);
    return Promise.reject(error);
  }}
}}
";
        }

        private string Generate_webApiCall(EndpointMethodSpec endpointMethod)
        {
            var inputParams = string.Join(", ", endpointMethod.AllInputParams.Select(p => p.Name + p.IsOptional.ToString("?", "") + ": " + p.GetParamTsString_Type(TsGenConfig)));
            var bodyParamPassed = "uriParamsObj";
            var letParamsTs = $"let uriParamsObj = {BuildOptionsTs(endpointMethod)};";
            if (endpointMethod.BodyParams.Any())
            {
                bodyParamPassed = endpointMethod.BodyParams[0].Name;
                letParamsTs = "";
            }
            var warningMessageMoreThan1BodyParam = (endpointMethod.BodyParams.Count() > 1)
                ? " // WARNING: cannot pass more than 1 body parameter to http." + endpointMethod.HttpMethod + ", however more than 1 was given: " + string.Join(", ", endpointMethod.BodyParams.Select(p => p.Name))
                : "";
            var thenTsReviverCode = (endpointMethod.BuildTsReviverString(TsGenConfig) != null)
                ? $".then(response => {endpointMethod.BuildTsReviverString(TsGenConfig, "response")})"
                : "";
            var responseTypeTsType = endpointMethod.ResponseTypeSpec.ToTsString(TsGenConfig);

            return $@"
/**
 * {endpointMethod.DocDescription.TrimEveryLine().IndentEveryLine(" * ", skipFirst: true)}
 * URL: {endpointMethod.DocHttpMethod} {endpointMethod.DocUrlFull}
 * {endpointMethod.DebugString(TsGenConfig).TrimEveryLine().IndentEveryLine(" * ", skipFirst: true)}
 */
public {endpointMethod.EndpointMethodName}({inputParams})
  : Promise<{responseTypeTsType}>
{{
  {letParamsTs.IndentEveryLine("  ", skipFirst: true)}
  let headers = this.getHttpHeaders();
  return this.httpIonic
    .{endpointMethod.HttpMethod}(`${{this.apiUrlPrefix}}/{endpointMethod.UrlTsFriendly}`{bodyParamPassed.PrefixIfNotEmpty(", ")}, headers){warningMessageMoreThan1BodyParam}
    .then(response => JSON.parse(response.data) as {responseTypeTsType})
    {thenTsReviverCode}
    .catch(error => this.handleError(error));
}}
";
        }

        public string BuildOptionsTs(EndpointMethodSpec endpointMethod)
        {
            var optionalUriParams = endpointMethod.UriParams; //.Where(p => p.IsOptional);
            if (!optionalUriParams.Any())
                return "{}";
            var optionalParamsTs = optionalUriParams
                .Select(p => new EndpointRequestHttpParamParamSpec
                {
                    Key = p.Name,
                    VarName = (endpointMethod.UriParamsWrapperClass != null)
                        ? $"{endpointMethod.UriParamsWrapperClass.Name}.{p.GetParamTsString_Name(TsGenConfig)}"
                        : p.Name,
                    IsString = (p.GetParamTsString_Type(TsGenConfig) == "string"),
                })
                .ToList();
            return TsHttpUtils.BuildHttpParamsFromTheseParams(optionalParamsTs);
        }
    }
}
