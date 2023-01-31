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
using System.Text;

namespace kv.Entities.SourceGen
{
    public static class StringBuilderExtensions
    {
        public static void EnableNullables(this StringBuilder stringBuilder)
        {
            stringBuilder.Append("#nullable enable");
            stringBuilder.NewLine();
        }
        
        public static void DisableNullables(this StringBuilder stringBuilder)
        {
            stringBuilder.Append("#nullable disable");
            stringBuilder.NewLine();
        }
        
        public static void AddUsings(this StringBuilder stringBuilder, IEnumerable<string> usings)
        {
            foreach (var ns in usings)
            {
                stringBuilder.Append($"using {ns};");
                stringBuilder.NewLine();
            }
        }

        public static void NewLine(this StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine();
        }

        public static void AddIndentation(this StringBuilder stringBuilder, int indentation)
        {
            for (var i = 0; i < indentation * 4; i++)
            {
                stringBuilder.Append(' ');
            }
        }

        public static void OpenBraces(this StringBuilder stringBuilder, int indentation, bool appendNewLineAfter)
        {
            stringBuilder.AddIndentation(indentation);
            stringBuilder.Append("{");
            if (appendNewLineAfter)
            {
                stringBuilder.NewLine();
            }
        }
        
        public static void CloseBraces(this StringBuilder stringBuilder, int indentation, bool appendNewLineAfter)
        {
            stringBuilder.AddIndentation(indentation);
            stringBuilder.Append("}");
            if (appendNewLineAfter)
            {
                stringBuilder.NewLine();
            }
        }

        public static void OpenParentheses(this StringBuilder stringBuilder)
        {
            stringBuilder.Append("(");
        }
        
        public static void CloseParentheses(this StringBuilder stringBuilder)
        {
            stringBuilder.Append(")");
        }
        
        public static void AppendCode(this StringBuilder stringBuilder, string text, int indentation, bool appendNewLineAfter)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            stringBuilder.AddIndentation(indentation);
            stringBuilder.Append(text);
            if (appendNewLineAfter)
            {
                stringBuilder.NewLine();
            }
        }
    }
}