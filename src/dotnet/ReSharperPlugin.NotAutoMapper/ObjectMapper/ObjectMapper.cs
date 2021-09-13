using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using JetBrains.Platform.MsBuildTask.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods.Queries;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace ReSharperPlugin.NotAutoMapper.ObjectMapper
{
    public static class ObjectMapper
    {
        public static Dictionary<IProperty, string> MapProperties(
            this ITypeElement resultType,
            TreeNodeCollection<ICSharpParameterDeclaration> parameters)
        {
            var result = resultType.Properties.ToDictionary(x => x, x => (string) null);

            var properties =
                parameters
                    .Select(p => new Candidate(p.Type, p.DeclaredName.ToLower(), p.DeclaredName))
                    .ToArray();

            FindMatches(result, properties, depth: 2);

            return result;
        }

        private static void FindMatches(
            Dictionary<IProperty, string> resultMap,
            IReadOnlyCollection<Candidate> candidates,
            int depth)
        {
            var candidateNames = candidates.ToDictionary(x => x.RelativeName);

            FindPerfectMatches(resultMap, candidates);
            //FindConvertableMatches(mapping, fromPropertiesMap);

            if (depth > 1 && resultMap.Count(x => x.Value is null) > 0)
            {
                var nextLevelProperties =
                    candidates.SelectMany(
                            candidate =>
                            {
                                var propertyType = candidate.Type.GetTypeElement();

                                return propertyType is null
                                    ? Array.Empty<Candidate>()
                                    : propertyType.Properties.Select(
                                        subProperty => new Candidate(
                                            subProperty.Type,
                                            candidate.RelativeName + subProperty.ShortName.ToLower(),
                                            $"{candidate.Path}.{subProperty.ShortName}"));
                            })
                        .ToArray();

                FindMatches(resultMap, nextLevelProperties, depth - 1);
            }
        }

        private static void FindPerfectMatches(
            Dictionary<IProperty, string> mapping,
            Dictionary<string, Candidate> propertiesMap)
        {
            var notUsedProperties = mapping.Keys.Where(property => mapping[property] == null).ToList();

            foreach (var targetProperty in notUsedProperties)
            {
                var perfectMatch = propertiesMap.TryGetValue(targetProperty.ShortName.ToLower(), out var candidate)
                                   && candidate.Type != null
                                   && candidate.Type.IsSubtypeOf(targetProperty.Type);

                if (perfectMatch)
                {
                    mapping[targetProperty] = candidate.Path;
                }
            }
        }

        private static void FindConvertableMatches(
            Dictionary<IProperty, string> mapping,
            Dictionary<string, Candidate> propertiesMap)
        {
            var notUsedProperties = mapping.Keys.Where(property => mapping[property] == null).ToList();

            foreach (var targetProperty in notUsedProperties)
            {
                var perfectMatch = propertiesMap.TryGetValue(targetProperty.ShortName, out var candidate);

                if (perfectMatch)
                {
                    var ext =
                        GetExtensionMethods(typeof(object).GetAssembly(), candidate.Type.GetType())
                            .Where(x => x.ReturnType == targetProperty.Type.GetType());

                    var meth = ext.FirstOrDefault();

                    if (meth != null)
                    {
                        mapping[targetProperty] = candidate.Path + meth.Name + "()";
                    }
                }

                ExtensionMethodsQuery kek;
            }
        }

        private static IEnumerable<MethodInfo> GetExtensionMethods(
            Assembly assembly,
            Type extendedType)
        {
            var query = from type in assembly.GetTypes()
                where type.IsSealed && !type.IsGenericType && !type.IsNested
                from method in type.GetMethods(
                    BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic)
                where method.IsDefined(typeof(ExtensionAttribute), false)
                where method.GetParameters()[0].ParameterType == extendedType
                select method;

            return query;
        }

        private class Candidate
        {
            public Candidate([NotNull] IType type, [NotNull] string relativeName, [NotNull] string path)
            {
                Type = type;
                RelativeName = relativeName;
                Path = path;
            }

            [CanBeNull] public IType Type { get; }

            /// <summary>
            /// Lower case concatenation of path elements
            /// </summary>
            [NotNull]
            public string RelativeName { get; }

            [NotNull] public string Path { get; }
        }
    }
}