# TypeProvider.Json
A prototype remake of FSharp.JsonTypeProvider in CSharp

Usage : 
```csharp
[EmitType] private static string SampleType { get;  } = "{ \"boolVal\" : true, \"numVal\" : 23.69, \"objVal\" : {\"nestedField\" : \"23\"}, \"arrVal\" : [23, 69]}";

private static void Main(string[] args)
{
  SampleType test = new SampleType();
  test.boolVal = true;
  test.numVal = 23.69M;
  test.objVal = new Objval_T
  {
    nestedField = "23"
  };
}
```

Generated : 
```csharp
file record Objval_T {
    String nestedField { get; set; }
}

public record test {
    Boolean boolVal { get; set; }
    Decimal numVal { get; set; }
    Objval_T objVal { get; set; }
    Decimal[] arrVal { get; set; }
}
```
