using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NBean.QueryParser
{

    public static class UrlQueryParser
    {
        const string ExpressionTokenPattern =
            @"(\[?\w+\]?)[:]\b(EQ|NE|GT|GE|LT|LE|LIKE|BETWEEN|NOTBETWEEN|IN|NOTIN|ISNULL|ISNOTNULL)\b(\{(.*?)\})?";

        private const string ExpressionTokenPatternForIsNull =
            @"(\[?\w+\]?)[:]\b(ISNULL|ISNOTNULL)\b";

        const string OrderTokenPattern = @"(\[?\w+\]?[:]?\b(ASC|DESC)\b)";

        const string LogicOpsPattern = @"(\[\b(AND|OR|NOT)\b\])";

        const string BracesPattern = @"(\[\(\])|(\[\)\])";

        private const string MultipleSpacesPattern = @"\s+";

        const string ExpressionOpsReplacement =
            "EQ:=|NE:<>|GT:>|GE:>=|LT:<|LE:<=|LIKE:LIKE|NOTLIKE:NOT LIKE|BETWEEN:BETWEEN|" +
            "NOTBETWEEN:NOT BETWEEN|IN:IN|NOTIN:NOT IN|ISNULL:IS NULL|ISNOTNULL:IS NOT NULL";

        const string FilterOpsReplacement = @"\[AND\]:AND|\[OR\]:OR|\[NOT\]:NOT|\[\(\]:(|\[\)\]:)";


        internal static string SanitizeUrlQuery(string query)
        {
            return
                string.Join(" ",
                    Regex.Matches(query.Trim(),
                            $"{ExpressionTokenPattern}|{ExpressionTokenPatternForIsNull}|{LogicOpsPattern}|{BracesPattern}")
                        .Cast<Match>()
                        .Where(qt => !IsAlwaysTrueEqExpression(qt.Groups[1].Value, qt.Groups[4].Value))
                        .SkipWhile(qt => "[AND]|[OR]|[NOT]|[)]".Contains(qt.Value))
                        .Reverse<Match>()
                        .SkipWhile(qt => "[AND]|[OR]|[NOT]|[(]".Contains(qt.Value))
                        .Reverse<Match>()
                        .Select(qt => qt.Value)
                        .ToArray()
                );
        }


        internal static string ParseTermOperator(string termOperator)
        {
            return ExpressionOpsReplacement
                .Split('|')
                .ToList()
                .FirstOrDefault(o => o.StartsWith(termOperator))?
                .Split(':')[1];
        }


        private static object[] ParseStringParams(IEnumerable<string> stringParams, bool useExtendedTypes = false)
        {
            return stringParams
                .Select(value => useExtendedTypes 
                    ? value.GetTypeAndValueEx().Item2 
                    : value.GetTypeAndValue().Item2)
                .ToArray();
        }


        private static bool IsAlwaysTrueEqExpression(string fieldName, string fieldValue)
        {
            return fieldName.IsNumeric() && fieldValue.IsNumeric() && fieldName == fieldValue;
        }


        internal static Dictionary<string, QueryExpression> TokenizeQueryTerms(string query, 
            bool useExtendedTypes = false)
        {
            query = SanitizeUrlQuery(query.Trim());

            var result = new Dictionary<string, QueryExpression>();

            var terms = Regex.Matches(query, $"{ExpressionTokenPattern}|{ExpressionTokenPatternForIsNull}");

            if (terms.Count == 0)
                return result;

            var paramIndex = 0;

            foreach (Match term in terms)
            {
                var fieldName = term.Groups[1].Success ? term.Groups[1].Value : null;
                var comparator = term.Groups[2].Success ? term.Groups[2].Value.ToUpper() : null;
                var fieldValue = term.Groups[4].Success ? term.Groups[4].Value : null;

                //if (fieldName != null && comparator != null)
                //{
                    if (IsAlwaysTrueEqExpression(fieldName, fieldValue))
                        continue;

                    var queryExpression = new QueryExpression()
                    {
                        Expression = $"{fieldName} {ParseTermOperator(comparator)}",
                        Parameters = new object[] { }
                    };

                    if (fieldValue != null)
                    {
                        switch (comparator)
                        {
                            case "BETWEEN":
                            case "NOTBETWEEN":
                                var betweenValues = fieldValue.Split(',');

                                if (betweenValues.Length == 2)
                                {
                                    queryExpression.Expression = $"{queryExpression.Expression} {{{paramIndex++}}} AND {{{paramIndex++}}}";
                                    queryExpression.Parameters = ParseStringParams(betweenValues);
                                }
                                else
                                {
                                    throw new Exception($"Error in (NOT) BETWEEN Condition: {term.Value}");
                                }
                                break;
                            case "IN":
                            case "NOTIN":
                                if (fieldValue.Trim() != string.Empty)
                                {
                                    var inValues = fieldValue.Split(',');

                                    queryExpression.Expression =
                                        $"{queryExpression.Expression} " +
                                        $"({string.Join(",", inValues.Select(value => $"{{{paramIndex++}}}").ToArray())})";

                                    queryExpression.Parameters = ParseStringParams(inValues, useExtendedTypes);
                                }
                                else
                                {
                                    throw new Exception($"Error in (NOT) IN Condition: {term.Value}");
                                }
                                break;
                            default:
                                queryExpression.Expression = $"{queryExpression.Expression} {{{paramIndex++}}}";
                                queryExpression.Parameters = new[]
                                {
                                    useExtendedTypes 
                                        ? fieldValue.GetTypeAndValueEx().Item2
                                        : fieldValue.GetTypeAndValue().Item2
                                };
                                break;
                        }
                    }

                    if (!result.ContainsKey(term.Value))
                    {
                        result.Add(term.Value, queryExpression);
                    }
                //}
                //else
                //{
                //    throw new Exception($"Incomplete Query Term: {term.Value}");
                //}
            }

            return result;
        }


        private static string ReplaceQueryOperators(string query)
        {
            foreach (var opReplacement in FilterOpsReplacement.Split('|'))
            {
                var splitOp = opReplacement.Split(':');
                query = Regex.Replace(query, splitOp[0], splitOp[1]);
            }

            return query;
        }


        public static string ParseOrder(string urlOrder)
        {
            var orderTokens = Regex.Matches(urlOrder, OrderTokenPattern);

            if (orderTokens.Count <= 1)
                return string.Empty;

            return string.Join(", ", orderTokens.Cast<Match>()
                    .Select(m => m.Value.Replace(":", " "))
                    .ToArray()); ;
        }


        public static Tuple<string, object[]> ParseQuery(string urlQuery,
            bool useExtendedTypes = false)
        {
            var queryTokens = TokenizeQueryTerms(urlQuery, useExtendedTypes);

            if (!queryTokens.Any())
                return Tuple.Create(string.Empty, new object[0]);

            var sqlQueryTerm = Regex.Replace(ReplaceQueryOperators(
                SanitizeUrlQuery(urlQuery)), MultipleSpacesPattern, " ");
            
            var parameters = new List<object>();

            foreach (var queryToken in queryTokens)
            {
                sqlQueryTerm = sqlQueryTerm.Replace(queryToken.Key, queryToken.Value.Expression);

                if (queryToken.Value.Parameters.Any())
                {
                    parameters.AddRange(queryToken.Value.Parameters);
                }
            }

            sqlQueryTerm = Regex.Replace(sqlQueryTerm, ExpressionTokenPattern, "");

            return Tuple.Create(sqlQueryTerm, parameters.ToArray()
            );
        }

    }

}
