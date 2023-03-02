// Copyright 2013-2015 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System;
using System.Collections.Generic;
using System.Reflection;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Serilog.Capturing
{

    static class GetablePropertyFinder
    {
#if NET5_0_OR_GREATER
    internal static IEnumerable<PropertyInfo> GetPropertiesRecursive(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] this Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(
            p => (p.Name != "Item" || p.GetIndexParameters().Length == 0));
    }
#else
        internal static IEnumerable<PropertyInfo> GetPropertiesRecursive(this Type type)
        {
            var seenNames = new HashSet<string>();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (!property.CanRead)
                {
                    continue;
                }

                if (seenNames.Contains(property.Name))
                {
                    continue;
                }

                if (property.Name == "Item" &&
                    property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                seenNames.Add(property.Name);
                yield return property;
            }
        }
#endif
    }

}

