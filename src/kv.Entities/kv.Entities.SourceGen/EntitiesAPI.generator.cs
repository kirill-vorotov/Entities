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
    public class EntitiesAPISourceGenerator : ISourceGenerator
    {
        private const string typeName = "EntitiesAPI";
        private const string typeNamespace = "kv.Entities.Generated";
        public const string IEntityComponent = "kv.Entities.IEntityComponent";
        public const string Type = "Type";
        public const string ComponentTypeCollection = "kv.Entities.ComponentTypeCollection";
        public const string World = "kv.Entities.World";
        
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
            
            var entityComponentTypes = namedTypeSymbols
                .Where(t => Helper.ImplementsInterface(t, IEntityComponent) && t.IsType && !t.IsAbstract && !t.IsStatic)
                .ToList();
            
            StringBuilder stringBuilder = new();
            var indentation = 0;
            
            stringBuilder.AppendCode("using System;", indentation, true);
            stringBuilder.AppendCode("using kv.Entities;", indentation, true);
            stringBuilder.AppendCode("using System.Collections.Generic;", indentation, true);
            
            stringBuilder.AppendCode($"namespace {typeNamespace}", indentation, true);
            stringBuilder.AppendCode("{", indentation, true);
            indentation++;
            {
                stringBuilder.AppendCode($"public static class {typeName}", indentation, true);
                stringBuilder.AppendCode("{", indentation, true);
                indentation++;
                {
                    stringBuilder.AppendCode($"public static {World} CreateWorld()", indentation, true);
                    stringBuilder.AppendCode("{", indentation, true);
                    indentation++;
                    {
                        stringBuilder.AppendCode($"{ComponentTypeCollection} componentTypeCollection = new();", indentation, true);
                        foreach (var entityComponentType in entityComponentTypes)
                        {
                            stringBuilder.AppendCode($"componentTypeCollection.RegisterType<{entityComponentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();", indentation, true);
                        }
                        
                        stringBuilder.AppendCode($"return new {World}(componentTypeCollection);", indentation, true);
                    }
                    indentation--;
                    stringBuilder.AppendCode("}", indentation, true);
                }
                indentation--;
                stringBuilder.AppendCode("}", indentation, true);
            }
            indentation--;
            stringBuilder.AppendCode("}", indentation, true);
            
            context.AddSource($"{context.Compilation.AssemblyName}.{typeName}.generated", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
        }
    }
}