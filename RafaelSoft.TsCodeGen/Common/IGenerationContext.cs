namespace RafaelSoft.TsCodeGen.Common
{
    public enum IdentifierCaseType
    {
        Unchanged,
        Lowercase,
        LowercaseOnlyFirst,
    }
    public interface ITsClassGenerationConfig
    {
        string SpecNamePrefix { get; }
        string SpecNamePostfix { get; }
        bool SortFieldsAlphabetically { get; }
        bool SortMethodsAlphabetically { get; }
        IdentifierCaseType FieldCaseType { get; }
    }

    public class TsClassGenerationManualConfig : ITsClassGenerationConfig
    {
        public string SpecNamePrefix { get; set; } = "";
        public string SpecNamePostfix { get; set; } = "";
        public bool SortFieldsAlphabetically { get; set; } = true;
        public bool SortMethodsAlphabetically { get; set; } = true;
        public IdentifierCaseType FieldCaseType { get; set; } = IdentifierCaseType.Unchanged;
    }
}
    
