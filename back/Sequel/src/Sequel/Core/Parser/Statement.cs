using System;
using System.Collections.Generic;
using System.Linq;

namespace Sequel.Core.Parser
{
    public class TokenList : List<Token>
    {
        public TokenList() { }

        public TokenList(IEnumerable<Token> tokens) : base(tokens) { }

        public TokenList Slice(int startIndex, int endIndex) => new TokenList(this.Take(endIndex + 1).Skip(startIndex).ToList());

        /// <summary>
        ///     Returns the previous token relative to the given token.
        /// </summary>
        public Token? GetPreviousToken(Token? startAtToken, bool skipMeaningless, string? skipText = null)
            => Read(startAtToken, skipMeaningless, skipText, true);

        /// <summary>
        ///     Returns the next/previous token relative to the given token.
        /// </summary>
        public Token? GetNextToken(Token? startAtToken, bool skipMeaningless, string? skipText = null)
            => Read(startAtToken, skipMeaningless, skipText, false);

        private Token? Read(Token? startAtToken, bool skipMeaningless, string? skipText, bool reverse)
        {
            int limit = reverse ? 0 : Count - 1;
            int idx;
            if (startAtToken is null)
            {
                idx = reverse ? Count : -1;
            }
            else
            {
                idx = FindIndex(x => x == startAtToken);
                if (idx == limit)
                {
                    return null;
                }
            }

            do
            {
                idx = reverse ? idx - 1 : idx + 1;
                if ((skipMeaningless && this[idx].IsMeaningless) || this[idx].UpperText == skipText)
                {
                    continue;
                }

                return this[idx];
            } while (reverse ? idx > limit : idx < limit);

            return null;
        }

        public override string ToString() => string.Join("", this.Select(x => x.Text));
    }

    public class TableAlias : TokenList
    {
        public TableAlias(IEnumerable<Token> tokens) : base(tokens)
        {
        }

        public TableAlias(string table, string? schema, IEnumerable<Token> tokens) : base(tokens)
        {
            Schema = schema;
            Table = Check.NotNullOrEmpty(table, nameof(table));
        }

        public Token Alias => this.Last();
        public string? Schema { get; }
        public string? Table { get; }

        public List<string> GetColumns()
        {
            var columns = new List<string>();
            if (Table != null)
            {
                return columns;
            }

            Token? token = null;
            int depth = (Alias.Depth ?? 0) + 1;

            while (true)
            {
                token = GetNextToken(token, skipMeaningless: true);
                if (token is null || token.UpperText == "FROM")
                {
                    return columns;
                }

                if (token.Type == TokenType.Name && token.Depth == depth)
                {
                    var nextToken = GetNextToken(token, skipMeaningless: true);
                    if (nextToken != null
                     && nextToken.UpperText != "AS"
                     && nextToken.UpperText != "."
                     && nextToken.UpperText != "(")
                    {
                        columns.Add(token.Text);
                    }
                }
            }
        }
    }

    public class Statement : TokenList
    {
        public Statement() { }

        public Statement(IEnumerable<Token> tokens) : base(tokens) { }

        public int? CodeLensLineNumber => this.FirstOrDefault(x => x.HasMeaning)?.Range.StartLineNumber;

        public Range Range => Count == 0
            ? new Range()
            : new Range
            {
                StartLineNumber = this.First().Range.StartLineNumber,
                StartColumn = this.First().Range.StartColumn,
                EndLineNumber = this.Last().Range.EndLineNumber,
                EndColumn = this.Last().Range.EndColumn
            };

        /// <summary>
        ///     The statement needs a semicolon, if another statement is going to be concatenated to it.
        /// </summary>
        public bool NeedsSemicolon { get; set; }

        public TableAlias? GetTableAlias(Token? alias)
        {
            var origin = FindAliasOrigin(alias);
            if (origin is null)
            {
                return null;
            }

            var previousToken = GetPreviousToken(origin, skipMeaningless: true, skipText: "AS");
            if (previousToken is null)
            {
                return null;
            }

            if (previousToken.Type == TokenType.Name)
            { // Found table
                string table = previousToken.Text;
                // Search for a schema : schema.table
                var startAtToken = GetPreviousToken(previousToken, skipMeaningless: false);
                if (startAtToken != null && startAtToken.Text == ".")
                {
                    startAtToken = GetPreviousToken(startAtToken, skipMeaningless: false);
                    if (startAtToken != null && startAtToken.Type == TokenType.Name)
                    {
                        return new TableAlias(table, schema: startAtToken.Text, Slice(FindIndex(x => x == startAtToken), FindIndex(x => x == origin)));
                    }
                }
                return new TableAlias(table, schema: null, Slice(FindIndex(x => x == previousToken), FindIndex(x => x == origin)));
            }

            if (previousToken.IsCloseParenthesis)
            { // Found subquery
                Token? startAtToken = null;
                while (true)
                { // Search for the corresponding open parenthesis
                    startAtToken = GetPreviousToken(startAtToken ?? previousToken, skipMeaningless: true);
                    if (startAtToken is null)
                    {
                        return null;
                    }
                    if (startAtToken.IsOpenParenthesis && startAtToken.Depth == origin.Depth + 1)
                    {
                        return new TableAlias(Slice(FindIndex(x => x == startAtToken), FindIndex(x => x == origin)));
                    }
                }
            }

            return null;
        }

        private Token? FindAliasOrigin(Token? alias)
        {
            if (alias is null)
            {
                return null;
            }

            for (int i = 0; i < Count; i++)
            {
                var currentToken = this[i];

                if (currentToken.Depth == alias.Depth
                 && currentToken.UpperText == alias.UpperText
                 && GetPreviousToken(currentToken, skipMeaningless: false)?.IsMeaningless == true
                 && GetNextToken(currentToken, skipMeaningless: false)?.IsMeaningless == true)
                {
                    return currentToken;
                }
            }

            return null;
        }
    }

    public class StatementAtPosition : Statement
    {
        public StatementAtPosition(Statement statement, int lineNumber, int column) : base(statement)
        {
            LineNumber = Check.Positive(lineNumber, nameof(lineNumber));
            Column = Check.Positive(column, nameof(column));
        }

        public int LineNumber { get; }
        public int Column { get; }

        /// <summary>
        ///     Returns the token at the position
        /// </summary>
        public Token GetCurrentToken()
        {
            var tokens = this.Where(x => x.Range.StartLineNumber <= LineNumber && x.Range.EndLineNumber >= LineNumber);
            var currentToken = tokens.Count() <= 1
                ? tokens.SingleOrDefault()
                : tokens.SingleOrDefault(x => x.Range.StartColumn < Column && x.Range.EndColumn >= Column);

            return currentToken ?? throw new Exception("Current token not found");
        }

        /// <summary>
        ///     Returns the previous token relative to the position.
        /// </summary>
        public Token? GetPreviousToken(bool skipMeaningless) => GetPreviousToken(GetCurrentToken(), skipMeaningless);

        /// <summary>
        ///     Returns the next/previous token relative to the position.
        /// </summary>
        public Token? GetNextToken(bool skipMeaningless) => GetNextToken(GetCurrentToken(), skipMeaningless);

        public override string ToString() => string.Join("", this.Select(x => x.Text));
    }

    public class StatementList : List<Statement>
    {
        public StatementAtPosition? GetStatementAtPosition(int lineNumber, int column)
        {
            var statements = this.Where(x => x.Range.StartLineNumber <= lineNumber && x.Range.EndLineNumber >= lineNumber);
            var statement = statements.Count() <= 1
                ? statements.SingleOrDefault()
                : statements.Where(x => x.Range.StartColumn <= column && x.Range.EndColumn >= column)
                            .OrderBy(x => x.Range.StartColumn)
                            .FirstOrDefault();

            return statement is null
                ? null
                : new StatementAtPosition(statement, lineNumber, column);
        }
    }
}
