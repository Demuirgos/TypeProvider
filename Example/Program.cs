using System.Xml;
using TypeExtensions.Generated;
internal class Program
{
    [EmitType] private static string PersonOfInterest { get; } = @"
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

    private static void Main(string[] args)
    {
        if (PersonOfInterest_T.TryParse(PersonOfInterest, out var person))
        {
            Console.WriteLine(person);
        } else {
            Console.WriteLine("Failed to parse PersonOfInterest");
        }
    }
}
