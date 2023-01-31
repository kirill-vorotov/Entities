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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace kv.Entities
{
    public sealed class ComponentTypeCollection
    {
        internal List<Type> Types = new();
        internal List<TypeInfo> TypesInfos = new();
        internal int TypeIndex = 0;

        public ComponentTypeCollection RegisterType<T>() where T : IEntityComponent
        {
            if (Types.Contains(typeof(T)))
            {
                return this;
            }
            
            var type = typeof(T);
            Types.Add(type);
            
            var size = SizeOf<T>();
            var alignment = GetAlignment(typeof(T));
            var isBlittable = IsBlittable(typeof(T)); // Altho arrays of blittable types are blittable, we want to store such components in managed arrays.
            var isGroupComponent = typeof(IGroupEntityComponent).IsAssignableFrom(typeof(T));
            var typeInfo = new TypeInfo(TypeIndex++, typeof(T), size, alignment, isBlittable, isGroupComponent);
            TypesInfos.Add(typeInfo);
            
            return this;
        }
        
        private static unsafe int SizeOf<T>()
        {
            var type = typeof(T);

            if (type.IsPrimitive || type.IsValueType || type.IsEnum)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return fields.Length == 0 ? 0 : Unsafe.SizeOf<T>();
            }

            if (type.IsInterface || type.IsClass || type.IsArray || type.IsPointer)
            {
                return sizeof(IntPtr);
            }
            
            throw new ArgumentException($"{nameof(type)}");
        }

        public static bool IsBlittable(Type type)
        {
            if (type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            if (type.IsClass || type.IsInterface || type.IsArray)
            {
                return false;
            }

            if (type.IsValueType)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return fields.All(fieldInfo => IsBlittable(fieldInfo.FieldType));
            }

            return true;
        }
        
        public static unsafe int GetAlignment(Type type)
        {
            if (type.IsInterface)
            {
                return sizeof(IntPtr);
            }
            
            if (type.IsClass)
            {
                return sizeof(IntPtr);
            }
            
            if (type.IsArray)
            {
                return sizeof(IntPtr);
            }

            if (type.IsPointer)
            {
                if (type == typeof(IntPtr))
                {
                    return sizeof(IntPtr);
                }

                if (type == typeof(UIntPtr))
                {
                    return sizeof(UIntPtr);
                }
                
                return sizeof(IntPtr);
            }

            if (type.IsEnum)
            {
                return Marshal.SizeOf(Enum.GetUnderlyingType(type));
            }

            if (type.IsPrimitive)
            {
                return GetPrimitiveTypeSize(type);
            }
            
            if (type.IsValueType)
            {
                if (type == typeof(decimal))
                {
                    return sizeof(decimal);
                }
                
                if (type == typeof(nint))
                {
                    return sizeof(nint);
                }
            
                if (type == typeof(nuint))
                {
                    return sizeof(nuint);
                }
                
                var maxAlignment = 0;
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var fieldInfo in fields)
                {
                    var alignment = GetAlignment(fieldInfo.FieldType);
                    if (alignment > maxAlignment)
                    {
                        maxAlignment = alignment;
                    }
                }

                var structLayoutAttribute = type.StructLayoutAttribute;
                if (type.IsExplicitLayout && type.IsLayoutSequential && structLayoutAttribute != null)
                {
                    var packingSize = structLayoutAttribute.Pack;
                    if (packingSize > 0 && packingSize < maxAlignment)
                    {
                        maxAlignment = packingSize;
                    }
                }
                
                return maxAlignment;
            }

            throw new ArgumentException($"{nameof(type)}");
        }
        
        private static unsafe int GetPrimitiveTypeSize(Type type)
        {
            if (!type.IsPrimitive)
            {
                throw new ArgumentException($"{nameof(type)}");
            }

            if (type == typeof(IntPtr))
            {
                return sizeof(IntPtr);
            }
            
            if (type == typeof(UIntPtr))
            {
                return sizeof(UIntPtr);
            }
            
            if (type == typeof(bool))
            {
                return sizeof(bool);
            }

            if (type == typeof(char))
            {
                return sizeof(char);
            }

            if (type == typeof(byte))
            {
                return sizeof(byte);
            }

            if (type == typeof(sbyte))
            {
                return sizeof(sbyte);
            }

            if (type == typeof(short))
            {
                return sizeof(short);
            }

            if (type == typeof(ushort))
            {
                return sizeof(ushort);
            }

            if (type == typeof(int))
            {
                return sizeof(int);
            }

            if (type == typeof(uint))
            {
                return sizeof(uint);
            }

            if (type == typeof(long))
            {
                return sizeof(long);
            }

            if (type == typeof(float))
            {
                return sizeof(float);
            }

            if (type == typeof(double))
            {
                return sizeof(double);
            }
            
            throw new ArgumentException($"{nameof(type)}");
        }
    }
}