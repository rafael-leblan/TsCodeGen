using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RafaelSoft.TsCodeGen.Generator.NgxGenerators
{
    public static class NgxTsSnippets
    {
        public static string Service_makeRootProviders(string angularClassName) => $@"
export const {angularClassName}_apiUrl = new InjectionToken<string>('{angularClassName}-ApiUrl');
export function {angularClassName}_makeRootProviders(apiUrl: string): Provider[] {{
  return [
    {{
      provide: {angularClassName}_apiUrl,
      useValue: apiUrl,
      multi: true
    }},
    {angularClassName}
  ];
}}";

    }
}
