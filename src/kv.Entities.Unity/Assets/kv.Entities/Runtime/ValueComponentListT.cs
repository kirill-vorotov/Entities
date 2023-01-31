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
using System.Runtime.InteropServices;

namespace kv.Entities
{
    public sealed class ValueComponentList<T> : IComponentList where T : struct, IEntityComponent
    {
        private readonly TypeInfo _typeInfo;
        public byte[] Buffer;
        public int Count { get; private set; }
        public int Capacity;

        public ValueComponentList(TypeInfo typeInfo, int initialCapacity = 4)
        {
            DebugHelper.Assert(typeInfo.Unmanaged);
            
            _typeInfo = typeInfo;
            if (typeInfo.IsZeroSized)
            {
                Buffer = Array.Empty<byte>();
                return;
            }

            Buffer = new byte[initialCapacity * typeInfo.Size];
            Capacity = initialCapacity;
        }
        
        public TypeInfo TypeInfo => _typeInfo;
        public Array GetArray() => default!;
        public Span<byte> GetBuffer() => Buffer.AsSpan();
        public Span<byte> GetBuffer(int index) => Buffer.AsSpan(index * _typeInfo.Size, _typeInfo.Size);
        
        public void AddComponent(T value)
        {
            DebugHelper.Assert(_typeInfo is { Unmanaged: true, IsZeroSized: false });

            if (Capacity == 0)
            {
                Array.Resize(ref Buffer, _typeInfo.Size * 4);
                Capacity = 4;
            }
            else if (Count == Capacity)
            {
                Array.Resize(ref Buffer, Capacity * _typeInfo.Size * 2);
                Capacity *= 2;
            }

            var span = Buffer.AsSpan(Count * _typeInfo.Size, _typeInfo.Size);
            MemoryMarshal.Write(span, ref value);
            Count++;
        }
    }
}