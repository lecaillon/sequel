using System.Linq;
using FluentAssertions;
using Sequel.Core.Parser;
using Xunit;
using static Sequel.Tests.TestContext;

namespace Sequel.Tests
{
    public class SplitterTests
    {
        [Fact]
        public void Should_split_simple_sql_into_statements()
        {
            // Arrange
            string sql0 = "select * from foo;\r\n";
            string sql1 = "select * from foo where bar = 'foo;bar'";

            // Act
            var statements = new Splitter().Process(sql0 + sql1);
            
            // Assert
            statements.Count.Should().Be(2);
            statements[0].ToString().Should().Be(sql0);
            statements[0].NeedsSemicolon.Should().BeFalse();
            statements[1].ToString().Should().Be(sql1);
            statements[1].NeedsSemicolon.Should().BeTrue();
        }

        [Fact]
        public void Should_split_dropif_sql_into_statements()
        {
            // Arrange
            string sql0 = "DROP TABLE IF EXISTS FOO;\n\n";
            string sql1 = "SELECT * FROM BAR;";

            // Act
            var statements = new Splitter().Process(sql0 + sql1);

            // Assert
            statements.Count.Should().Be(2);
            statements[0].ToString().Should().Be(sql0);
            statements[0].NeedsSemicolon.Should().BeFalse();
            statements[1].ToString().Should().Be(sql1);
            statements[1].NeedsSemicolon.Should().BeFalse();
        }

        [Theory]
        [InlineData("function.sql")]
        [InlineData("function_pgsql.sql")]
        [InlineData("function_pgsql2.sql")]
        [InlineData("function_pgsql3.sql")]
        [InlineData("function_pgsql4.sql")]
        [InlineData("dashcomment.sql", 3)]
        [InlineData("begintag.sql", 3)]
        [InlineData("begintag2.sql")]
        [InlineData("casewhen_procedure.sql", 2)]
        [InlineData("mysql_handler.sql", 2)]
        public void Should_split_script_into_statements(string file, int stmtCount = 1)
        {
            // Arrange
            string sql = ReadFile(file);

            // Act
            var statements = new Splitter().Process(sql);

            // Assert
            statements.Count.Should().Be(stmtCount);
            string.Join("", statements.Select(x => x.ToString())).Should().Be(sql);
            statements.All(x => x.NeedsSemicolon == false).Should().BeTrue();
        }
    }
}
