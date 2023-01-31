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
    public sealed class CommandBuffer
    {
        internal struct RemoveComponentCommand
        {
            public Entity Entity;
            public int TypeId;
        }
        
        internal struct AddComponentCommand
        {
            public Entity Entity;
            public int TypeId;
            public int ValueIndex;
        }
        
        internal World World;
        internal int ThreadId;
        internal List<Entity> ToDestroy = new();
        internal List<EntityToCreate> ToCreate = new();
        internal List<RemoveComponentCommand> ToRemove = new();
        internal List<AddComponentCommand> ToAdd = new();
        
        internal IComponentList?[] Components;
        
        internal CommandBuffer(World world, int threadId)
        {
            World = world;
            ThreadId = threadId;
            Components = new IComponentList[World.ComponentTypes.Length];
        }
        
        public FutureEntity CreateEntity()
        {
            var id = ToCreate.Count;
            ToCreate.Add(new EntityToCreate(id));
            return new FutureEntity(id);
        }
        
        public void DestroyEntity(Entity entity)
        {
            ToDestroy.Add(entity);
        }
        
        public void RemoveComponent<T>(Entity entity) where T : IEntityComponent
        {
            var typeId = World.ComponentTypes.TypeInfosMap[typeof(T)].TypeId;
            ToRemove.Add(new RemoveComponentCommand
            {
                Entity = entity,
                TypeId = typeId,
            });
        }
        
        public void AddComponent<T>(Entity entity, T value) where T : unmanaged, IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            DebugHelper.Assert(typeInfo.Unmanaged);

            if (typeInfo.IsZeroSized)
            {
                ToAdd.Add(new AddComponentCommand()
                {
                    Entity = entity,
                    TypeId = typeInfo.TypeId,
                    ValueIndex = -1,
                });
                return;
            }

            Components[typeInfo.TypeId] ??= new ValueComponentList<T>(typeInfo);
            var listOfT = (ValueComponentList<T>)Components[typeInfo.TypeId]!;
            listOfT.AddComponent(value);
            var valueIndex = listOfT.Count - 1;
            
            ToAdd.Add(new AddComponentCommand()
            {
                Entity = entity,
                TypeId = typeInfo.TypeId,
                ValueIndex = valueIndex,
            });
        }
        
        public void AddManagedComponent<T>(Entity entity, T value) where T : IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            DebugHelper.Assert(!typeInfo.Unmanaged);

            Components[typeInfo.TypeId] ??= new ComponentList<T>(typeInfo);
            var listOfT = (ComponentList<T>)Components[typeInfo.TypeId]!;
            listOfT.AddComponent(value);
            var valueIndex = listOfT.Count - 1;

            ToAdd.Add(new AddComponentCommand()
            {
                Entity = entity,
                TypeId = typeInfo.TypeId,
                ValueIndex = valueIndex,
            });
        }
        
        public void AddComponent<T>(Entity entity) where T : IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            ToAdd.Add(new AddComponentCommand()
            {
                Entity = entity,
                TypeId = typeInfo.TypeId,
                ValueIndex = -1,
            });
        }
        
        public void AddComponent<T>(FutureEntity entity, T value) where T : unmanaged, IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            DebugHelper.Assert(typeInfo.Unmanaged);

            if (typeInfo.IsZeroSized)
            {
                ToCreate[entity.Id].Values.Add((typeInfo.TypeId, -1));
                return;
            }

            Components[typeInfo.TypeId] ??= new ValueComponentList<T>(typeInfo);
            var listOfT = (ValueComponentList<T>)Components[typeInfo.TypeId]!;
            listOfT.AddComponent(value);
            var valueIndex = listOfT.Count - 1;

            ToCreate[entity.Id].Values.Add((typeInfo.TypeId, valueIndex));
        }
        
        public void AddComponent<T>(FutureEntity entity) where T : IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            ToCreate[entity.Id].Values.Add((typeInfo.TypeId, -1));
        }
        
        public void AddManagedComponent<T>(FutureEntity entity, T value) where T : IEntityComponent
        {
            var typeInfo = World.ComponentTypes.TypeInfosMap[typeof(T)];
            DebugHelper.Assert(!typeInfo.Unmanaged);

            Components[typeInfo.TypeId] ??= new ComponentList<T>(typeInfo);
            var listOfT = (ComponentList<T>)Components[typeInfo.TypeId]!;
            listOfT.AddComponent(value);
            var valueIndex = listOfT.Count - 1;
            
            ToCreate[entity.Id].Values.Add((typeInfo.TypeId, valueIndex));
        }

#region Group components
        
        public void AddGroupComponent<T>(Entity entity) where T : unmanaged, IGroupEntityComponent
        {
            throw new System.NotImplementedException();
        }
        
        public void AddGroupComponent<T>(Entity entity, T value) where T : unmanaged, IGroupEntityComponent
        {
            throw new System.NotImplementedException();
        }
        
        public void AddGroupComponent<T>(FutureEntity entity) where T : unmanaged, IGroupEntityComponent
        {
            throw new System.NotImplementedException();
        }
        
        public void AddGroupComponent<T>(FutureEntity entity, T value) where T : unmanaged, IGroupEntityComponent
        {
            throw new System.NotImplementedException();
        }
        
        public void SetGroupComponent<T>(Entity entity, T value) where T : unmanaged, IGroupEntityComponent
        {
            throw new System.NotImplementedException();
        }
        
        public void SetGroupComponent<T>(FutureEntity entity, T value) where T : unmanaged, IGroupEntityComponent
        {
            throw new System.NotImplementedException();
        }
        
#endregion
    }
}