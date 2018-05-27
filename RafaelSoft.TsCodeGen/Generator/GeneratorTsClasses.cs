using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Common;
using RafaelSoft.TsCodeGen.Models;

namespace RafaelSoft.TsCodeGen.Generator
{
    public class GeneratorTsClasses : ITsClassGenerationConfig
    {
        private TsClassCollection tsClasses;

        //TODO: 04FC4F88: maybe have TsClassGenConfig as a parameter in constructor
        public string SpecNamePrefix { get; set; } = "";
        public string SpecNamePostfix { get; set; } = "";
        public bool SortFieldsAlphabetically { get; set; } = true;
        public bool SortMethodsAlphabetically { get; set; } = true;
        public IdentifierCaseType FieldCaseType { get; set; } = IdentifierCaseType.Unchanged;

        public GeneratorTsClasses(TsClassCollection tsClasses, TsImportsManager importsManager)
        {
            this.tsClasses = tsClasses;
            importsManager.AddImportStatement(TsCodeImports.Lodash);
        }

        public string Generate()
        {
            var enumsCode = tsClasses.CompiledTsClasses
                .Where(spec => spec.IsEnum)
                //.ConditionallyIf(SortClassesAlphabetically, thisLinq => thisLinq.OrderBy(spec => spec.Name)) // NOTE: a3c189f5: there is custom sorting inside, don't need this
                .Select(spec => Generate_enum(spec))
                .StringJoin("\n");

            var classesCode = tsClasses.CompiledTsClasses
                .Where(spec => !spec.IsEnum)
                //.ConditionallyIf(SortClassesAlphabetically, thisLinq => thisLinq.OrderBy(spec => spec.Name)) // NOTE: a3c189f5: there is custom sorting inside, don't need this
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
                .OrderBy(enumVal => enumVal.Value) // TODO: needed?
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

            var code_specClassName = this.TransformTsClassName(spec.Name);
            var code_propValues = spec.Properties
                .ConditionallyIf(SortFieldsAlphabetically, thisLinq => thisLinq.OrderBy(x => x.GetTsName(this)))
                .Select(prop => $"{prop.ToTsString(this)};")
                .StringJoin("\n");
            var code_extendsSuperclass = this.TransformTsClassName(spec.NameOfSuperClass).PrefixIfNotEmpty(" extends ");
            var code_callSuperctor = (spec.NameOfSuperClass != null) ? "super(init);" : "";
            var code_passInitIfNoSuper = (spec.NameOfSuperClass == null) ? "init, " : "";
            var code_revivers = spec.Properties
                .Where(x => x.ToTsReviverString(this) != null)
                .ConditionallyIf(SortFieldsAlphabetically, thisLinq => thisLinq.OrderBy(x => x.GetTsName(this)))
                .Select(prop => $"{prop.GetTsName(this)}: {prop.ToTsReviverString(this, "init." + prop.GetTsName(this))},")
                .StringJoin("\n");
            var code_constructor = $@"constructor(init?: Partial<{code_specClassName}>) {{
  {code_callSuperctor}
  Object.assign(this, {code_passInitIfNoSuper}{{
    {code_revivers.IndentEveryLine("    ", skipFirst: true)}
  }});
}}";

            // TODO: endpointClass.ToDebugJson() in comments
            return $@"
/**
 * Source class: {spec.OriginalBackendType.FullName}
 */
export class {code_specClassName}{code_extendsSuperclass} {{
  {code_propValues.IndentEveryLine("  ", skipFirst: true)}

  {code_constructor.RemoveEmptyLines().IndentEveryLine("  ", skipFirst: true)}
}}";
        }
    }
}
