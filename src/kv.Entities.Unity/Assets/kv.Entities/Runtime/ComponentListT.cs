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
    internal sealed class ComponentList<T> : IComponentList where T : IEntityComponent
    {
        private readonly TypeInfo _typeInfo;
        public T[] Array;
        public int Count { get; private set; }
        public int Capacity;

        public ComponentList(TypeInfo typeInfo, int initialCapacity = 4)
        {
            DebugHelper.Assert(!typeInfo.Unmanaged);
            
            _typeInfo = typeInfo;
            Array = new T[initialCapacity];
            Capacity = initialCapacity;
        }
        
        public TypeInfo TypeInfo => _typeInfo;
        public Array GetArray() => Array;
        public Span<byte> GetBuffer() => default;
        public Span<byte> GetBuffer(int index) => default;

        public void AddComponent(T value)
        {
            DebugHelper.Assert(_typeInfo is { Unmanaged: false, IsZeroSized: false });

            if (Capacity == 0)
            {
                System.Array.Resize(ref Array, 4);
                Capacity = 4;
            }
            else if (Count == Capacity)
            {
                System.Array.Resize(ref Array, Capacity * 2);
                Capacity *= 2;
            }
            Array[Count] = value;
            Count++;
        }
    }
}