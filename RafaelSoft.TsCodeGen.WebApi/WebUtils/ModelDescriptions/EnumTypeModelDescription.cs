using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RafaelSoft.TsCodeGen.WebApi.WebUtils.ModelDescriptions
{
    public class EnumTypeModelDescription : ModelDescription
    {
        public EnumTypeModelDescription()
        {
            Values = new Collection<EnumValueDescription>();
        }

        public Collection<EnumValueDescription> Values { get; private set; }
    }
}