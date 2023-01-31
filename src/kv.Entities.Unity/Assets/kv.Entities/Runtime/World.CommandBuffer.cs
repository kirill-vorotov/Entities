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
using System.Threading;

namespace kv.Entities
{
    public sealed partial class World
    {
        internal ConcurrentDictionary<int, CommandBuffer> CommandBuffers = new();
        
        public CommandBuffer GetCommandBuffer()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            
            if (!CommandBuffers.TryGetValue(currentThreadId, out var buffer))
            {
                buffer = new CommandBuffer(this, currentThreadId);
                CommandBuffers[currentThreadId] = buffer;
            }
            
            return buffer;
        }
        
        public void ExecuteCommandBuffer(ref CommandBuffer? commandBuffer)
        {
            if (commandBuffer is null)
            {
                return;
            }
            
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            DebugHelper.Assert(commandBuffer.ThreadId == currentThreadId);
            CommandBuffers.TryRemove(commandBuffer.ThreadId, out _);
            
            DestroyEntities(commandBuffer.ToDestroy);
            CreateEntities(commandBuffer.ToCreate, commandBuffer.Components);
            UpdateEntities(commandBuffer.ToRemove, commandBuffer.ToAdd, commandBuffer.Components);
            
            UpdateCachedQueries();

            commandBuffer = default;
        }
        
        public void ExecuteCommandBuffers()
        {
            foreach (var commandBuffer in CommandBuffers.Values)
            {
                DestroyEntities(commandBuffer.ToDestroy);
                CreateEntities(commandBuffer.ToCreate, commandBuffer.Components);
                UpdateEntities(commandBuffer.ToRemove, commandBuffer.ToAdd, commandBuffer.Components);
            }
            
            CommandBuffers.Clear();
            
            UpdateCachedQueries();
        }
        
        private void DestroyEntities(List<Entity> toDestroy)
        {
            foreach (var entity in toDestroy)
            {
                EntityRepo.DestroyEntity(entity);
            }
        }

        private void CreateEntities(List<EntityToCreate> toCreate, IComponentList?[] components)
        {
            var bitMaskBufferLength = BitMask.GetBitMaskBufferLength(ComponentTypes.Length);
            var bitMask = new BitMask(new ulong[bitMaskBufferLength]);

            Group? group = default;

            foreach (var entityToCreate in toCreate)
            {
                bitMask.SetAll(false);

                foreach (var value in entityToCreate.Values)
                {
                    bitMask[value.typeId] = true;
                }

                if (bitMask.IsZero())
                {
                    continue;
                }

                if (group is null || !group.BitMask.Equals(bitMask))
                {
                    group = EntityRepo.FindOrCreateGroup(bitMask);
                }

                var entity = EntityRepo.CreateEntity(group, out var chunk, out var indexInChunk);

                foreach (var value in entityToCreate.Values)
                {
                    var srcList = components[value.typeId];
                    if (value.index == -1 || srcList is null)
                    {
                        continue;
                    }

                    var typeInfo = srcList.TypeInfo;

                    if (typeInfo.IsZeroSized)
                    {
                        continue;
                    }
                    
                    if (typeInfo.Unmanaged)
                    {
                        var span = srcList.GetBuffer(value.index);
                        chunk.CopyComponentFrom(typeInfo, span, indexInChunk);
                    }
                    else
                    {
                        chunk.CopyComponentFrom(typeInfo, srcList.GetArray(), value.index, indexInChunk);
                    }
                }
            }
        }

        private void UpdateEntities(List<CommandBuffer.RemoveComponentCommand> toRemove,
            List<CommandBuffer.AddComponentCommand> toAdd, IComponentList?[] components)
        {
            var bitMaskBufferLength = BitMask.GetBitMaskBufferLength(ComponentTypes.Length);
            var bitMaskPool = new ulong[(toRemove.Count + toAdd.Count) * bitMaskBufferLength];
            var occupied = 0;
            
            List<EntityToUpdate> toUpdate = new(toRemove.Count + toAdd.Count);
            var toUpdateIndices = new int[EntityRepo.EntityInfos.Count];
            toUpdateIndices.AsSpan().Fill(-1);

            Group? group = default;
            foreach (var command in toRemove)
            {
                DebugHelper.Assert(EntityRepo.IsValid(command.Entity));

                var entityInfo = EntityRepo.EntityInfos[command.Entity.Id];
                if (group is null || group.GroupId != entityInfo.GroupId)
                {
                    group = EntityRepo.Groups[entityInfo.GroupId];
                }
                
                var index = toUpdateIndices[command.Entity.Id];
                EntityToUpdate entityToUpdate;
                
                if (index == -1)
                {
                    var mask = new BitMask(bitMaskPool.AsMemory(occupied, bitMaskBufferLength));
                    mask.SetAll(group.BitMask);
                    occupied += bitMaskBufferLength;
                    
                    entityToUpdate = new EntityToUpdate
                    {
                        Entity = command.Entity,
                        BitMask = mask,
                        Values = new List<(int typeId, int index)>()
                    };
                    
                    toUpdate.Add(entityToUpdate);
                    index = toUpdate.Count - 1;
                    toUpdateIndices[command.Entity.Id] = index;
                }
                else
                {
                    entityToUpdate = toUpdate[index];
                }

                entityToUpdate.BitMask[command.TypeId] = false;
                toUpdate[index] = entityToUpdate;
            }

            group = default;
            foreach (var command in toAdd)
            {
                DebugHelper.Assert(EntityRepo.IsValid(command.Entity));
                
                var entityInfo = EntityRepo.EntityInfos[command.Entity.Id];
                if (group is null || group.GroupId != entityInfo.GroupId)
                {
                    group = EntityRepo.Groups[entityInfo.GroupId];
                }
                
                var index = toUpdateIndices[command.Entity.Id];
                EntityToUpdate entityToUpdate;
                
                if (index == -1)
                {
                    var mask = new BitMask(bitMaskPool.AsMemory(occupied, bitMaskBufferLength));
                    mask.SetAll(group.BitMask);
                    occupied += bitMaskBufferLength;
                    
                    entityToUpdate = new EntityToUpdate
                    {
                        Entity = command.Entity,
                        BitMask = mask,
                        Values = new List<(int typeId, int index)>()
                    };
                    
                    toUpdate.Add(entityToUpdate);
                    index = toUpdate.Count - 1;
                    toUpdateIndices[command.Entity.Id] = index;
                }
                else
                {
                    entityToUpdate = toUpdate[index];
                }

                entityToUpdate.BitMask[command.TypeId] = true;
                if (command.ValueIndex >= 0)
                {
                    entityToUpdate.Values.Add((command.TypeId, command.ValueIndex));
                }
                toUpdate[index] = entityToUpdate;
            }

            group = default;
            foreach (var entityToUpdate in toUpdate)
            {
                var entity = entityToUpdate.Entity;
                if (entityToUpdate.BitMask.IsZero())
                {
                    EntityRepo.DestroyEntity(entity);
                    continue;
                }
                
                if (group is null || !group.BitMask.Equals(entityToUpdate.BitMask))
                {
                    group = EntityRepo.FindOrCreateGroup(entityToUpdate.BitMask);
                }
                
                EntityRepo.MoveEntity(entity, group, out var chunk, out var indexInChunk);
                
                foreach (var value in entityToUpdate.Values)
                {
                    var srcList = components[value.typeId];
                    if (value.index == -1 || srcList is null)
                    {
                        continue;
                    }

                    var typeInfo = srcList.TypeInfo;

                    if (typeInfo.IsZeroSized)
                    {
                        continue;
                    }
                    
                    if (typeInfo.Unmanaged)
                    {
                        var span = srcList.GetBuffer(value.index);
                        chunk.CopyComponentFrom(typeInfo, span, indexInChunk);
                    }
                    else
                    {
                        chunk.CopyComponentFrom(typeInfo, srcList.GetArray(), value.index, indexInChunk);
                    }
                }
            }
        }
    }
}