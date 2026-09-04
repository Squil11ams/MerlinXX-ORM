using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MerlinORM.Generators;

/// <summary>Generates direct-assignment mappers for supported Merlin model classes.</summary>
[Generator(LanguageNames.CSharp)]
public sealed class MerlinMappingGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor FallbackDiagnostic = new(
        "MERLINSG001", "Runtime mapping fallback", "Model '{0}' uses runtime mapping: {1}",
        "MerlinORM.Generation", DiagnosticSeverity.Warning, isEnabledByDefault: true);
    private static readonly DiagnosticDescriptor RequiredDiagnostic = new(
        "MERLINSG002", "Generated mapping required", "Model '{0}' cannot be generated: {1}",
        "MerlinORM.Generation", DiagnosticSeverity.Error, isEnabledByDefault: true);
    private const string ModelBaseName = "MerlinORM.Client.MerlinModelBase";
    private const string ExcludeName = "MerlinORM.Client.Exclude";
    private const string AutoPopName = "MerlinORM.Client.AutoPopSettings";
    private const string RequiredName = "MerlinORM.Client.MerlinRequired";
    private const string NestedName = "MerlinORM.Client.MerlinObject";
    private const string ModelAttributeName = "MerlinORM.Client.MerlinModelAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (syntaxContext, _) => GetModel(syntaxContext))
            .Where(static model => model is not null)
            .Collect();

        var requireGenerated = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            options.GlobalOptions.TryGetValue("build_property.MerlinRequireGeneratedMappings", out var value) &&
            bool.TryParse(value, out var enabled) && enabled);

        context.RegisterSourceOutput(models.Combine(requireGenerated), static (productionContext, input) =>
        {
            var candidates = input.Left;
            var required = input.Right;
            var emitted = new HashSet<string>(StringComparer.Ordinal);

            foreach (var candidate in candidates)
            {
                if (candidate is null || !emitted.Add(candidate.TypeName))
                {
                    continue;
                }

                if (candidate.FallbackReason != null)
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(
                        required ? RequiredDiagnostic : FallbackDiagnostic,
                        candidate.Location, candidate.TypeName.Replace("global::", string.Empty), candidate.FallbackReason));
                    continue;
                }

                productionContext.AddSource(
                    candidate.HintName,
                    SourceText.From(Render(candidate), Encoding.UTF8));
            }
        });
    }

    private static ModelInfo? GetModel(GeneratorSyntaxContext context)
    {
        var declaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type ||
            (!DerivesFrom(type, ModelBaseName) && !HasAttribute(type, ModelAttributeName)))
        {
            return null;
        }

        if (type.IsAbstract)
        {
            return ModelInfo.Fallback(type, declaration.Identifier.GetLocation(), "the model is abstract");
        }
        if (type.IsGenericType)
        {
            return ModelInfo.Fallback(type, declaration.Identifier.GetLocation(), "generic model types are not supported");
        }
        if (!IsAccessible(type))
        {
            return ModelInfo.Fallback(type, declaration.Identifier.GetLocation(), "the model or a containing type is not accessible");
        }

        var properties = new List<PropertyInfo>();
        var canGenerateShim = declaration.Modifiers.Any(SyntaxKind.PartialKeyword) && type.ContainingType is null;
        var propertyNames = new HashSet<string>(StringComparer.Ordinal);
        var current = type;

        while (current != null && current.ToDisplayString() != ModelBaseName)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.IsIndexer ||
                    property.DeclaredAccessibility != Accessibility.Public ||
                    HasAttribute(property, ExcludeName))
                {
                    continue;
                }

                if (!propertyNames.Add(property.Name))
                {
                    continue;
                }

                if (property.SetMethod is null)
                {
                    continue;
                }

                var nestedAttribute = property.GetAttributes()
                    .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == NestedName);
                var isNested = nestedAttribute != null;

                properties.Add(new PropertyInfo(
                    property.Name,
                    property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    isNested ? string.Empty : GetColumnName(property),
                    HasAttribute(property, RequiredName),
                    isNested,
                    GetNestedPrefix(nestedAttribute),
                    GetNestedCreation(nestedAttribute),
                    property.SetMethod.DeclaredAccessibility == Accessibility.Public && !property.SetMethod.IsInitOnly,
                    property.SetMethod.IsInitOnly));
            }

            current = current.BaseType;
        }

        if (properties.Count == 0)
        {
            return ModelInfo.Fallback(type, declaration.Identifier.GetLocation(), "no mappable properties were found");
        }

        var constructors = type.InstanceConstructors
            .Where(ctor => ctor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .OrderByDescending(ctor => ctor.Parameters.Length);
        IMethodSymbol? selectedConstructor = null;
        foreach (var constructor in constructors)
        {
            var matches = constructor.Parameters.All(parameter => properties.Any(property =>
                !property.IsNested && string.Equals(property.PropertyName, parameter.Name, StringComparison.OrdinalIgnoreCase) &&
                property.TypeName == parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            if (!matches)
            {
                continue;
            }

            var boundNames = new HashSet<string>(constructor.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            if (properties.All(property => property.CanAssign || boundNames.Contains(property.PropertyName) ||
                canGenerateShim && !property.IsInitOnly))
            {
                selectedConstructor = constructor;
                foreach (var property in properties)
                {
                    property.ConstructorBound = boundNames.Contains(property.PropertyName);
                    property.UseShim = !property.CanAssign && !property.ConstructorBound;
                }
                break;
            }
        }

        if (selectedConstructor is null)
        {
            var blocked = properties.First(property => !property.CanAssign);
            return ModelInfo.Fallback(type, declaration.Identifier.GetLocation(),
                $"property '{blocked.PropertyName}' is not publicly assignable and no accessible constructor binds it");
        }

        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var identifier = Sanitize(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        return new ModelInfo(
            typeName,
            identifier + ".MerlinMapper.g.cs",
            identifier,
            properties.ToImmutableArray(),
            selectedConstructor.Parameters.Select(parameter =>
                properties.FindIndex(property => string.Equals(property.PropertyName, parameter.Name, StringComparison.OrdinalIgnoreCase))).ToImmutableArray(),
            type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
            type.Name,
            type.IsRecord,
            OverridesHook(type, "OnBeforeAutoPopulate"),
            OverridesHook(type, "OnAfterAutoPopulate"));
    }

    private static bool DerivesFrom(INamedTypeSymbol type, string baseTypeName)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.ToDisplayString() == baseTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAccessible(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public &&
                current.DeclaredAccessibility != Accessibility.Internal)
            {
                return false;
            }
        }

        return true;
    }

    private static bool OverridesHook(INamedTypeSymbol type, string methodName)
    {
        for (var current = type; current != null && current.ToDisplayString() != ModelBaseName; current = current.BaseType)
        {
            if (current.GetMembers(methodName).OfType<IMethodSymbol>().Any(method => method.IsOverride))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeName);

    private static string GetColumnName(IPropertySymbol property)
    {
        var attribute = property.GetAttributes()
            .FirstOrDefault(candidate => candidate.AttributeClass?.ToDisplayString() == AutoPopName);

        return attribute?.ConstructorArguments.Length > 0 &&
               attribute.ConstructorArguments[0].Value is string columnName
            ? columnName
            : property.Name;
    }

    private static string GetNestedPrefix(AttributeData? attribute)
    {
        return attribute?.ConstructorArguments.Length > 0 &&
               attribute.ConstructorArguments[0].Value is string prefix
            ? prefix
            : string.Empty;
    }

    private static int GetNestedCreation(AttributeData? attribute)
    {
        return attribute?.ConstructorArguments.Length > 1 &&
               attribute.ConstructorArguments[1].Value is int creation
            ? creation
            : 0;
    }

    private static string Render(ModelInfo model)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        source.AppendLine("namespace MerlinORM.Generated");
        source.AppendLine("{");
        source.Append("    internal sealed class ").Append(model.Identifier)
            .AppendLine("_Mapper : global::MerlinORM.Client.IMerlinGeneratedMapper");
        source.AppendLine("    {");
        source.Append("        private static readonly string?[] Columns = new string?[] { ");
        source.Append(string.Join(", ", model.Properties.Select(property =>
            property.IsNested ? "null" : Literal(property.ColumnName))));
        source.AppendLine(" };");
        source.Append("        private static readonly bool[] Required = new bool[] { ");
        source.Append(string.Join(", ", model.Properties.Select(property => property.Required ? "true" : "false")));
        source.AppendLine(" };");
        source.AppendLine();

        var nestedProperties = model.Properties.Where(property => property.IsNested).ToArray();
        source.Append("        public bool CanMap => ");
        source.Append(nestedProperties.Length == 0
            ? "true"
            : string.Join(" && ", nestedProperties.Select(property =>
                "global::MerlinORM.Client.MerlinGeneratedMapping<" + property.TypeName + ">.Mapper?.CanMap == true")));
        source.AppendLine(";");
        source.AppendLine();
        source.Append("        public global::System.Type ModelType => typeof(").Append(model.TypeName).AppendLine(");");
        source.AppendLine();
        source.AppendLine("        public global::MerlinORM.Client.MerlinGeneratedMappingPlan CreatePlan(");
        source.AppendLine("            global::System.Data.IDataRecord schema,");
        source.AppendLine("            global::MerlinORM.Client.MappingStrictness strictness,");
        source.AppendLine("            string prefix = \"\")");
        source.AppendLine("        {");
        source.AppendLine("            var plan = global::MerlinORM.Client.MerlinGeneratedMappingPlan.Create(");
        source.AppendLine("                schema, Columns, Required, strictness, prefix,");
        source.Append("                ").Append(Literal(model.TypeName.Replace("global::", string.Empty))).AppendLine(",");
        source.Append("                ").Append(model.HasBeforeHook ? "true" : "false").Append(", ")
            .Append(model.HasAfterHook ? "true" : "false").AppendLine(");");

        for (var index = 0; index < model.Properties.Length; index++)
        {
            var property = model.Properties[index];
            if (!property.IsNested)
            {
                continue;
            }

            source.Append("            var nestedMapper").Append(index)
                .Append(" = global::MerlinORM.Client.MerlinGeneratedMapping<")
                .Append(property.TypeName).Append(">.Mapper ?? throw new global::System.InvalidOperationException(")
                .Append(Literal("Generated nested mapper was not registered for " + property.TypeName)).AppendLine(");");
            source.Append("            var nestedPlan").Append(index).Append(" = nestedMapper")
                .Append(index).Append(".CreatePlan(schema, strictness, ")
                .Append(Literal(property.NestedPrefix)).AppendLine(");");
            source.Append("            plan.AddNestedPlan(").Append(index).Append(", nestedMapper")
                .Append(index).Append(", nestedPlan").Append(index)
                .Append(", (global::MerlinORM.Client.NestedObjectCreation)")
                .Append(property.NestedCreation).Append(", ")
                .Append(property.Required ? "true" : "false").AppendLine(");");
        }

        source.AppendLine("            return plan;");
        source.AppendLine("        }");
        source.AppendLine();
        source.Append("        public object Create(global::System.Data.IDataRecord data, global::MerlinORM.Client.MerlinGeneratedMappingPlan plan) => new ")
            .Append(model.TypeName).Append('(');
        source.Append(string.Join(", ", model.ConstructorPropertyIndexes.Select(index =>
            "plan.TryGetPropertyOrdinal(" + index + ", out var constructorOrdinal" + index + ") ? global::MerlinORM.Client.MerlinConvert.To<" +
            model.Properties[index].TypeName + ">(data.GetValue(constructorOrdinal" + index + "))! : default!")));
        source.AppendLine(");");
        source.AppendLine();
        source.AppendLine("        public void Populate(");
        source.AppendLine("            object target,");
        source.AppendLine("            global::System.Data.IDataRecord data,");
        source.AppendLine("            global::MerlinORM.Client.MerlinGeneratedMappingPlan plan)");
        source.AppendLine("        {");
        source.Append("            var model = (").Append(model.TypeName).AppendLine(")target;");

        for (var index = 0; index < model.Properties.Length; index++)
        {
            var property = model.Properties[index];

            if (property.ConstructorBound)
            {
                continue;
            }

            if (property.IsNested)
            {
                source.Append("            if (plan.TryGetNestedPlan(").Append(index)
                    .Append(", out var nested").Append(index).AppendLine("))");
                source.AppendLine("            {");
                source.Append("                if (nested").Append(index)
                    .Append(".Creation == global::MerlinORM.Client.NestedObjectCreation.Always || nested")
                    .Append(index).AppendLine(".Plan.HasAnyValue(data))");
                source.AppendLine("                {");
                source.Append(property.UseShim ? "                    model.__MerlinSet_" + property.PropertyName + "((" : "                    model.@" + property.PropertyName + " = (")
                    .Append(property.TypeName).Append(")global::MerlinORM.Client.MerlinGeneratedRuntime.CreateAndPopulate(nested")
                    .Append(index).Append(".Mapper, (global::System.Data.IDataReader)data, nested")
                    .Append(index).Append(property.UseShim ? ".Plan));" : ".Plan);").AppendLine();
                source.AppendLine("                }");
                source.AppendLine("                else");
                source.AppendLine("                {");
                source.Append(property.UseShim
                    ? "                    model.__MerlinSet_" + property.PropertyName + "(null!);"
                    : "                    model.@" + property.PropertyName + " = null!;").AppendLine();
                source.AppendLine("                }");
                source.AppendLine("            }");
                continue;
            }

            source.Append("            if (plan.TryGetPropertyOrdinal(").Append(index).Append(", out var ordinal")
                .Append(index).AppendLine("))");
            source.AppendLine("            {");
            source.Append(property.UseShim
                ? "                model.__MerlinSet_" + property.PropertyName + "("
                : "                model.@" + property.PropertyName + " = ")
                .Append("global::MerlinORM.Client.MerlinConvert.To<").Append(property.TypeName)
                .Append(">(data.GetValue(ordinal").Append(index).Append(property.UseShim ? "))!);" : "))!;").AppendLine();
            source.AppendLine("            }");
        }

        source.AppendLine("        }");
        source.AppendLine("    }");
        source.AppendLine();
        source.Append("    internal static class ").Append(model.Identifier).AppendLine("_Registration");
        source.AppendLine("    {");
        source.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        source.AppendLine("        internal static void Register()");
        source.AppendLine("        {");
        source.Append("            global::MerlinORM.Client.MerlinGeneratedMapping<")
            .Append(model.TypeName).Append(">.Register(new ").Append(model.Identifier).AppendLine("_Mapper());");
        source.AppendLine("        }");
        source.AppendLine("    }");
        source.AppendLine("}");

        var shimProperties = model.Properties.Where(property => property.UseShim).ToArray();
        if (shimProperties.Length > 0)
        {
            if (model.Namespace != null)
            {
                source.Append("namespace ").Append(model.Namespace).AppendLine(";");
            }
            source.Append("partial ").Append(model.IsRecord ? "record " : "class ").Append(model.SimpleName).AppendLine();
            source.AppendLine("{");
            foreach (var property in shimProperties)
            {
                source.Append("    internal void __MerlinSet_").Append(property.PropertyName).Append('(')
                    .Append(property.TypeName).Append(" value) => @").Append(property.PropertyName).AppendLine(" = value;");
            }
            source.AppendLine("}");
        }
        return source.ToString();
    }

    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private sealed class ModelInfo
    {
        public ModelInfo(
            string typeName,
            string hintName,
            string identifier,
            ImmutableArray<PropertyInfo> properties,
            ImmutableArray<int> constructorPropertyIndexes,
            string? @namespace,
            string simpleName,
            bool isRecord,
            bool hasBeforeHook,
            bool hasAfterHook,
            Location? location = null,
            string? fallbackReason = null)
        {
            TypeName = typeName;
            HintName = hintName;
            Identifier = identifier;
            Properties = properties;
            ConstructorPropertyIndexes = constructorPropertyIndexes;
            Namespace = @namespace;
            SimpleName = simpleName;
            IsRecord = isRecord;
            HasBeforeHook = hasBeforeHook;
            HasAfterHook = hasAfterHook;
            Location = location;
            FallbackReason = fallbackReason;
        }

        public string TypeName { get; }
        public string HintName { get; }
        public string Identifier { get; }
        public ImmutableArray<PropertyInfo> Properties { get; }
        public ImmutableArray<int> ConstructorPropertyIndexes { get; }
        public string? Namespace { get; }
        public string SimpleName { get; }
        public bool IsRecord { get; }
        public bool HasBeforeHook { get; }
        public bool HasAfterHook { get; }
        public Location? Location { get; }
        public string? FallbackReason { get; }

        public static ModelInfo Fallback(INamedTypeSymbol type, Location location, string reason)
        {
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var identifier = Sanitize(typeName);
            return new ModelInfo(typeName, identifier + ".MerlinMapper.g.cs", identifier,
                ImmutableArray<PropertyInfo>.Empty, ImmutableArray<int>.Empty, null, type.Name, type.IsRecord,
                false, false, location, reason);
        }
    }

    private sealed class PropertyInfo
    {
        public PropertyInfo(
            string propertyName,
            string typeName,
            string columnName,
            bool required,
            bool isNested,
            string nestedPrefix,
            int nestedCreation,
            bool canAssign,
            bool isInitOnly)
        {
            PropertyName = propertyName;
            TypeName = typeName;
            ColumnName = columnName;
            Required = required;
            IsNested = isNested;
            NestedPrefix = nestedPrefix;
            NestedCreation = nestedCreation;
            CanAssign = canAssign;
            IsInitOnly = isInitOnly;
        }

        public string PropertyName { get; }
        public string TypeName { get; }
        public string ColumnName { get; }
        public bool Required { get; }
        public bool IsNested { get; }
        public string NestedPrefix { get; }
        public int NestedCreation { get; }
        public bool CanAssign { get; }
        public bool IsInitOnly { get; }
        public bool ConstructorBound { get; set; }
        public bool UseShim { get; set; }
    }
}
