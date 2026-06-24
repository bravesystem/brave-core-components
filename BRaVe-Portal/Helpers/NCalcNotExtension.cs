using NCalc;
using System;

namespace BRaVe_Portal.Services.Expressions
{
    public static class NCalcNotExtension
    {
        /// <summary>
        /// Registers a NOT() function for NCalc expressions.
        /// Usage: NOT([field] == 'value')
        /// </summary>
        public static void RegisterNotFunction(this Expression expr)
        {
            expr.EvaluateFunction += (name, args) =>
            {
                if (!name.Equals("not", StringComparison.OrdinalIgnoreCase))
                    return;

                if (args.Parameters.Length != 1)
                    throw new ArgumentException("NOT() requires exactly 1 argument.");

                var value = args.Parameters[0].Evaluate();

                if (value is bool b)
                    args.Result = !b;
                else
                    throw new ArgumentException("NOT() argument must evaluate to a boolean value.");
            };
        }
    }
}
