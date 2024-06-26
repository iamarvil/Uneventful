﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uneventful.CodeGenerator;

[Generator]
public class EventStoreBuilderExtensionCodeGenerator : ISourceGenerator {
    public void Initialize(GeneratorInitializationContext context) {

    }

    public void Execute(GeneratorExecutionContext context) {
        var compilation = context.Compilation;
        var syntaxTrees = compilation.SyntaxTrees;

        var assemblyName = context.Compilation.AssemblyName;
        var name = assemblyName?.Replace(".", "");
        var eventTypes = new Dictionary<string, List<string>>();
        var namespaces = new HashSet<string>();

        foreach (var syntaxTree in syntaxTrees) {
            var root = syntaxTree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var records = root
                .DescendantNodes()
                .OfType<RecordDeclarationSyntax>()
                .Where(record => record.BaseList != null &&
                                 !record.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
                                 IsDerivedFromEventBase(record, semanticModel));

            foreach (var record in records) {
                var recordName = record.Identifier.Text;
                var parent = record.Identifier.Parent?.Parent;

                if (parent is ClassDeclarationSyntax syntax) {
                    var className = syntax.Identifier.Text;
                    if (!eventTypes.ContainsKey(className)) {
                        eventTypes.Add(className, [$"{className}.{recordName}"]);
                    } else {
                        eventTypes[className].Add($"{className}.{recordName}");
                    }
                    AddNamespace(namespaces, syntax);
                } else {
                    if (parent == null) continue;
                    
                    var nsName = GetNamespaceName(parent);
                    if (nsName == null) continue;
                    
                    namespaces.Add(nsName);
                    if (!eventTypes.ContainsKey(nsName)) {
                        eventTypes.Add(nsName, [recordName]);
                    } else {
                        eventTypes[nsName].Add(recordName);
                    }


                }
            }
        }

        if (eventTypes.Count == 0) return;

        var namespaceData = string.Join("\r\n", namespaces.Select(ns => $"using {ns};"));

        if (assemblyName != null && name != null) {
            context.AddSource("EventStoreBuilderExtensions.g.cs",
                BuildEventStoreBuilderExtensions(assemblyName, eventTypes, namespaceData, name));
            
            context.AddSource($"EventTypeRegistry.g.cs", BuildEventwrapperConverterEventTypeRegistration(assemblyName, eventTypes, namespaceData));
        }
    }
    
    private static bool IsDerivedFromEventBase(RecordDeclarationSyntax record, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetDeclaredSymbol(record);
        if (symbol == null)
        {
            return false;
        }

        var baseType = symbol.BaseType;
        while (baseType != null)
        {
            if (baseType.ToDisplayString() == "Uneventful.EventStore.EventBase")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    private static string BuildEventwrapperConverterEventTypeRegistration(string assemblyName, Dictionary<string, List<string>> eventTypes, string namespaceData) {
        var source = $$"""
                          // <auto-generated />
                          using System;
                          using Uneventful.EventStore;
                          using Uneventful.EventStore.Serialization;
                          {{namespaceData}}
                          
                          namespace {{assemblyName}};
                          
                          public static class EventTypeRegistry {
                              public static readonly Type[] Events = [
                                {{string.Join($", {Environment.NewLine}", eventTypes.Values.SelectMany(eventType => eventType).Select(eventType => $"\t\ttypeof({eventType})"))}}
                              ];
                              
                              
                          }
                       """;

        return source;

    }

    private static string BuildEventStoreBuilderExtensions(string assemblyName, Dictionary<string, List<string>> eventTypes, string namespaceData,
        string name) {
        var source = $$"""
                       // <auto-generated />
                       using System;
                       using Uneventful.EventStore;
                       {{namespaceData}}

                       namespace {{assemblyName}};

                       public static class EventStoreBuilderExtensions {
                           public static EventStoreBuilder Register{{name}}Events(this EventStoreBuilder builder, string domain) {
                       {{string.Join(Environment.NewLine, eventTypes.Values.SelectMany(x => x).Select(eventType => $"\t\tbuilder.RegisterEvent<{eventType}>(domain);"))}}
                       
                               return builder;
                           }
                       } 
                       """;
        
        return source;
    }

    private static void AddNamespace(HashSet<string> namespaces, SyntaxNode parent) {
        switch (parent) {
            case FileScopedNamespaceDeclarationSyntax fsns:
                namespaces.Add(fsns.Name.ToString());
                break;
            case NamespaceDeclarationSyntax ns:
                namespaces.Add(ns.Name.ToString());
                break;
            default: {
                if (parent?.Ancestors().FirstOrDefault(x => x is FileScopedNamespaceDeclarationSyntax or NamespaceDeclarationSyntax) is { } ancestor) {
                    if (ancestor is FileScopedNamespaceDeclarationSyntax fileScopedNs) {
                        namespaces.Add(fileScopedNs.Name.ToString());
                    } else if (ancestor is NamespaceDeclarationSyntax nestedNs) {
                        namespaces.Add(nestedNs.Name.ToString());
                    }
                }

                break;
            }
        }
    }

    private static string? GetNamespaceName(SyntaxNode parent) {
        while (true) {
            switch (parent) {
                case FileScopedNamespaceDeclarationSyntax fsns:
                    return fsns.Name.ToString();

                case NamespaceDeclarationSyntax ns:
                    return ns.Name.ToString();

                default: {
                    if (parent?.Ancestors().FirstOrDefault(x => x is FileScopedNamespaceDeclarationSyntax or NamespaceDeclarationSyntax) is { } ancestor) {
                        parent = ancestor;
                        continue;
                    }

                    break;
                }
            }

            return null;
        }
    }
}