using System;

[System.AttributeUsage(System.AttributeTargets.Property)]
public class EmitTypeAttribute : System.Attribute
{
    private string filePath;
    public EmitTypeAttribute(string FilePath = null)
        => filePath = FilePath;
}

[System.AttributeUsage(System.AttributeTargets.Property)]
public class XmlAttribute : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Property)]
public class CsvAttribute : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Property)]
public class JsonAttribute : System.Attribute
{
}