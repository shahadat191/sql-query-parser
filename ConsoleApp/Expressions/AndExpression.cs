using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp.Expressions
{

    public abstract class LogicalExpression: IExpression
    {
        public readonly IExpression Expr1, Expr2;
        public LogicalExpression(IExpression left, IExpression right)
        {
            this.Expr1 = left;
            this.Expr2 = right;
        }
        public abstract bool interpret(Dictionary<string, string> record);
    }

    public class AndExpression : LogicalExpression
    {
        public AndExpression(IExpression left, IExpression right): base(left, right)
        {
        }

        public override bool interpret(Dictionary<string, string> record)
        {
            return Expr1.interpret(record) && Expr2.interpret(record);
        }


        public class OrExpression : LogicalExpression
        {
            public OrExpression(IExpression left, IExpression right): base(left, right)
            {

            }

            public override bool interpret(Dictionary<string, string> record)
            {
                return Expr1.interpret(record) && Expr2.interpret(record);
            }
        }

        public class PredicateExpression : IExpression
        {
            public readonly string Predicate;
            public readonly string PropertyName;
            public readonly string OperatorName;
            public readonly string Value;

            public PredicateExpression(string predicate)
            {
                this.Predicate = predicate;
            }

            public PredicateExpression(string propertyName, string operatorName, string value)
            {
                this.PropertyName = propertyName;
                this.OperatorName = operatorName;
                this.Value = value;
                this.Predicate = $"{propertyName} {operatorName} {value}";
            }

            public bool interpret(Dictionary<string, string> record)
            {
                return PredicateFactory.intercept(Predicate, record);
            }
        }
    }

}
