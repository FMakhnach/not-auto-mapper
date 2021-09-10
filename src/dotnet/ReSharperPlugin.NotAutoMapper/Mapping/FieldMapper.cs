using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace ReSharperPlugin.NotAutoMapper.Mapping
{
    public static class FieldMapper
    {
        public static Dictionary<ITypeMember, ITypeMember> MapOnto(
            this IEnumerable<ITypeMember> fromFields,
            IEnumerable<ITypeMember> toFields)
        {
            var toFieldsDict = toFields.ToDictionary(x => x.ShortName.ToLower(), x => x);
            var result = fromFields.ToDictionary(
                x => x,
                x => toFieldsDict.TryGetValue(x.ShortName.ToLower(), out var field)
                    ? field
                    : ClosestOrDefault(toFieldsDict, x));

            return result;
        }

        private static ITypeMember ClosestOrDefault(Dictionary<string, ITypeMember> fieldsDict, ITypeMember field)
        {
            var fieldShortNameLower = field.ShortName.ToLower();
            var result = fieldsDict.TakeMin(pair => StringSimilarityMetric(pair.Key, fieldShortNameLower)).Value;


            if (AreCloseEnough(result.ShortName.ToLower(), fieldShortNameLower))
            {
                return result;
            }

            return default;
        }

        private static int StringSimilarityMetric(string s1, string s2)
        {
            if (s1.Contains(s2) || s2.Contains(s1))
            {
                return 0;
            }

            return Fastenshtein.Levenshtein.Distance(s1, s2) - Math.Abs(s1.Length - s2.Length);
        }

        /// <summary>
        /// Whether we accept given strings as similar enough to map one onto other (heuristic).
        /// </summary>
        private static bool AreCloseEnough(string s1, string s2)
        {
            var distance = StringSimilarityMetric(s1, s2);

            const double minLengthCriticalPercent = 0.5;

            return distance <= Math.Min(s1.Length, s2.Length) * minLengthCriticalPercent;
        }
    }
}