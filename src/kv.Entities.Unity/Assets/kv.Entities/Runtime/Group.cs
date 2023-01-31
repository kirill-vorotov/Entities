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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace kv.Entities
{
    public sealed class Group
    {
        private int _lastFreedChunkId = -1;
        private int _lastAvailableChunkId = -1;

        internal World World;
        internal int GroupId;
        internal BitMask BitMask;
        internal readonly int BufferSize;
        internal readonly int Capacity;
        
        public List<ChunkInfo> ChunkInfos = new();
        public List<(Chunk Chunk, int NextChunk)> Chunks = new();

        internal TypeInfo[] TypeInfos;
        internal readonly int[] IndexLookup;
        internal readonly int[] Offsets;
        
        internal byte[] GroupCmponentBuffer;

        internal unsafe Group(World world, int groupId, BitMask bitMask, int bufferSize, TypeInfo[] allComponentTypes)
        {
            World = world;
            GroupId = groupId;
            BitMask = bitMask;
            BufferSize = bufferSize;

            var popCount = BitMask.PopCount();
            TypeInfos = new TypeInfo[popCount];
            Offsets = new int[popCount];
            IndexLookup = new int[allComponentTypes.Length];

            GroupCmponentBuffer = Array.Empty<byte>();

            var sumSize = 0;
            var maxAlignment = 0;
            var sumAlignment = 0;
            var count = 0;
            for (var i = 0; i < allComponentTypes.Length; i++)
            {
                var typeInfo = allComponentTypes[i];
                if (!BitMask[i])
                {
                    IndexLookup[i] = -1;
                    continue;
                }

                TypeInfos[count] = typeInfo;
                IndexLookup[i] = count;
                
                if (typeInfo is { Unmanaged: true, IsZeroSized: false })
                {
                    sumSize += typeInfo.Size;
                    sumAlignment += typeInfo.MemoryAlignment;
                    if (typeInfo.MemoryAlignment > maxAlignment)
                    {
                        maxAlignment = typeInfo.MemoryAlignment;
                    }
                }
                count++;
            }

            if (sumSize > 0)
            {
                Capacity = (bufferSize - sumAlignment) / sumSize;
                var offset = 0;
                for (var i = 0; i < Offsets.Length; i++)
                {
                    var typeInfo = TypeInfos[i];
                    if (!typeInfo.Unmanaged || typeInfo.IsZeroSized)
                    {
                        continue;
                    }

                    var size = typeInfo.Size * Capacity;
                    var alignment = typeInfo.MemoryAlignment;

                    if (offset % alignment > 0)
                    {
                        var tmp = (offset / alignment) + 1;
                        offset = tmp * alignment;
                    }

                    Offsets[i] = offset;

                    offset += size;
                }
            }
            else
            {
                Capacity = bufferSize / sizeof(IntPtr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Chunk GetLastAvailableChunk()
        {
            var chunkId = _lastAvailableChunkId;
            if (chunkId == -1)
            {
                CreateChunk(out chunkId);
            }

            var chunkInfo = ChunkInfos[chunkId];
            var chunk = Chunks[chunkInfo.IndexInGroup].Chunk;
            return chunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateChunk(out int chunkId)
        {
            chunkId = -1;
            var indexInGroup = Chunks.Count;
            
            if (_lastFreedChunkId == -1)
            {
                chunkId = ChunkInfos.Count;
                
                ChunkInfos.Add(new ChunkInfo
                {
                    ChunkId = chunkId,
                    IndexInGroup = indexInGroup
                });
            }
            else
            {
                chunkId = _lastFreedChunkId;
                _lastFreedChunkId = ChunkInfos[_lastFreedChunkId].ChunkId;
                ChunkInfos[chunkId] = new ChunkInfo
                {
                    ChunkId = chunkId,
                    IndexInGroup = indexInGroup
                };
            }

            var chunk = new Chunk(World, chunkId, TypeInfos, IndexLookup, Offsets, BufferSize, Capacity);
            Chunks.Add((chunk, _lastAvailableChunkId));
            _lastAvailableChunkId = chunkId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkChunkAvailalbe(int chunkId)
        {
            var chunkinfo = ChunkInfos[chunkId];
            var entry = Chunks[chunkinfo.IndexInGroup];
            
            entry.NextChunk = _lastAvailableChunkId;
            _lastAvailableChunkId = chunkId;
            
            Chunks[chunkinfo.IndexInGroup] = entry;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkChunkFull(int chunkId)
        {
            DebugHelper.Assert(chunkId == _lastAvailableChunkId);
            
            var chunkinfo = ChunkInfos[chunkId];
            var entry = Chunks[chunkinfo.IndexInGroup];

            _lastAvailableChunkId = entry.NextChunk;
            entry.NextChunk = -1;

            Chunks[chunkinfo.IndexInGroup] = entry;
        }

        public T GetGroupComponent<T>() where T : unmanaged, IGroupEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            DebugHelper.Assert(typeInfo.GroupComponent);

            var index = IndexLookup[typeInfo.TypeId];
            var offset = Offsets[index];
            var span = GroupCmponentBuffer.AsSpan(offset, typeInfo.Size);
            return MemoryMarshal.Cast<byte, T>(span)[0];
        }
    }
}