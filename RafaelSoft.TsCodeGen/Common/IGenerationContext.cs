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
        IdentifierCaseType FieldCaseType { get; }
    }

    public class TsClassGenerationManualConfig : ITsClassGenerationConfig
    {
        public string SpecNamePrefix { get; set; } = "";
        public string SpecNamePostfix { get; set; } = "";
        public IdentifierCaseType FieldCaseType { get; set; } = IdentifierCaseType.Unchanged;
    }
}
    
