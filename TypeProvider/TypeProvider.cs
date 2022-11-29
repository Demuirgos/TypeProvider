using System;
using System.Globalization;
using System.Text;
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

public class InvalidPropertyType : Exception {
    public InvalidPropertyType() : base($"Build failed with Error : {nameof(InvalidPropertyType)} (Tagged property must be a string)") { }
}
public class InvalidPropertyAccess : Exception {
    public InvalidPropertyAccess() : base($"Build failed with Error : {nameof(InvalidPropertyValue)} (Tagged property must be a correct json)") { }
}
public class InvalidPropertyValue : Exception {
    public InvalidPropertyValue() : base($"Build failed with Error : {nameof(InvalidPropertyAccess)} (Tagged property must be readonly)") { }
}

public static class ToolExtensions {
    public static string ToPascal(this string Identifier) 
        =>  CultureInfo.CurrentCulture
                        .TextInfo
                        .ToTitleCase(Identifier.ToLower().Replace("_", " ")).Replace(" ", string.Empty);

    public static DiagnosticDescriptor Rule(Exception ex) => new DiagnosticDescriptor(
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
    public string EmitForm(IEnumerable<(string type, string name)> props, bool IsTarget = false)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        if (IsTarget)
        {
            foreach(var typedef in type_impl)
            {
                sb.Append(TypeTemplate("public", typedef.Key, typedef.Value, "record", 1));
            }
        }
        foreach (var prop in props)
        {
            sb.Append($"\tpublic {prop.type} {prop.name} {{ get; set; }}\n");
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
    string GetObjectType(JsonElement.ObjectEnumerator obj, string name, bool properType = false)
    {
        string typename(string name, int i) => $"{name}{i}";
        List<(string type, string name)> properties = new();
        foreach (var node in obj)
        {
            var prop_name = node.Name;
            var prop_type = GetPropertyKind(node.Value, $"{prop_name.ToPascal()}_T");
            properties.Add((prop_type, prop_name));
        }

        var recordForm = EmitForm(properties, properType);
        if(properType)
        {
            emmited_types.Add(0, name);
            type_impl.Add(name, recordForm);
            return name;
        } else
        {
            var hashedForm = recordForm.GetHashCode();
            if(emmited_types.TryGetValue(hashedForm, out var tname))
            {
                return tname;
            } else
            {

                while (type_impl.ContainsKey(name))
                {
                    name = typename(name, ++i);
                }

                emmited_types[hashedForm] = name;
                type_impl[name] = recordForm;
                return name;
            }
        }
    }

    string GetPropertyKind(JsonElement values, string name)
    {
        return values.ValueKind switch
        {
            JsonValueKind.Array => GetEnumerableType(values.EnumerateArray(), name),
            JsonValueKind.Object => GetObjectType(values.EnumerateObject(), name),
            _ => GetValueType(values.ValueKind)
        };
    }

    string GetEnumerableType(JsonElement.ArrayEnumerator values, string name)
    {
        JsonElement? first = values.FirstOrDefault();

        if (first == null)
        {
            return $"Object[]";
        }

        try
        {
            return $"{GetEnumerableType(first.Value.EnumerateArray(), name)}[]";
        }
        catch
        {
            try
            {
                return $"{GetObjectType(first.Value.EnumerateObject(), name)}[]";
            }
            catch
            {
                return $"{GetValueType(first.Value.ValueKind)}[]";
            }
        }
    }

    string GetValueType(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.Number => typeof(Decimal).Name,
            JsonValueKind.String => typeof(String).Name,
            JsonValueKind.Null or JsonValueKind.Undefined => typeof(Object).Name,
            JsonValueKind.False or JsonValueKind.True => typeof(bool).Name,

        };
    }

    public void GenerateTypes(string name, string sample, bool isRecord = false)
    {
        try
        {
            JsonDocument tree = JsonDocument.Parse(sample, new JsonDocumentOptions() { AllowTrailingCommas = true });
            _ = GetObjectType(tree.RootElement.EnumerateObject(), name, properType: true);
        } catch
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
                        return (typename, typesample);
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
                engine.GenerateTypes(name, sample);
                context.AddSource($"{name}.Type.g.cs", SourceText.From(engine.EmitTypes(name), Encoding.UTF8));
            }

        }
        catch (Exception ex){
            context.ReportDiagnostic(Diagnostic.Create(ToolExtensions.Rule(ex), Location.None));
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