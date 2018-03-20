using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.WebApi.Models;

namespace RafaelSoft.TsCodeGen.WebApi.Gen
{
    public static class TsHttpUtils
    {
        public static string BuildReadableJsonStructWithProperties(List<string> properties, string indentation = "  ")
        {
            if (properties.Count == 0)
                return "{}";
            if (properties.Count == 1)
                return "{ " + properties[0] + " }";
            return string.Concat("{\n", string.Join(",\n", properties.Select(p => indentation + p)), "\n}");
        }

        //new HttpParams()
        //  .set('solution', solution)
        //  .set('paxid', paxid)
        //  .set('flight', flight)
        public static string BuildHttpParamsFromTheseParams(List<EndpointRequestHttpParamParamSpec> paramz, string indentation = "  ")
        {
            if (paramz.Count == 0)
                return "new HttpParamsBuilder()";
            return string.Concat(
                "new HttpParamsBuilder()\n",
                string.Join("\n", paramz.Select(p => p.IsString
                    ? $"{indentation}.set('{p.Key}', {p.VarName})"
                    : $"{indentation}.setAsStr('{p.Key}', {p.VarName})")),
                $"\n{indentation}.toHttpParams()"
            );
        }
    }
}
