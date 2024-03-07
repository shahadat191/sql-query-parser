using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using ConsoleApp.Expressions;
using ConsoleApp.Tokenizer2;
using static ConsoleApp.Expressions.AndExpression;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // For parsing tokens.
            var tokens = Tokenizer.Tokenize("(T1 >= 35 AND (T1 <= 45 And T1 = 67)) OR (T3 = 'Intern' AND (T2 = 45 AND T2 = 67)) ");

            foreach (var token in tokens)
            {
                Console.Write($"Type: {token.Type}, ");
                if (token is PredicateToken predicate)
                {
                    Console.WriteLine($"       : {predicate}");
                }
                else if (token is Token normalToken)
                {
                    Console.WriteLine($" : {normalToken.Value}");
                }

            }

            // For expression from tokens
            var expression = ExpressionParser.ParseTokens(tokens);
            ExpressionParser.PrintExpressionTree(expression);


            // THis is for parse queries for different tables
            var (isValid, queriesDict) = ParseQueries(expression);

            Console.WriteLine(isValid);
            foreach (var item in queriesDict)
            {
                Console.WriteLine(item.Key);
                ExpressionParser.PrintExpressionTree(item.Value);
                Console.WriteLine("");

            }

        }

        private static (bool isValid, Dictionary<string, IExpression> queriesDict) ParseQueries(IExpression expression)
        {
            switch (expression)
            {
                case PredicateExpression predicateExpression:
                    return (true, new Dictionary<string, IExpression> { { predicateExpression.PropertyName, expression } });

                case OrExpression orExpression:
                    return MergeQueries(orExpression, (left, right) => new OrExpression(left, right));

                case AndExpression andExpression:
                    return MergeQueries(andExpression, (left, right) => new AndExpression(left, right));

                default:
                    // Handling of unsupported expressions can be decided here.
                    return (false, new Dictionary<string, IExpression>());
            }
        }

        private static (bool isValid, Dictionary<string, IExpression> queriesDict) MergeQueries(LogicalExpression expression, Func<IExpression, IExpression, IExpression> mergeExpression)
        {
            var (leftValid, leftQueryDict) = ParseQueries(expression.Expr1);
            var (rightValid, rightQueryDict) = ParseQueries(expression.Expr2);

            var queryDict = new Dictionary<string, IExpression>();
            var unionKeys = leftQueryDict.Keys.Union(rightQueryDict.Keys);

            foreach (var key in unionKeys)
            {
                if (leftQueryDict.TryGetValue(key, out var leftValue) && rightQueryDict.TryGetValue(key, out var rightValue))
                {
                    queryDict.Add(key, mergeExpression(leftValue, rightValue));
                }
                else
                {
                    queryDict.Add(key, leftQueryDict.ContainsKey(key) ? leftQueryDict[key] : rightQueryDict[key]);
                }
            }

            bool isValid = leftValid && rightValid && (unionKeys.Count() == 1 || unionKeys.Count() == leftQueryDict.Count + rightQueryDict.Count);
            return (isValid, queryDict);
        }
    }
        
}
