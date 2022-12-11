# TypeProvider.General
A prototype remake of FSharp.TypeProvider in CSharp

Usage : 
```csharp
[EmitType][Json] private static string PersonOfInterest1 { get; } = @"{ 
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
    
    [EmitType][Xml] private static string PersonOfInterest2 { get; } = @"
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

            
    [EmitType][Csv] private static string PersonOfInterest3 { get; } = @"
""Last name"", ""First name"", ""Date Of Birth"", ""Project1"", ""Project2"", ""Project3""
""Doe"",   ""John"",   1999-07-23, 23.0, 69.0, 42.0
""Doe"",   ""Jane"",   2001-02-16, 16.0, 48.0, 84.0
""Doe"",   ""John"",   2010-11-25, 25.0, 75.0, 168.0";


    private static void Main(string[] args)
    {
        if (PersonOfInterest1_T.TryParse(PersonOfInterest1, out var person1))
        {
            Console.WriteLine(person1);
        }

        if (PersonOfInterest2_T.TryParse(PersonOfInterest2, out var person2))
        {
            Console.WriteLine(person2);
        }

        foreach(var person3 in PersonOfInterest3_T.ParseTable(PersonOfInterest3, hasHeader: true))
        {
            Console.WriteLine(person3);
        }
    }
```

Generated (without parsers): 
* Xml Case : 
```csharp
public record PersonOfInterest {
	public record Value_T {
		public Decimal Estimated { get; set; }
		public Decimal Actual { get; set; }
	}
	
	public record Project_T {
		public Value_T Value { get; set; }
		public String Title { get; set; }
	}
	
	public record Projects_T {
		public Project_T[] Project { get; set; }
		public String test { get; set; }
	}
	
	public record CurrentProject_T {
		public Project_T Project { get; set; }
	}
	
	public record PersonOfInterest_T {
		public Projects_T Projects { get; set; }
		public Decimal[] Keys { get; set; }
		public CurrentProject_T CurrentProject { get; set; }
		public String Name { get; set; }
		public Decimal Age { get; set; }
	}
	
	public PersonOfInterest_T PersonOfInterest { get; set; }
}
```

* Json Case : 
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

* Csv Case :
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
