using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static ConsoleApp.Expressions.AndExpression;

namespace ConsoleApp.Expressions
{
    public class ExpressionParser
    {
        public static IExpression ParseTokens(List<Token> tokens)
        {
            var expressions = new Stack<IExpression>();
            var operators = new Stack<Token>();

            foreach (var token in tokens)
            {
                if(token.Type is TokenType.OpenPar)
                {
                    operators.Push(token);
                }
                else if(token.Type is TokenType.ClosePar)
                {
                    while(HasLogicalOperatorOnPeek(operators))
                    {
                        MergeExpression(expressions, operators);
                    }

                    if (operators.Count > 0 && operators.Peek().Type == TokenType.OpenPar)
                    {
                        operators.Pop();
                    }
                }
                else if(token.Type == TokenType.And || token.Type == TokenType.Or)
                {
                    while (HasLogicalOperatorOnPeek(operators) && OperatorPrecedence(operators.Peek()) >= OperatorPrecedence(token))
                    {
                        MergeExpression(expressions, operators);
                    }
                    operators.Push(token);
                }
                else
                {
                    expressions.Push(new PredicateExpression(token.Value));
                }
            }

            while (HasLogicalOperatorOnPeek(operators))
            {
                MergeExpression(expressions, operators);
            }

            if(!expressions.Any() || expressions.Count > 1)
            {
                throw new OverflowException("Input is not correct");
            }

            return expressions.Pop();

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

        private static bool HasLogicalOperatorOnPeek(Stack<Token> operators)
        {
            if (!operators.Any()) return false;
            var peekToken = operators.Peek();
            return peekToken.Type == TokenType.And || peekToken.Type == TokenType.Or;
        }

        private static void MergeExpression(Stack<IExpression> expressions, Stack<Token> operators)
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

        private static IExpression GetLogicalExpression(Token operatorType, IExpression left, IExpression right)
        {
            if (operatorType.Type == TokenType.And)
            {
                return new AndExpression(left, right);
            }
            return new OrExpression(left, right);
        }

        private static int OperatorPrecedence(Token token)
        {
            return token.Type switch
            {
                TokenType.Or => 1,
                TokenType.And => 2,
                _ => 0
            };
        }
    }
}
