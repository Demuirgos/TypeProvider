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

[Generator]
public class TypesGenerator : ISourceGenerator
{
    private (string, string, string)[] GetAllMarkedProperties(Compilation context)
    {
        IEnumerable<SyntaxNode> allNodes = context.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
        return allNodes
            .Where(d => d.IsKind(SyntaxKind.PropertyDeclaration))
            .OfType<PropertyDeclarationSyntax>()
            .Select(propDef =>
            {
                var semanticModel = context.GetSemanticModel(propDef.SyntaxTree);
                (string name, string emitType, bool fromFile, string sample) result = (String.Empty, String.Empty, false, String.Empty);
                foreach (var attr in propDef.AttributeLists.SelectMany(x => x.Attributes))
                {
                    var attrName = attr.Name.ToString();
                    if(attrName == nameof(EmitTypeAttribute).Replace("Attribute", String.Empty)) {
                        var type = semanticModel.GetTypeInfo(propDef.Type).Type;
                        
                        if (type.SpecialType != SpecialType.System_String)
                        {
                            throw new InvalidPropertyType();
                        }

                        if (propDef.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration))
                        {
                            throw new InvalidPropertyAccess();
                        }

                        result.name = $"{propDef.Identifier.Value}_T";
                        result.sample = semanticModel.GetConstantValue(propDef.Initializer.Value).ToString();
                    } 

                    if( attrName == nameof(XmlAttribute).Replace("Attribute", String.Empty) ||
                        attrName == nameof(JsonAttribute).Replace("Attribute", String.Empty) ||
                        attrName == nameof(CsvAttribute).Replace("Attribute", String.Empty)) {
                        result.emitType = attrName.Replace("Attribute", String.Empty);
                    }

                    if( attrName == nameof(FromUriAttribute).Replace("Attribute", String.Empty)) {
                        result.fromFile = true;
                    }
                }

                
                if (result.fromFile)
                {
                    bool isUrl = Uri.TryCreate(result.sample, UriKind.Absolute, out var uriResult) 
                        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                    if (isUrl)
                    {
                        using var client = new System.Net.WebClient();
                        result.sample = client.DownloadString(result.sample);
                    } else {
                        string path = result.sample;
                        if(result.sample[0] == '.') {
                            path = Path.Combine(Environment.CurrentDirectory, result.sample);
                        }

                        if (!File.Exists(path))
                        {
                            throw new FileNotFoundException();
                        } 
                        else {
                            result.sample = File.ReadAllText(path);
                        }
                    }
                } 

                if (result.emitType == String.Empty)
                {
                    throw new Exception("Missing Type Emission Target");
                }

                return (result.name, result.emitType, result.sample);
            })
            .Where(result => !String.IsNullOrWhiteSpace(result.name) 
                                                                        && !String.IsNullOrWhiteSpace(result.emitType) 
                                                                        && !String.IsNullOrWhiteSpace(result.sample))
            .ToArray();
    }
    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            (string, string, string)[]? allSamples = GetAllMarkedProperties(context.Compilation);
            foreach (var (name, type, sample) in allSamples)
            {
                
                ITypeEmitter engine = type switch
                {
                    "Xml" => new TypeProvider.Xml.TypeInstantiator(),
                    "Json" => new TypeProvider.Json.TypeInstantiator(),
                    "Csv" => new TypeProvider.Csv.TypeInstantiator(),
                    _ => throw new Exception("Invalid Type Emission Target")
                };

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