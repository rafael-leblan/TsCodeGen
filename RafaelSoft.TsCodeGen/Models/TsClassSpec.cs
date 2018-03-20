using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Common;
using RafaelSoft.TsCodeGen.Generator;

namespace RafaelSoft.TsCodeGen.Models
{
    public class TsClassSpec
    {
        public string Name { get; set; }
        public string NameOfSuperClass { get; set; }
        public List<TsClassSpecProperty> Properties { get; set; }
        public bool IsEnum { get; set; }
        public TsEnumProperty[] EnumValues { get; set; }
        public Type OriginalBackendType { get; set; }
    }

    public class TsEnumProperty
    {
        public string Name { get; set; }
        public long Value { get; set; }
    }

    public class TsClassSpecProperty
    {
        public string Name { get; set; }
        public string CustomTsReviverScript { get; set; }
        public TsCsTypeSpec TypeSpec { get; set; }

        public string GetTsName(ITsClassGenerationConfig genConfig) =>
            Name.IdentifierConvertCase(genConfig.FieldCaseType);

        public string ToTsString(ITsClassGenerationConfig genConfig)
        {
            var typeSpecTs = TypeSpec.ToTsString(genConfig);
            var isOptionalStr = TypeSpec.IsOptional ? "?" : "";
            var tsName = GetTsName(genConfig);
            return $"{tsName}{isOptionalStr}: {typeSpecTs}";
        }

        public string ToTsReviverString(ITsClassGenerationConfig genConfig, string rawPropertyName = null)
        {
            if (rawPropertyName == null)
                rawPropertyName = $"init.{Name}";
            if (GetTsAtomicReviver_withMyCustomScriptCheck(genConfig) == null)
                return null; // NOTE: no atomic revival needed

            return TypeSpec.GetTsFullReviver(genConfig, rawPropertyName, GetTsAtomicReviver_withMyCustomScriptCheck);
        }

        public string GetTsAtomicReviver_withMyCustomScriptCheck(ITsClassGenerationConfig genConfig, string param = "x")
        {
            if (CustomTsReviverScript != null)
                return CustomTsReviverScript.Replace("$x", param);
            return TypeSpec.GetTsAtomicReviver(genConfig, param);
        }
    }

    public class TsCsTypeSpec
    {
        public Type Type { get; set; }
        public string TypeName { get; set; }
        public bool IsEnum { get; set; }
        public bool IsDate { get; set; }
        public bool IsMine { get; set; }
        public bool IsArray { get; set; }
        public bool IsDictionary { get; set; }
        public bool IsDictionaryOfArrays { get; set; } // TODO: IsDictionaryOfArrays
        public bool IsOptional { get; set; }
        public string TypeNameDictKey { get; set; }

        public string ToTsString(ITsClassGenerationConfig genConfig)
        {
            var isArrayStr = IsArray ? "[]" : "";
            var isOptionalStr = IsOptional ? "?" : "";
            var tsUsageTypeName = IsMine
                ? genConfig.TransformTsClassName(TypeName)
                : TypeName;

            if (IsDictionaryOfArrays)
                return $"{{ [id: {TypeNameDictKey}]: {tsUsageTypeName}[]; }}";
            if (IsDictionary)
                return $"{{ [id: {TypeNameDictKey}]: {tsUsageTypeName}; }}";
            return $"{tsUsageTypeName}{isArrayStr}";
        }

        public string GetTsAtomicReviver(ITsClassGenerationConfig genConfig, string param)
        {
            var tsUsageTypeName = IsMine
                ? genConfig.TransformTsClassName(TypeName)
                : TypeName;
            if (IsEnum)
                return $"isNaN({param}) ? {tsUsageTypeName}[{param}] : {param}";
            if (IsDate)
                return $"{param} ? new Date({param}) : null";
            if (IsMine)
                return $"{param} ? new {tsUsageTypeName}({param}) : null";
            return null;
        }

        public string GetTsFullReviver(ITsClassGenerationConfig genConfig, string inputParam, Func<ITsClassGenerationConfig, string, string> funcBuildAtomicReviver = null)
        {
            if (funcBuildAtomicReviver == null)
                funcBuildAtomicReviver = GetTsAtomicReviver;

            if (IsDictionaryOfArrays)
                return $"_.mapValues({inputParam}, y => _.map(y, x => {funcBuildAtomicReviver(genConfig, "x")}))";
            else if (IsDictionary)
                return $"_.mapValues({inputParam}, x => {funcBuildAtomicReviver(genConfig, "x")})";
            else if (IsArray)
                return $"_.map({inputParam}, x => {funcBuildAtomicReviver(genConfig, "x")})";
            else if (IsMine)
            {
                var normalScript = funcBuildAtomicReviver(genConfig,  inputParam);
                if (normalScript != null && normalScript.Contains("\n"))
                    return $"({funcBuildAtomicReviver(genConfig, "x")})({inputParam})"; // NOTE: special case: if the script has many lines (AKA custom TS) and not in any lodash construct, it must be invoked
                return normalScript;
            }
            else if (IsEnum || IsDate)
                return funcBuildAtomicReviver(genConfig, inputParam);
            return null;
        }
    }

    public class TsCsTypeSpecOptions
    {
        public string TypeName { get; set; }
    }

}
