using System;
using System.Text;
using System.Globalization;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text.Json;
using System.Data;
using System.IO;
using System.Reflection.PortableExecutable;
using TypeProvider.Shared;

namespace TypeProvider.Csv;

public class TypeInstantiator : ITypeEmitter
{
    public string EmitParser(string name, IEnumerable<(string name, string type)> props) {
        var sb = new StringBuilder();
        sb.Append($"result = new {name}();\n");
        sb.Append($"\t\t\tstring[] values = csvLine.Split(',');\n");
        for (int i = 0; i < props.Count(); i++)
        {
            (string propName, string propType) = props.ElementAt(i);
            propName = propName.Trim(new Char[] { ' ', '\"'}).ToPascal();
            if(propType == nameof(String))
                sb.Append($"\t\t\tresult.{propName} = values[{i}].Trim();\n");
            else 
                sb.Append($"\t\t\tresult.{propName} = {propType}.Parse(values[{i}].Trim());\n");   
        }
        return sb.ToString();
    }
    public string EmitForm(string name, IEnumerable<(string name, string type)> props)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        foreach (var prop in props)
        {
            var cleanedName = prop.name.Trim(new Char[] { ' ', '\"'}).ToPascal();
            sb.Append($"\tpublic {prop.type} {cleanedName} {{ get; set; }}\n");
        }

        sb.Append($@"
    public static bool TryParse(string csvLine, out {name} result) {{
        try {{
            {EmitParser(name, props)}
            return true;
        }} catch (Exception) {{
            result = default;
            return false;
        }}
    }}
");

        sb.Append($@"
public static IEnumerable<{name}> ParseTable(string csvTable, bool hasHeader) {{
    byte[] byteArray = Encoding.UTF8.GetBytes(csvTable);
    MemoryStream stream = new MemoryStream(byteArray);
    StreamReader reader = new StreamReader(stream);

    string line = reader.ReadLine();
    while (line != null && String.IsNullOrWhiteSpace(line))
           line = reader.ReadLine(); 

    if(hasHeader) line = reader.ReadLine();
    do {{
        if(TryParse(line, out var result))
            yield return result;
        else throw new Exception(""Invalid csv line"");
    }} while((line = reader.ReadLine()) != null);
}}
        ");

        sb.Append($"\tpublic override string ToString() => JsonSerializer.Serialize<{name}>(this);\n");
        sb.Append("}\n");
        return sb.ToString();
    }
    public static string FileTemplate(string fileBody)
        => $"using System;\nusing System.Text.Json;\nusing System.Text;\nnamespace TypeExtensions.Generated;\n\n{fileBody}";
    public static string TypeTemplate(string scope, string typename, String body, string typekind, int nesting = 0)
    {
        string prefix = new string('\t', nesting);
        body = body.Replace("\n", $"\n{prefix}");
        return $"{prefix}{scope} {typekind} {typename} {body}\n";
    }

    private int i = 0;
    private Dictionary<int, string> emmited_types = new(); // hashform => typename
    private Dictionary<string, string> type_impl = new(); // typename => codeform
    string AddTypeToEmitionTargets(ref string name, List<(string type, string name)> properties)
    {
        string typename(string name, int i) => $"{name}{i}";
        var recordForm = EmitForm(name, properties);
        emmited_types.Add(0, name);
        type_impl.Add(name, recordForm);
        return name;
    }
    string GetObjectType(List<(string name, string type)> properties, string name)
    {
        return AddTypeToEmitionTargets(ref name, properties);
    }
    string GetValueType(string kind)
    {
        if(kind == "null") return nameof(Object);
        
        if(Decimal.TryParse(kind, out _)) 
        {
            return nameof(Decimal);
        }
        else if (Boolean.TryParse(kind, out _))
        {
            return nameof(Boolean);
        }
        else if (DateTime.TryParse(kind, out _))
        {
            return nameof(DateTime);
        } 
        else if(String.IsNullOrEmpty(kind)) 
        {
            throw new Exception("Invalid field : empty string");
        } 
        else return nameof(String);
    }
    public Dictionary<int, (string name, string type)> ExtractStructure(string csvString)
    {
        byte[] byteArray = Encoding.UTF8.GetBytes(csvString);
        MemoryStream stream = new MemoryStream(byteArray);
        StreamReader reader = new StreamReader(stream);
        Dictionary<int, (string name, string type)> fields = new();

        string line = reader.ReadLine();
        while (line != null && String.IsNullOrWhiteSpace(line))
            line = reader.ReadLine();

        string[] values = line.Split(',');
        for (int i = 0; i < values.Length; i++)
        {
            fields.Add(i, (values[i], "String"));
        }

        while((line = reader.ReadLine()) != null) {
            values = line.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                fields[i] = (fields[i].name, GetValueType(values[i].Trim()));
            }
        }
        return fields;
    }
    
    public void GenerateTypes(string name, string sample, bool isRecord = false)
    {
        try
        {
            var table = ExtractStructure(sample);
            _ = GetObjectType(table.Values.ToList(), name);
        } catch (Exception ex)
        {
            throw new InvalidPropertyValue(ex);
        }
    }
    public string EmitTypes(string name)
    {
        StringBuilder sb = new StringBuilder();
        var type_code = TypeTemplate("public" /*isRoot ? "file" : "public"*/, name, type_impl[name], "record");
        sb.Append(type_code);
        return FileTemplate(sb.ToString());
    }
}