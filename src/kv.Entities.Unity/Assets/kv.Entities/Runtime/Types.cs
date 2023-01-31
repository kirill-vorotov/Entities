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

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace kv.Entities
{
    public sealed class Types
    {
        public TypeInfo[] TypeInfos;
        public Type[] ComponentTypes;
        public Dictionary<Type, TypeInfo> TypeInfosMap;
        public int Length => ComponentTypes.Length;

        public Types(TypeInfo[] typeInfos)
        {
            TypeInfos = typeInfos;
            ComponentTypes = TypeInfos.Select(i => i.Type).ToArray();
            TypeInfosMap = TypeInfos.ToDictionary(i => i.Type, i => i);
        }
        
        public Types(ComponentTypeCollection componentTypeCollection)
        {
            TypeInfos = componentTypeCollection.TypesInfos.ToArray();
            ComponentTypes = TypeInfos.Select(i => i.Type).ToArray();
            TypeInfosMap = TypeInfos.ToDictionary(i => i.Type, i => i);
        }
    }
}