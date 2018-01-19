using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RafaelSoft.TsCodeGen.Generator;

namespace RafaelSoft.TsCodeGen.Models
{
    public class TsClassCollection
    {
        private Dictionary<string, Type> typesViaFullName = new Dictionary<string, Type>();
        private Dictionary<string, Type> typesViaShortName = new Dictionary<string, Type>();
        private Dictionary<string, string> mapFullName2ShortName = new Dictionary<string, string>();
        private List<TsClassSpec> compiledTsClasses = null;

        // configurable properties
        public IEnumerable<Type> IgnoreTheseTypesCustom { get; set; } = Enumerable.Empty<Type>();

        public int Count => typesViaFullName.Count;

        public void AddType(Type type)
        {
            var name4debug = type.FullName; // NOTE: .NET debugger does not allow conditional breakpoints with reflection code

            // TODO: add enums to another dict
            if (type.IsGenericType)
            {
                if (StandardGeneric.Contains(type.GetGenericTypeDefinition()) ||
                    EnumerableTypes.Contains(type.GetGenericTypeDefinition()))
                {
                    var innerType = type.GenericTypeArguments.FirstOrDefault();
                    AddType(innerType);
                    return;
                }
                if (DictionaryTypes.Contains(type.GetGenericTypeDefinition()))
                {
                    var keyType = type.GenericTypeArguments.FirstOrDefault();
                    var valueType = type.GenericTypeArguments.Skip(1).FirstOrDefault();
                    AddType(keyType);
                    AddType(valueType);
                    return;
                }
            }

            if (type.IsArray)
                type = type.GetElementType();

            if (type.IsPrimitive || IgnoreTheseTypes.Contains(type) || IgnoreTheseTypesCustom.Contains(type))
                return;

            // if all the above checks passed, we can proceed
            if (!typesViaFullName.ContainsKey(type.FullName))
            {
                Debug.WriteLine($"RafaelSoft.TsCodeGen> AddType {type.FullName}");
                typesViaFullName.Add(type.FullName, type);
            }

            // find any inheriting types
            var explicitlyMentionedInheritingTypes = GetExplicitlyMentionedInheritingTypes(type);
            if (explicitlyMentionedInheritingTypes.Any())
            {
                foreach (var typeImplementation in explicitlyMentionedInheritingTypes)
                    AddType(typeImplementation);
            }
        }

        public bool HasType(Type type) =>  typesViaFullName.ContainsKey(type.FullName);

        private IEnumerable<Type> GetExplicitlyMentionedInheritingTypes(Type type)
        {
            var implementedByAttr = type.GetCustomAttributes(true).FirstOrDefault(attr => attr is IsImplementedByAttribute) as IsImplementedByAttribute;
            if (implementedByAttr == null)
                return Enumerable.Empty<Type>();
            return implementedByAttr.InheritingTypes;
        }

        public void Compile()
        {
            // NOTE: 50 levels deep should be enough to extract all possible inner types and enums
            const int maxDepth = 50;
            for (int i = 0; i < maxDepth; i++)
            {
                Debug.WriteLine($"RafaelSoft.TsCodeGen> InspectClassInternalsOneLevel ({i+1}/{maxDepth})");
                var prevCount = Count;
                InspectClassInternalsOneLevel();
                if (Count == prevCount)
                    break;
            }

            typesViaFullName.RemapWithUniqueKeys(typesViaShortName, mapFullName2ShortName, (oldKey, ttt) => ttt.GetFriendlyClassName());

            var specArray = typesViaShortName
                .OrderBy(kv => kv.Key)
                .Select(kv => BuildTsClassSpec(kv.Key, kv.Value))
                .ToArray();

            specArray.SortByDependencyParentsEarlier(
                child => child.NameOfSuperClass != null,
                (child, parent) => child.NameOfSuperClass == parent.Name);
            compiledTsClasses = specArray.ToList();
        }

        public List<TsClassSpec> CompiledTsClasses => compiledTsClasses;

        private void InspectClassInternalsOneLevel()
        {
            var typesOldList = typesViaFullName.Values.ToList();
            foreach (var typeObj in typesOldList)
            {
                var properties = GetJsonableProperties(typeObj);
                foreach (var pInfo in properties)
                {
                    var oldCount = Count;
                    AddType(pInfo.PropertyType);
                    if (oldCount != Count)
                        Debug.WriteLine($"RafaelSoft.TsCodeGen>   --> from property {pInfo.Name} of {typeObj.FullName}");
                }
            }
        }

        private IEnumerable<PropertyInfo> GetJsonableProperties(Type typeObj)
        {
            PropertyInfo[] properties = typeObj.GetProperties(BindingFlags.Public | BindingFlags.Instance); // NOTE: this line is copy+pasted from samplegeneration/objectgenerator.cs
            foreach (var pInfo in properties)
            {
                if (pInfo.GetCustomAttributes(true).Any(attr => attr is JsonIgnoreAttribute))
                    continue;
                yield return pInfo;
            }
        }

        private TsClassSpec BuildTsClassSpec(string name, Type typeObj)
        {
            if (typeObj.IsEnum)
            {
                return new TsClassSpec
                {
                    Name = name,
                    IsEnum = true,
                    EnumValues = BuildEnumSpec(typeObj)
                };
            }
            var resultSpec = new TsClassSpec
            {
                Name = name,
                Properties = new List<TsClassSpecProperty>(),
                NameOfSuperClass = GetBaseTypeNameIfPresentInList(typeObj),
                OriginalBackendType = typeObj,
            };
            var propsJsonable = GetJsonableProperties(typeObj);
            var typeObjBase = GetBaseTypeIfPresentInList(typeObj);
            if (typeObjBase != null)
                propsJsonable = propsJsonable.Where(p => typeObjBase.GetProperty(p.Name) == null);

            foreach (var pInfo in propsJsonable)
            {
                if (resultSpec.Properties.Any(prop => prop.Name == pInfo.Name))
                    continue;
                resultSpec.Properties.Add(MapSpecProperty(pInfo));
            }

            return resultSpec;
        }

        private TsEnumProperty[] BuildEnumSpec(Type typeObj)
        {
            var names = typeObj.GetEnumNames();
            var values = typeObj.GetEnumValues().ArrayToIEnumerable();

            return names.SelectFromMeAndAnotherListInParallel(values, (name, val) => new TsEnumProperty
            {
                Name = name,
                Value = EnumObj2Int(val)
            }).ToArray();
        }

        private int EnumObj2Int(object eVal)
        {
            var typecode = ((Enum)eVal).GetTypeCode();
            if (typecode == TypeCode.Int64)
                return (int)((long)eVal);
            if (typecode == TypeCode.Int16)
                return (int)((short)eVal);
            if (typecode == TypeCode.Char)
                return (int)((char)eVal);
            if (typecode == TypeCode.Byte)
                return (int)((byte)eVal);
            return (int)eVal;
        }

        private Type GetBaseTypeIfPresentInList(Type typeObj)
        {
            var baseType = typeObj.BaseType;
            if (baseType == typeof(object))
                baseType = typeObj.GetInterfaces().FirstOrDefault();
            if (baseType != null && !typesViaFullName.ContainsKey(baseType.FullName))
                return null;
            return baseType;
        }
        private string GetBaseTypeNameIfPresentInList(Type typeObj)
        {
            var baseType = GetBaseTypeIfPresentInList(typeObj);
            if (baseType == null)
                return null;
            if (typesViaFullName.ContainsKey(baseType.FullName))
                return mapFullName2ShortName[baseType.FullName];
            return null;
        }

        private TsClassSpecProperty MapSpecProperty(PropertyInfo pInfo)
        {
            var propertyType = pInfo.PropertyType;
            var customNameJsonPropertyAttribute = pInfo.GetCustomAttributes(true).FirstOrDefault(attr => attr is JsonPropertyAttribute) as JsonPropertyAttribute;
            var customTsReviverAttribute = pInfo.GetCustomAttributes(true).FirstOrDefault(attr => attr is TsCustomReviverAttribute) as TsCustomReviverAttribute;
            var tsModelTypeAttributeAttribute = pInfo.GetCustomAttributes(true).FirstOrDefault(attr => attr is TsModelTypeAttribute) as TsModelTypeAttribute;

            var propSpec = new TsClassSpecProperty
            {
                Name = customNameJsonPropertyAttribute?.PropertyName ?? pInfo.Name,
                TypeSpec = GetTypeSpecComplex(propertyType, new TsCsTypeSpecOptions
                {
                    TypeName = tsModelTypeAttributeAttribute?.TypeName
                }),
                CustomTsReviverScript = (customTsReviverAttribute != null) ? customTsReviverAttribute.TsScript : null,
            };

            return propSpec;
        }

        public TsCsTypeSpec GetTypeSpecComplex(Type propertyType, TsCsTypeSpecOptions options = null)
        {
            if (propertyType == null)
                return new TsCsTypeSpec { TypeName = "void" };
            // unwrap Nullable and IEnumerable generics.
            // when Nullable make that prop optional in TS AKA: name2?: string;
            if (propertyType.IsGenericType)
            {
                if (propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var innerType = propertyType.GenericTypeArguments.FirstOrDefault();
                    var typeSpec = GetTypeSpecBasic(innerType, options);
                    typeSpec.IsOptional = true;
                    return typeSpec;
                }
                else if (EnumerableTypes.Contains(propertyType.GetGenericTypeDefinition()))
                {
                    var innerType = propertyType.GenericTypeArguments.FirstOrDefault();
                    var typeSpec = GetTypeSpecBasic(innerType, options);
                    typeSpec.IsArray = true;
                    return typeSpec;
                }
                else if (DictionaryTypes.Contains(propertyType.GetGenericTypeDefinition()))
                {
                    var keyType = propertyType.GenericTypeArguments.FirstOrDefault();
                    var valueType = propertyType.GenericTypeArguments.Skip(1).FirstOrDefault();
                    var typeSpec = GetTypeSpecComplex(valueType, options);
                    if (typeSpec.IsArray)
                    {
                        typeSpec.IsArray = false;
                        typeSpec.IsDictionaryOfArrays = true;
                    }
                    else
                        typeSpec.IsDictionary = true;
                    typeSpec.TypeNameDictKey = GetTypeNameForDictKeyAs(keyType);
                    return typeSpec;
                }
            }
            else if (propertyType.IsArray)
            {
                var elemType = propertyType.GetElementType();
                var typeSpec = GetTypeSpecBasic(elemType, options);
                typeSpec.IsArray = true;
                return typeSpec;
            }
            // not generic, not array
            return GetTypeSpecBasic(propertyType, options);
        }

        private TsCsTypeSpec GetTypeSpecBasic(Type propertyType, TsCsTypeSpecOptions options)
        {
            if (propertyType.IsGenericType && StandardGeneric.Contains(propertyType.GetGenericTypeDefinition()))
                propertyType = propertyType.GenericTypeArguments.FirstOrDefault();

            // Enumerable (TODO: this should nbever be called should it?)
            if (propertyType.IsGenericType && EnumerableTypes.Contains(propertyType.GetGenericTypeDefinition()))
            {
                var genArgumentSpec = GetTypeSpecBasic(propertyType.GenericTypeArguments.FirstOrDefault(), options);
                genArgumentSpec.TypeName = genArgumentSpec.TypeName + "[]";
                return genArgumentSpec;
            }

            var typeSpec = new TsCsTypeSpec { TypeName = "void" }; // NOTE: this will be determined later, there should be no voids in the final result!!!!

            if (options?.TypeName != null)
                typeSpec.TypeName = options?.TypeName;
            else if(StandardTypeMap.ContainsKey(propertyType))
                typeSpec.TypeName = StandardTypeMap[propertyType];
            else if (mapFullName2ShortName.ContainsKey(propertyType.FullName))
                typeSpec.TypeName = mapFullName2ShortName[propertyType.FullName];

            if (mapFullName2ShortName.ContainsKey(propertyType.FullName))
                typeSpec.IsMine = true;
            if (propertyType.IsEnum)
            {
                typeSpec.IsEnum = true;
                typeSpec.IsMine = false; // NOTE: need to cancel the above if true, since we dont want enums to be revived like objects
            }
            if (propertyType == typeof(DateTime))
                typeSpec.IsDate = true;
            return typeSpec;
        }

        private string GetTypeNameForDictKeyAs(Type propertyType)
        {
            if (propertyType.IsGenericType && StandardGeneric.Contains(propertyType.GetGenericTypeDefinition()))
                propertyType = propertyType.GenericTypeArguments.FirstOrDefault();

            if (NumericTypes.Contains(propertyType))
                return "number";
            return "string";
        }

        private readonly Dictionary<Type, string> StandardTypeMap = new Dictionary<Type, string>
        {
            { typeof(string), "string" },
            { typeof(Guid), "string" },
            { typeof(DateTime), "Date" },
            { typeof(TimeSpan), "string" }, // TODO: consider using https://github.com/electricessence/TypeScript.NET
            { typeof(bool),  "boolean" },
            { typeof(bool?), "boolean" },
            { typeof(byte),  "number" },
            { typeof(byte?), "number" },
            { typeof(short),  "number" },
            { typeof(short?), "number" },
            { typeof(int),  "number" },
            { typeof(int?), "number" },
            { typeof(long),  "number" },
            { typeof(long?), "number" },
            { typeof(Single), "number" }, // NOTE: Single = float
            { typeof(Single?), "number" },
            { typeof(decimal),  "number" },
            { typeof(decimal?), "number" },
            //{ typeof(short),  "number" },
            //{ typeof(short?), "number" },
            { typeof(double),  "number" },
            { typeof(double?), "number" },
            { typeof(byte[]), "string" }, // NOTE: used as base64 string in JS/TS
            { typeof(string[]), "string[]" },
            { typeof(bool[]), "bool[]" },
        };

        private readonly List<Type> IgnoreTheseTypes = new List<Type>
        {
            typeof(void),
            typeof(object),
            typeof(string),
            typeof(decimal),
            typeof(DateTime),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(object[]),
            typeof(string[]),
            typeof(decimal[]),
            typeof(DateTime[]),
            typeof(Guid[]),
            typeof(bool[]),
            typeof(byte[]),
            typeof(short[]),
            typeof(int[]),
            typeof(long[]),
            typeof(float[]),
            typeof(double[]),
            typeof(Type),
        };

        private readonly List<Type> NumericTypes = new List<Type>
        {
            typeof(byte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal),
        };

        private readonly List<Type> EnumerableTypes = new List<Type>
        {
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IList<>),
            typeof(ISet<>),
            typeof(List<>),
        };

        private readonly List<Type> DictionaryTypes = new List<Type>
        {
            typeof(IDictionary<,>),
            typeof(Dictionary<,>),
        };

        private readonly List<Type> StandardGeneric = new List<Type>
        {
            typeof(Nullable<>),
        };
    }

}
