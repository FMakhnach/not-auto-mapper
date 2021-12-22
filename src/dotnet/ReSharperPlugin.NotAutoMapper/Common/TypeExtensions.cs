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
        public static IEnumerable<IProperty> CollectAllReadableProperties([NotNull] this ITypeElement type)
        {
            var result = type.CollectTypeReadableProperties();
            var superTypes = type.GetSuperTypeElements();

            while (superTypes.Count != 0)
            {
                var superTypesProperties = superTypes.SelectMany(CollectTypeReadableProperties);
                // Add super type properties
                result = superTypesProperties.Concat(result);
                // Going up in hierarchy
                superTypes = superTypes.SelectMany(st => st.GetSuperTypeElements()).ToList();
            }

            return result;
        }

        /// <summary>
        /// Returns all properties of type which have set/init, considering inheritance
        /// </summary>
        [NotNull]
        public static IEnumerable<IProperty> CollectAllInitializableProperties([NotNull] this ITypeElement type)
        {
            var result = type.CollectTypeInitializableProperties();
            var superTypes = type.GetSuperTypeElements();

            while (superTypes.Count != 0)
            {
                var superTypesProperties = superTypes.SelectMany(CollectTypeInitializableProperties);
                // Add super type properties
                result = superTypesProperties.Concat(result);
                // Going up in hierarchy
                superTypes = superTypes.SelectMany(st => st.GetSuperTypeElements()).ToList();
            }

            return result;
        }

        /// <summary>
        /// Returns all properties of type which have get
        /// </summary>
        [NotNull]
        private static IEnumerable<IProperty> CollectTypeReadableProperties([NotNull] this ITypeElement type)
        {
            return type.Properties.Where(p =>
                p.IsReadable
                && p.TryGetAccessor(AccessorKind.GETTER)?.GetAccessRights() == AccessRights.PUBLIC);
        }

        /// <summary>
        /// Returns all properties of type which have set/init
        /// </summary>
        [NotNull]
        private static IEnumerable<IProperty> CollectTypeInitializableProperties([NotNull] this ITypeElement type)
        {
            return type.Properties.Where(p =>
                p.IsWritable
                && p.TryGetAccessor(AccessorKind.SETTER)?.GetAccessRights() == AccessRights.PUBLIC);
        }
    }
}