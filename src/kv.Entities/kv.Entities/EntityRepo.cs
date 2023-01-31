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
using System.Linq;
using System.Runtime.CompilerServices;

namespace kv.Entities
{
    internal sealed class EntityRepo
    {
        private int _lastDestroyedEntityId = -1;

        public World World;
        public Types Types;
        public List<EntityInfo> EntityInfos = new();
        public List<Group> Groups = new();

        internal EntityRepo(World world, Types types)
        {
            World = world;
            Types = types;
        }
        
        internal const int DefaultBufferSize = 1024 * 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Group FindOrCreateGroup(BitMask bitMask)
        {
            var group = Groups.FirstOrDefault(g => g.BitMask.Equals(bitMask));
            if (group is null)
            {
                var groupId = Groups.Count;
                group = new Group(World, groupId, new BitMask(bitMask), DefaultBufferSize, Types.TypeInfos);
                Groups.Add(group);
            }

            return group;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid(Entity entity)
        {
            return entity is { Id: >= 0, Version: >= 0 } && EntityInfos[entity.Id].Id == entity.Id && EntityInfos[entity.Id].Version == entity.Version;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity CreateEntity(Group group, out Chunk chunk, out int indexInChunk)
        {
            chunk = group.GetLastAvailableChunk();

            indexInChunk = -1;
            if (_lastDestroyedEntityId == -1)
            {
                var entity = new Entity(EntityInfos.Count, 0);
                indexInChunk = chunk.Count;
                chunk.Entities[chunk.Count++] = entity;
                
                EntityInfos.Add(new EntityInfo(entity.Id, entity.Version, group.GroupId, chunk.ChunkId, indexInChunk));
                
                if (chunk.Count == chunk.Capacity)
                {
                    group.MarkChunkFull(chunk.ChunkId);
                }
                return entity;
            }
            else
            {
                var entity = new Entity(_lastDestroyedEntityId, EntityInfos[_lastDestroyedEntityId].Version);
                indexInChunk = chunk.Count;
                chunk.Entities[chunk.Count++] = entity;
                
                _lastDestroyedEntityId = EntityInfos[_lastDestroyedEntityId].Id;
                EntityInfos[entity.Id] = new EntityInfo(entity.Id, entity.Version, group.GroupId, chunk.ChunkId, indexInChunk);
                
                if (chunk.Count == chunk.Capacity)
                {
                    group.MarkChunkFull(chunk.ChunkId);
                }
                return entity;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(Entity entity)
        {
            DebugHelper.Assert(IsValid(entity));
            
            var destroyedEntityInfo = EntityInfos[entity.Id];
            var group = Groups[destroyedEntityInfo.GroupId];
            var chunkInfo = group.ChunkInfos[destroyedEntityInfo.ChunkId];
            var chunk = group.Chunks[chunkInfo.IndexInGroup].Chunk;

            if (destroyedEntityInfo.IndexInChunk < chunk.Count - 1)
            {
                var lastEntityInChunk = chunk.Entities[chunk.Count - 1];
                chunk.RemoveAt(destroyedEntityInfo.IndexInChunk);
                EntityInfos[lastEntityInChunk.Id] = new EntityInfo(lastEntityInChunk.Id, lastEntityInChunk.Version, group.GroupId, chunk.ChunkId, destroyedEntityInfo.IndexInChunk);
            }
            else
            {
                chunk.RemoveAt(chunk.Count - 1);
            }
            
            var newVersion = EntityInfos[entity.Id].Version + 1;
            EntityInfos[entity.Id] = new EntityInfo(_lastDestroyedEntityId, newVersion, -1, -1, -1);
            _lastDestroyedEntityId = entity.Id;

            if (chunk.Count * 3 == chunk.Capacity * 2)
            {
                group.MarkChunkAvailalbe(chunkInfo.ChunkId);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveEntity(Entity entity, Group targetGroup, out Chunk dstChunk, out int dstIndexInChunk)
        {
            DebugHelper.Assert(IsValid(entity));
            
            var entityInfo = EntityInfos[entity.Id];
            var srcGroup = Groups[entityInfo.GroupId];
            var srcChunkInfo = srcGroup.ChunkInfos[entityInfo.ChunkId];
            var srcChunk = srcGroup.Chunks[srcChunkInfo.IndexInGroup].Chunk;

            dstChunk = targetGroup.GetLastAvailableChunk();
            dstIndexInChunk = dstChunk.Count;
            dstChunk.CopyFrom(srcChunk, entityInfo.IndexInChunk, dstIndexInChunk);
            dstChunk.Count++;
            EntityInfos[entity.Id] = new EntityInfo(entity.Id, entity.Version, targetGroup.GroupId, dstChunk.ChunkId, dstIndexInChunk);
            if (dstChunk.Count == dstChunk.Capacity)
            {
                targetGroup.MarkChunkFull(dstChunk.ChunkId);
            }

            if (entityInfo.IndexInChunk < srcChunk.Count - 1)
            {
                var lastEntityInChunk = srcChunk.Entities[srcChunk.Count - 1];
                srcChunk.RemoveAt(entityInfo.IndexInChunk);
                EntityInfos[lastEntityInChunk.Id] = new EntityInfo(lastEntityInChunk.Id, lastEntityInChunk.Version, srcGroup.GroupId, srcChunk.ChunkId, entityInfo.IndexInChunk);
            }
            else
            {
                srcChunk.RemoveAt(srcChunk.Count - 1);
            }

            if (srcChunk.Count * 3 == srcChunk.Capacity * 2)
            {
                srcGroup.MarkChunkAvailalbe(srcChunk.ChunkId);
            }
        }
    }
}