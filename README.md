# TypeProvider.Csv
A prototype remake of FSharp.TypeProvider in CSharp

Usage : 
```csharp
[EmitType] private static string PersonOfInterest { get; } = @"
""Last name"", ""First name"", ""Date Of Birth"", ""Project1"", ""Project2"", ""Project3""
""Doe"",   ""John"",   1999-07-23, 23.0, 69.0, 42.0
""Dane"",   ""Jane"",   1969-07-23, 123.0, 69.0, 420.0";

    private static void Main(string[] args)
    {
        PersonOfInterest_T person = new PersonOfInterest_T();
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
