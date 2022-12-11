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

namespace TypeProvider.Shared;
public interface ITypeEmitter {
    void GenerateTypes(string name, string sample, bool isRecord = false);
    public string EmitTypes(string name);
}  
public class InvalidPropertyType : Exception {
    public InvalidPropertyType(Exception ex = null) : base($"Build failed with Error : {nameof(InvalidPropertyType)} (Tagged property must be a string)", ex) { }
}
public class InvalidPropertyAccess : Exception {
    public InvalidPropertyAccess(Exception ex = null) : base($"Build failed with Error : {nameof(InvalidPropertyAccess)} (Tagged property must be readonly)", ex) { }
}
public class InvalidPropertyValue : Exception {
    public InvalidPropertyValue(Exception ex = null) : base($"Build failed with Error : {nameof(InvalidPropertyValue)} (Tagged property must be a correct)", ex) { }
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
        messageFormat: $"{ex.Message}\n{ex.InnerException?.StackTrace ?? ex.StackTrace}");

}