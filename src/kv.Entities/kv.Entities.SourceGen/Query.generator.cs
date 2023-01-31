/*
 * MIT License
 * Copyright (c) 2023 Kirill Vorotov

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. 
 */

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace kv.Entities.SourceGen
{
    [Generator]
    public class QuerySourceGenerator : ISourceGenerator
    {
        private const string readAttribute = "kv.Entities.ReadAttribute";
        private const string writeAttribute = "kv.Entities.WriteAttribute";
        private const string withoutAttribute = "kv.Entities.WithoutAttribute";
        private const string withGroupComponentAttribute = "kv.Entities.WithGroupComponentAttribute";
        private const string IEntityComponent = "kv.Entities.IEntityComponent";
        private const string IGroupEntityComponent = "kv.Entities.IGroupEntityComponent";
        private const string Entry = "Entry";
        private const string Enumerator = "Enumerator";
        private const string Chunk = "kv.Entities.Chunk";
        private const string ChunkNullable = "kv.Entities.Chunk?";
        private const string Entity = "kv.Entities.Entity";
        private const string IReadOnlyList = "System.Collections.Generic.IReadOnlyList";
        private const string Array = "Array";
        private const string World = "kv.Entities.World";
        private const string List = "System.Collections.Generic.List";
        private const string Func = "Func";
        
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if UNITY_ENGINE
            if (context.Compilation.AssemblyName == Assemblies.AssemblyNames.UnityRootAsseblyname)
            {
                return;
            }
#endif
            if (context.Compilation.AssemblyName is Assemblies.AssemblyNames.EntitiesAsseblyname or Assemblies.AssemblyNames.EntitiesSourceGenAsseblyname)
            {
                return;
            }
            
            var filteredNamespaces = context.Compilation.GlobalNamespace
                .GetNamespaceMembers()
                .Where(ns => !Assemblies.ExcludeRootNamespaces.Contains(ns.Name));
            
            List<INamedTypeSymbol> namedTypeSymbols = new();
            Helper.GetNamedTypes(filteredNamespaces, ref namedTypeSymbols);

            var queryTypes = namedTypeSymbols
                .Where(t => t.GetAttributes().Any(a => a.AttributeClass is not null &&
                                                       (a.AttributeClass.ToDisplayString() == readAttribute ||
                                                        a.AttributeClass.ToDisplayString() == writeAttribute ||
                                                        a.AttributeClass.ToDisplayString() == withoutAttribute ||
                                                        a.AttributeClass.ToDisplayString() == withGroupComponentAttribute)));

            StringBuilder stringBuilder = new();
            foreach (var queryType in queryTypes)
            {
                CreateQuery(context, stringBuilder, queryType);
            }
        }

        private static void CreateQuery(GeneratorExecutionContext context, StringBuilder stringBuilder, INamedTypeSymbol namedTypeSymbol)
        {
            stringBuilder.Clear();
            
            var typeName = namedTypeSymbol.Name;
            List<INamedTypeSymbol> containingTypes = new();
            
            var readAttributes = namedTypeSymbol
                .GetAttributes()
                .Where(a => a.AttributeClass is not null && a.AttributeClass.ToDisplayString() == readAttribute);
            
            var writeAttributes = namedTypeSymbol
                .GetAttributes()
                .Where(a => a.AttributeClass is not null && a.AttributeClass.ToDisplayString() == writeAttribute);
            
            var withoutAttributes = namedTypeSymbol
                .GetAttributes()
                .Where(a => a.AttributeClass is not null && a.AttributeClass.ToDisplayString() == withoutAttribute);
            
            var withGroupComponentAttributes = namedTypeSymbol
                .GetAttributes()
                .Where(a => a.AttributeClass is not null && a.AttributeClass.ToDisplayString() == withGroupComponentAttribute);

            var writeComponentTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var writeOptionalComponentTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            
            var readComponentTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var readOptionalComponentTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            
            var withoutComponentTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            
            var withGroupComponentTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            
            foreach (var attributeData in writeAttributes)
            {
                if (attributeData.ConstructorArguments.Length < 2)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments[0].Value is INamedTypeSymbol type &&
                    attributeData.ConstructorArguments[1].Value is bool optional)
                {
                    if (!Helper.ImplementsInterface(type, IEntityComponent))
                    {
                        continue;
                    }
                    if (optional)
                    {
                        writeOptionalComponentTypes.Add(type);
                    }
                    else
                    {
                        writeComponentTypes.Add(type);
                    }
                }
            }
            
            foreach (var attributeData in readAttributes)
            {
                if (attributeData.ConstructorArguments.Length < 2)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments[0].Value is INamedTypeSymbol type &&
                    attributeData.ConstructorArguments[1].Value is bool optional)
                {
                    if (!Helper.ImplementsInterface(type, IEntityComponent))
                    {
                        continue;
                    }
                    if (optional)
                    {
                        readOptionalComponentTypes.Add(type);
                    }
                    else
                    {
                        readComponentTypes.Add(type);
                    }
                }
            }
            
            foreach (var attributeData in withoutAttributes)
            {
                foreach (var typedConstant in attributeData.ConstructorArguments)
                {
                    if (typedConstant.Value is not INamedTypeSymbol arg)
                    {
                        continue;
                    }
                    if (!Helper.ImplementsInterface(arg, IEntityComponent))
                    {
                        continue;
                    }

                    if (writeComponentTypes.Contains(arg) || 
                        writeOptionalComponentTypes.Contains(arg) || 
                        readComponentTypes.Contains(arg) || 
                        readOptionalComponentTypes.Contains(arg))
                    {
                        continue;
                    }
                    withoutComponentTypes.Add(arg);
                }
            }
            
            foreach (var attributeData in withGroupComponentAttributes)
            {
                foreach (var typedConstant in attributeData.ConstructorArguments)
                {
                    if (typedConstant.Value is not INamedTypeSymbol arg)
                    {
                        continue;
                    }
                    if (!arg.IsUnmanagedType || !Helper.ImplementsInterface(arg, IGroupEntityComponent))
                    {
                        continue;
                    }

                    if (writeComponentTypes.Contains(arg) || 
                        writeOptionalComponentTypes.Contains(arg) || 
                        readComponentTypes.Contains(arg) || 
                        readOptionalComponentTypes.Contains(arg))
                    {
                        continue;
                    }
                    withoutComponentTypes.Add(arg);
                }
            }
            
            FindContainingTypes(namedTypeSymbol, ref containingTypes);
            containingTypes.Reverse();

            var indentation = 0;
            
            stringBuilder.AppendCode("#nullable enable", indentation, true);
            stringBuilder.AppendCode("using System;", indentation, true);
            stringBuilder.AppendCode("using kv.Entities;", indentation, true);
            stringBuilder.AppendCode("using System.Collections.Generic;", indentation, true);
            
            stringBuilder.AppendCode($"namespace {namedTypeSymbol.ContainingNamespace}", indentation, true);
            stringBuilder.AppendCode("{", indentation, true);
            indentation++;
            {
                List<string> generics = new();
                foreach (var containingType in containingTypes)
                {
                    generics.Clear();
                    if (containingType.IsGenericType)
                    {
                        foreach (var typeParameterSymbol in containingType.TypeParameters)
                        {
                            generics.Add(typeParameterSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        }
                    }

                    var containingTypeDefinition = containingType.TypeKind switch
                    {
                        TypeKind.Class => "class",
                        TypeKind.Struct => "struct",
                        _ => ""
                    };

                    var isStatic = containingType.IsStatic ? " static" : string.Empty;
                    var isRefLikeType = containingType.IsRefLikeType ? " ref" : string.Empty;
                    var genericsString = generics.Count == 0 ? string.Empty : $"<{string.Join(", ", generics)}>";

                    stringBuilder.AppendCode(
                        $"public{isRefLikeType}{isStatic} partial {containingTypeDefinition} {containingType.Name}{genericsString}",
                        indentation, true);
                    stringBuilder.AppendCode("{", indentation, true);
                    indentation++;
                }

                {
                    generics.Clear();
                    if (namedTypeSymbol.IsGenericType)
                    {
                        foreach (var typeParameterSymbol in namedTypeSymbol.TypeParameters)
                        {
                            generics.Add(typeParameterSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        }
                    }

                    var typeDefinition = namedTypeSymbol.TypeKind switch
                    {
                        TypeKind.Class => "class",
                        TypeKind.Struct => "struct",
                        _ => ""
                    };

                    var isStatic = namedTypeSymbol.IsStatic ? " static" : string.Empty;
                    var isRefLikeType = namedTypeSymbol.IsRefLikeType ? " ref" : string.Empty;
                    var genericsString = generics.Count == 0 ? string.Empty : $"<{string.Join(", ", generics)}>";

                    stringBuilder.AppendCode(
                        $"public{isRefLikeType}{isStatic} partial {typeDefinition} {namedTypeSymbol.Name}{genericsString}",
                        indentation, true);
                    stringBuilder.AppendCode("{", indentation, true);
                    indentation++;
                    {
                        stringBuilder.AppendCode($"public delegate bool {Func}({Entry} entry);", indentation, true);
#region Entry
                        
                        stringBuilder.AppendCode($"public ref struct {Entry}", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"private {ChunkNullable} _chunk;", indentation, true);
                            stringBuilder.AppendCode($"private int _index;", indentation, true);
                            
                            stringBuilder.AppendLine();
                            
                            stringBuilder.AppendCode($"private {Entity}[] _entities;", indentation, true);
                            foreach (var componentType in writeComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"private Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span;", indentation, true);
                            }
                            
                            foreach (var componentType in readComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"private Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span;", indentation, true);
                            }
                            
                            foreach (var componentType in writeOptionalComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"private Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span;", indentation, true);
                            }
                            
                            foreach (var componentType in readOptionalComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"private Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span;", indentation, true);
                            }

                            stringBuilder.AppendLine();
                            
                            stringBuilder.AppendCode($"public {Entity} Entity => _entities[_index];", indentation, true);
                            foreach (var componentType in writeComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"public ref {componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {componentName} => ref {componentName}_Span[_index];", indentation, true);
                            }
                            
                            foreach (var componentType in readComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"public {componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {componentName} => {componentName}_Span[_index];", indentation, true);
                            }
                            
                            foreach (var componentType in writeOptionalComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"public ref {componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {componentName} => ref {componentName}_Span[_index];", indentation, true);
                            }
                            
                            foreach (var componentType in readOptionalComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"public {componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {componentName} => {componentName}_Span[_index];", indentation, true);
                            }

                            stringBuilder.AppendLine();
                            
                            stringBuilder.AppendCode($"public bool HasComponent<T>() where T : IEntityComponent => _chunk?.HasComponent<T>() ?? false;", indentation, true);
                            
                            stringBuilder.AppendLine();

                            stringBuilder.AppendCode($"public Entry(", indentation, true);
                            indentation++;
                            stringBuilder.AppendCode($"{ChunkNullable} chunk,", indentation, true);
                            stringBuilder.AppendCode($"int index,", indentation, true);
                            stringBuilder.AppendCode($"{Entity}[] entities,", indentation, true);

                            List<string> componentArgs = new();
                            foreach (var componentType in writeComponentTypes)
                            {
                                var componentName = componentType.Name;
                                componentArgs.Add($"{new string(' ', indentation * 4)}Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span");
                            }
                            
                            foreach (var componentType in readComponentTypes)
                            {
                                var componentName = componentType.Name;
                                componentArgs.Add($"{new string(' ', indentation * 4)}Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span");
                            }
                            
                            foreach (var componentType in writeOptionalComponentTypes)
                            {
                                var componentName = componentType.Name;
                                componentArgs.Add($"{new string(' ', indentation * 4)}Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span");
                            }
                            
                            foreach (var componentType in readOptionalComponentTypes)
                            {
                                var componentName = componentType.Name;
                                componentArgs.Add($"{new string(' ', indentation * 4)}Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span");
                            }
                            
                            stringBuilder.AppendCode(string.Join(",\n", componentArgs), 0, false);
                            stringBuilder.AppendCode($")", 0, true);
                            indentation--;
                            
                            stringBuilder.AppendCode("{", indentation, true);
                            indentation++;
                            {
                                stringBuilder.AppendCode($"_chunk = chunk;", indentation, true);
                                stringBuilder.AppendCode($"_index = index;", indentation, true);
                                stringBuilder.AppendCode($"_entities = entities;", indentation, true);
                                
                                foreach (var componentType in writeComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    stringBuilder.AppendCode($"this.{componentName}_Span = {componentName}_Span;", indentation, true);
                                }
                                
                                foreach (var componentType in readComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    stringBuilder.AppendCode($"this.{componentName}_Span = {componentName}_Span;", indentation, true);
                                }
                                
                                foreach (var componentType in writeOptionalComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    stringBuilder.AppendCode($"this.{componentName}_Span = {componentName}_Span;", indentation, true);
                                }
                                
                                foreach (var componentType in readOptionalComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    stringBuilder.AppendCode($"this.{componentName}_Span = {componentName}_Span;", indentation, true);
                                }
                            }
                            indentation--;
                            stringBuilder.AppendCode("}", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
#endregion

                        stringBuilder.AppendLine();

#region Enumerator
                        
                        stringBuilder.AppendCode($"public ref struct {Enumerator}", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"private {IReadOnlyList}<{Chunk}> _chunks;", indentation, true);
                            stringBuilder.AppendCode($"private {ChunkNullable} _currentChunk;", indentation, true);
                            stringBuilder.AppendCode($"private int _currentChunkIndex;", indentation, true);
                            stringBuilder.AppendCode($"private int _currentIndexInChunk;", indentation, true);

                            stringBuilder.AppendLine();
                            
                            stringBuilder.AppendCode($"private {List}<{Func}> _predicates;", indentation, true);
                            
                            stringBuilder.AppendLine();
                            
                            stringBuilder.AppendCode($"private {Entity}[] _entities;", indentation, true);
                            
                            foreach (var componentType in writeComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"private Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span;", indentation, true);
                            }
                            
                            foreach (var componentType in readComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"private Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span;", indentation, true);
                            }
                            
                            foreach (var componentType in writeOptionalComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"private Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span;", indentation, true);
                            }
                            
                            foreach (var componentType in readOptionalComponentTypes)
                            {
                                var componentName = componentType.Name;
                                stringBuilder.AppendCode($"private Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {componentName}_Span;", indentation, true);
                            }

                            stringBuilder.AppendLine();
                            stringBuilder.AppendCode($"public {Enumerator}({IReadOnlyList}<{Chunk}> chunks, {List}<{Func}> predicates)", indentation, true);
                            stringBuilder.AppendCode("{", indentation, true);
                            indentation++;
                            {
                                stringBuilder.AppendCode($"_chunks = chunks;", indentation, true);
                                stringBuilder.AppendCode($"_currentChunkIndex = -1;", indentation, true);
                                stringBuilder.AppendCode($"_currentIndexInChunk = -1;", indentation, true);
                                stringBuilder.AppendCode($"_currentChunk = default;", indentation, true);
                                stringBuilder.AppendCode($"_predicates = predicates;", indentation, true);
                                stringBuilder.AppendCode($"_entities = {Array}.Empty<{Entity}>();", indentation, true);
                                
                                foreach (var componentType in writeComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    stringBuilder.AppendCode($"{componentName}_Span = Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;", indentation, true);
                                }
                                
                                foreach (var componentType in readComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    stringBuilder.AppendCode($"{componentName}_Span = Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;", indentation, true);
                                }
                                
                                foreach (var componentType in writeOptionalComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    stringBuilder.AppendCode($"{componentName}_Span = Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;", indentation, true);
                                }
                                
                                foreach (var componentType in readOptionalComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    stringBuilder.AppendCode($"{componentName}_Span = Span<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Empty;", indentation, true);
                                }
                            }
                            indentation--;
                            stringBuilder.AppendCode("}", indentation, true);

                            stringBuilder.AppendLine();
                            
                            stringBuilder.AppendCode($"public bool MoveNext()", indentation, true);
                            stringBuilder.AppendCode("{", indentation, true);
                            indentation++;
                            {
                                stringBuilder.AppendCode($"do", indentation, true);
                                stringBuilder.AppendCode("{", indentation, true);
                                indentation++;
                                {
                                    stringBuilder.AppendCode($"_currentIndexInChunk++;", indentation, true);
                                    stringBuilder.AppendCode($"if (_currentChunk is null || _currentIndexInChunk >= _currentChunk.Count)", indentation, true);
                                    stringBuilder.AppendCode("{", indentation, true);
                                    indentation++;
                                    {
                                        stringBuilder.AppendCode($"_currentChunkIndex++;", indentation, true);
                                        stringBuilder.AppendCode($"if (_currentChunkIndex >= _chunks.Count) return false;", indentation, true);
                                        stringBuilder.AppendCode($"_currentChunk = _chunks[_currentChunkIndex];", indentation, true);
                                        stringBuilder.AppendCode($"_currentIndexInChunk = 0;", indentation, true);
                                        stringBuilder.AppendCode($"Update(_currentChunk);", indentation, true);
                                    }
                                    indentation--;
                                    stringBuilder.AppendCode("}", indentation, true);
                                }
                                indentation--;
                                stringBuilder.AppendCode("} while (!CheckPredicate(Current));", indentation, true);
                                stringBuilder.AppendCode($"return true;", indentation, true);
                            }
                            indentation--;
                            stringBuilder.AppendCode("}", indentation, true);
                            
                            stringBuilder.AppendLine();
                            
                            stringBuilder.AppendCode($"public {Entry} Current =>", indentation, true);
                            indentation++;
                            {
                                stringBuilder.AppendCode("new(", indentation, true);
                                indentation++;
                                {
                                    stringBuilder.AppendCode($"_currentChunk,", indentation, true);
                                    stringBuilder.AppendCode($"_currentIndexInChunk,", indentation, true);
                                    stringBuilder.AppendCode($"_entities,", indentation, true);
                                    
                                    List<string> componentArgs = new();
                                    foreach (var componentType in writeComponentTypes)
                                    {
                                        var componentName = componentType.Name;
                                        componentArgs.Add($"{new string(' ', indentation * 4)}{componentName}_Span");
                                    }
                                    
                                    foreach (var componentType in readComponentTypes)
                                    {
                                        var componentName = componentType.Name;
                                        componentArgs.Add($"{new string(' ', indentation * 4)}{componentName}_Span");
                                    }
                                    
                                    foreach (var componentType in writeOptionalComponentTypes)
                                    {
                                        var componentName = componentType.Name;
                                        componentArgs.Add($"{new string(' ', indentation * 4)}{componentName}_Span");
                                    }
                                    
                                    foreach (var componentType in readOptionalComponentTypes)
                                    {
                                        var componentName = componentType.Name;
                                        componentArgs.Add($"{new string(' ', indentation * 4)}{componentName}_Span");
                                    }
                                    stringBuilder.AppendCode(string.Join(",\n", componentArgs), 0, true);
                                }
                                indentation--;
                                stringBuilder.AppendCode(");", indentation, true);
                            }
                            indentation--;
                            
                            stringBuilder.AppendLine();
                            
                            stringBuilder.AppendCode($"public void Reset()", indentation, true);
                            stringBuilder.AppendCode("{", indentation, true);
                            indentation++;
                            {
                                stringBuilder.AppendCode($"_currentChunk = default;", indentation, true);
                                stringBuilder.AppendCode($"_currentChunkIndex = -1;", indentation, true);
                                stringBuilder.AppendCode($"_currentIndexInChunk = -1;", indentation, true);
                            }
                            indentation--;
                            stringBuilder.AppendCode("}", indentation, true);
                            
                            stringBuilder.AppendCode($"private void Update({Chunk} chunk)", indentation, true);
                            stringBuilder.AppendCode("{", indentation, true);
                            indentation++;
                            {
                                stringBuilder.AppendCode($"_entities = chunk.Entities;", indentation, true);
                                
                                foreach (var componentType in writeComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    if (componentType.IsUnmanagedType)
                                    {
                                        stringBuilder.AppendCode($"{componentName}_Span = chunk.GetSpan<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                                    }
                                    else
                                    {
                                        stringBuilder.AppendCode($"{componentName}_Span = chunk.GetManagedSpan<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                                    }
                                }
                                
                                foreach (var componentType in readComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    if (componentType.IsUnmanagedType)
                                    {
                                        stringBuilder.AppendCode($"{componentName}_Span = chunk.GetSpan<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                                    }
                                    else
                                    {
                                        stringBuilder.AppendCode($"{componentName}_Span = chunk.GetManagedSpan<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                                    }
                                }
                                
                                foreach (var componentType in writeOptionalComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    if (componentType.IsUnmanagedType)
                                    {
                                        stringBuilder.AppendCode($"{componentName}_Span = chunk.GetSpan<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                                    }
                                    else
                                    {
                                        stringBuilder.AppendCode($"{componentName}_Span = chunk.GetManagedSpan<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                                    }
                                }
                                
                                foreach (var componentType in readOptionalComponentTypes)
                                {
                                    var componentName = componentType.Name;
                                    if (componentType.IsUnmanagedType)
                                    {
                                        stringBuilder.AppendCode($"{componentName}_Span = chunk.GetSpan<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                                    }
                                    else
                                    {
                                        stringBuilder.AppendCode($"{componentName}_Span = chunk.GetManagedSpan<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                                    }
                                }
                            }
                            indentation--;
                            stringBuilder.AppendCode("}", indentation, true);
                            
                            stringBuilder.AppendCode($"private bool CheckPredicate(Entry current)", indentation, true);
                            stringBuilder.AppendCode("{", indentation, true);
                            indentation++;
                            {
                                stringBuilder.AppendCode($"foreach (var predicate in _predicates)", indentation, true);
                                stringBuilder.AppendCode("{", indentation, true);
                                indentation++;
                                {
                                    stringBuilder.AppendCode($"if (!(predicate?.Invoke(current) ?? true))", indentation, true);
                                    stringBuilder.AppendCode("{", indentation, true);
                                    indentation++;
                                    {
                                        stringBuilder.AppendCode($"return false;", indentation, true);
                                    }
                                    indentation--;
                                    stringBuilder.AppendCode("}", indentation, true);
                                }
                                indentation--;
                                stringBuilder.AppendCode("}", indentation, true);
                                stringBuilder.AppendCode($"return true;", indentation, true);
                            }
                            indentation--;
                            stringBuilder.AppendCode("}", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
#endregion

                        stringBuilder.AppendLine();
                        stringBuilder.AppendCode($"private {IReadOnlyList}<{Chunk}> _chunks;", indentation, true);
                        stringBuilder.AppendCode($"private {List}<{Func}> _predicates;", indentation, true);
                        stringBuilder.AppendCode($"private int _initialCount;", indentation, true);
                        
                        stringBuilder.AppendLine();
                        
                        stringBuilder.AppendCode($"private {namedTypeSymbol.Name}({IReadOnlyList}<{Chunk}> chunks, int count, {List}<{Func}> predicates)", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"_chunks = chunks;", indentation, true);
                            stringBuilder.AppendCode($"_predicates = predicates;", indentation, true);
                            stringBuilder.AppendCode($"_initialCount = count;", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        stringBuilder.AppendCode($"public {Enumerator} GetEnumerator()", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"return new {Enumerator}(_chunks, _predicates);", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);

                        #region Linq
                        
                        stringBuilder.AppendCode($"public {namedTypeSymbol.Name} Where({Func} predicate)", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"return new {namedTypeSymbol.Name}(_chunks, _initialCount, new {List}<{Func}>(_predicates) {{ predicate }});", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        stringBuilder.AppendCode($"public {Entry} FirstOrDefault()", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"var enumerator = GetEnumerator();", indentation, true);
                            stringBuilder.AppendCode($"return enumerator.MoveNext() ? enumerator.Current : default;", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        stringBuilder.AppendCode($"public {Entry} FirstOrDefault({Func} predicate)", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"var newQuery = this.Where(predicate);", indentation, true);
                            stringBuilder.AppendCode($"var enumerator = newQuery.GetEnumerator();", indentation, true);
                            stringBuilder.AppendCode($"return enumerator.MoveNext() ? enumerator.Current : default;", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        stringBuilder.AppendCode($"public bool TryGetFirst(out {Entry} entry)", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"var enumerator = GetEnumerator();", indentation, true);
                            stringBuilder.AppendCode($"var moveNext = enumerator.MoveNext();", indentation, true);
                            stringBuilder.AppendCode($"entry = moveNext ? enumerator.Current : default;", indentation, true);
                            stringBuilder.AppendCode($"return moveNext;", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        stringBuilder.AppendCode($"public bool TryGetFirst({Func} predicate, out {Entry} entry)", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"var newQuery = this.Where(predicate);", indentation, true);
                            stringBuilder.AppendCode($"var enumerator = newQuery.GetEnumerator();", indentation, true);
                            stringBuilder.AppendCode($"var moveNext = enumerator.MoveNext();", indentation, true);
                            stringBuilder.AppendCode($"entry = moveNext ? enumerator.Current : default;", indentation, true);
                            stringBuilder.AppendCode($"return moveNext;", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        stringBuilder.AppendCode($"public bool Any()", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"var enumerator = GetEnumerator();", indentation, true);
                            stringBuilder.AppendCode($"return enumerator.MoveNext();", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        stringBuilder.AppendCode($"public bool Any({Func} predicate)", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"var newQuery = this.Where(predicate);", indentation, true);
                            stringBuilder.AppendCode($"var enumerator = newQuery.GetEnumerator();", indentation, true);
                            stringBuilder.AppendCode($"return enumerator.MoveNext();", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        stringBuilder.AppendCode($"public int Count()", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"if (_predicates is null or {{ Count: 0 }}) return _initialCount;", indentation, true);
                            stringBuilder.AppendLine();
                            stringBuilder.AppendCode($"var count = 0;", indentation, true);
                            stringBuilder.AppendCode($"foreach (var entry in this) count++;", indentation, true);
                            stringBuilder.AppendLine();
                            stringBuilder.AppendCode($"return count;", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                        
                        #endregion
                        
                        stringBuilder.AppendCode($"public static {namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} Create({World} world)", indentation, true);
                        stringBuilder.AppendCode("{", indentation, true);
                        indentation++;
                        {
                            stringBuilder.AppendCode($"if (!world.CachedQueries.TryGetValue(typeof({namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), out var cachedQuery))", indentation, true);
                            stringBuilder.AppendCode("{", indentation, true);
                            indentation++;
                            {
                                stringBuilder.AppendCode($"cachedQuery = world.CreateEntityQuery()", indentation, true);
                                
                                indentation++;
                                List<string> components = new();
                                foreach (var componentType in writeComponentTypes)
                                {
                                    stringBuilder.AppendCode($".WithAll<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()", indentation, true);
                                }
                                
                                foreach (var componentType in readComponentTypes)
                                {
                                    stringBuilder.AppendCode($".WithAll<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()", indentation, true);
                                }
                                
                                foreach (var componentType in writeOptionalComponentTypes)
                                {
                                    stringBuilder.AppendCode($".WithAny<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()", indentation, true);
                                }
                                
                                foreach (var componentType in readOptionalComponentTypes)
                                {
                                    stringBuilder.AppendCode($".WithAny<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()", indentation, true);
                                }

                                foreach (var componentType in withoutComponentTypes)
                                {
                                    stringBuilder.AppendCode($".WithNone<{componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()", indentation, true);
                                }
                                stringBuilder.AppendCode($".Update();", indentation, true);
                                indentation--;
                                
                                stringBuilder.AppendCode($"world.CachedQueries[typeof({namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})] = cachedQuery;", indentation, true);
                            }
                            indentation--;
                            stringBuilder.AppendCode("}", indentation, true);
                            
                            stringBuilder.AppendCode($"return new {namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(cachedQuery.Chunks, cachedQuery.Count, new {List}<{Func}>());", indentation, true);
                        }
                        indentation--;
                        stringBuilder.AppendCode("}", indentation, true);
                    }
                    indentation--;
                    stringBuilder.AppendCode("}", indentation, true);
                }

                for (var i = 0; i < containingTypes.Count; i++)
                {
                    indentation--;
                    stringBuilder.AppendCode("}", indentation, true);
                }
            }
            indentation--;
            stringBuilder.AppendCode("}", indentation, true);
            
            var hintName = string.Join('.', containingTypes.Select(t => t.Name));
            if (string.IsNullOrEmpty(hintName))
            {
                context.AddSource($"{typeName}.generated", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
            }
            else
            {
                context.AddSource($"{hintName}.{typeName}.generated", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
            }
        }
        
        public static void FindContainingTypes(INamedTypeSymbol namedTypeSymbol, ref List<INamedTypeSymbol> containingTypes)
        {
            while (true)
            {
                if (namedTypeSymbol.ContainingType is null)
                {
                    return;
                }

                containingTypes.Add(namedTypeSymbol.ContainingType);
                namedTypeSymbol = namedTypeSymbol.ContainingType;
            }
        }
    }
}