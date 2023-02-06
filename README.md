# TypeProvider.General
A prototype remake of FSharp.TypeProvider in CSharp

Project Nuget : ``dotnet add package Csharp.TypeProvider --version 0.1.0``

Usage : 
```csharp
[EmitType, Csv] private static string Grades { get; } = @"
""Last name"", ""First name"", ""Date Of Birth"", ""Subject1"", ""Subject2"", ""Subject3""
""Doe"",   ""John"",   1999-07-23, 23.0, 69.0, 42.0
""Doe"",   ""Jane"",   2001-02-16, 16.0, 48.0, 84.0
""Doe"",   ""John"",   2010-11-25, 25.0, 75.0, 168.0";

[EmitType, Json, FromUri] private static string TodoItem { get; } = "https://jsonplaceholder.typicode.com/todos/1";

[EmitType, Xml, FromUri] private static string PersonOfInterest { get; } = "D:\\Projects\\TypeProvider\\FileSample.xml";

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
```

Generated (without parsers): 
* Xml Case : 
```csharp
public record PersonOfInterest_T {
	public DateTime DateOfBirth { get; set; }
	public String[] Nationalities { get; set; }
	public String Name { get; set; }
	public Decimal Age { get; set; }
}
```

* Json Case : 
```csharp
public record TodoItem_T {
	public Decimal userId { get; set; }
	public Decimal id { get; set; }
	public String title { get; set; }
	public Boolean completed { get; set; }
}
```

* Csv Case :
```csharp
public record Grades_T {
	public String LastName { get; set; }
	public String FirstName { get; set; }
	public DateTime DateOfBirth { get; set; }
	public Decimal Subject1 { get; set; }
	public Decimal Subject2 { get; set; }
	public Decimal Subject3 { get; set; }
}
```
