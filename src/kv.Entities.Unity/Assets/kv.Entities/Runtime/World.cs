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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace kv.Entities
{
    public sealed partial class World
    {
        internal EntityRepo EntityRepo;
        internal Types ComponentTypes;
        
        public ConcurrentDictionary<Type, EntityQuery> CachedQueries = new();
        
        public World(ComponentTypeCollection componentTypeCollection)
        {
            ComponentTypes = new Types(componentTypeCollection);
            EntityRepo = new EntityRepo(this, ComponentTypes);
        }

        public IEnumerable<Entity> GetEntities(BitMask mask)
        {
            var group = EntityRepo.Groups.FirstOrDefault(g => g.BitMask.Equals(mask));
            if (group is null)
            {
                yield break;
            }

            foreach (var groupChunk in group.Chunks)
            {
                for (var i = 0; i < groupChunk.Chunk.Count; i++)
                {
                    yield return groupChunk.Chunk.Entities[i];
                }
            }
        }

        public EntityQuery CreateEntityQuery()
        {
            var bufferLength = BitMask.GetBitMaskBufferLength(ComponentTypes.Length);
            return new EntityQuery(this, new BitMask(new ulong[bufferLength]),
                new BitMask(new ulong[bufferLength]), new BitMask(new ulong[bufferLength]),
                new BitMask(new ulong[bufferLength]));
        }

        public void UpdateCachedQueries()
        {
            foreach (var cachedQuery in CachedQueries.Values)
            {
                cachedQuery.Update();
            }
        }
    }
}