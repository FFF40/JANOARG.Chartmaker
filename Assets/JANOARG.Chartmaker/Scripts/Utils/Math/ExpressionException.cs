

using System;
using System.Runtime.Serialization;

namespace JANOARG.Chartmaker.Utils.Math
{
    public class ExpressionException : Exception
    {
        public ExpressionException()
        {
        }

        public ExpressionException(string message) : base(message)
        {
        }

        public ExpressionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}