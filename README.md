# TypeProvider.Xml
A prototype remake of FSharp.XmlTypeProvider in CSharp

Usage : 
```csharp
[EmitType] private static string PersonOfInterest { get; } = @"
            <PersonOfInterest Name=""John Doe"" Age=""23"">
                <Projects test=""testing"">    
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
                <CurrentProject>
                    <Project Title=""Project DOS"">    
                        <Value Estimated=""69"" Actual=""123""/>
                    </Project>
                </CurrentProject>
            </PersonOfInterest>";

    private static void Main(string[] args)
    {
        PersonOfInterest person = new PersonOfInterest();
    }
```

Generated : 
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
