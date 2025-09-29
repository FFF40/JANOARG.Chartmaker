

namespace JANOARG.Chartmaker.Utils.Math
{
    internal abstract class ExpressionToken
    {
        public override string ToString()
        {
            return $"unidentified tokens";
        }
    }

    internal class ConstantExpressionToken : ExpressionToken
    {
        public double Value;
        public override string ToString()
        {
            return $"numeric literal '{Value}'";
        }
    }

    internal class OperatorExpressionToken : ExpressionToken
    {
        public string Operator;
        public override string ToString()
        {
            return $"operator '{Operator}'";
        }
    }

    internal class StartExpressionToken : ExpressionToken
    {
        public override string ToString()
        {
            return "'('";
        }
    }

    internal class EndExpressionToken : ExpressionToken
    {
        public override string ToString()
        {
            return "')'";
        }
    }
}