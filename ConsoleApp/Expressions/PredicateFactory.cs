using System;
using System.Collections.Generic;

namespace ConsoleApp.Expressions
{
    public class PredicateFactory
    {
        internal static bool intercept(string predicate, Dictionary<string, string> record)
        {
            return true;
        }
    }
}