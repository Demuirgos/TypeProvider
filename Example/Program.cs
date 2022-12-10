using TypeExtensions.Generated;
internal class Program
{
    [EmitType] private static string PersonOfInterest { get; } = @"
""Last name"", ""First name"", ""Date Of Birth"", ""Project1"", ""Project2"", ""Project3""
""Doe"",   ""John"",   1999-07-23, 23.0, 69.0, 42.0";

    private static void Main(string[] args)
    {
        PersonOfInterest_T person= new PersonOfInterest_T();
    }
}
