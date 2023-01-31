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
using System.Collections.Generic;

namespace kv.Entities
{
    public struct EntityQuery
    {
        public struct Entry
        {
            internal Chunk Chunk;
            internal int IndexInChunk;

            public ref T GetComponent<T>() where T : unmanaged, IEntityComponent
            {
                return ref Chunk.GetComponent<T>(IndexInChunk);
            }
            
            public ref T GetManagedComponent<T>() where T : IEntityComponent
            {
                return ref Chunk.GetManagedComponent<T>(IndexInChunk);
            }
        }

        private World _world;
        private BitMask _all, _any, _none, _group;
        private List<Chunk> _chunks;

        public IReadOnlyList<Chunk> Chunks => _chunks;
        public int Count { get; private set; }

        internal EntityQuery(World world, BitMask all, BitMask any, BitMask none, BitMask group)
        {
            _world = world;
            _all = all;
            _any = any;
            _none = none;
            _group = group;
            _chunks = new List<Chunk>();
            Count = 0;
        }
        
        public EntityQuery WithAll<TComponent>() where TComponent : IEntityComponent
        {
            var typeInfo = _world.ComponentTypes.TypeInfosMap[typeof(TComponent)];
            _all[typeInfo.TypeId] = true;
            return this;
        }
        
        public EntityQuery WithAny<TComponent>() where TComponent : IEntityComponent
        {
            var typeInfo = _world.ComponentTypes.TypeInfosMap[typeof(TComponent)];
            _any[typeInfo.TypeId] = true;
            return this;
        }
        
        public EntityQuery WithNone<TComponent>() where TComponent : IEntityComponent
        {
            var typeInfo = _world.ComponentTypes.TypeInfosMap[typeof(TComponent)];
            _none[typeInfo.TypeId] = true;
            return this;
        }
        
        public EntityQuery WithGroupComponent<TGroupComponent>(TGroupComponent value) where TGroupComponent : unmanaged, IGroupEntityComponent
        {
            var typeInfo = _world.ComponentTypes.TypeInfosMap[typeof(TGroupComponent)];
            _group[typeInfo.TypeId] = true;
            return this;
        }

        public EntityQuery Update()
        {
            _chunks.Clear();
            Count = 0;
            foreach (var group in _world.EntityRepo.Groups)
            {
                if (!Test(group.BitMask))
                {
                    continue;
                }
                foreach (var groupChunk in group.Chunks)
                {
                    if (groupChunk.Chunk.Count == 0)
                    {
                        continue;
                    }
                    _chunks.Add(groupChunk.Chunk);
                    Count += groupChunk.Chunk.Count;
                }
            }

            return this;
        }

        public IEnumerable<Entry> GetEntities()
        {
            foreach (var chunk in _chunks)
            {
                for (var indexInChunk = 0; indexInChunk < chunk.Count; indexInChunk++)
                {
                    yield return new Entry
                    {
                        Chunk = chunk,
                        IndexInChunk = indexInChunk,
                    };
                }
            }
        }
        
        private bool Test(BitMask mask)
        {
            if (mask.Buffer.Length != _all.Buffer.Length)
            {
                return false;
            }

            var any = 0UL;
            var foundAny = false;
            
            for (var i = 0; i < mask.Buffer.Length; i++)
            {
                var targetValue = mask.Buffer.Span[i];
                
                var noneValue = _none.Buffer.Span[i];
                if ((noneValue & targetValue) > 0)
                {
                    return false;
                }

                var allValue = _all.Buffer.Span[i];
                if ((allValue & targetValue) != allValue)
                {
                    return false;
                }

                var anyValue = _any.Buffer.Span[i];
                any |= anyValue;
                if (!foundAny)
                {
                    foundAny = (anyValue & targetValue) > 0;
                }
            }

            return any == 0 || foundAny;
        }
    }
}