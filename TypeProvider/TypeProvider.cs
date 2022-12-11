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

public class InvalidPropertyType : Exception {
    public InvalidPropertyType(Exception ex = null) : base($"Build failed with Error : {nameof(InvalidPropertyType)} (Tagged property must be a string)", ex) { }
}
public class InvalidPropertyAccess : Exception {
    public InvalidPropertyAccess(Exception ex = null) : base($"Build failed with Error : {nameof(InvalidPropertyAccess)} (Tagged property must be readonly)", ex) { }
}
public class InvalidPropertyValue : Exception {
    public InvalidPropertyValue(Exception ex = null) : base($"Build failed with Error : {nameof(InvalidPropertyValue)} (Tagged property must be a correct json)", ex) { }
}

public static class ToolExtensions {
    public static string ToPascal(this string Identifier) 
        =>  CultureInfo.CurrentCulture
                        .TextInfo
                        .ToTitleCase(Identifier.ToLower().Replace("_", " ")).Replace(" ", string.Empty);

    public static DiagnosticDescriptor Rule(Exception ex) => new DiagnosticDescriptor(
        id: "XTP01",
        title: "Argument format error",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Property Type Emission must be : Readonly, a string and private static",
        messageFormat: $"{ex.Message}\n{ex.InnerException?.StackTrace ?? ex?.StackTrace}");

}
public class TypeInstantiator
{
    public string InjectParser(string form, string Parsers) {
        var lastBraket = form.LastIndexOf("}");
        return $"{form.Remove(lastBraket)}{Parsers}\n}}";
    }
    public string EmitParser(string name, IEnumerable<(string ptype, string pname)> props, bool isTarget = false) {
        bool isList(string type) => type.EndsWith("[]");
        bool isObject(string type) => type.EndsWith("_T");
        StringBuilder sb = new();
        string prefix = isTarget ? String.Empty : "\t";
        sb.Append($@"
{prefix}    public static bool TryParse(string xmlLine, out {name} result) {{
{prefix}        try {{
{prefix}            XmlDocument doc = new XmlDocument();
{prefix}            doc.LoadXml(xmlLine);");
        sb.Append($@"
{prefix}            result = new {name}();");
        int i = 0;
        foreach (var (ptype, pname) in props)
        {   
            if (isList(ptype))
            {
                string typeName = ptype.Replace("[]", "");
                sb.Append($@"
{prefix}            var temp_{pname} = new List<{typeName}>();
{prefix}            foreach (XmlNode node in doc.SelectNodes(""{pname}""))
{prefix}            {{
{prefix}                if({typeName}.TryParse(node.InnerText, out {typeName} item_{i})) {{
{prefix}                    temp_{pname}.Add(item_{i++});
{prefix}                }} else {{
{prefix}                    throw new Exception(""Invalid Property Type"");
{prefix}                }}
{prefix}            }}
{prefix}            result.{pname} = temp_{pname}.ToArray();
                ");
            }
            else if (isObject(ptype))
            {
                sb.Append($@"
{prefix}            if({ptype}.TryParse(doc.SelectSingleNode(""{pname}"").InnerText, out {ptype} item_{i})) {{
{prefix}                result.{pname} = item_{i++};
{prefix}            }} else {{
{prefix}                throw new Exception(""Invalid Property Type"");
{prefix}            }}
                ");
            }
            else
            {
                if(ptype == nameof(String)) {
                    sb.Append($@"
{prefix}            result.{pname} = doc.SelectSingleNode(""{pname}"").InnerText;
                    ");
                } else 
                {
                    sb.Append($@"
{prefix}            if({ptype}.TryParse(doc.SelectSingleNode(""{pname}"").InnerText, out {ptype} item_{i})) {{
{prefix}                result.{pname} = item_{i++};
{prefix}            }} else {{
{prefix}                throw new Exception(""Invalid Property Type"");
{prefix}            }}
                    ");
                }
            }
        }
        sb.Append($@"
{prefix}            return true;
{prefix}        }} catch (Exception ex) {{
{prefix}            result = default;
{prefix}            return false;
{prefix}        }}
{prefix}    }}"
        );

        return sb.ToString();
    }
    public string EmitForm(string name, IEnumerable<(string type, string name)> props, bool IsTarget = false)
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

        sb.Append($"\tpublic override string ToString() => JsonSerializer.Serialize(this);\n");
        sb.Append("}\n");
        return sb.ToString();
    }
    public static string FileTemplate(string fileBody)
        => $"using System;\nusing System.Text.Json;\nusing System.Xml;\nusing System.Text;\nnamespace TypeExtensions.Generated;\n\n{fileBody}";

    public string TypeTemplate(string scope, string typename, String body, string typekind, int nesting = 0)
    {
        string prefix = new string('\t', nesting);
        body = body.Replace("\n", $"\n{prefix}");
        return $"{prefix}{scope} {typekind} {typename} {InjectParser(body, type_parser[typename])}\n";
    }

    private int i = 0;
    private Dictionary<int, string> emmited_types = new(); // hashform => typename
    private Dictionary<string, string> type_impl = new(); // typename => codeform
    private Dictionary<string, string> type_parser = new(); // typename => codeform
    string AddTypeToEmitionTargets(ref string name, bool properType, List<(string type, string name)> properties)
    {
        string typename(string name, int i) => $"{name}{i}";
        var recordForm = EmitForm(name, properties, properType);
        if (properType)
        {
            emmited_types.Add(0, name);
            type_impl.Add(name, recordForm);
            type_parser.Add(name, EmitParser(name, properties, properType));
            return name;
        }
        else
        {
            var hashedForm = recordForm.GetHashCode();
            if (emmited_types.TryGetValue(hashedForm, out var tname))
            {
                return tname;
            }
            else
            {

                while (type_impl.ContainsKey(name))
                {
                    name = typename(name, ++i);
                }

                emmited_types[hashedForm] = name;
                type_impl[name] = recordForm;
                type_parser[name] = EmitParser(name, properties, properType);
                return name;
            }
        }
    }
    string GetObjectType(XmlNode obj, string name, bool properType = false)
    {
        List<(string type, string name)> properties = new();
        foreach (XmlNode node in obj.ChildNodes)
        {
            var prop_name = node.Name;
            var prop_type = GetPropertyKind(node, $"{prop_name}_T");
            properties.Add((prop_type, prop_name));
        }
        if (obj.Attributes is not null)
        {
            foreach (XmlAttribute node in obj.Attributes)
            {
                var prop_name = node.Name;
                var prop_type = GetValueType(node.InnerText);
                properties.Add((prop_type, prop_name));
            }
        }

        return AddTypeToEmitionTargets(ref name, properType, properties);
    }

    string GetPropertyKind(XmlNode value, string name, bool targetType = false)
    {
        bool isValue(XmlNode node) => node.ChildNodes.Count == 0 && node.Attributes.Count == 0;
        if (isValue(value)) {
            return "Object?";
        }else {
            if (value.ChildNodes.Count == 1)
            {
                XmlNode first = value.ChildNodes.Item(0);
                if(first.NodeType == XmlNodeType.Text)
                {
                    return GetValueType(value.InnerText);
                } 
            }

            if (value.ChildNodes.Count > 1)
            {
                bool isList = true;
                XmlElement first = (XmlElement)value.ChildNodes.Item(0);
                foreach(XmlElement child in value.ChildNodes) {
                    isList &=
                            child.ChildNodes.Count == first.ChildNodes.Count &&
                            child.Attributes.Count == first.Attributes.Count && 
                            child.Name == first.Name;
                }

                if(isList) {
                    var mainType = GetEnumerableType(value, $"{first.Name}_T");
                    if (value.Attributes.Count == 0)
                        return mainType;
                    List<(string type, string name)> properties = new();
                    properties.Add((mainType, first.Name));
                    if (value.Attributes is not null)
                    {
                        foreach (XmlAttribute node in value.Attributes)
                        {
                            var prop_name = node.Name;
                            var prop_type = GetValueType(node.InnerText);
                            properties.Add((prop_type, prop_name));
                        }
                    }
                    AddTypeToEmitionTargets(ref name, false, properties);
                    return name;
                }        
            }
            return GetObjectType(value, $"{value.Name}_T", targetType);
        }
    }

    string GetEnumerableType(XmlNode values, string name)
    {
        XmlElement first = (XmlElement)values.ChildNodes.Item(1);
        return $"{GetPropertyKind(first, name)}[]";
    }

    string GetValueType(string kind)
    {
        if(Decimal.TryParse(kind, out _)) {
            return nameof(Decimal);
        } else if(Boolean.TryParse(kind, out _)) {
            return nameof(Boolean);
        } else if(DateTime.TryParse(kind, out _)) {
            return nameof(Boolean);
        } else if(String.IsNullOrEmpty(kind)) {
            return nameof(Object);
        } else return nameof(String);
    }

    public void GenerateTypes(string name, string sample, bool isRecord = false)
    {
        try
        {
            XmlDocument tree = new XmlDocument();
            tree.LoadXml(sample);
            _ = GetPropertyKind(tree.FirstChild, name, true);
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