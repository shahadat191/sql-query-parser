using ConsoleApp.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ConsoleApp.Expressions.AndExpression;

namespace ConsoleApp.Tokenizer2
{
    public class ExpressionParser
    {
        public static IExpression ParseTokens(List<BaseToken> tokens)
        {
            var expressions = new Stack<IExpression>();
            var operators = new Stack<BaseToken>();

            foreach (var token in tokens)
            {
                if (token.Type is TokenType.ParenthesisOpen)
                {
                    operators.Push(token);
                }
                else if (token.Type is TokenType.ParenthesesClose)
                {
                    while (HasLogicalOperatorOnPeek(operators))
                    {
                        MergeExpression(expressions, operators);
                    }

                    if (operators.Count > 0 && operators.Peek().Type == TokenType.ParenthesisOpen)
                    {
                        operators.Pop();
                    }
                }
                else if (token.Type == TokenType.LogicalOperator)
                {
                    while (HasLogicalOperatorOnPeek(operators) && OperatorPrecedence(operators.Peek() as Token) >= OperatorPrecedence(token as Token))
                    {
                        MergeExpression(expressions, operators);
                    }
                    operators.Push(token);
                }
                else
                {
                    var predicateToken = token as PredicateToken;
                    expressions.Push(new PredicateExpression(predicateToken.PropertyToken.Value, predicateToken.OperatorToken.Value, predicateToken.ValueToken.Value));
                }
            }

            while (HasLogicalOperatorOnPeek(operators))
            {
                MergeExpression(expressions, operators);
            }

            if (!expressions.Any() || expressions.Count > 1)
            {
                throw new OverflowException("Input is not correct");
            }

            return expressions.Pop();

        }

        private static bool HasLogicalOperatorOnPeek(Stack<BaseToken> operators)
        {
            if (!operators.Any()) return false;
            var peekToken = operators.Peek();
            return peekToken.Type == TokenType.LogicalOperator;
        }

        private static void MergeExpression(Stack<IExpression> expressions, Stack<BaseToken> operators)
        {
            if (!operators.Any()) return;
            var logicalOperator = operators.Pop();

            if (!expressions.Any()) return;
            var rightExpression = expressions.Pop();

            if (!expressions.Any()) return;
            var leftExpression = expressions.Pop();

            var logicalExpression = GetLogicalExpression(logicalOperator, leftExpression, rightExpression);
            expressions.Push(logicalExpression);
        }

        private static IExpression GetLogicalExpression(BaseToken token, IExpression left, IExpression right)
        {
            if ((token as Token).Value == "AND")
                return new AndExpression(left, right);
            return new OrExpression(left, right);
        }

        private static int OperatorPrecedence(Token token)
        {
            var tokenValue = token.Value;
            return tokenValue switch
            {
                "OR" => 1,
                "AND" => 2,
                _ => 0
            };
        }

        public static void PrintExpressionTree(IExpression expression, string indent = "", string nodePrefix = "")
        {
            // Determine the type of the expression to print the appropriate node label
            switch (expression)
            {
                case AndExpression andExpr:
                    Console.WriteLine($"{indent}{nodePrefix}AND");
                    PrintExpressionTree(andExpr.Expr1, indent + "|   ", "├── ");
                    PrintExpressionTree(andExpr.Expr2, indent + "|   ", "└── ");
                    break;
                case OrExpression orExpr:
                    Console.WriteLine($"{indent}{nodePrefix}OR");
                    PrintExpressionTree(orExpr.Expr1, indent + "|   ", "├── ");
                    PrintExpressionTree(orExpr.Expr2, indent + "|   ", "└── ");
                    break;
                case PredicateExpression predExpr:
                    Console.WriteLine($"{indent}{nodePrefix}{predExpr.Predicate}");
                    break;
                default:
                    Console.WriteLine($"{indent}{nodePrefix}Unknown Expression Type");
                    break;
            }
        }
    }
}
