using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tools.Test")]


namespace Tools
{
    public class Program
    {
        public static void Main()
        {
            new GeneratePhonePrefixData("..\\..\\resources\\carrier").Run();
            new GenerateTimeZonesMapData("..\\..\\resources\\timezones\\map_data.txt").Run();
        }
    }
}
