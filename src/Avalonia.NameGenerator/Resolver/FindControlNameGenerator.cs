using System.Collections.Generic;
using System.Linq;
using Avalonia.NameGenerator.Domain;

namespace Avalonia.NameGenerator.Resolver
{
    internal class FindControlNameGenerator : ICodeGenerator
    {
        public string GenerateCode(string className, string nameSpace, IEnumerable<ResolvedName> names)
        {
            var namedControls = names
                .Select(info => "        " +
                                $"{info.FieldModifier} global::{info.TypeName} {info.Name} => " +
                                $"this.FindControl<global::{info.TypeName}>(\"{info.Name}\");")
                .ToList();
            var lines = string.Join("\n", namedControls);
            return $@"// <auto-generated />

using Avalonia.Controls;

namespace {nameSpace}
{{
    partial class {className}
    {{
{lines}
    }}
}}
";
        }
    }
}