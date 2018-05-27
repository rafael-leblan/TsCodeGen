using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Common;
using RafaelSoft.TsCodeGen.Models;

namespace RafaelSoft.TsCodeGen.Generator
{
    public class GeneratorTsClassesMapping
    {
        private TsClassCollection tsClasses;

        public ITsClassGenerationConfig TsClassGenConfigLHS { get; set; }
        public ITsClassGenerationConfig TsClassGenConfigRHS { get; set; }

        public GeneratorTsClassesMapping(TsClassCollection tsClasses, TsImportsManager importsManager)
        {
            this.tsClasses = tsClasses;
            importsManager.AddImportStatement(TsCodeImports.Lodash);
        }

        public string Generate()
        {
            var lhsVmClassesGenerator = new GeneratorTsClasses(tsClasses, new TsImportsManager()) // HACK: TsImportsManager will not be used in this case
            {
                FieldCaseType = TsClassGenConfigLHS.FieldCaseType,
                SpecNamePrefix = TsClassGenConfigLHS.SpecNamePrefix,
                SpecNamePostfix = TsClassGenConfigLHS.SpecNamePostfix,
            };
            var code_lhsClasses = lhsVmClassesGenerator.Generate();
            var code_classesMapping = tsClasses.CompiledTsClasses
                .Where(spec => !spec.IsEnum)
                .Select(spec => Generate_classMappingFunc(spec))
                .StringJoin("\n");

            return $@"
{code_lhsClasses}

export class SampleMappingService {{

  {code_classesMapping.IndentEveryLine("  ", skipFirst: true)}

}}";
        }

        //----------------------------------------------------------------------

        //public map_PreferenceCategory_to_PreferenceCategoryVM(rest_cat: GTARest.PreferenceCategory): PreferenceCategoryVM {
        //  return <PreferenceCategoryVM> {
        //    rank: rest_cat.rank,
        //    id: rest_cat.id,
        //    name: rest_cat.name,
        //    type: this.map_GTAString_to_TravelCategoryType(rest_cat.id), // NOTE: id is type of this category AKA FLT or HTL
        //    description: rest_cat.description,
        //    attributes: _.map(rest_cat.attributes, rest_attr => <PreferenceAttributeVM> {
        //      rank: rest_attr.rank,
        //      id: rest_attr.id,
        //      name: rest_attr.name,
        //      type: this.map_GtaString_to_TravelPreferenceDialogType(rest_attr.type),
        //      description: rest_attr.description,
        //      values: _.map(rest_attr.values, rest_val => this.map_PreferenceValue_to_PreferenceValueVM(rest_val)),
        //    }),
        //  };
        //}

        private string Generate_classMappingFunc(TsClassSpec spec)
        {
            var specStack = new Stack<TsClassSpec>();
            var lhsSpecName = TsClassGenConfigLHS.TransformTsClassName(spec.Name);
            var rhsVarName = $"rhs_{spec.Name}";
            var code_classMappings = Generate_classMapping_only(spec, rhsVarName, TsClassGenConfigLHS.SortFieldsAlphabetically, specStack);

            return $@"public map_{spec.Name}_to_{lhsSpecName}({rhsVarName}: {TsClassGenConfigRHS.TransformTsClassName(spec.Name)}): {lhsSpecName} {{
  return {code_classMappings.IndentEveryLine("  ", skipFirst: true)};
}}
";
        }

        private string Generate_classMapping_only(TsClassSpec spec, string rhsVarName, bool sortFieldsAlphabetically, Stack<TsClassSpec> specStack)
        {
            specStack.Push(spec);
            var lhsSpecName = TsClassGenConfigLHS.TransformTsClassName(spec.Name);
            var code_propMappings = spec.Properties
                .ConditionallyIf(sortFieldsAlphabetically, thisLinq => thisLinq.OrderBy(x => x.Name))
                .Select(prop => Generate_mappingSnippetFor(prop, rhsVarName, sortFieldsAlphabetically, specStack))
                .StringJoin("\n");
            specStack.Pop();

            return $@"<{lhsSpecName}> {{
  {code_propMappings.IndentEveryLine("  ", skipFirst: true)}
}}";
        }

        private string Generate_mappingSnippetFor(TsClassSpecProperty prop, string rhsVarName, bool sortFieldsAlphabetically, Stack<TsClassSpec> specStack)
        {
            var nameLHS = prop.GetTsName(TsClassGenConfigLHS);
            var nameRHS = prop.GetTsName(TsClassGenConfigRHS);
            if (prop.TypeSpec.RequiresLodashMapping() && prop.TypeSpec.IsMine)
            {
                var specInner = tsClasses.GetCompiledTsClassSpecByName(prop.TypeSpec.TypeName);
                if (specStack.Contains(specInner))
                    return $"{nameLHS}: null, // recursion alert!";
                var rhsVarNameInner = $"rhs_{specInner.Name}";
                var code_classMappingsInner = Generate_classMapping_only(specInner, rhsVarNameInner, sortFieldsAlphabetically, specStack);
                return $"{nameLHS}: _.map({rhsVarName}.{nameRHS}, {rhsVarNameInner} => {code_classMappingsInner}),";
            }
            else if (prop.TypeSpec.RequiresLodashMapping())
            {
                return $"{nameLHS}: _.map({rhsVarName}.{nameRHS}, x => x),";
            }
            else if (prop.TypeSpec.IsMine)
            {
                var specInner = tsClasses.GetCompiledTsClassSpecByName(prop.TypeSpec.TypeName);
                if (specStack.Contains(specInner))
                    return $"{nameLHS}: null, // recursion alert!";
                var rhsVarNameInner = $"{rhsVarName}.{nameRHS}";
                var code_classMappingsInner = Generate_classMapping_only(specInner, rhsVarNameInner, sortFieldsAlphabetically, specStack);
                return $"{nameLHS}: {code_classMappingsInner},";
            }
            return $"{nameLHS}: {rhsVarName}.{nameRHS},";
        }

    }
}
