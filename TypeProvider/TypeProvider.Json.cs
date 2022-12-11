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
using System.Text.Json.Nodes;
using TypeProvider.Shared;

namespace TypeProvider.Json;

public class TypeInstantiator : ITypeEmitter
{
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

        if(IsTarget) {
            sb.Append($@"
    public static bool TryParse(string jsonText, out {name} result) {{
        try {{
            result = JsonSerializer.Deserialize<{name}>(jsonText);
            return true;
        }} catch {{
            result = default;
            return false;
        }}
    }}
");

            sb.Append($"\tpublic override string ToString() => JsonSerializer.Serialize<{name}>(this);\n");
        }

        sb.Append("}\n");
        return sb.ToString();
    }
    public static string FileTemplate(string fileBody)
        => $"using System;\nusing System.Text.Json;\nnamespace TypeExtensions.Generated;\n\n{fileBody}";

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

        var recordForm = EmitForm(name, properties, properType);
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
            _ => GetValueType(values.ValueKind, values.ToString())
        };
    }

    string GetEnumerableType(JsonElement.ArrayEnumerator values, string name)
    {
        JsonElement? first = values.FirstOrDefault();

        if (first == null)
        {
            return $"Object[]";
        }

        return $"{GetPropertyKind(first.Value, name)}[]";
    }

    string GetValueType(JsonValueKind kind, string value)
    {
        return kind switch
        {
            JsonValueKind.Number => typeof(Decimal).Name,
            JsonValueKind.String => DateTime.TryParse(value, out _) ? typeof(DateTime).Name : typeof(String).Name,
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