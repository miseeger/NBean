namespace NBean.Interfaces
{
    public interface IValueRelaxations
    {
        bool TrimStrings { get; set; }
        bool ConvertEmptyStringToNull { get; set; }
        bool RecognizeIntegers { get; set; }
    }
}
