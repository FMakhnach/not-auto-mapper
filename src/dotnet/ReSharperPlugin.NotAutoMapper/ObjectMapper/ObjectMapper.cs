using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Features.Navigation.Core.Search.SearchRequests;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;
using ReSharperPlugin.NotAutoMapper.Common;

namespace ReSharperPlugin.NotAutoMapper.ObjectMapper
{
    public class ObjectMapper
    {
        private readonly ISolution _solution;
        private readonly IPsiModule _psiModule;

        public ObjectMapper(ISolution solution, IPsiModule psiModule)
        {
            _solution = solution;
            _psiModule = psiModule;
        }

        public IDictionary<IProperty, string> MapProperties(
            ITypeElement resultType,
            TreeNodeCollection<ICSharpParameterDeclaration> parameters)
        {
            const int searchDepth = 3;

            var properties =
                parameters
                    .Select(p => new PropertyInfo(p.Type, p.DeclaredName, Array.Empty<string>()))
                    .ToList();

            var result = FindMatches(resultType.Properties.ToArray(), properties, searchDepth);

            return result;
        }

        private Dictionary<IProperty, string> FindMatches(
            IReadOnlyCollection<IProperty> targetProperties,
            List<PropertyInfo> candidates,
            int depth)
        {
            var result = targetProperties.ToDictionary(x => x, x => (string)null);
            candidates = CollectDeeperCandidates(candidates, depth);

            foreach (var property in targetProperties)
            {
                var foundPerfect = false;
                var assignableProperties = new List<(PropertyInfo Property, PropertyComparisonResult ComparisonInfo)>();

                foreach (var candidate in candidates)
                {
                    var comparisonResult = candidate.CompareTo(property, _solution, _psiModule);

                    if (comparisonResult.IsAssignable || comparisonResult.ExtensionConversionMethods?.Count > 0)
                    {
                        if (comparisonResult.NamesMatch)
                        {
                            foundPerfect = true;
                            result[property] = candidate.GetPath();
                            if (comparisonResult.ExtensionConversionMethods?.Count > 0)
                            {
                                // TODO: change extension method choice logic
                                result[property] += "." + comparisonResult.ExtensionConversionMethods.First().ShortName + "()";
                            }

                            break;
                        }

                        if (comparisonResult.AreNamesSimilar)
                        {
                            assignableProperties.Add((candidate, comparisonResult));
                        }
                    }
                }

                if (!foundPerfect && assignableProperties.Count > 0)
                {
                    var (propertyInfo, comparisonInfo) = assignableProperties
                        .OrderBy(x => x.ComparisonInfo.NamesLevenshteinDistance)
                        .First();

                    var propertyPath = propertyInfo.GetPath();
                    if (comparisonInfo.ExtensionConversionMethods?.Count > 0)
                    {
                        // TODO: change extension method choice logic
                        propertyPath += "." + comparisonInfo.ExtensionConversionMethods.First().ShortName + "()";
                    }

                    result[property] = propertyPath;
                }
            }

            return result;
        }

        private static List<PropertyInfo> CollectDeeperCandidates(List<PropertyInfo> candidates, int depth)
        {
            var result = new List<PropertyInfo>(candidates);
            var prevLevelCandidates = candidates;

            for (int i = 0; i < depth; i++)
            {
                var nextLevelProperties = prevLevelCandidates
                    .SelectMany(candidate => candidate.Type
                        .CollectAllInitializableProperties()
                        .Select(subProperty => new PropertyInfo(subProperty, candidate)))
                    .ToList();

                result.AddRange(nextLevelProperties);
                prevLevelCandidates = nextLevelProperties;
            }

            return result;
        }

        private class PropertyInfo
        {
            [NotNull]
            public IType Type { get; }

            [NotNull]
            private string Name { get; }

            [NotNull]
            private IReadOnlyCollection<string> Path { get; }

            public PropertyInfo([NotNull] IType type, [NotNull] string name, [NotNull] IReadOnlyCollection<string> path)
            {
                Type = type;
                Name = name;
                Path = path;
            }

            public PropertyInfo([NotNull] IProperty property, [NotNull] PropertyInfo parent)
            {
                Type = property.Type;
                Name = property.ShortName;
                var path = new List<string>(parent.Path) { parent.Name };
                Path = path;
            }

            public string GetPath()
            {
                return string.Join("", Path.Select(x => x + ".")) + Name;
            }

            public PropertyComparisonResult CompareTo(IProperty target, ISolution solution, IPsiModule psiModule)
            {
                var targetNameLower = target.ShortName.ToLower();

                var namesMatch = false;
                var namesCloseEnough = false;
                var bestLevenshteinDistance = int.MaxValue;
                foreach (var name in GetPossibleNamesLower())
                {
                    if (name == targetNameLower)
                    {
                        namesMatch = true;
                        bestLevenshteinDistance = 0;
                    }

                    bestLevenshteinDistance = Math.Min(bestLevenshteinDistance, Fastenshtein.Levenshtein.Distance(name, targetNameLower));
                    namesCloseEnough |= AreCloseEnough(name, targetNameLower);
                }

                var isAssignable = Type.IsSubtypeOf(target.Type)
                                   || psiModule.GetTypeConversionRule().IsImplicitlyConvertibleTo(Type, target.Type);

                IReadOnlyCollection<IMethod> extensionConversionMethods = null;
                if (!isAssignable && namesCloseEnough)
                {
                    var extensionMethodsSearchRequest = new ExtensionMethodsSearchRequest(
                        Type,
                        solution,
                        null,
                        target.PresentationLanguage,
                        psiModule
                    );
                    var occurrences = extensionMethodsSearchRequest.Search() ?? Array.Empty<IOccurrence>();

                    var kek = occurrences
                        .Select(x => x.As<DeclaredElementOccurrence>().DisplayElement.GetValidDeclaredElement().As<IMethod>());

                    extensionConversionMethods = kek
                        .Where(x => target.Type.IsSubtypeOf(x.ReturnType)
                                    || psiModule.GetTypeConversionRule().IsImplicitlyConvertibleTo(target.Type, x.ReturnType))
                        .ToArray();
                }

                return new PropertyComparisonResult
                {
                    NamesMatch = namesMatch,
                    NamesLevenshteinDistance = bestLevenshteinDistance,
                    AreNamesSimilar = namesCloseEnough,
                    IsAssignable = isAssignable,
                    ExtensionConversionMethods = extensionConversionMethods
                };
            }

            private IEnumerable<string> GetPossibleNamesLower()
            {
                var name = Name.ToLower();
                yield return name;

                foreach (var part in Path.Reverse())
                {
                    name = part.ToLower() + name;
                    yield return name;
                }
            }

            /// <summary>
            /// Whether we accept given strings as similar enough to map one onto other (heuristic).
            /// </summary>
            private bool AreCloseEnough(string s1, string s2)
            {
                var distance = Fastenshtein.Levenshtein.Distance(s1, s2);

                const double minLengthCriticalPercent = 0.5;

                return distance <= Math.Min(s1.Length, s2.Length) * minLengthCriticalPercent;
            }
        }

        private class PropertyComparisonResult
        {
            /// <summary>
            /// Are names equal (case ignored)
            /// </summary>
            public bool NamesMatch { get; set; }

            /// <summary>
            /// Levenshtein distance between property names
            /// </summary>
            public int NamesLevenshteinDistance { get; set; }

            /// <summary>
            /// Should names considered similar
            /// </summary>
            public bool AreNamesSimilar { get; set; }

            /// <summary>
            /// Candidate can be assigned to target
            /// </summary>
            public bool IsAssignable { get; set; }

            /// <summary>
            /// Extension methods for convertation, if needed (and exist)
            /// </summary>
            [CanBeNull]
            public IReadOnlyCollection<IMethod> ExtensionConversionMethods { get; set; }
        }
    }
}