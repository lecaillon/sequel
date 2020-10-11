using System.Collections.Generic;
using System.Linq;

namespace Sequel.Core.Parser
{
    public class Token
    {
        private static readonly List<TokenType> TokenTypeKeywords = new List<TokenType>
        {
            TokenType.Keyword,
            TokenType.KeywordCTE,
            TokenType.KeywordDDL,
            TokenType.KeywordDML,
            TokenType.KeywordOrder,
            TokenType.KeywordTZCast
        };

        public Token(TokenType type, string text, int position)
        {
            Type = type;
            Text = text;
            Position = position;
        }

        public TokenType Type { get; }
        public string Text { get; }
        public int? Depth { get; set; } // -1 : error
        public int Position { get; }
        
        public bool IsKeyword() => TokenTypeKeywords.Contains(Type);
        public bool IsOpenParenthesis() => Type == TokenType.Punctuation && Text == "(";
        public bool IsCloseParenthesis() => Type == TokenType.Punctuation && Text == ")";
    }

    public class Statement : List<Token>
    {
        /// <summary>
        ///     The statement needs a semicolon, if another statement is going to be concatenated to it.
        /// </summary>
        public bool NeedsSemicolon { get; set; }

        public override string ToString() => string.Join("", this.Select(x => x.Text));
    }

    public enum TokenType
    {
        Assignment,
        Comment,
        CommentHint,
        CommentMultiline,
        Command,
        Comparison,
        Keyword,
        KeywordCTE,
        KeywordDDL,
        KeywordDML,
        KeywordOrder,
        KeywordTZCast,
        Literal,
        Name,
        NamePlaceholder,
        NameBuiltin,
        Newline,
        NumberHexadecimal,
        NumberFloat,
        NumberInteger,
        Operator,
        OperatorComparison,
        Punctuation,
        StringSymbol,
        StringSingle,
        Wildcard,
        Whitespace,
    }
}
