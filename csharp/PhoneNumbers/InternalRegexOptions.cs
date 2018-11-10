using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    /// <summary>
    /// This utility class determines the correct set of <see cref="RegexOptions"/> to specify when creating
    /// <see cref="Regex"/> instances at runtime within this library.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    /// todo relocate maybe
    public static class InternalRegexOptions
    {
        public static RegexOptions Default { get; }

        static InternalRegexOptions()
        {
#if PORTABLE
            Default = RegexOptions.None;
#else
            Default = RegexOptions.Compiled;
#endif
        }
    }
}