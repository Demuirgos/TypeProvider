# TypeProvider.Json
A prototype remake of FSharp.JsonTypeProvider in CSharp

Usage : 
```csharp
[EmitType] private static string PersonOfInterest { get; } = @"{ 
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
            }
    }";

    private static void Main(string[] args)
    {
        PersonOfInterest_T person = new PersonOfInterest_T();
    }
```

Generated : 
```csharp
public record PersonOfInterest {
	public record Value_T {
		public String Estimated { get; set; }
		public String Actual { get; set; }
	}
	
	public record Projects_T {
		public String Title { get; set; }
		public Value_T Value { get; set; }
	}
	
	public String Name { get; set; }
	public Decimal Age { get; set; }
	public Projects_T[] Projects { get; set; }
	public Decimal[] Keys { get; set; }
	public Projects_T CurrentProject { get; set; }
}
```
