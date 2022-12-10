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

public class InvalidPropertyType : Exception {
    public InvalidPropertyType() : base($"Build failed with Error : {nameof(InvalidPropertyType)} (Tagged property must be a string)") { }
}
public class InvalidPropertyAccess : Exception {
    public InvalidPropertyAccess() : base($"Build failed with Error : {nameof(InvalidPropertyAccess)} (Tagged property must be readonly)") { }
}
public class InvalidPropertyValue : Exception {
    public InvalidPropertyValue() : base($"Build failed with Error : {nameof(InvalidPropertyValue)} (Tagged property must be a correct json)") { }
}

public static class ToolExtensions {
    public static string ToPascal(this string Identifier) 
        =>  CultureInfo.CurrentCulture
                        .TextInfo
                        .ToTitleCase(Identifier.ToLower().Replace("_", " ")).Replace(" ", string.Empty);

    public static DiagnosticDescriptor Rule(this Exception ex) => new DiagnosticDescriptor(
        id: "JTP01",
        title: "Argument format error",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Property Type Emission must be : Readonly, a string and private static",
        messageFormat: ex.Message);

}
public class TypeInstantiator
{
    public string EmitForm(IEnumerable<(string name, string type)> props)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        foreach (var prop in props)
        {
            var cleanedName = prop.name.Trim(new Char[] { ' ', '\"'}).ToPascal();
            sb.Append($"\tpublic {prop.type} {cleanedName} {{ get; set; }}\n");
        }
        sb.Append("}\n");
        return sb.ToString();
    }
    public static string FileTemplate(string fileBody)
        => $"using System;\nnamespace TypeExtensions.Generated;\n\n{fileBody}";

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
        var recordForm = EmitForm(properties);
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
            throw new InvalidPropertyValue();
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

[Generator]
public class TypesGenerator : ISourceGenerator
{
    private (string, string)[] GetAllMarkedProperties(Compilation context)
    {
        IEnumerable<SyntaxNode> allNodes = context.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
        return allNodes
            .Where(d => d.IsKind(SyntaxKind.PropertyDeclaration))
            .OfType<PropertyDeclarationSyntax>()
            .SelectMany(propDef =>
            {
                var semanticModel = context.GetSemanticModel(propDef.SyntaxTree);
                var type = semanticModel.GetTypeInfo(propDef.Type).Type;
                if (type.SpecialType != SpecialType.System_String)
                {
                    throw new InvalidPropertyType();
                }

                if (propDef.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration))
                {
                    throw new InvalidPropertyAccess();
                }

                return propDef.AttributeLists
                    .SelectMany(x => x.Attributes)
                    .Where(attr =>
                    {
                        var attrName = attr.Name.ToString();
                        return attrName == nameof(EmitTypeAttribute).Replace("Attribute", String.Empty);
                    }).Select(attr =>
                    {
                        var typename = propDef.Identifier.Value.ToString();
                        bool fromFile = attr.ArgumentList?.Arguments.Count > 0;
                        string typesample;
                        if (fromFile)
                        {
                            var path = semanticModel.GetConstantValue(attr.ArgumentList.Arguments.Single().Expression).ToString();
                            typesample = File.ReadAllText(path);
                        } else
                        {
                            typesample = semanticModel.GetConstantValue(propDef.Initializer.Value).ToString();
                        }
                        return ($"{typename}_T", typesample);
                    });
            }).ToArray();
    }
    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            var allSamples = GetAllMarkedProperties(context.Compilation);
            foreach (var (name, sample) in allSamples)
            {
                var engine = new TypeInstantiator();
                engine.GenerateTypes(name, sample.Trim());
                context.AddSource($"{name}.Type.g.cs", SourceText.From(engine.EmitTypes(name), Encoding.UTF8));
            }

        }
        catch (Exception ex){
            context.ReportDiagnostic(Diagnostic.Create(ex.Rule(), Location.None));
        }

    }

    public void Initialize(GeneratorInitializationContext context)
    {
#if DEBUG
          // if (!Debugger.IsAttached)
          // {
          //     Debugger.Launch();
          // }
#endif 
    }
}