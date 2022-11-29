using System.Text.Json;
using TypeExtensions.Generated;
internal class Program
{
    [EmitType] private static string PersonOfInterest { get; } = @"{ 
            ""Name"" : ""John Doe"", 
            ""Age"" : 23, 
            ""Projects"" : [
                {
                    ""Title"" : ""Project UNO"",
                    ""Value"" : {
                        ""Estimated"" : ""23"",
                        ""Actual"" : ""7""
                    }
                },
                {
                    ""Title"" : ""Project DOS"",
                    ""Value"" : {
                        ""Estimated"" : ""69"",
                        ""Actual"" : ""123""
                    }
                }
            ], 
            ""Keys"" : [23, 69, 123], 
            ""CurrentProject"" :{
                ""Title"" : ""Project TRES"",
                ""Value"" : {
                    ""Estimated"" : ""7"",
                    ""Actual"" : ""5""
                }
            }
    }";

    [EmitType("D:\\Projects\\TypeProvider\\TypeProvider.Json\\FileSample.json")] private static string SampleType2 { get; }

    private static void Main(string[] args)
    {
        var JohnDoe = JsonSerializer.Deserialize<PersonOfInterest>(PersonOfInterest);
        Console.WriteLine(JohnDoe);
    }
}