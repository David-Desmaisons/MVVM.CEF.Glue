﻿using Neutronium.Core.Infra.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neutronium.Core.Infra
{
    public static class TypeExtensions
    {
        private static readonly Type _NullableType = typeof(Nullable<>);
        private static readonly Type _EnumerableType = typeof(IEnumerable<>);
        private static readonly Type _UInt16Type = typeof(UInt16);
        private static readonly Type _UInt32Type = typeof(UInt32);
        private static readonly Type _UInt64Type = typeof(UInt64);

        public static IEnumerable<Type> GetBaseTypes(this Type type) 
        {
            if (type == null) throw new ArgumentNullException();
            yield return type;

            while ((type = type.BaseType) != null)
            {
                yield return type;
            }
        }

        public static Type GetEnumerableBase(this Type type)
        {
            if (type == null)
                return null;

            if (type.IsArray)
                return type.GetElementType();

            if (!type.IsGenericType)
                return null;

            if (type.GetGenericTypeDefinition() == _EnumerableType)
                return type.GetGenericArguments()[0];

            return type.GetInterfaces()
                .FirstOrDefault(@interface => @interface.IsGenericType &&
                                              @interface.GetGenericTypeDefinition() == _EnumerableType)
                ?.GetGenericArguments()[0];
        }

        public static Type GetUnderlyingNullableType(this Type type)
        {
            if (type == null)
                return null;

            if (!type.IsGenericType)
                return null;

            return type.GetGenericTypeDefinition() == _NullableType ? type.GetGenericArguments()[0] : null;
        }

        public static Type GetUnderlyingType(this Type type) => GetUnderlyingNullableType(type) ?? type;

        public static bool IsUnsigned(this Type targetType) 
        {
            return (targetType != null) && ((targetType == _UInt16Type) || (targetType == _UInt32Type) || (targetType == _UInt64Type));
        }

        private static readonly ConcurrentDictionary<Type, TypePropertyAccessor> _TypePropertyInfos = new ConcurrentDictionary<Type, TypePropertyAccessor>();
        internal static TypePropertyAccessor GetTypePropertyInfo(this Type @type)
        {
            return _TypePropertyInfos.GetOrAdd(@type, TypePropertyAccessor.FromType);
        }
    }
}