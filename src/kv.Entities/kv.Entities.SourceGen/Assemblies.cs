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

namespace kv.Entities.SourceGen
{
    public class Assemblies
    {
        public static class AssemblyNames
        {
            public const string UnityRootAsseblyname = "Assembly-CSharp";
            public const string EntitiesAsseblyname = "kv.Entities";
            public const string EntitiesSourceGenAsseblyname = "kv.Entities.SourceGen";
        }
        
        public const string System = "System";
        public const string UnityEngine = "UnityEngine";
        public const string UnityEditor = "UnityEditor";
        public const string Microsoft = "Microsoft";
        public const string Unity = "Unity";
        public const string UnityEngineInternal = "UnityEngineInternal";
        public const string UnityEditorInternal = "UnityEditorInternal";
        public const string Serilog = "Serilog";
        public const string JetBrains = "JetBrains";
        public const string NUnit = "NUnit";
        public const string Mono = "Mono";
        public const string Rider = "Rider";
        public const string TMPro = "TMPro";
        public const string Cysharp = "Cysharp";
        public const string CSharpx = "CSharpx";
        public const string CommandLine = "CommandLine";
        public const string Newtonsoft = "Newtonsoft";
        public const string Packages = "Packages";
        public const string DG = "DG";
        public const string FxResources = "FxResources";
        public const string TreeEditor = "TreeEditor";
        public const string RailwaySharp = "RailwaySharp";
        public const string NiceIO = "NiceIO";
        public const string Uniject = "Uniject";
        public const string ProResOut = "ProResOut";
        public const string AOT = "AOT";

        public static HashSet<string> ExcludeRootNamespaces = new()
        {
            System,
            UnityEngine,
            UnityEditor,
            Microsoft,
            Unity,
            UnityEngineInternal,
            UnityEditorInternal,
            Serilog,
            JetBrains,
            NUnit,
            Mono,
            Rider,
            TMPro,
            Cysharp,
            CSharpx,
            CommandLine,
            Newtonsoft,
            Packages,
            DG,
            FxResources,
            TreeEditor,
            RailwaySharp,
            NiceIO,
            Uniject,
            ProResOut,
            AOT,
        };
    }
}