using System.Text.Json;
using TypeExtensions.Generated;
internal class Program
{
    [EmitType] private static string SampleType { get; } = @"{ 
            ""boolVal"" : true, 
            ""numVal"" : 23.69, 
            ""objVal"" : {
                ""nestedField"" : ""23""
            }, 
            ""arrVal"" : [23, 69], 
            ""arrobjVal"" : [
                {
                    ""nestedField"" : ""23""
                }
            ] 
    }";

    private static void Main(string[] args)
    {
        SampleType test = JsonSerializer.Deserialize<SampleType>(SampleType);
        Console.WriteLine(test.numVal);
    }
}