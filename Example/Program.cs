using System.Text.Json;
using TypeExtensions.Generated;
internal class Program
{
    [EmitType] private static string SampleType1 { get; } = @"{ 
            ""bool_Val"" : true, 
            ""num_Val"" : 23.69, 
            ""obj_Val"" : {
                ""nested_Field"" : ""23""
            }, 
            ""arr_Val"" : [23, 69], 
            ""arr_obj_Val"" : [
                {
                    ""nested_Field"" : ""23""
                }
            ] 
    }";

    [EmitType("./FileSample.json")] private static string SampleType2 { get; }

    private static void Main(string[] args)
    {
        SampleType1 test = JsonSerializer.Deserialize<SampleType1>(SampleType1);
        Console.WriteLine(test.num_Val);
    }
}