using System.Xml;
using TypeExtensions.Generated;

internal class Program
{
    [EmitType][Json] private static string PersonOfInterest1 { get; } = @"{ 
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
    [EmitType][Xml] private static string PersonOfInterest2 { get; } = @"
            <PersonOfInterest Name=""John Doe"" Age=""23"">
                <Projects>    
                    <Project Title=""Project UNO"">    
                        <Value Estimated=""23"" Actual=""7""/>
                    </Project>
                    <Project Title=""Project DOS"">    
                        <Value Estimated=""69"" Actual=""123""/>
                    </Project>
                    <Project Title=""Project MINU"">    
                        <Value Estimated=""69"" Actual=""123""/>
                    </Project>
                </Projects>
                <Keys> 
                    <Key>23 </Key>
                    <Key>69 </Key>
                    <Key>123</Key>
                </Keys>
                <CurrentProject Title=""Project DOS"">
                    <Value Estimated=""69"" Actual=""123""/>
                </CurrentProject>
            </PersonOfInterest>";
    [EmitType][Csv] private static string PersonOfInterest3 { get; } = @"
""Last name"", ""First name"", ""Date Of Birth"", ""Project1"", ""Project2"", ""Project3""
""Doe"",   ""John"",   1999-07-23, 23.0, 69.0, 42.0
""Doe"",   ""Jane"",   2001-02-16, 16.0, 48.0, 84.0
""Doe"",   ""John"",   2010-11-25, 25.0, 75.0, 168.0";
    [EmitType][Json][FromUri] private static string PersonOfInterest4 { get; } = "D:\\Projects\\TypeProvider\\FileSample.json";
    [EmitType][Json][FromUri] private static string PersonOfInterest5 { get; } = "https://jsonplaceholder.typicode.com/todos/1";
    
    private static void Main(string[] args)
    {
        if (PersonOfInterest1_T.TryParse(PersonOfInterest1, out var person1))
        {
            Console.WriteLine(person1);
        }

        if (PersonOfInterest2_T.TryParse(PersonOfInterest2, out var person2))
        {
            Console.WriteLine(person2);
        }

        foreach(var person3 in PersonOfInterest3_T.ParseTable(PersonOfInterest3, hasHeader: true))
        {
            Console.WriteLine(person3);
        }

    }
}
