using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RafaelSoft.TsCodeGen.Models
{
    public class IsImplementedByAttribute : Attribute
    {
        public Type[] InheritingTypes { get; private set; }
        public IsImplementedByAttribute(Type[] inheritingTypes)
        {
            InheritingTypes = inheritingTypes;
        }
    }

    /// <summary>
    /// NOTE: $x in the TsScript will then be replaced with the contextual parameter during revival codegen
    /// </summary>
    public class TsCustomReviverAttribute : Attribute
    {
        public string TsScript { get; private set; }
        public TsCustomReviverAttribute(string tsScript)
        {
            TsScript = tsScript;
        }
    }

    /// <summary>
    /// Don't generate sample JSON for this property if it leads to a circular reference for example
    /// </summary>
    public class IgnorePropertyWhenMakingSampleJsonAttribute : Attribute
    { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
    public sealed class TsModelNameAttribute : Attribute
    {
        public TsModelNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class TsModelTypeAttribute : Attribute
    {
        public TsModelTypeAttribute(string typename)
        {
            TypeName = typename;
        }

        public string TypeName { get; private set; }
    }
}
