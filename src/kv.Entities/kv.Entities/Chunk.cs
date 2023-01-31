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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace kv.Entities
{
    public sealed class Chunk
    {
        internal struct OffsetInfo
        {
            public int TypeId;
            public int Offset;
            public int ComponentSize;

            public OffsetInfo(int typeId, int offset, int componentSize)
            {
                TypeId = typeId;
                Offset = offset;
                ComponentSize = componentSize;
            }
        }
        
        public readonly int ChunkId;
        public readonly int Capacity;
        public int Count { get; internal set; }

        internal readonly World World;
        internal readonly TypeInfo[] TypeInfos;
        
        public Entity[] Entities;
        internal byte[] Buffer;
        internal Array[] ComponentArrays;
        
        internal readonly int[] IndexLookup;
        internal readonly int[] Offsets;

        internal Chunk(World world, int chunkId, TypeInfo[] typeInfos, int[] indexLookup, int[] offsets, int bufferSize, int capacity)
        {
            World = world;
            ChunkId = chunkId;
            Capacity = capacity;
            Count = 0;

            TypeInfos = typeInfos;
            
            Entities = new Entity[capacity];
            Buffer = new byte[bufferSize];
            ComponentArrays = new Array[typeInfos.Length];
            
            IndexLookup = indexLookup;
            Offsets = offsets;
            
            for (var index = 0; index < typeInfos.Length; index++)
            {
                var typeInfo = typeInfos[index];

                if (typeInfo.IsZeroSized)
                {
                    continue;
                }
                
                if (!typeInfo.Unmanaged)
                {
                    ComponentArrays[index] = Array.CreateInstance(typeInfo.Type, capacity);
                }
            }
        }

        public bool HasComponent<T>() where T : IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            return IndexLookup[typeInfo.TypeId] != -1;
        }

        public ref T GetComponent<T>(int index) where T : unmanaged, IEntityComponent
        {
            return ref GetSpan<T>()[index];
        }
        
        public ref T GetManagedComponent<T>(int index) where T : IEntityComponent
        {
            return ref GetManagedSpan<T>()[index];
        }

        public Span<T> GetSpan<T>() where T : unmanaged, IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            DebugHelper.Assert(typeInfo is { Unmanaged: true });
            
            if (typeInfo.IsZeroSized)
            {
                return Span<T>.Empty;
            }
            
            var arrayIndex = IndexLookup[typeInfo.TypeId];
            if (arrayIndex < 0)
            {
                return Span<T>.Empty;
            }
            var offset = Offsets[arrayIndex];
            var span = Buffer.AsSpan(offset, Capacity * typeInfo.Size);
            return MemoryMarshal.Cast<byte, T>(span);
        }
        
        public Span<T> GetManagedSpan<T>() where T : IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            DebugHelper.Assert(typeInfo is { Unmanaged: true, IsZeroSized: false });
            
            var arrayIndex = IndexLookup[typeInfo.TypeId];
            if (arrayIndex < 0)
            {
                return Span<T>.Empty;
            }
            var array = ComponentArrays[arrayIndex];
            return Unsafe.As<T[]>(array).AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyFrom(Chunk src, int srcIndex, int dstIndex)
        {
            foreach (var typeInfo in src.TypeInfos)
            {
                var dstArrayIndex = IndexLookup[typeInfo.TypeId];
                var srcArrayIndex = src.IndexLookup[typeInfo.TypeId];
                
                if (dstArrayIndex == -1 || typeInfo.IsZeroSized)
                {
                    continue;
                }

                if (typeInfo.Unmanaged)
                {
                    var srcOffset = src.Offsets[srcArrayIndex];
                    var srcSpan = src.Buffer.AsSpan(srcOffset + srcIndex * typeInfo.Size, typeInfo.Size);
                    CopyComponentFrom(typeInfo, srcSpan, dstIndex);
                }
                else
                {
                    var srcArary = src.ComponentArrays[srcArrayIndex];
                    CopyComponentFrom(typeInfo, srcArary, srcIndex, dstIndex);
                }
            }

            Entities[dstIndex] = src.Entities[srcIndex];
        }

        internal void RemoveAt(int index)
        {
            if (index == Count - 1)
            {
                Clear(index);
                Count--;
                return;
            }
            
            var lastIndex = Count - 1;
            
            foreach (var typeInfo in TypeInfos)
            {
                if (typeInfo.IsZeroSized)
                {
                    continue;
                }
                
                var arrayIndex = IndexLookup[typeInfo.TypeId];

                if (typeInfo.Unmanaged)
                {
                    var srcOffset = Offsets[arrayIndex];
                    var span0 = Buffer.AsSpan(srcOffset + index * typeInfo.Size, typeInfo.Size);
                    var span1 = Buffer.AsSpan(srcOffset + lastIndex * typeInfo.Size, typeInfo.Size);
                    span1.CopyTo(span0);
                }
                else
                {
                    var array = ComponentArrays[arrayIndex];
                    Array.Copy(array, lastIndex, array, index, 1);
                    Array.Clear(array, lastIndex, 1);
                }
            }

            Entities[index] = Entities[lastIndex];
            Entities[lastIndex] = new Entity(-1, -1);
            Count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyComponentFrom(TypeInfo typeInfo, Span<byte> src, int dstIndex)
        {
            DebugHelper.Assert(typeInfo is { Unmanaged: true, IsZeroSized: false });

            var dstArrayIndex = IndexLookup[typeInfo.TypeId];
            var dstOffset = Offsets[dstArrayIndex];
            var dstSpan = Buffer.AsSpan(dstOffset + dstIndex * typeInfo.Size, typeInfo.Size);
            src.CopyTo(dstSpan);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CopyComponentFrom(TypeInfo typeInfo, Array src, int srcIndex, int dstIndex)
        {
            DebugHelper.Assert(typeInfo is { Unmanaged: false, IsZeroSized: false });

            var dstArrayIndex = IndexLookup[typeInfo.TypeId];
            var dst = ComponentArrays[dstArrayIndex];
            Array.Copy(src, srcIndex, dst, dstIndex, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear(int index)
        {
            Entities[index] = new Entity(-1, -1);
            
            foreach (var typeInfo in TypeInfos)
            {
                if (typeInfo.IsZeroSized || typeInfo.Unmanaged)
                {
                    continue;
                }

                var arrayIndex = IndexLookup[typeInfo.TypeId];
                var array = ComponentArrays[arrayIndex];
                Array.Clear(array, index, 1);
            }
        }
    }
}