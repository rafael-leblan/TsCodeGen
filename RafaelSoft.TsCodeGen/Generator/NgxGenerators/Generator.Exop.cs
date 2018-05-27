using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Common;
using RafaelSoft.TsCodeGen.Generator;
using RafaelSoft.TsCodeGen.Models;

namespace RafaelSoft.TsCodeGen.Generator.NgxGenerators
{
    public class GeneratorNgxExop
    {
        public TsMethodCollection InvokerInterface { get; set; }
        public TsMethodCollection ExvokerInterface { get; set; }
        public string AngularClassName { get; set; }
        public string ExinvokerGlobalName { get; set; } = "exinvoker";
        public ITsClassGenerationConfig TsClassGenConfig { get; set; } = new TsClassGenerationManualConfig(); // NOTE: default

        public GeneratorNgxExop(TsImportsManager importsManager)
        {
            importsManager.AddImportStatements(new[]
            {
                TsCodeImports.Injectable,
                TsCodeImports.NgZone,
                TsCodeImports.Subject,
            });
        }

        public string Generate()
        {
            var code_invocationSubjectField = InvokerInterface.Methods
                .ConditionallyIf(TsClassGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.MethodName))
                .Select(Generate_invocationSubjectField)
                .StringJoin("\n");
            var code_invocation = InvokerInterface.Methods
                .ConditionallyIf(TsClassGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.MethodName))
                .Select(Generate_invocation)
                .StringJoin("\n");
            var code_exvocationFuncs = ExvokerInterface.Methods
                .ConditionallyIf(TsClassGenConfig.SortMethodsAlphabetically, thisLinq => thisLinq.OrderBy(m => m.MethodName))
                .Select(Generate_exvocation)
                .StringJoin("\n");
            return $@"
@Injectable()
export class {AngularClassName} {{
  
  {code_invocationSubjectField.Trim().IndentEveryLine("  ", skipFirst: true)}

  constructor(
    private ngZone: NgZone,
  ) {{
    {code_invocation.Trim().IndentEveryLine("    ", skipFirst: true)}
  }}

  //--------------------------------------------------------------------------------------

  readonly exinvokerGlobalName:string = '{ExinvokerGlobalName}';

  {code_exvocationFuncs.IndentEveryLine("  ", skipFirst: true)}

}}";
        }

        private string Generate_invocationSubjectField(TsMethodSpec spec)
        {
            if (spec.ParamSpecs.Length > 1)
                throw new ArgumentException($"Angular invocation method should only have 1 param, because Subject<T> takes 1 param. Method: {spec.MethodName}");
            var param1 = spec.ParamSpecs[0];
            return $"public readonly {spec.MethodName}:Subject<{param1.GetTsParamTypeName(TsClassGenConfig)}> = new Subject<{param1.GetTsParamTypeName(TsClassGenConfig)}>();";
        }
        private string Generate_invocation(TsMethodSpec spec)
        {
            if (spec.ParamSpecs.Length > 1)
                throw new ArgumentException($"Angular invocation method should only have 1 param, because Subject<T> takes 1 param. Method: {spec.MethodName}");
            var param1 = spec.ParamSpecs[0];
            return $@"
window['exinvoke_{spec.MethodName}'] = ({param1.ParamName}:{param1.GetTsParamTypeName(TsClassGenConfig)}) => {{
  this.ngZone.run(() => {{
    {param1.ParamName} = {param1.TsParamTypeReviver(TsClassGenConfig, param1.ParamName) ?? param1.ParamName};
    this.{spec.MethodName}.next({param1.ParamName});
  }});
}};";
        }
        private string Generate_exvocation(TsMethodSpec spec)
        {
            var paramsStrWithType = spec.ParamSpecs
                .Select(p => $"{p.ParamName}:{p.GetTsParamTypeName(TsClassGenConfig)}")
                .StringJoin(", ");
            var paramsStr = spec.ParamSpecs
                .Select(p => p.ParamName)
                .StringJoin(", ");
            return $@"
exinvoke_{spec.MethodName}({paramsStrWithType}) {{
  if (!window[this.exinvokerGlobalName]) {{
    console.error(`${{this.exinvokerGlobalName}} has not been globally set for this chromium control! Failed to call ${{this.exinvokerGlobalName}}.{spec.MethodName}`, {paramsStr});
    return;
  }}
  window[this.exinvokerGlobalName].{spec.MethodName}({paramsStr});
}}";
        }
    }

}
