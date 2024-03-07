using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApp.Tokenizer2
{
    public enum TokenType
    {
        PropertyName,
        Operator,
        LogicalOperator,
        Value,
        ParenthesisOpen,
        ParenthesesClose,
        Predicate
    }

    public abstract class BaseToken
    {
        public TokenType Type { get; protected set; }
    }

    public class Token: BaseToken
    {
        public string Value { get; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }

    public class PredicateToken: BaseToken
    {
        public Token PropertyToken { get; set; }
        public Token OperatorToken { get; set; }
        public Token ValueToken { get; set; }

        public PredicateToken()
        {
            this.Type = TokenType.Predicate;
        }

        public override string ToString()
        {
            return $"{this.PropertyToken.Value} {this.OperatorToken.Value} {this.ValueToken.Value}";
        }
    }

    public static class TokenConstants
    {
        public const string IN = "IN";
    }

    public class Tokenizer
    {
        private static readonly List<string> _operators = new List<string> { "<", ">", "<=", ">=", "=", "!=", "LIKE", "ILIKE", "IN", "BETWEEN" };
        private static readonly char[] _comparisonSymbols = new char[] { '<', '>', '!', '=' };
        private static readonly List<string> _logicalOperators = new List<string> { "AND", "OR" };
        private static readonly char _stringDelimeter = '\'';

        public static List<BaseToken> Tokenize(string input)
        {
            var tokens = new List<BaseToken>();
            var currentToken = string.Empty;

            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (char.IsWhiteSpace(c))
                {
                    if (currentToken.Length > 0)
                    {
                        var tokenType = GetTokenType(currentToken);
                        if (tokenType == TokenType.PropertyName)
                        {
                            AddPredicateToken(input, tokens, ref currentToken, ref i);
                        }
                        else
                        {
                            AddToken(tokens, ref currentToken);
                        }

                    }
                    continue;
                }

                switch (c)
                {
                    case '(':
                        tokens.Add(new Token(TokenType.ParenthesisOpen, c.ToString()));
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.ParenthesesClose, c.ToString()));
                        break;
                    default:
                        currentToken += c;
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentToken))
            {
                AddToken(tokens, ref currentToken);
            }

            return tokens;
        }

        private static void AddPredicateToken(string input, List<BaseToken> tokens, ref string currentToken, ref int i)
        {
            var propertyToken = new Token(TokenType.PropertyName, currentToken);
            var operatorToken = GetOperatorToken(input, ref i);
            var valueToken = GetValueToken(operatorToken, input, ref i);
            var predicateToken = new PredicateToken
            {
                PropertyToken = propertyToken,
                OperatorToken = operatorToken,
                ValueToken = valueToken
            };
            tokens.Add(predicateToken);
            currentToken = string.Empty;
        }

        private static bool ExpectKeyword(string input, string keyword, ref int index)
        {
            SkipWhitespace(input, ref index);
            int startIndex = index; 
            int endIndex = startIndex + keyword.Length;
            char d = input[startIndex];
            char c = input[endIndex];
            if (endIndex <= input.Length)
            {
                var currentWord = input.Substring(startIndex, keyword.Length);
                if (currentWord.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    index += keyword.Length; // Move the index past the keyword.
                    return true;
                }
            }

            return false;
        }

        private static string ExtractValue(string input, ref int index)
        {
            SkipWhitespace(input, ref index);
            if (index >= input.Length) throw new ArgumentException("Unexpected end of input.");

            var valueBuilder = new StringBuilder();
            bool isQuoted = input[index] == _stringDelimeter;

            if (isQuoted)
            {
                char quote = input[index++];
                while (index < input.Length && input[index] != quote)
                {
                    valueBuilder.Append(input[index++]);
                }
                if (index < input.Length) index++;
            }
            else
            {
                while (index < input.Length && char.IsDigit(input[index]))
                {
                    valueBuilder.Append(input[index++]);
                }
                index--;
            }

            string value = valueBuilder.ToString();
            return value;
        }

        private static Token GetValueToken(Token operatorToken, string input, ref int index)
        {
            switch(operatorToken.Value)
            {
                case "IN":
                    return GetInOperatorToken(input, ref index);
                case "BETWEEN":
                    return GetBetweenOperationValueToken(input, ref index);
                default:
                    var value = ExtractValue(input, ref index);
                    return new Token(TokenType.Value, value);
            }
        }

        private static Token GetBetweenOperationValueToken(string input, ref int index)
        {
            var startValue = ExtractValue(input, ref index);

            // Ensure "AND" is present
            ExpectKeyword(input, "AND ", ref index);

            // Extract the end value of the range
            var endValue = ExtractValue(input, ref index);

            // Return a range token combining both values
            return new Token(TokenType.Value, $"{startValue} AND {endValue}");
        }

        private static void SkipWhitespace(string input, ref int index)
        {
            while (index < input.Length && char.IsWhiteSpace(input[index]))
            {
                index++;
            }
        }

        private static Token GetOperatorToken(string input, ref int i)
        {
            while (i < input.Length && char.IsWhiteSpace(input[i])) i++;

            if (i >= input.Length) throw new ArgumentException("Input ends unexpectedly.");

            var currentTokenBuilder = new StringBuilder();
            var startCharacter = input[i];
            currentTokenBuilder.Append(input[i++]);

            var startWihtSymbolics = _comparisonSymbols.Contains(startCharacter); // Starts with <, >, !
            if (i < input.Length && startWihtSymbolics) 
            {
                // Check for <=, >=, !=
                string potentialOperator = currentTokenBuilder.ToString() + input[i];
                if (_operators.Contains(potentialOperator))
                {
                    currentTokenBuilder.Append(input[i++]);
                }
            }
            else
            {
                // Check for Like or ILIKE
                while (i < input.Length && !char.IsWhiteSpace(input[i]))
                {
                    currentTokenBuilder.Append(input[i++]);
                }
            }

            string currentToken = currentTokenBuilder.ToString();
            if (!_operators.Contains(currentToken))
            {
                throw new InvalidOperationException($"Unrecognized operator: {currentToken}");
            }

            return new Token(TokenType.Operator, currentToken);
        }

        private static Token GetInOperatorToken(string input, ref int index)
        {
            SkipWhitespace(input, ref index);
            var hasOpenKeyword = ExpectKeyword(input, "(", ref index);
            if(!hasOpenKeyword)
            {
                //TODO:
            }
            var temp = input[index];

            var inItems = new List<string>();
            while(index < input.Length)
            {
                char c = input[index];
                if(c == ')')
                {
                    index++;
                    break;
                }
                else if (c == ',')
                {
                    index++;
                    continue;
                }
                var value = ExtractValue(input, ref index);
                inItems.Add(value);
            }

            return new Token(TokenType.Value, string.Join(",", inItems));
        }

        private static void AddToken(List<BaseToken> tokens, ref string currentToken)
        {
            if (!string.IsNullOrEmpty(currentToken))
            {
                if (_logicalOperators.Contains(currentToken.ToUpper()))
                {
                    tokens.Add(new Token(TokenType.LogicalOperator, currentToken.ToUpper()));
                }
                else if(_operators.Contains(currentToken.ToUpper()))
                {
                    tokens.Add(new Token(TokenType.Operator, currentToken.ToUpper()));
                }
                else
                {
                    tokens.Add(new Token(TokenType.PropertyName, currentToken));
                }
                currentToken = string.Empty;
            }
        }

        private static TokenType GetTokenType(string currentToken)
        {
            if(string.IsNullOrWhiteSpace(currentToken))
            {
                throw new Exception("Token can't be empty.");
            }

            if (_logicalOperators.Contains(currentToken.ToUpper()))
            {
                return TokenType.LogicalOperator;
            }
            else if (_operators.Contains(currentToken.ToUpper()))
            {
                return TokenType.Operator;
            }
            else
            {
                return TokenType.PropertyName;
            }
        }

    }

}
