using RafaelSoft.TsCodeGen.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RafaelSoft.TsCodeGen.Services
{
    public interface ITsCodeGenLogger
    {
        void LogAddType(CodeGenLoggerAddTypeEntry entry);
    }

    //==========================================================================================================================================================================

    public class TsCodeGenLogger_Noop : ITsCodeGenLogger
    {
        public void LogAddType(CodeGenLoggerAddTypeEntry entry) { }
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    public abstract class TsCodeGenLoggerConsoleBase : ITsCodeGenLogger
    {
        protected abstract void LogText(string s);

        public void LogAddType(CodeGenLoggerAddTypeEntry entry)
        {
            LogText($"AddType {entry.TypeName}");
            if (entry.Reason == AddTypeReasonType.FromApi)
                LogText($"  --> from api {entry.ApiId}");
            if (entry.Reason == AddTypeReasonType.PropertyOf)
                LogText($"  --> from property {entry.PropertyOrParam} of {entry.OfType}");
            if (entry.Reason == AddTypeReasonType.Manually)
                LogText($"  --> manually");
            if (entry.Reason == AddTypeReasonType.ExplicitlyMentionedInheritingTypes)
                LogText($"  --> Explicitly Mentioned Inheriting Types of {entry.OfType}");
            if (entry.Reason == AddTypeReasonType.FromInterfaceParam)
                LogText($"  --> from interface method {entry.OfMethod} param {entry.PropertyOrParam}");
            if (entry.Reason == AddTypeReasonType.FromInterfaceReturnType)
                LogText($"  --> from interface method {entry.OfMethod} return type");
        }
    }

    public class TsCodeGenLogger_DebugTrace : TsCodeGenLoggerConsoleBase
    {
        protected override void LogText(string s)
            => Debug.WriteLine(s.IndentEveryLine("TSCODEGEN> "));
    }

    public class TsCodeGenLogger_BigStringFroWeb : TsCodeGenLoggerConsoleBase
    {
        private readonly StringBuilder sb = new StringBuilder();
        public string LogAsOneString => sb.ToString();

        protected override void LogText(string s)
            => sb.Append(s + "\n");
    }

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    public class TsCodeGenLogger_Json : ITsCodeGenLogger
    {
        public List<CodeGenLoggerAddTypeEntry> Entries { get; } = new List<CodeGenLoggerAddTypeEntry>();

        public void LogAddType(CodeGenLoggerAddTypeEntry entry)
            => Entries.Add(entry);
    }

    //==========================================================================================================================================================================

    public enum AddTypeReasonType
    {
        FromApi,
        PropertyOf,
        Manually,
        ExplicitlyMentionedInheritingTypes,
        FromInterfaceReturnType,
        FromInterfaceParam,
    }

    public class CodeGenLoggerAddTypeEntry
    {
        public string TypeName { get; set; }
        public AddTypeReasonType Reason { get; set; }
        public string ApiId { get; set; }
        public string PropertyOrParam { get; set; }
        public string OfType { get; set; }
        public string OfMethod { get; set; }
    }
}
