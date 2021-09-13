using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace ReSharperPlugin.NotAutoMapper.EnumMapper
{
    public static class EnumMapper
    {
        public static Dictionary<IField, IField> Map(IEnum resultEnum, IEnum fromEnum)
        {
            var resultFieldsDict = resultEnum.EnumMembers.ToDictionary(x => x.ShortName.ToLower(), x => x);
            var result = fromEnum.EnumMembers.ToDictionary(
                x => x,
                x => resultFieldsDict.TryGetValue(x.ShortName.ToLower(), out var field)
                    ? field
                    : ClosestOrDefault(resultFieldsDict, x));

            return result;
        }

        private static IField ClosestOrDefault(Dictionary<string, IField> fieldsDict, IField field)
        {
            var fieldShortNameLower = field.ShortName.ToLower();
            var result = fieldsDict.TakeMin(pair => StringSimilarityMetric(pair.Key, fieldShortNameLower)).Value;

            if (result != null && AreCloseEnough(result.ShortName.ToLower(), fieldShortNameLower))
            {
                return result;
            }

            return default;
        }

        private static int StringSimilarityMetric(string s1, string s2)
        {
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