namespace NBean.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class |
                           System.AttributeTargets.Method |
                           System.AttributeTargets.Property |
                           System.AttributeTargets.Field)
    ]
    public class OmitFromCodeCoverageAttribute : System.Attribute
    {
    }
}
