using System;
using System.Text.Json;
using Xunit;

namespace NBean.Tests
{
    public class UrlQueryParserTests
    {
        private string testOrder = "Foo:ASC, [Bar]:DESC, Baz";
        private string resSqlOrder = "Foo ASC, [Bar] DESC";

        private string testInfectedOrder = "Foo:ASC, [Bar]:DESC, Baz; DROP DATABASE; DROP TABLE USERS;";
        private string resSqlInfectedOrder = "Foo ASC, [Bar] DESC";

        private string testQueryEqNeIsNull = "[(]Foo:EQ{Bar} [AND] Baz:NE{12}[)] [OR] Zap:ISNULL";
        private string resSqlQueryEqNeIsNull = "( Foo = {0} AND Baz <> {1} ) OR Zap IS NULL";
        private string resSqlParamsEqNeIsNull = "[\"Bar\",12]";

        private string resQueryTokensEqNeIsNull =
            "{\"Foo:EQ{Bar}\":{\"Expression\":\"Foo = {0}\",\"Parameters\":[\"Bar\"]}," +
            "\"Baz:NE{12}\":{\"Expression\":\"Baz \\u003C\\u003E {1}\",\"Parameters\":[12]}," +
            "\"Zap:ISNULL\":{\"Expression\":\"Zap IS NULL\",\"Parameters\":[]}}";

        private string testQueryGtLtIsNotNull = "Foo:GT{18} [AND] Bar:LT{70} [AND] Baz:ISNOTNULL; INSERT INTO USERS VALUES(99999, 'ADMIN', '');";
        private string testSqlQueryGtLtIsNotNull = "Foo > {0} AND Bar < {1} AND Baz IS NOT NULL";
        private string resSqlParamsGtLtIsNotNull = "[18,70]";

        private string resQueryTokensGtLtIsNotNull =
            "{\"Foo:GT{18}\":{\"Expression\":\"Foo \\u003E {0}\",\"Parameters\":[18]}," +
            "\"Bar:LT{70}\":{\"Expression\":\"Bar \\u003C {1}\",\"Parameters\":[70]}," +
            "\"Baz:ISNOTNULL\":{\"Expression\":\"Baz IS NOT NULL\",\"Parameters\":[]}}";

        private string testQueryGeLeLike = "[(]Foo:GE{18}[)] [AND] [(][Bar]:LE{70}[)] [AND] [NOT] [(]Baz:LIKE{Bang%}[)]";
        private string testSqlQueryGeLeLike = "( Foo >= {0} ) AND ( [Bar] <= {1} ) AND NOT ( Baz LIKE {2} )";
        private string resSqlParamsGeLeLike = "[18,70,\"Bang%\"]";

        private string resQueryTokensGeLeLike =
            "{\"Foo:GE{18}\":{\"Expression\":\"Foo \\u003E= {0}\",\"Parameters\":[18]}," +
            "\"[Bar]:LE{70}\":{\"Expression\":\"[Bar] \\u003C= {1}\",\"Parameters\":[70]}," +
            "\"Baz:LIKE{Bang%}\":{\"Expression\":\"Baz LIKE {2}\",\"Parameters\":[\"Bang%\"]}}";

        private string testQueryBetweenNotIn = "[Foo]:BETWEEN{18,70} [AND] Bar:NOTIN{Baz,Bang,Bong}";
        private string testSqlQueryBetweenNotIn = "[Foo] BETWEEN {0} AND {1} AND Bar NOT IN ({2},{3},{4})";
        private string testSqlParamsBetweenNotIn = "[18,70,\"Baz\",\"Bang\",\"Bong\"]";

        private string resQueryTokensBetweenNotIn =
            "{\"[Foo]:BETWEEN{18,70}\":{\"Expression\":\"[Foo] BETWEEN {0} AND {1}\",\"Parameters\":[18,70]}," +
            "\"Bar:NOTIN{Baz,Bang,Bong}\":{\"Expression\":\"Bar NOT IN ({2},{3},{4})\",\"Parameters\":[\"Baz\",\"Bang\",\"Bong\"]}}";

        private string testQueryNotBetweenIn = "[Foo]:NOTBETWEEN{18,70} [AND] Bar:IN{Baz,Bang,Bong}";
        private string testSqlQueryNotBetweenIn = "[Foo] NOT BETWEEN {0} AND {1} AND Bar IN ({2},{3},{4})";
        private string testSqlParamsNotBetweenIn = "[18,70,\"Baz\",\"Bang\",\"Bong\"]";

        private string resQueryTokensNotBetweenIn =
            "{\"[Foo]:NOTBETWEEN{18,70}\":{\"Expression\":\"[Foo] NOT BETWEEN {0} AND {1}\",\"Parameters\":[18,70]}," +
            "\"Bar:IN{Baz,Bang,Bong}\":{\"Expression\":\"Bar IN ({2},{3},{4})\",\"Parameters\":[\"Baz\",\"Bang\",\"Bong\"]}}";

        private string testInfectedQuery = "[AND] [)] [OR] 99:EQ{99} [Foo]:NOTBETWEEN{18,70}; DROP TABLE USERS; [AND] Bar:IN{Baz,Bang,Bong} [OR] 1:EQ{1} [(] [AND]";
        private string testSanitizedQuery = "[Foo]:NOTBETWEEN{18,70} [AND] Bar:IN{Baz,Bang,Bong}";
        private string testSqlInfectedQuery = "[Foo] NOT BETWEEN {0} AND {1} AND Bar IN ({2},{3},{4})";
        private string testSqlParamsInfectedQuery = "[18,70,\"Baz\",\"Bang\",\"Bong\"]";

        private string resTokensInfectedQuery =
            "{\"[Foo]:NOTBETWEEN{18,70}\":{\"Expression\":\"[Foo] NOT BETWEEN {0} AND {1}\",\"Parameters\":[18,70]}," +
            "\"Bar:IN{Baz,Bang,Bong}\":{\"Expression\":\"Bar IN ({2},{3},{4})\",\"Parameters\":[\"Baz\",\"Bang\",\"Bong\"]}}";


        [Fact]
        public void ParsesTermOperator()
        {
            Assert.Equal("=", UrlQueryParser.ParseTermOperator("EQ"));
            Assert.Equal("<>", UrlQueryParser.ParseTermOperator("NE"));
            Assert.Equal(">", UrlQueryParser.ParseTermOperator("GT"));
            Assert.Equal(">=", UrlQueryParser.ParseTermOperator("GE"));
            Assert.Equal("<", UrlQueryParser.ParseTermOperator("LT"));
            Assert.Equal("<=", UrlQueryParser.ParseTermOperator("LE"));
            Assert.Equal("LIKE", UrlQueryParser.ParseTermOperator("LIKE"));
            Assert.Equal("NOT LIKE", UrlQueryParser.ParseTermOperator("NOTLIKE"));
            Assert.Equal("BETWEEN", UrlQueryParser.ParseTermOperator("BETWEEN"));
            Assert.Equal("NOT BETWEEN", UrlQueryParser.ParseTermOperator("NOTBETWEEN"));
            Assert.Equal("IN", UrlQueryParser.ParseTermOperator("IN"));
            Assert.Equal("NOT IN", UrlQueryParser.ParseTermOperator("NOTIN"));
            Assert.Equal("IS NULL", UrlQueryParser.ParseTermOperator("ISNULL"));
            Assert.Equal("IS NOT NULL", UrlQueryParser.ParseTermOperator("ISNOTNULL"));
        }


        [Fact]
        public void ParsesOrder()
        {
            Assert.Equal(resSqlOrder, UrlQueryParser.ParseOrder(testOrder));
            Assert.Equal(resSqlInfectedOrder, UrlQueryParser.ParseOrder(testInfectedOrder));
        }


        [Fact]
        public void SanitizesQuery()
        {
            Assert.Equal(testSanitizedQuery, UrlQueryParser.SanitizeUrlQuery(testInfectedQuery));
        }


        [Fact]
        public void TokenizesQuery()
        {
            Assert.Equal(resQueryTokensEqNeIsNull, 
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryEqNeIsNull)));
            Assert.Equal(resQueryTokensGtLtIsNotNull, 
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryGtLtIsNotNull)));
            Assert.Equal(resQueryTokensGeLeLike, 
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryGeLeLike)));
            Assert.Equal(resQueryTokensBetweenNotIn, 
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryBetweenNotIn)));
            Assert.Equal(resQueryTokensNotBetweenIn, 
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryNotBetweenIn)));
            Assert.Equal(resTokensInfectedQuery, 
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testInfectedQuery)));
        }


        [Fact]
        public void TokenizesQueryWithExtendedTypes()
        {
            Assert.Equal(resQueryTokensEqNeIsNull,
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryEqNeIsNull, true)));
            Assert.Equal(resQueryTokensGtLtIsNotNull,
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryGtLtIsNotNull, true)));
            Assert.Equal(resQueryTokensGeLeLike,
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryGeLeLike, true)));
            Assert.Equal(resQueryTokensBetweenNotIn,
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryBetweenNotIn, true)));
            Assert.Equal(resQueryTokensNotBetweenIn,
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testQueryNotBetweenIn, true)));
            Assert.Equal(resTokensInfectedQuery,
                JsonSerializer.Serialize(UrlQueryParser.TokenizeQueryTerms(testInfectedQuery, true)));
        }


        private void AssertValidQueryTerm(string query, string resultSqlQuery, 
            string resultSqlParams, bool useExtendedTypes = false)
        {
            var (sqlQuery, sqlParams) = UrlQueryParser.ParseQuery(query, useExtendedTypes);

            Assert.Equal(resultSqlQuery, sqlQuery);
            Assert.Equal(resultSqlParams, JsonSerializer.Serialize(sqlParams));
        }


        [Fact]
        public void ParsesQuery()
        {
            AssertValidQueryTerm(testQueryEqNeIsNull, resSqlQueryEqNeIsNull, 
                resSqlParamsEqNeIsNull);
            AssertValidQueryTerm(testQueryGtLtIsNotNull, testSqlQueryGtLtIsNotNull, 
                resSqlParamsGtLtIsNotNull);
            AssertValidQueryTerm(testQueryGeLeLike, testSqlQueryGeLeLike, 
                resSqlParamsGeLeLike);
            AssertValidQueryTerm(testQueryBetweenNotIn, testSqlQueryBetweenNotIn, 
                testSqlParamsBetweenNotIn);
            AssertValidQueryTerm(testQueryNotBetweenIn, testSqlQueryNotBetweenIn, 
                testSqlParamsNotBetweenIn);
            AssertValidQueryTerm(testInfectedQuery, testSqlInfectedQuery, 
                testSqlParamsInfectedQuery);
        }


        [Fact]
        public void ParsesQueryWithExtendedTypes()
        {
            AssertValidQueryTerm(testQueryEqNeIsNull, resSqlQueryEqNeIsNull,
                resSqlParamsEqNeIsNull, true);
            AssertValidQueryTerm(testQueryGtLtIsNotNull, testSqlQueryGtLtIsNotNull,
                resSqlParamsGtLtIsNotNull, true);
            AssertValidQueryTerm(testQueryGeLeLike, testSqlQueryGeLeLike,
                resSqlParamsGeLeLike, true);
            AssertValidQueryTerm(testQueryBetweenNotIn, testSqlQueryBetweenNotIn,
                testSqlParamsBetweenNotIn, true);
            AssertValidQueryTerm(testQueryNotBetweenIn, testSqlQueryNotBetweenIn,
                testSqlParamsNotBetweenIn, true);
            AssertValidQueryTerm(testInfectedQuery, testSqlInfectedQuery,
                testSqlParamsInfectedQuery, true);
        }

    }

}
