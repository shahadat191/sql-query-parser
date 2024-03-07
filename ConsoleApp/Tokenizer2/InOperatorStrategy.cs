using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp.Tokenizer2
{
    public interface IValueExtractionStrategy
    {
        Token GetValueToken(string input, ref int index);
    }
    public class InOperatorStrategy : IValueExtractionStrategy
    {
        public Token GetValueToken(string input, ref int index)
        {
            //SkipWhitespace(input, ref index);
            //ExpectKeyword(input, "(", ref index);

            var sb = new StringBuilder();
            //while (index < input.Length && input[index] != ')')
            //{
            //    if (input[index] == ',')
            //    {
            //        index++;
            //        continue;
            //    }
            //    sb.Append(ExtractValue(input, ref index));
            //    if (index < input.Length && input[index] == ',')
            //    {
            //        sb.Append(",");
            //    }
            //}

            index++; // Skip the closing parenthesis
            return new Token(TokenType.Value, sb.ToString());
        }
    }
}
