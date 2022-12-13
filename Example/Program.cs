using System.Xml;
using TypeExtensions.Generated;

internal class Program
{
    [EmitType][Csv] private static string Grades { get; } = @"
""Last name"", ""First name"", ""Date Of Birth"", ""Subject1"", ""Subject2"", ""Subject3""
""Doe"",   ""John"",   1999-07-23, 23.0, 69.0, 42.0
""Doe"",   ""Jane"",   2001-02-16, 16.0, 48.0, 84.0
""Doe"",   ""John"",   2010-11-25, 25.0, 75.0, 168.0";
    [EmitType][Json][FromUri] private static string TodoItem { get; } = "https://jsonplaceholder.typicode.com/todos/1";
    [EmitType][Xml][FromUri] private static string PersonOfInterest { get; } = "D:\\Projects\\TypeProvider\\FileSample.xml";
    
    private static void Main(string[] args)
    {
        var todoItem_string = (new System.Net.WebClient()).DownloadString("https://jsonplaceholder.typicode.com/todos/23");
        var person_string = File.ReadAllText("D:\\Projects\\TypeProvider\\FileSample.xml");
        if (TodoItem_T.TryParse(todoItem_string, out var todoItem))
        {
            Console.WriteLine(todoItem);
        }

        if (PersonOfInterest_T.TryParse(person_string, out var person2))
        {
            Console.WriteLine(person2);
        }

        foreach(var student in Grades_T.ParseTable(Grades, hasHeader: true))
        {
            Console.WriteLine(student);
        }

    }
}
