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

using TypeProvider.Shared;

namespace TypeProvider.Xml;
public class TypeInstantiator : ITypeEmitter
{
    public string InjectParser(string form, string Parsers) {
        var lastBraket = form.LastIndexOf("}");
        return $"{form.Remove(lastBraket)}{Parsers}\n}}";
    }
    public string EmitParser(string name, IEnumerable<(string ptype, string pname, bool isAttribute)> props, bool isTarget = false) {
        bool isList(string type) => type.EndsWith("[]");
        bool isObject(string type) => type.EndsWith("_T");
        bool isPrimitiveType(string type) => !isList(type) && !isObject(type);
        StringBuilder sb = new();
        string prefix = isTarget ? String.Empty : "\t";
        sb.Append($@"
{prefix}    public static bool TryParse(string xmlLine, out {name} result, string? sourceProperty = null) {{
{prefix}        try {{
{prefix}            XmlDocument doc = new XmlDocument();
{prefix}            doc.LoadXml(xmlLine);");
        sb.Append($@"
{prefix}            result = new {name}();");
        int i = 0;
        name = name.Replace("_T", "");
        foreach (var (ptype, pname, isAttr) in props)
        {   
            if (isList(ptype))
            {
                i = EmitListParser(name, sb, prefix, i, ptype, pname);
            }
            else if (isObject(ptype))
            {
                i = EmitObjectParser(name, sb, prefix, i, ptype, pname);
            }
            else
            {
                if(!isAttr) {
                    if(ptype == nameof(String)) {
                        sb.Append($@"
{prefix}            result.{pname} = doc.FirstChild.SelectSingleNode($""{pname}"").InnerText;
                        ");
                    } else 
                    {
                        sb.Append($@"
{prefix}            if({ptype}.TryParse(doc.FirstChild.SelectSingleNode($""{pname}"").InnerText, out {ptype} item_{pname}{i})) {{
{prefix}                result.{pname} = item_{pname}{i++};
{prefix}            }} else {{
{prefix}                throw new Exception(""Invalid Property Type"");
{prefix}            }}

                        ");
                    }
                }
                else {
                    if(ptype == nameof(String)) {
                        sb.Append($@"
{prefix}            result.{pname} = doc.FirstChild.Attributes[""{pname}""].InnerText;
                        ");
                    } else 
                    {
                        sb.Append($@"
{prefix}            if({ptype}.TryParse(doc.FirstChild.Attributes[""{pname}""].InnerText, out {ptype} item_{pname}{i})) {{
{prefix}                result.{pname} = item_{pname}{i++};
{prefix}            }} else {{
{prefix}                throw new Exception(""Invalid Property Type"");
{prefix}            }}

                        ");
                    }
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

        int EmitListParser(string name, StringBuilder sb, string prefix, int j, string ptype, string pname)
        {
            int i = 0;
            string typeName = ptype.Substring(0, ptype.Length - 2);
            sb.Append($@"
{prefix}            var temp_{pname}{i + j} = new List<{typeName}>();");
            if(i == 0) {
                    sb.Append($@"
{prefix}            foreach (XmlElement node_{i + j} in doc.FirstChild.SelectSingleNode($""{pname}"").ChildNodes)
{prefix}            {{");
            } else {
                sb.Append($@"
{prefix}            foreach (XmlElement node_{i + j} in node_{i + j - 1}.ChildNodes)
{prefix}            {{");
            }

            if (!isList(typeName))
            {
                if(typeName == nameof(String)) {
                    sb.Append($@"
{prefix}                temp_{pname}{i + j}.Add(node_{i + j}.InnerText);
                    }}");
                } else {
                    sb.Append($@"
{prefix}                if({typeName}.TryParse(node_{i + j}.{(isPrimitiveType(typeName) ? "InnerXml" : "OuterXml")}, out {typeName} item_{pname}{i + j})) {{");
                    sb.Append($@"
{prefix}                    temp_{pname}{i + j}.Add(item_{pname}{j + i});
{prefix}                }} else {{
{prefix}                    throw new Exception(""Invalid Property Type"");
{prefix}                }}
{prefix}            }}");
                }
            }
            else
            {
                typeName = ptype.Substring(0, ptype.Length - 2);
                j = EmitListParser(name, sb, $"{prefix}\t", i+1, typeName, pname);
                sb.Append($@"
{prefix}            }}");
            }
            
            if(i > 0)
                sb.Append($@"
{prefix}            temp_{pname}{i + j - 2}.Add(temp_{pname}{i + j - 1}.ToArray());
                ");
            else
                sb.Append($@"
{prefix}            result.{pname} = temp_{pname}{i + j}.ToArray();
                ");

            return i + j;
        }

        int EmitObjectParser(string name, StringBuilder sb, string prefix, int j, string ptype, string pname)
        {
            int i = 0;
            if (isPrimitiveType(ptype))
            {
                if(ptype == nameof(String)) {
                    sb.Append($@"
{prefix}                result.{pname} = doc.FirstChild.SelectSingleNode($""{pname}"").InnerXml;
                    ");
                } else 
                {
                    sb.Append($@"
{prefix}                if({ptype}.TryParse(doc.FirstChild.SelectSingleNode($""{pname}"").InnerXml, out {ptype} item_{pname}{i + j})) {{");
                }
            }
            else
            {
                sb.Append($@"
{prefix}                if({ptype}.TryParse(doc.FirstChild.SelectSingleNode($""{pname}"").OuterXml, out {ptype} item_{pname}{i + j}, ""{pname}"")) {{");
            }
            sb.Append($@"
{prefix}                result.{pname} = item_{pname}{j + i++};
{prefix}            }} else {{
{prefix}                throw new Exception(""Invalid Property Type"");
{prefix}            }}
                ");
            return i + j;
        }
    }
    public string EmitForm(string name, IEnumerable<(string type, string name, bool isAttribute)> props, bool IsTarget = false)
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
    string AddTypeToEmitionTargets(ref string name, bool properType, List<(string type, string name, bool isAttribute)> properties)
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
        List<(string type, string name, bool isAttribute)> properties = new();
        foreach (XmlNode node in obj.ChildNodes)
        {
            var prop_name = node.Name;
            var prop_type = GetPropertyKind(node, $"{prop_name}_T");
            properties.Add((prop_type, prop_name, false));
        }
        if (obj.Attributes is not null)
        {
            foreach (System.Xml.XmlAttribute node in obj.Attributes)
            {
                var prop_name = node.Name;
                var prop_type = GetValueType(node.InnerText);
                properties.Add((prop_type, prop_name, true));
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
                    List<(string type, string name, bool isAttribute)> properties = new();
                    properties.Add((mainType, first.Name, false));
                    if (value.Attributes is not null)
                    {
                        foreach (System.Xml.XmlAttribute node in value.Attributes)
                        {
                            var prop_name = node.Name;
                            var prop_type = GetValueType(node.InnerText);
                            properties.Add((prop_type, prop_name, true));
                        }
                    }
                    AddTypeToEmitionTargets(ref name, false, properties);
                    return name;
                }        
            }
            if(targetType){
                return GetObjectType(value, name, targetType);
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
            return nameof(DateTime);
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
