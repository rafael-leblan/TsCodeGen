using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RafaelSoft.TsCodeGen.Models;

namespace RafaelSoft.TsCodeGen.Common
{
    public static class UtilsTsCodeGen
    {
        #region -------------------------- string extensions -------------------------

        public static string RemoveEmptyLines(this string str)
        {
            return Regex.Replace(str, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
        }

        public static string TrimAndMinorFormat(this string str)
        {
            str = str.Trim();
            str = Regex.Replace(str, @"(\n|\r\n){3,}", "\n\n");
            return str;
        }

        public static string IndentEveryLine(this string str, string indent, bool skipFirst = false)
        {
            if (str == null)
                return null;
            return skipFirst
                ? string.Join("\n", str.Split('\n').Select((x, i) => (i == 0) ? x : indent + x))
                : string.Join("\n", str.Split('\n').Select(x => indent + x));
        }

        public static string TrimEveryLine(this string str)
        {
            if (str == null)
                return null;
            str = Regex.Replace(str, @"^\s+", "", RegexOptions.Multiline);
            str = Regex.Replace(str, @"\s+$", "", RegexOptions.Multiline);
            return str;
        }

        public static string PrefixIfNotEmpty(this string str, string prefix)
        {
            return !String.IsNullOrEmpty(str) ? prefix + str : str;
        }

        public static string ToString(this bool val, string ifTrue, string ifFalse)
        {
            return val ? ifTrue : ifFalse;
        }

        public static bool IsValidIdentifier(this string s)
        {
            return Regex.IsMatch(s, "^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        public static string StringJoin(this IEnumerable<string> list, string separator)
        {
            return string.Join(separator, list);
        }

        public static string ReplaceRegex(this string input, string regexSearch, string regexReplace)
        {
            if (input == null)
                return null;
            return Regex.Replace(input, regexSearch, regexReplace);
        }

        public static string FirstLetterToLower(this string str)
        {
            if (str == null)
                return null;
            if (str.Length > 1)
                return char.ToLower(str[0]) + str.Substring(1);
            return str.ToLower();
        }

        // TODO: is this used?
        //public static string BuildReadableJsonStructWithProperties(List<string> properties, string indentation = "  ")
        //{
        //    if (properties.Count == 0)
        //        return "{}";
        //    if (properties.Count == 1)
        //        return "{ " + properties[0] + " }";
        //    return string.Concat("{\n", string.Join(",\n", properties.Select(p => indentation + p)), "\n}");
        //}

        #endregion

        public static string GetUniqueNameForMap<T>(this Dictionary<string, T> map, string name)
        {
            var newName = name;
            var index = 2;
            while (map.ContainsKey(newName))
            {
                newName = $"{name}_{index}";
                index++;
            }
            return newName;
        }

        public static Dictionary<string, T> RemapWithUniqueKeys<T>(
            this Dictionary<string, T> map,
            Func<string, T, string> funcGetNewKey,
            Dictionary<string, string> keyMappingOld2New = null)
        {
            var newMap = new Dictionary<string, T>();
            foreach (var kv in map)
            {
                string newKey = funcGetNewKey(kv.Key, kv.Value);
                if (newMap.ContainsKey(newKey))
                    newKey = newMap.GetUniqueNameForMap(newKey);
                newMap.Add(newKey, kv.Value);
                keyMappingOld2New?.Add(kv.Key, newKey);
            }
            return newMap;
        }

        // FROM: https://stackoverflow.com/questions/4185521/c-sharp-get-generic-type-name
        private static readonly Dictionary<Type, string> _typeToFriendlyName = new Dictionary<Type, string>
        {
            { typeof(string), "string" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(short), "short" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(sbyte), "sbyte" },
            { typeof(float), "float" },
            { typeof(ushort), "ushort" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(void), TsCommonTypeNames.Void }
        };

        public static string GetFriendlyName(this Type type)
        {
            string friendlyName;
            if (_typeToFriendlyName.TryGetValue(type, out friendlyName))
            {
                return friendlyName;
            }

            friendlyName = type.Name;
            var modelNameAttribute = type.GetCustomAttribute<TsModelNameAttribute>();
            if (modelNameAttribute != null && !String.IsNullOrEmpty(modelNameAttribute.Name))
                friendlyName = modelNameAttribute.Name;

            if (type.IsGenericType)
            {
                int backtick = friendlyName.IndexOf('`');
                if (backtick > 0)
                {
                    friendlyName = friendlyName.Remove(backtick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    string typeParamName = typeParameters[i].GetFriendlyName();
                    friendlyName += (i == 0 ? typeParamName : ", " + typeParamName);
                }
                friendlyName += ">";
            }

            if (type.IsArray)
            {
                return type.GetElementType().GetFriendlyName() + "[]";
            }

            return friendlyName;
        }

        public static string GetFriendlyClassName(this Type type)
        {
            return type.GetFriendlyName()
                .Replace("<", "_of_")
                .Replace(">", "")
                .Replace(", ", "_and_")
                .Replace("[]", "")
                ;
        }

        public static void SortByDependencyParentsEarlier<T>(this T[] input, Func<T, bool> funcHasParent, Func<T, T, bool> funcChildParentTest)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (!funcHasParent(input[i]))
                    continue;
                for (int iParent = input.Length - 1; iParent > i; iParent--)
                {
                    if (funcChildParentTest(input[i], input[iParent]))
                    {
                        input.Swap(i, iParent);
                        i--; // NOTE: may need to swap again with an even more base-type further down, so need to keep the index here to check.
                        break;
                    }
                }
            }
        }

        public static void Swap<T>(this T[] input, int index1, int index2)
        {
            var tmp = input[index2];
            input[index2] = input[index1];
            input[index1] = tmp;
        }

        public static IEnumerable<object> ArrayToIEnumerable(this Array array)
        {
            foreach (var x in array)
                yield return x;
        }

        /// <summary>
        /// Iterates the lists together, mapping parallel entries to another list as result.
        /// If lists are not same length, the shorter list length is iterated and result with the shorter length is returned.
        /// </summary>
        public static IEnumerable<TV> SelectFromMeAndAnotherListInParallel<T1, T2, TV>(
            this IEnumerable<T1> list1,
            IEnumerable<T2> list2,
            Func<T1, T2, TV> mapping
            )
        {
            if (list1 == null || list2 == null)
                yield break;
            var en1 = list1.GetEnumerator();
            var en2 = list2.GetEnumerator();
            while (en1.MoveNext() && en2.MoveNext())
            {
                yield return mapping(en1.Current, en2.Current);
            }
        }

        public static bool RequiresLodashMapping(this TsCsTypeSpec spec) =>
            (spec.IsArray || spec.IsDictionary || spec.IsDictionaryOfArrays);

        public static string IdentifierConvertCase(this string input, IdentifierCaseType caseType)
        {
            if (caseType == IdentifierCaseType.Lowercase)
                return input.ToLower();
            else if (caseType == IdentifierCaseType.LowercaseOnlyFirst)
                return input.FirstLetterToLower();
            return input;
        }
        public static string TransformTsClassName(this ITsClassGenerationConfig genConfig, string typenameSimple) => !string.IsNullOrEmpty(typenameSimple)
            ? $"{genConfig.SpecNamePrefix}{typenameSimple}{genConfig.SpecNamePostfix}"
            : null;
    }
}
