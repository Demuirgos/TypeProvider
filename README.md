# TypeProvider.Json
A prototype remake of FSharp.JsonTypeProvider in CSharp

Usage : 
```csharp
[EmitType] private static string SampleType1 { get; } = @"{ 
    ""bool_Val"" : true, 
    ""num_Val"" : 23.69, 
    ""obj_Val"" : {
      ""nested_Field"" : ""23""
    }, 
    ""arr_Val"" : [23, 69], 
    ""arr_obj_Val"" : [
    {
      ""nested_Field"" : ""23""
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
using System;
namespace TypeExtensions.Generated;

public record ObjVal_T {
    public String nested_Field { get; set; }
}

public record SampleType1 {
    public Boolean bool_Val { get; set; }
    public Decimal num_Val { get; set; }
    public ObjVal_T obj_Val { get; set; }
    public Decimal[] arr_Val { get; set; }
    public ObjVal_T[] arr_obj_Val { get; set; }
}

public record SampleType2 {
    public Boolean bool_Val { get; set; }
    public Decimal num_Val { get; set; }
    public ObjVal_T obj_Val { get; set; }
    public Decimal[] arr_Val { get; set; }
    public ObjVal_T[] arr_obj_Val { get; set; }
}

public record ObjVal_T {
    public String nestedField { get; set; }
}
```
