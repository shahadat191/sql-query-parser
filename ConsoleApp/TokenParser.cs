using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ConsoleApp
{
    public enum TokenType
    {
        Predicate,
        And,
        Or,
        OpenPar,
        ClosePar
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }

    public class Tokenizer
    {
        private const char OpenParen = '(';
        private const char CloseParen = ')';
        public List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            var currentTokenValue = new StringBuilder();
            for (int idx = 0; idx < input.Length; idx++)
            {
                Char c = input[idx];
                if (char.IsWhiteSpace(c))
                {
                    AddToken(currentTokenValue, tokens);
                    if(tokens.Any() && tokens.Last().Value == "IN")
                    {
                        
                        idx = HandleInOperator(input, idx, currentTokenValue);
                    }

                    if (tokens.Any() && tokens.Last().Value == "BETWEEN")
                    {

                        idx = HandleBetweenOperator(input, idx, currentTokenValue);
                    }
                }
                else if (c == OpenParen || c == CloseParen)
                {
                    AddToken(currentTokenValue, tokens);
                    AddToken(new StringBuilder(c.ToString()), tokens);
                }
                else
                {
                    currentTokenValue.Append(c);
                }
            }

            AddToken(currentTokenValue, tokens);

            var updatedPredicates = UpdatePredicates(tokens);
            return updatedPredicates;
        }

        private int HandleInOperator(string input, int startIndex, StringBuilder currentTokenValue)
        {
            int closeParenIndex = input.IndexOf(CloseParen, startIndex);
            if (closeParenIndex == -1)
            {
                return startIndex; // Return current index to continue processing
            }

            string inList = input.Substring(startIndex, closeParenIndex - startIndex + 1);
            currentTokenValue.Append(inList);
            return closeParenIndex;
        }

        private int HandleBetweenOperator(string input, int startIndex, StringBuilder currentTokenValue)
        {
            const string and = "AND";
            int opIndex = input.IndexOf(and, startIndex, StringComparison.OrdinalIgnoreCase);
            if (opIndex == -1)
            {
                return startIndex; // Return current index to continue processing
            }

            string inList = input.Substring(startIndex, opIndex - startIndex + 1 + and.Length);
            currentTokenValue.Append(inList);
            return opIndex + and.Length;
        }

        private void AddToken(StringBuilder tokenValueBuilder, List<Token> tokens)
        {
            var tokenValue = tokenValueBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(tokenValue))
            {
                var type = DetermineTokenType(tokenValue);
                tokens.Add(new Token(type, tokenValue));
                tokenValueBuilder.Clear(); 
            }
        }

        private static List<Token> UpdatePredicates(List<Token> tokens)
        {
            var updatedTokens = new List<Token>();

            var predicates = new List<string>();
            foreach (var token in tokens)
            {
                if(token.Type is TokenType.Predicate)
                {
                    predicates.Add(token.Value);
                }
                else
                {
                    MergePredicateAndAdd(updatedTokens, predicates);
                    updatedTokens.Add(token);
                }
            }

            MergePredicateAndAdd(updatedTokens, predicates);
            return updatedTokens;
        }

        private static void MergePredicateAndAdd(List<Token> updatedTokens, List<string> predicates)
        {
            if (predicates.Any())
            {
                var mergePredicate = string.Join(' ', predicates);
                updatedTokens.Add(new Token(TokenType.Predicate, mergePredicate));
                predicates.Clear();
            }
        }

        private TokenType DetermineTokenType(string tokenValue)
        {
            var upperTokenValue = tokenValue.ToUpperInvariant();
            switch (upperTokenValue)
            {
                case "AND":
                    return TokenType.And;
                case "OR":
                    return TokenType.Or;
                case "(":
                    return TokenType.OpenPar;
                case ")":
                    return TokenType.ClosePar;
                default:
                    return TokenType.Predicate;
            }
        }
    }
}
