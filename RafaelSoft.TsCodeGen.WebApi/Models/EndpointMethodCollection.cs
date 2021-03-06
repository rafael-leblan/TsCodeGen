using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http; // LESSON: need to install Microsoft.AspNet.WebApi.Core for this
using System.Web.Http.Description;
using RafaelSoft.TsCodeGen.Models;
using RafaelSoft.TsCodeGen.Services;
using RafaelSoft.TsCodeGen.WebApi.Models;
using RafaelSoft.TsCodeGen.WebApi.WebUtils;
using RafaelSoft.TsCodeGen.WebApi.WebUtils.SampleGeneration;

namespace RafaelSoft.TsCodeGen.WebApi.Models
{
    public class EndpointMethodCollection
    {
        private readonly List<Type> IgnoreTheseTypes = new List<Type>
        {
            typeof(HttpRequestMessage),
            typeof(HttpResponseMessage),
            typeof(IHttpActionResult),
            typeof(HttpStatusCode),
        };
        private readonly string[] PreferUrlMethodNameIfTheseHttpActions = new[] { "get", "put", "post", "delete" };
        private readonly ITsCodeGenLogger tsLogger;
        private readonly TsClassCollection classes;
        private readonly HttpConfiguration httpConfig;
        private readonly List<EndpointMethodSpec> apiSpecs = new List<EndpointMethodSpec>();

        public IEnumerable<EndpointMethodSpec> Methods => apiSpecs;

        public IEnumerable<Type> IgnoreTheseTypesCustom { get; set; } = Enumerable.Empty<Type>();
        public Assembly FilterApiControllers_ByAssembly { get; set; }
        public IEnumerable<Type> FilterApiControllers_ExcludeControllers { get; set; }

        public EndpointMethodCollection(ITsCodeGenLogger tsLogger, TsClassCollection classes, HttpConfiguration httpConfig)
        {
            this.tsLogger = tsLogger;
            this.classes = classes;
            this.httpConfig = httpConfig;
        }

        public void Build()
        {
            IEnumerable<ApiDescription> apis = httpConfig.Services.GetApiExplorer().ApiDescriptions;
            if (FilterApiControllers_ByAssembly != null)
                apis = apis.Where(api => api.ActionDescriptor.ControllerDescriptor.ControllerType.Assembly == FilterApiControllers_ByAssembly);
            if (FilterApiControllers_ExcludeControllers != null)
                apis = apis.Where(api => !FilterApiControllers_ExcludeControllers.Contains(api.ActionDescriptor.ControllerDescriptor.ControllerType));
            apis = apis.ToArray();

            // .... 1 Gather all class info into classes
            foreach (var api in apis)
            {
                var apiId = api.GetFriendlyId();

                foreach (var para in api.ParameterDescriptions)
                {
                    if (para.ParameterDescriptor.ParameterType != null)
                        AddTypeToTsClassesFromApi(para.ParameterDescriptor.ParameterType, apiId);
                }
                var responseType = api.ResponseDescription.ResponseType ?? api.ResponseDescription.DeclaredType;
                if (responseType != null)
                    AddTypeToTsClassesFromApi(responseType, apiId);
            }

            // .... 2 Gather all method info
            foreach (var api in apis)
            {
                var apiId = api.GetFriendlyId();
                var apiModel = httpConfig.GetHelpPageApiModel(apiId);
                var apiSpecRaw = GetApiSpecRawData(api, apiModel);
                var responseType = api.ResponseDescription.ResponseType ?? api.ResponseDescription.DeclaredType;

                var endpointMethodName = $"{apiSpecRaw.ControllerName}_{apiSpecRaw.MethodName}";
                endpointMethodName = MakeUniqueName(endpointMethodName, apiSpecs, old => old.EndpointMethodName);

                var bodyParams = api.ParameterDescriptions
                    .Where(para => para.Source == ApiParameterSource.FromBody)
                    .Select(ToEndpointRequestParamSpec)
                    .ToList();
                var uriParams = api.ParameterDescriptions
                   .Where(para => para.Source == ApiParameterSource.FromUri)
                   .Select(ToEndpointRequestParamSpec)
                   .ToList();
                EndpointRequestParamSpec uriParamsWrapperClass = null;

                // NOTE: need to remove all optional URI params from urlTsFriendly
                var urlTsFriendly = apiSpecRaw.UrlTsFriendly;
                foreach (var param in uriParams.Where(p => p.IsOptional))
                {
                    var tsInterpolator = "${" + param.Name + "}";
                    if (urlTsFriendly.Contains("/" + tsInterpolator))
                        urlTsFriendly = urlTsFriendly.Replace("/" + tsInterpolator, "");
                    else if (urlTsFriendly.Contains(tsInterpolator + "/"))
                        urlTsFriendly = urlTsFriendly.Replace(tsInterpolator + "/", "");
                    else if (urlTsFriendly.Contains(tsInterpolator))
                        urlTsFriendly = urlTsFriendly.Replace(tsInterpolator, "");
                }

                // QUICKFIX for [FromUri]
                if (uriParams.Count == 1 && api.ParameterDescriptions.FirstOrDefault(para => para.Source == ApiParameterSource.FromUri).ParameterDescriptor.ParameterBinderAttribute != null)
                {
                    if (classes.HasType(uriParams[0].ParamType))
                        uriParamsWrapperClass = uriParams[0];
                    uriParams = apiModel.UriParameters
                        .Select(para => new EndpointRequestParamSpec
                        {
                            IsOptional = true,
                            Name = para.Name,
                            ParamType = para.TypeDescription.ModelType
                        })
                        .ToList();
                }

                apiSpecs.Add(new EndpointMethodSpec
                {
                    // TS-specific fields
                    EndpointId = apiId,
                    EndpointMethodName = endpointMethodName,
                    HttpMethod = apiSpecRaw.HttpMethod.ToLower(),
                    UrlTsFriendly = urlTsFriendly,
                    ResponseType = responseType,
                    BodyParams = bodyParams,
                    UriParams = uriParams,
                    UriParamsWrapperClass = uriParamsWrapperClass,

                    // documentation-specific fields
                    DocHttpMethod = apiSpecRaw.HttpMethod,
                    DocUrlFull = apiSpecRaw.UrlFull,
                    DocRequestJson = apiSpecRaw.JsonRequestText,
                    DocResponseJson = apiSpecRaw.JsonResponseText,
                    DocDescription = api.Documentation,

                    //JsUrlBackendMockRegex = path,
                });
            }
        }

        private void AddTypeToTsClassesFromApi(Type type, string apiId)
        {
            if (IgnoreTheseTypes.Contains(type))
                return;
            if (IgnoreTheseTypesCustom.Contains(type))
                return;
            classes.AddType(type, new CodeGenLoggerAddTypeEntry
            {
                Reason = AddTypeReasonType.FromApi,
                ApiId = apiId,
            });
        }

        public void CompileAfterClassesCompiled()
        {
            // some post-processing to link EndpointMethods and classes
            foreach (var methodSpec in apiSpecs)
            {
                methodSpec.ResponseTypeSpec = classes.CreateTypeSpecComplex(methodSpec.ResponseType);
                foreach (var param in methodSpec.UriParams)
                    param.ParamTypeTsSpec = classes.CreateTypeSpecComplex(param.ParamType);
                foreach (var param in methodSpec.BodyParams)
                    param.ParamTypeTsSpec = classes.CreateTypeSpecComplex(param.ParamType);
                if (methodSpec.UriParamsWrapperClass != null)
                    methodSpec.UriParamsWrapperClass.ParamTypeTsSpec = classes.CreateTypeSpecComplex(methodSpec.UriParamsWrapperClass.ParamType);
            }
        }

        private EndpointRequestParamSpec ToEndpointRequestParamSpec(ApiParameterDescription para)
        {
            return new EndpointRequestParamSpec
            {
                Name = para.Name,
                IsOptional = para.ParameterDescriptor.IsOptional,
                ParamType = para.ParameterDescriptor.ParameterType
            };
        }

        private string MakeUniqueName(string endpointMethodName, List<EndpointMethodSpec> apiSpecs, Func<EndpointMethodSpec, string> getName)
        {
            var oldName = endpointMethodName;
            int index = 2;
            while (apiSpecs.Any(old => getName(old) == endpointMethodName))
            {
                endpointMethodName = oldName + "_" + index;
                index++;
            }
            return endpointMethodName;
        }

        private ApiSpecRawData GetApiSpecRawData(ApiDescription api, HelpPageApiModel apiModel)
        {
            var jsonKey = new MediaTypeHeaderValue("text/json");

            var urlFull = apiModel.ApiDescription.RelativePath;
            var urlTsFriendly = urlFull
                .Replace("REST/", "")
                .Replace("api/", "");
            urlTsFriendly = Regex.Replace(urlTsFriendly, @"({.*?})", @"$$$1"); // add $ in front of all {...} constructs
            urlTsFriendly = Regex.Replace(urlTsFriendly, @"\?.*$", ""); // strip away everything past "?"

            var urlMethodNamePart = GetUrlMethodName(api, apiModel);
            if (urlMethodNamePart != null)
                urlMethodNamePart = urlMethodNamePart.Replace("/", "_");
            if (string.IsNullOrEmpty(urlMethodNamePart))
                urlMethodNamePart = null;

            var actionName = apiModel.ApiDescription.ActionDescriptor.ActionName;
            if (PreferUrlMethodNameIfTheseHttpActions.Contains(actionName?.ToLower()))
                actionName = urlMethodNamePart; // NOTE: give preference to urlMethodNamePart if its just an HttpAction, they are usually more descriptive

            var jsonRequestText = "null";
            if (apiModel.SampleRequests.ContainsKey(jsonKey))
            {
                var jsonResponse = apiModel.SampleRequests[jsonKey] as TextSample;
                jsonRequestText = jsonResponse?.Text ?? "null";
            }

            var jsonResponseText = "null";
            if (apiModel.SampleResponses.ContainsKey(jsonKey))
            {
                var jsonResponse = apiModel.SampleResponses[jsonKey] as TextSample;
                jsonResponseText = jsonResponse?.Text ?? "null";
            }

            return new ApiSpecRawData
            {
                UrlFull = urlFull,
                UrlTsFriendly = urlTsFriendly,
                ControllerName = apiModel.ApiDescription.ActionDescriptor.ControllerDescriptor.ControllerName,
                MethodName = actionName
                    ?? urlMethodNamePart
                    ?? apiModel.ApiDescription.HttpMethod.Method,
                UrlMethodNamePart = urlMethodNamePart,
                HttpMethod = apiModel.ApiDescription.HttpMethod.Method,
                JsonRequestText = jsonRequestText,
                JsonResponseText = jsonResponseText,
            };
        }

        private string GetUrlMethodName(ApiDescription api, HelpPageApiModel apiModel)
        {
            var urlFull = apiModel.ApiDescription.RelativePath;
            var controllerName = apiModel.ApiDescription.ActionDescriptor.ControllerDescriptor.ControllerName;
            var controllerRoutreAttrs = api.ActionDescriptor.ControllerDescriptor.GetCustomAttributes<System.Web.Http.RoutePrefixAttribute>();
            var controllerRoutreAttr = controllerRoutreAttrs.FirstOrDefault();
            var controllerRoutrePrefix = (controllerRoutreAttr != null)
                ? controllerRoutreAttr.Prefix
                : null;
            var regexPattern = $@"{controllerRoutrePrefix?.Replace("/", "\\/") ?? controllerName}([^\?]*)(\?)?";

            //var urlRelative = urlFull.Replace("REST", "");
            //urlRelative = Regex.Replace(urlRelative, @"^(.*)\?.*", "$1");

            var matchAfterController = Regex.Match(urlFull, regexPattern);
            if (!matchAfterController.Success)
                return null;

            var group1 = matchAfterController.Groups[1].Value;
            var withoutUrlParams = Regex.Replace(group1, @"\/{.*?}", ""); // remove all the /{...}
            withoutUrlParams = Regex.Replace(withoutUrlParams, @"^\/", ""); // remove leading slash

            if (String.IsNullOrEmpty(withoutUrlParams))
                return null;

            return withoutUrlParams;
        }


        private class ApiSpecRawData
        {
            public string UrlFull { get; set; }
            public string UrlTsFriendly { get; set; }
            public string ControllerName { get; set; }
            public string MethodName { get; set; }
            public object UrlMethodNamePart { get; set; }
            public string HttpMethod { get; set; }
            public string JsonRequestText { get; set; }
            public string JsonResponseText { get; set; }
        }
    }
}
