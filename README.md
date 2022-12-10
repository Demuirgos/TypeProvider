# TypeProvider.Csv
A prototype remake of FSharp.TypeProvider in CSharp

Usage : 
```csharp
[EmitType] private static string PersonOfInterest { get; } = @"
""Last name"", ""First name"", ""Date Of Birth"", ""Project1"", ""Project2"", ""Project3""
Doe,   John,   1999-07-23, 23.0, 69.0, 42.0
Doe,   Jane,   2001-02-16, 16.0, 48.0, 84.0
Doe,   John,   2010-11-25, 25.0, 75.0, 168.0";

    private static void Main(string[] args)
    {
        if(PersonOfInterest_T.TryParse(PersonOfInterest.Split("\n")[2], out var result))
        {
            Console.WriteLine($"{result.FirstName} {result.LastName}");
        }

        foreach(var item in PersonOfInterest_T.ParseTable(PersonOfInterest, true) ) { 
            Console.WriteLine(item.ToString());
        }
    }
```

Generated : 
```csharp
public record PersonOfInterest_T {
	public String LastName { get; set; }
	public String FirstName { get; set; }
	public DateTime DateOfBirth { get; set; }
	public Decimal Project1 { get; set; }
	public Decimal Project2 { get; set; }
	public Decimal Project3 { get; set; }
}
```
