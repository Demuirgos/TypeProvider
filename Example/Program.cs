using System.Xml;
using TypeExtensions.Generated;
internal class Program
{
    /*
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
            }*/
    [EmitType] private static string PersonOfInterest { get; } = @"
            <PersonOfInterest Name=""John Doe"" Age=""23"">
                <Projects test=""testing"">    
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
                <CurrentProject>
                    <Project Title=""Project DOS"">    
                        <Value Estimated=""69"" Actual=""123""/>
                    </Project>
                </CurrentProject>
            </PersonOfInterest>";

    private static void Main(string[] args)
    {
        PersonOfInterest_T person = new PersonOfInterest_T();
    }
}