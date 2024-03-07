using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp.Expressions
{
    public interface IExpression
    {
        bool interpret(Dictionary<string, string> record);
    }
}
