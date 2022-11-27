using System;

[System.AttributeUsage(System.AttributeTargets.Property)]
public class EmitTypeAttribute : System.Attribute
{
    private string filePath;
    public EmitTypeAttribute(string FilePath = null)
        => filePath = FilePath;
}