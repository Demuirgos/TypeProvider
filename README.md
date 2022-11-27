# TypeProvider.Json
A prototype remake of FSharp.JsonTypeProvider in CSharp

Usage : 
```csharp
[EmitType] private static string SampleType1 { get; } = @"{ 
    ""boolVal"" : true, 
    ""numVal"" : 23.69, 
    ""objVal"" : {
      ""nestedField"" : ""23""
    }, 
    ""arrVal"" : [23, 69], 
    ""arrObjVal"" : [
    {
      ""nestedField"" : ""23""
    }
  ] 
}";

[EmitType("./FileSample.json")] private static string SampleType2 { get; }

private static void Main(string[] args)
{
  SampleType1 test = JsonSerializer.Deserialize<SampleType1>(SampleType1);
}
```

Generated : 
```csharp
public record SampleType1 {
	public record ObjVal_T {
		public String nestedField { get; set; }
	}
	
	public Boolean boolVal { get; set; }
	public Decimal numVal { get; set; }
	public ObjVal_T objVal { get; set; }
	public Decimal[] arrVal { get; set; }
	public ObjVal_T[] arrObjVal { get; set; }
}
```
