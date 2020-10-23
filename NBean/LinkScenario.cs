namespace NBean
{
    public class LinkScenario
    {
        // referencing Bean (m Bean)
        public string LinkingKind { get; set; }
        public object LinkingKindPkValue { get; set; }
        public string LinkingKindFkName { get; set; }

        // referenced Bean (n Bean)
        public string LinkedKind { get; set; }
        public string LinkedKindPkName { get; set; }
        public string LinkedKindFkName { get; set; }
        
        // linking Bean (m:n Bean)
        public string LinkKind { get; set; }
        public string LinkKindPkName { get; set; }
    }
}