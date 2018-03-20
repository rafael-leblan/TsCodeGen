using System.Collections.Generic;
using RafaelSoft.TsCodeGen.Common;

namespace RafaelSoft.TsCodeGen.Models
{
    public class TsImportsManager
    {
        private List<string> imports = new List<string>();

        public TsImportsManager() { }

        public void AddImportStatement(string statement)
        {
            if (!imports.Contains(statement))
                imports.Add(statement);
        }

        public void AddImportStatements(string[] importStatements)
        {
            foreach (var statement in importStatements)
                AddImportStatement(statement);
        }

        public string GenerateCode()
        {
            return imports.StringJoin("\n");
        }
    }
}
