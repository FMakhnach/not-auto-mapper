using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;

namespace ReSharperPlugin.NotAutoMapper.Common
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Returns all properties of type which have set/init, considering inheritance
        /// </summary>
        [NotNull]
        public static IEnumerable<IProperty> CollectAllInitializableProperties([NotNull] this IType type)
        {
            var result = type.CollectTypeInitializableProperties();
            var superTypes = type.GetTypeElement()?.GetSuperTypes() ?? System.Array.Empty<IDeclaredType>();

            while (superTypes.Count != 0)
            {
                var superTypesProperties = superTypes.SelectMany(CollectTypeInitializableProperties);
                // Add super type properties
                result = result.Concat(superTypesProperties);
                // Going up in hierarchy
                superTypes = superTypes.SelectMany(st => st.GetSuperTypes()).ToList();
            }

            return result;
        }

        /// <summary>
        /// Returns all properties of type which have set/init
        /// </summary>
        [NotNull]
        public static IEnumerable<IProperty> CollectTypeInitializableProperties([NotNull] this IType type)
        {
            return type
                       .GetTypeElement()
                       ?.Properties
                       .Where(p => p.IsWritable)
                   ?? System.Array.Empty<IProperty>();
        }
    }
}