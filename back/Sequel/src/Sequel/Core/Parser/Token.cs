using System;
using System.Collections.Generic;
using System.Linq;

namespace Sequel.Core.Parser
{
    public class Token
    {
        private static readonly string[] NewlineChars = new string[] { "\r\n", "\r", "\n" };
        private static readonly List<TokenType> TokenTypeKeywords = new List<TokenType>
        {
            TokenType.Keyword,
            TokenType.KeywordCTE,
            TokenType.KeywordDDL,
            TokenType.KeywordDML,
            TokenType.KeywordOrder,
            TokenType.KeywordTZCast
        };
        private static readonly List<TokenType> TokenTypeComments = new List<TokenType>
        {
            TokenType.Comment,
            TokenType.CommentHint,
            TokenType.CommentMultiline
        };
        private static readonly List<TokenType> TokenTypeMeaningless = new List<TokenType>
        {
            TokenType.Comment,
            TokenType.CommentHint,
            TokenType.CommentMultiline,
            TokenType.Newline,
            TokenType.Whitespace
        };

        public Token(TokenType type, string text, Token? previousToken = null)
        {
            Type = type;
            Text = text;
            UpperText = text.ToUpperInvariant();

            /******************/
            /* Start position */
            /******************/

            if (previousToken is null)
            {
                Range.StartLineNumber = 1;
                Range.StartColumn = 1;
            }
            else
            {
                if (previousToken.Type == TokenType.Newline || previousToken.Type == TokenType.Comment)
                { // beginning of a new line
                    Range.StartLineNumber = previousToken.Range.EndLineNumber + 1;
                    Range.StartColumn = 1;
                }
                else
                { // continue on the same line
                    Range.StartLineNumber = previousToken.Range.EndLineNumber;
                    Range.StartColumn = previousToken.Range.EndColumn;
                }
            }

            /****************/
            /* End position */
            /****************/

            if (type == TokenType.CommentMultiline || type == TokenType.Literal)
            { // probably a multiple line token
                var splitText = Text.Split(NewlineChars, StringSplitOptions.None);
                Range.EndLineNumber = Range.StartLineNumber + splitText.Length - 1;
                Range.EndColumn = splitText.Length == 1
                    ? Range.StartColumn + Text.Length // single line token finally, continue on the same line
                    : Range.EndColumn = 1 + splitText.Last().Length; // beginning of a new line
            }
            else
            { // single line token
                Range.EndLineNumber = Range.StartLineNumber;

                if (type == TokenType.Newline)
                {
                    Range.EndColumn = Range.StartColumn;
                }
                else if (type == TokenType.Comment)
                {
                    Range.EndColumn = Range.StartColumn + Text.Replace("\r\n", "").Replace("\r", "").Replace("\n", "").Length;
                }
                else
                {
                    Range.EndColumn = Range.StartColumn + Text.Length;
                }
            }
        }

        public TokenType Type { get; }
        public string Text { get; }
        public string UpperText { get; }
        public int? Depth { get; set; } // -1 : error
        public Range Range { get; } = new Range();
        public bool IsKeyword => TokenTypeKeywords.Contains(Type);
        public bool IsMeaningless => TokenTypeMeaningless.Contains(Type);
        public bool HasMeaning => !IsMeaningless;
        public bool IsComment => TokenTypeComments.Contains(Type);
        public bool IsOpenParenthesis => Type == TokenType.Punctuation && Text == "(";
        public bool IsCloseParenthesis => Type == TokenType.Punctuation && Text == ")";
    }

    public class Range
    {
        public int StartLineNumber { get; set; } = 1;
        public int StartColumn { get; set; } = 1;
        public int EndLineNumber { get; set; } = 1;
        public int EndColumn { get; set; } = 1;
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
