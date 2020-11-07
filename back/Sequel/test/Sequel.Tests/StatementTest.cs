using FluentAssertions;
using Sequel.Core.Parser;
using Xunit;
using static Sequel.Tests.TestContext;

namespace Sequel.Tests
{
    public class StatementTest
    {
        [Fact]
        public void Should_split_script_into_statements_with_codelens_line_number()
        {
            // Arrange
            string sql = ReadFile("start_line_number.sql");

            // Act
            var statements = new Splitter().Process(sql);

            // Assert
            statements.Count.Should().Be(5);
            statements[0].CodeLensLineNumber.Should().Be(3);
            statements[1].CodeLensLineNumber.Should().Be(5);
            statements[2].CodeLensLineNumber.Should().Be(6);
            statements[3].CodeLensLineNumber.Should().Be(6);
            statements[4].CodeLensLineNumber.Should().Be(10);
        }

        [Fact]
        public void Should_split_script_into_statements_and_get_range()
        {
            // Arrange
            string sql = ReadFile("start_line_number.sql");

            // Act
            var statements = new Splitter().Process(sql);

            // Assert
            statements.Count.Should().Be(5);
            statements[0].Range.Should().BeEquivalentTo(new Range { StartLineNumber = 1, StartColumn = 1, EndLineNumber = 3, EndColumn = 23 });
            statements[1].Range.Should().BeEquivalentTo(new Range { StartLineNumber = 4, StartColumn = 1, EndLineNumber = 5, EndColumn = 24 });
            statements[2].Range.Should().BeEquivalentTo(new Range { StartLineNumber = 6, StartColumn = 1, EndLineNumber = 6, EndColumn = 22 });
            statements[3].Range.Should().BeEquivalentTo(new Range { StartLineNumber = 6, StartColumn = 22, EndLineNumber = 6, EndColumn = 60 });
            statements[4].Range.Should().BeEquivalentTo(new Range { StartLineNumber = 7, StartColumn = 1, EndLineNumber = 14, EndColumn = 1 });
        }

        [Fact]
        public void Should_get_statement_at_position()
        {
            string sql = ReadFile("start_line_number.sql");
            var statements = new Splitter().Process(sql);

            statements.GetStatementAtPosition(lineNumber: 1, column: 1)!.Should().BeEquivalentTo(statements[0]);
            statements.GetStatementAtPosition(lineNumber: 3, column: 23)!.Should().BeEquivalentTo(statements[0]);
            statements.GetStatementAtPosition(lineNumber: 4, column: 20)!.Should().BeEquivalentTo(statements[1]);
            statements.GetStatementAtPosition(lineNumber: 6, column: 22)!.Should().BeEquivalentTo(statements[2]);
            statements.GetStatementAtPosition(lineNumber: 6, column: 29)!.Should().BeEquivalentTo(statements[3]);
            statements.GetStatementAtPosition(lineNumber: 12, column: 9)!.Should().BeEquivalentTo(statements[4]);
            statements.GetStatementAtPosition(lineNumber: 99, column: 99).Should().BeNull();
        }

        [Fact]
        public void Should_get_current_token_relative_to_position()
        {
            string sql = ReadFile("start_line_number.sql");
            var statements = new Splitter().Process(sql);

            statements.GetStatementAtPosition(lineNumber: 8, column: 6)!.GetCurrentToken()!.Type.Should().Be(TokenType.CommentMultiline);
            statements.GetStatementAtPosition(lineNumber: 12, column: 9)!.GetCurrentToken()!.Text.Should().Be(".");
            statements.GetStatementAtPosition(lineNumber: 10, column: 9)!.GetCurrentToken()!.Text.Should().Be("*");
            statements.GetStatementAtPosition(lineNumber: 10, column: 18)!.GetCurrentToken()!.Text.Should().Be("table5");
            statements.GetStatementAtPosition(lineNumber: 10, column: 23)!.GetCurrentToken()!.Text.Should().Be("t");
            statements.GetStatementAtPosition(lineNumber: 99, column: 99).Should().BeNull();
        }

        [Fact]
        public void Should_get_previous_token_relative_to_position()
        {
            string sql = ReadFile("start_line_number.sql");
            var statements = new Splitter().Process(sql);

            statements.GetStatementAtPosition(lineNumber: 6, column: 56)!.GetPreviousToken(skipMeaningless: true)!.Text.Should().Be("from");
            statements.GetStatementAtPosition(lineNumber: 6, column: 32)!.GetPreviousToken(skipMeaningless: true).Should().BeNull();
            statements.GetStatementAtPosition(lineNumber: 12, column: 10)!.GetPreviousToken(skipMeaningless: true)!.Text.Should().Be(".");
            statements.GetStatementAtPosition(lineNumber: 12, column: 4)!.GetPreviousToken(skipMeaningless: true)!.Text.Should().Be("t");
            statements.GetStatementAtPosition(lineNumber: 12, column: 4)!.GetPreviousToken(skipMeaningless: false)!.Text.Should().Be("-- comment\r\n");
            statements.GetStatementAtPosition(lineNumber: 1, column: 1)!.GetPreviousToken(skipMeaningless: false).Should().BeNull();
            statements.GetStatementAtPosition(lineNumber: 6, column: 8)!.GetPreviousToken(skipMeaningless: false)!.Text.Should().Be("select");
        }

        [Fact]
        public void Should_get_next_token_relative_to_position()
        {
            string sql = ReadFile("start_line_number.sql");
            var statements = new Splitter().Process(sql);

            statements.GetStatementAtPosition(lineNumber: 1, column: 1)!.GetNextToken(skipMeaningless: true)!.Text.Should().Be("select");
            statements.GetStatementAtPosition(lineNumber: 6, column: 56)!.GetNextToken(skipMeaningless: true)!.Text.Should().Be(";");
            statements.GetStatementAtPosition(lineNumber: 6, column: 60)!.GetNextToken(skipMeaningless: true).Should().BeNull();
            statements.GetStatementAtPosition(lineNumber: 8, column: 6)!.GetNextToken(skipMeaningless: true)!.Text.Should().Be("select");
        }

        [Theory]
        [InlineData(0, 0, "0")]
        [InlineData(0, 1, "01")]
        [InlineData(0, 2, "012")]
        [InlineData(1, 1, "1")]
        [InlineData(1, 2, "12")]
        [InlineData(2, 2, "2")]
        public void Should_get_a_valid_token_range_when_slice_tokens(int startIndex, int endIndex, string expextedText)
        {
            var tokens = new Statement
            {
                new Token(TokenType.Name, "0"),
                new Token(TokenType.Name, "1"),
                new Token(TokenType.Name, "2")
            };

            tokens.Slice(startIndex, endIndex).ToString().Should().Be(expextedText);
        }

        [Fact]
        public void Should_get_corresponding_schema_and_table_from_alias()
        {
            // Arrange
            string sql = ReadFile("alias_origin.sql");
            var statements = new Splitter().Process(sql);
            var statement = statements.GetStatementAtPosition(lineNumber: 10, column: 20)!;
            var alias = statement.GetCurrentToken()!;

            // Act
            var tableAlias = statement.GetTableAlias(alias)!;

            // Assert
            tableAlias.ToString().Should().Be("schema2.table2 as  y");
            tableAlias.Alias.Text.Should().Be("y");
            tableAlias.Schema.Should().Be("schema2");
            tableAlias.Table.Should().Be("table2");
            tableAlias.GetColumns().Should().BeEmpty();
        }

        [Fact]
        public void Should_get_corresponding_table_from_alias()
        {
            // Arrange
            string sql = ReadFile("alias_origin.sql");
            var statements = new Splitter().Process(sql);
            var statement = statements.GetStatementAtPosition(lineNumber: 11, column: 8)!;
            var alias = statement.GetCurrentToken()!;

            // Act
            var tableAlias = statement.GetTableAlias(alias)!;

            // Assert
            tableAlias.ToString().Should().Be("table1 x");
            tableAlias.Alias.Text.Should().Be("x");
            tableAlias.Schema.Should().BeNull();
            tableAlias.Table.Should().Be("table1");
            tableAlias.GetColumns().Should().BeEmpty();
        }

        [Fact]
        public void Should_get_corresponding_subquery_from_alias()
        {
            // Arrange
            string sql = ReadFile("alias_origin.sql");
            var statements = new Splitter().Process(sql);
            var statement = statements.GetStatementAtPosition(lineNumber: 10, column: 13)!;
            var alias = statement.GetCurrentToken()!;

            // Act
            var tableAlias = statement.GetTableAlias(alias)!;

            // Assert
            tableAlias.Alias.Text.Should().Be("z");
            tableAlias.Schema.Should().BeNull();
            tableAlias.Table.Should().BeNull();
            tableAlias.GetColumns().Should().BeEquivalentTo("id", "name", "total");
        }

        [Fact]
        public void Should_return_null_when_alias_origin_is_not_found()
        {
            // Arrange
            string sql = ReadFile("alias_origin.sql");
            var statements = new Splitter().Process(sql);
            var statement = statements.GetStatementAtPosition(lineNumber: 13, column: 5)!;
            var alias = statement.GetCurrentToken()!;

            // Act
            var tableAlias = statement.GetTableAlias(alias)!;

            // Assert
            tableAlias.Should().BeNull();
        }
    }
}
