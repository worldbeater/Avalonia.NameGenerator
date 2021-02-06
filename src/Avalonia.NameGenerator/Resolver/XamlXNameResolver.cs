﻿using System.Collections.Generic;
using System.Linq;
using Avalonia.NameGenerator.Compiler;
using Microsoft.CodeAnalysis.CSharp;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;

namespace Avalonia.NameGenerator.Resolver
{
    internal class XamlXNameResolver : INameResolver, IXamlAstVisitor
    {
        private const string AvaloniaXmlnsAttribute = "Avalonia.Metadata.XmlnsDefinitionAttribute";
        private readonly List<ResolvedName> _items = new();
        private readonly MiniCompiler _compiler;

        public XamlXNameResolver(CSharpCompilation compilation) =>
            _compiler = MiniCompiler
                .CreateDefault(
                    new RoslynTypeSystem(compilation),
                    AvaloniaXmlnsAttribute);

        public IReadOnlyList<ResolvedName> ResolveNames(string xaml)
        {
            var parsed = XDocumentXamlParser.Parse(xaml, new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            });

            _compiler.Transform(parsed);
            parsed.Root.Visit(this);
            parsed.Root.VisitChildren(this);
            return _items;
        }

        IXamlAstNode IXamlAstVisitor.Visit(IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode objectNode)
                return node;

            var clrType = objectNode.Type.GetClrType();
            var isAvaloniaControl = clrType
                .Interfaces
                .Any(abstraction => abstraction.IsInterface &&
                                    abstraction.FullName == "Avalonia.Controls.IControl");

            if (!isAvaloniaControl)
                return node;

            foreach (var child in objectNode.Children)
            {
                if (child is XamlAstXamlPropertyValueNode propertyValueNode &&
                    propertyValueNode.Property is XamlAstNamePropertyReference namedProperty &&
                    namedProperty.Name == "Name" &&
                    propertyValueNode.Values.Count > 0 &&
                    propertyValueNode.Values[0] is XamlAstTextNode text)
                {
                    var typeName = $@"{clrType.Namespace}.{clrType.Name}";
                    var resolvedName = new ResolvedName(typeName, text.Text, "internal");
                    if (_items.Contains(resolvedName))
                        continue;
                    _items.Add(resolvedName);
                }
            }

            return node;
        }

        void IXamlAstVisitor.Push(IXamlAstNode node) { }

        void IXamlAstVisitor.Pop() { }
    }
}