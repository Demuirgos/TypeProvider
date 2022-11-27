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

public class InvalidPropertyType : Exception { }
public class InvalidPropertyAccess : Exception { }
public class InvalidPropertyValue : Exception { }

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
        messageFormat: ex switch
        {
            InvalidPropertyType _ => $"Build failed with Error : {nameof(InvalidPropertyType)} (Tagged property must be a string)",
            InvalidPropertyValue _ => $"Build failed with Error : {nameof(InvalidPropertyValue)} (Tagged property must be a correct json)",
            InvalidPropertyAccess _ => $"Build failed with Error : {nameof(InvalidPropertyAccess)} (Tagged property must be readonly)",
        });

}
public class TypeInstantiator
{
    public static string EmitForm(IEnumerable<(string type, string name)> props)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        foreach (var prop in props)
        {
            sb.Append($"    public {prop.type} {prop.name} {{ get; set; }}\n");
        }
        sb.Append("}\n");
        return sb.ToString();
    }
    public static string FileTemplate(string fileBody)
        => $"using System;\nnamespace TypeExtensions.Generated;\n\n{fileBody}";

    public static string TypeTemplate(string scope, string typename, String body, string typekind)
        => $"{scope} {typekind} {typename} {body}\n";

    private int i = 0;
    private Dictionary<int, List<(string, string)>> emmited_types = new();
    string GetObjectType(JsonElement.ObjectEnumerator obj, string name, bool properType = false)
    {
        string typename(int i) => name ?? $"__GeneratedType__{i}";
        List<(string type, string name)> properties = new();
        foreach (var node in obj)
        {
            var prop_name = node.Name;
            var prop_type = GetPropertyKind(node.Value, $"{prop_name.ToPascal()}_T");
            properties.Add((prop_type, prop_name));
        }

        var recordForm = EmitForm(properties);
        if(properType)
        {
            if(emmited_types.ContainsKey(0))
            {
                emmited_types.TryGetValue(0, out var types);
                types.Add((name, recordForm));

            } else
            {
                emmited_types.Add(0, new List<(string, string)> { (name, recordForm) });
            }
            return name;
        } else
        {
            var hashedForm = recordForm.GetHashCode();
            if (emmited_types.TryGetValue(hashedForm, out var metadata))
            {
                return metadata.Last().Item1;
            }
            else
            {
                emmited_types.Add(hashedForm, new List<(string, string)>{ (name, recordForm) });
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

    public string EmitTypes()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var types in emmited_types)
        {
            foreach(var type in types.Value)
            {
                bool isRoot = type.Item1.Contains("__GeneratedType__");
                var type_code = TypeTemplate("public" /*isRoot ? "file" : "public"*/, type.Item1, type.Item2, "record");
                sb.Append(type_code);
            }
        }
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
        var engine = new TypeInstantiator();
        try
        {
            var allSamples = GetAllMarkedProperties(context.Compilation);
            foreach (var (name, sample) in allSamples)
            {
                engine.GenerateTypes(name, sample);
            }

            context.AddSource("EmitedTypes.g.cs", SourceText.From(engine.EmitTypes(), Encoding.UTF8));
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