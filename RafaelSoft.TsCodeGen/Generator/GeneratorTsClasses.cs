using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Models;

namespace RafaelSoft.TsCodeGen.Generator
{
    public class GeneratorTsClasses
    {
        private TsClassCollection tsClasses;

        public bool IsAllFieldsLowercase { get; set; } = false;

        public GeneratorTsClasses(TsClassCollection tsClasses, TsImportsManager importsManager)
        {
            this.tsClasses = tsClasses;
            importsManager.AddImportStatement(TsCodeImports.Lodash);
        }

        public string Generate()
        {
            var enumsCode = tsClasses.CompiledTsClasses
                .Where(spec => spec.IsEnum)
                .Select(spec => Generate_enum(spec))
                .StringJoin("\n");

            var classesCode = tsClasses.CompiledTsClasses
                .Where(spec => !spec.IsEnum)
                .Select(spec => Generate_class(spec))
                .StringJoin("\n");

            return $@"
//============================ enums ============================

{enumsCode}

//============================ classes ============================

{classesCode}

";
        }

        //----------------------------------------------------------------------

        private string Generate_enum(TsClassSpec spec)
        {
            var enumValues = spec.EnumValues
                .Where(enumVal => enumVal.Name.IsValidIdentifier())
                .Select(enumVal => $"{enumVal.Name} = {enumVal.Value},")
                .StringJoin("\n");
            return $@"
export enum {spec.Name} {{
  {enumValues.IndentEveryLine("  ", skipFirst: true)}
}}";
        }

        private string Generate_class(TsClassSpec spec)
        {
            // TODO: spec.ToDebugJson(), add to comments
            // IsEnum: {prop.TypeSpec.IsEnum}, IsMine: {prop.TypeSpec.IsMine}, IsArray: {prop.IsArray}, IsDictionary: {prop.IsDictionary}

            var code_propValues = spec.Properties
                .Select(prop => $"{prop.ToTsString(IsAllFieldsLowercase)};")
                .StringJoin("\n");
            var code_extendsSuperclass = spec.NameOfSuperClass.PrefixIfNotEmpty(" extends ");
            var code_callSuperctor = (spec.NameOfSuperClass != null) ? "super(init);" : "";
            var code_passInitIfNoSuper = (spec.NameOfSuperClass == null) ? "init, " : "";
            var code_revivers = spec.Properties
                .Where(x => x.ToTsReviverString() != null)
                .Select(prop => $"{prop.GetTsName(IsAllFieldsLowercase)}: {prop.ToTsReviverString("init." + prop.GetTsName(IsAllFieldsLowercase))},")
                .StringJoin("\n");
            var code_constructor = $@"constructor(init?:Partial<{spec.Name}>) {{
  {code_callSuperctor}
  Object.assign(this, {code_passInitIfNoSuper}{{
    {code_revivers.IndentEveryLine("    ", skipFirst: true)}
  }});
}}";

            return $@"
/**
 * Source class: {spec.OriginalBackendType.FullName}
 * TODO: endpointClass.ToDebugJson()
 */
export class {spec.Name}{code_extendsSuperclass} {{
  {code_propValues.IndentEveryLine("  ", skipFirst: true)}

  {code_constructor.RemoveEmptyLines().IndentEveryLine("  ", skipFirst: true)}
}}";
        }

        public string GetPropertyName(Type paramType, string name)
        {
            var spec = tsClasses.CompiledTsClasses.FirstOrDefault(x => x.OriginalBackendType == paramType);
            var prop = spec.Properties.FirstOrDefault(x => x.Name == name);
            return prop.GetTsName(IsAllFieldsLowercase);
        }
    }
}
