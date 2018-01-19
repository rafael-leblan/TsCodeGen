using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string GetTsName(bool isLowercase) => isLowercase
            ? Name.ToLower()
            : Name;

        public string ToTsString(bool isLowercase)
        {
            var typeSpecTs = TypeSpec.ToTsString();
            var isOptionalStr = TypeSpec.IsOptional ? "?" : "";
            var tsName = GetTsName(isLowercase);
            return $"{tsName}{isOptionalStr}: {typeSpecTs}";
        }

        public string ToTsReviverString(string rawPropertyName = null)
        {
            if (rawPropertyName == null)
                rawPropertyName = $"init.{Name}";
            if (GetTsAtomicReviver_withMyCustomScriptCheck() == null)
                return null; // NOTE: no atomic revival needed

            return TypeSpec.GetTsFullReviver(rawPropertyName, GetTsAtomicReviver_withMyCustomScriptCheck);
        }

        public string GetTsAtomicReviver_withMyCustomScriptCheck(string param = "x")
        {
            if (CustomTsReviverScript != null)
                return CustomTsReviverScript.Replace("$x", param);
            return TypeSpec.GetTsAtomicReviver(param);
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

        public string ToTsString()
        {
            var isArrayStr = IsArray ? "[]" : "";
            var isOptionalStr = IsOptional ? "?" : "";

            if (IsDictionaryOfArrays)
                return $"{{ [id: {TypeNameDictKey}] : {TypeName}[]; }}";
            if (IsDictionary)
                return $"{{ [id: {TypeNameDictKey}] : {TypeName}; }}";
            return $"{TypeName}{isArrayStr}";
        }

        public string GetTsAtomicReviver(string param)
        {
            if (IsEnum)
                return $"isNaN({param}) ? {TypeName}[{param}] : {param}";
            if (IsDate)
                return $"{param} ? new Date({param}) : null";
            if (IsMine)
                return $"{param} ? new {TypeName}({param}) : null";
            return null;
        }

        public string GetTsFullReviver(string inputParam, Func<string, string> funcBuildAtomicReviver = null)
        {
            if (funcBuildAtomicReviver == null)
                funcBuildAtomicReviver = GetTsAtomicReviver;

            if (IsDictionaryOfArrays)
                return $"_.mapValues({inputParam}, y => _.map(y, x => {funcBuildAtomicReviver("x")}))";
            else if (IsDictionary)
                return $"_.mapValues({inputParam}, x => {funcBuildAtomicReviver("x")})";
            else if (IsArray)
                return $"_.map({inputParam}, x => {funcBuildAtomicReviver("x")})";
            else if (IsMine)
            {
                var normalScript = funcBuildAtomicReviver(inputParam);
                if (normalScript != null && normalScript.Contains("\n"))
                    return $"({funcBuildAtomicReviver("x")})({inputParam})"; // NOTE: special case: if the script has many lines (AKA custom TS) and not in any lodash construct, it must be invoked
                return normalScript;
            }
            else if (IsEnum || IsDate)
                return funcBuildAtomicReviver(inputParam);
            return null;
        }
    }

    public class TsCsTypeSpecOptions
    {
        public string TypeName { get; set; }
    }

}
