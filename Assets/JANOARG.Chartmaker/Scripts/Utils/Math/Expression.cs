

namespace JANOARG.Chartmaker.Utils.Math
{
    internal abstract class Expression
    {
        public abstract double Evaluate();
    }

    internal class ConstantExpression : Expression
    {
        public double Value;
        public override double Evaluate()
        {
            return Value;
        }
        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal class PrefixOperatorExpression : Expression
    {
        public Operator Operator;
        public Expression RightExpression;
        public override double Evaluate()
        {
            return Operator.PrefixFunction(RightExpression.Evaluate());
        }
        public override string ToString()
        {
            return $"({Operator} {RightExpression})";
        }
    }

    internal class InfixOperatorExpression : Expression
    {
        public Operator Operator;
        public Expression LeftExpression;
        public Expression RightExpression;
        public override double Evaluate()
        {
            return Operator.InfixFunction(LeftExpression.Evaluate(), RightExpression.Evaluate());
        }
        public override string ToString()
        {
            return $"({Operator} {LeftExpression} {RightExpression})";
        }
    }
}