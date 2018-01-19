using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Generator;
using RafaelSoft.TsCodeGen.Models;
using RafaelSoft.TsCodeGen.WebApi.Gen;

namespace RafaelSoft.TsCodeGen.WebApi.Models
{
    public class EndpointMethodSpec
    {
        public string EndpointMethodName { get; set; }
        public string HttpMethod { get; set; }
        public string UrlTsFriendly { get; set; }
        public Type ResponseType { get; internal set; }
        public TsCsTypeSpec ResponseTypeSpec { get; internal set; }
        public List<EndpointRequestParamSpec> BodyParams { get; set; }
        public List<EndpointRequestParamSpec> UriParams { get; set; }
        public EndpointRequestParamSpec UriParamsWrapperClass { get; internal set; }

        // documentation-specific fields
        public string DocHttpMethod { get; set; }
        public string DocUrlFull { get; set; }
        public string DocRequestJson { get; set; }
        public string DocResponseJson { get; set; }
        public string DocDescription { get; set; }

        // computed properties
        public IEnumerable<EndpointRequestParamSpec> AllInputParams =>
            (UriParamsWrapperClass != null)
                ? new[] { UriParamsWrapperClass }
                : UriParams
                    .Where(para => !para.IsOptional)
                    .Union(BodyParams)
                    .Union(UriParams.Where(para => para.IsOptional));

        public string DebugString => $@"
HttpMethod: {HttpMethod}
UrlFull: {DocUrlFull}
UrlVar:  {UrlTsFriendly}
ResponseType: {ResponseTypeSpec.ToTsString()}
UriParams:
{string.Join("\n", UriParams.Select(x => " - " + x.DebugString))}
BodyParams:
{string.Join("\n", BodyParams.Select(x => " - " + x.DebugString))}
";

        public string BuildTsReviverString(string param = "response")
        {
            if (ResponseTypeSpec.GetTsAtomicReviver(param) == null)
                return null; // NOTE: no atomic revival needed

            // TODO: maybe in the future we will have custom revivers on some of the EndpointMethodSpec
            //if (ResponseTypeSpec.IsMine && CustomTsReviverScript != null)
            //    return $"({GetTsAtomicReviver_withMyCustomScriptCheck("x")})({param})"; // NOTE: special case: custom TS reviver script must be treated/invoked like a lambda
            return ResponseTypeSpec.GetTsFullReviver(param);
        }
    }

    //public class EndpointMethodSpec
    //{
    //    public string EndpointMethodName { get; set; }
    //    public string UrlFull { get; set; }
    //    public string Url { get; set; }
    //    public string TsResultType { get; set; }
    //    
    //}

    public class EndpointRequestParamSpec
    {
        public string Name { get; set; }
        public bool IsOptional { get; set; }
        public Type ParamType { get; set; }
        public string ParamTypeTsString { get; set; }

        public string DebugString => $"{Name}:{ParamTypeTsString}:{IsOptional.ToString("optional", "required")}";
    }

    public class EndpointRequestHttpParamParamSpec
    {
        public string Key { get; set; }
        public string VarName { get; set; }
        public bool IsString { get; set; }
    }
}
