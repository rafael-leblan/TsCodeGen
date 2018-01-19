using System;
using System.Reflection;

namespace RafaelSoft.TsCodeGen.WebApi.WebUtils.ModelDescriptions
{
    public interface IModelDocumentationProvider
    {
        string GetDocumentation(MemberInfo member);

        string GetDocumentation(Type type);
    }
}