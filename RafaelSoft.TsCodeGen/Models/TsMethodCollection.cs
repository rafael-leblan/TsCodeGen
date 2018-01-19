using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RafaelSoft.TsCodeGen.Models
{
    public class TsMethodCollection
    {
        private TsClassCollection classes;
        private List<TsMethodSpec> methods = new List<TsMethodSpec>();
        public IEnumerable<TsMethodSpec> Methods => methods;

        public TsMethodCollection(TsClassCollection classes)
        {
            this.classes = classes;
        }

        //------------------------------------------------

        public static TsMethodCollection FromInterface(TsClassCollection classes, Type t)
        {
            var result = new TsMethodCollection(classes);
            var reflectMethods = t.GetMethods(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            foreach (var reflex in reflectMethods)
            {
                classes.AddType(reflex.ReturnType);
                foreach (var reflexPara in reflex.GetParameters())
                    classes.AddType(reflexPara.ParameterType);
                result.methods.Add(new TsMethodSpec(classes)
                {
                    MethodName = reflex.Name,
                    ReturnType = reflex.ReturnType,
                    ParamSpecs = reflex.GetParameters().Select(reflexPara => new TsMethodParamSpec(classes)
                    {
                        ParamName = reflexPara.Name,
                        ParamType = reflexPara.ParameterType
                    }).ToArray()
                });
            }

            return result;
        }
    }

    public class TsMethodSpec
    {
        private TsClassCollection classes;
        public TsMethodSpec(TsClassCollection classes) { this.classes = classes; }

        public string MethodName { get; set; }
        public TsMethodParamSpec[] ParamSpecs { get; set; }
        public Type ReturnType { get; set; }

        public string TsReturnTypeName => classes.GetTypeSpecComplex(ReturnType).ToTsString();
    }

    public class TsMethodParamSpec
    {
        private TsClassCollection classes;
        public TsMethodParamSpec(TsClassCollection classes) { this.classes = classes; }

        public string ParamName { get; set; }
        public Type ParamType { get; set; }

        public string TsParamTypeName => classes.GetTypeSpecComplex(ParamType).ToTsString();
        public string TsParamTypeReviver(string paramName) => classes.GetTypeSpecComplex(ParamType).GetTsFullReviver(paramName);
    }
}
