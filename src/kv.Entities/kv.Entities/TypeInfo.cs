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

namespace kv.Entities
{
    public readonly struct TypeInfo
    {
        public readonly int TypeId;
        public readonly Type Type;
        public readonly int Size;
        public readonly int MemoryAlignment;
        public readonly bool Unmanaged;
        public readonly bool GroupComponent;

        public TypeInfo(int typeId, Type type, int size, int memoryAlignment, bool unmanaged, bool groupComponent)
        {
            TypeId = typeId;
            Type = type;
            Size = size;
            MemoryAlignment = memoryAlignment;
            Unmanaged = unmanaged;
            GroupComponent = groupComponent;
        }

        public bool IsZeroSized => Unmanaged && Size == 0;
    }
}