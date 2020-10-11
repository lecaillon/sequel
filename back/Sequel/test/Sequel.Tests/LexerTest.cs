using FluentAssertions;
using Sequel.Core.Parser;
using Xunit;

namespace Sequel.Tests
{
    public class LexerTest
    {
        [Theory]
        [InlineData(TokenType.Assignment, " :=")]
        [InlineData(TokenType.Command, @" \pgsql")]
        [InlineData(TokenType.Comment, " --comment1\r\n")]
        [InlineData(TokenType.CommentHint, " --+8\r\n")]
        [InlineData(TokenType.CommentMultiline, " /* comment1 \r\n comment2 */")]
        [InlineData(TokenType.Comparison, " NOT IN")]
        [InlineData(TokenType.Keyword, " FROM")]
        [InlineData(TokenType.Keyword, " LEFT JOIN")]
        [InlineData(TokenType.Keyword, " END IF")]
        [InlineData(TokenType.Keyword, " NOT NULL")]
        [InlineData(TokenType.Keyword, " NULLS LAST")]
        [InlineData(TokenType.Keyword, " UNION ALL")]
        [InlineData(TokenType.Keyword, " GROUP BY")]
        [InlineData(TokenType.Keyword, " ORDER BY")]
        [InlineData(TokenType.Keyword, " HANDLER FOR")]
        [InlineData(TokenType.Keyword, " LATERAL VIEW")]
        [InlineData(TokenType.Keyword, " INLINE")]
        [InlineData(TokenType.KeywordDDL, " CREATE OR REPLACE")]
        [InlineData(TokenType.KeywordTZCast, " AT TIME ZONE 'Central European Standard Time'")]
        [InlineData(TokenType.Name, " `pg_constraint`")]
        [InlineData(TokenType.Name, " ´pg_constraint´")]
        [InlineData(TokenType.Name, " @X1")]
        [InlineData(TokenType.Name, "public.", "public", 0, 0)]
        [InlineData(TokenType.Name, ".v_table_constraints", "v_table_constraints")]
        [InlineData(TokenType.Name, "COUNT(", "COUNT", 0, 0)]
        [InlineData(TokenType.NamePlaceholder, " ?")]
        [InlineData(TokenType.NameBuiltin, " DOUBLE PRECISION")]
        [InlineData(TokenType.Newline, " \r\n")]
        [InlineData(TokenType.NumberFloat, " 12.07E-2")]
        [InlineData(TokenType.NumberFloat, " 9.21")]
        [InlineData(TokenType.NumberInteger, " 75")]
        [InlineData(TokenType.NumberHexadecimal, " 0x23")]
        [InlineData(TokenType.Literal, " $$\r\nDECLARE\r\nEND;\r\n$$")]
        [InlineData(TokenType.Operator, " ||")]
        [InlineData(TokenType.OperatorComparison, " NOT LIKE")]
        [InlineData(TokenType.OperatorComparison, " =")]
        [InlineData(TokenType.Punctuation, " ;")]
        [InlineData(TokenType.Punctuation, " ::")]
        [InlineData(TokenType.StringSingle, " 'Sequel'")]
        [InlineData(TokenType.StringSymbol, " \"Sequel\"")]
        [InlineData(TokenType.Whitespace, "/* comment1 comment2 */   ", "   ", 23)]
        [InlineData(TokenType.Wildcard, " *")]
        [InlineData(TokenType.KeywordDML, " insert")]
        public void Should_get_tokens(TokenType tokenType, string sql, string? expected = null, int position = 1, int index = 1)
        {
            var tokens = Lexer.GetTokens(sql);
            tokens.Count.Should().Be(2);
            tokens[index].Should().BeEquivalentTo(new Token(tokenType, expected ?? sql.Substring(1), position));
        }
    }
}
