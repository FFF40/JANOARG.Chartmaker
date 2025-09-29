

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace JANOARG.Chartmaker.Utils.Math
{
    /// <summary>
    /// Class for parsing and evaluating math expressions
    /// </summary>
    public static class ExpressionUtils
    {

        /// <summary>
        /// Evaluate an expression string.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="expression">The expression string to be evaluated.</param>
        /// <returns>The evaluation result of the expression string.</returns>
        public static T Evaluate<T>(string expression)
        {
            expression = expression.ToLowerInvariant();
            return (T)Convert.ChangeType(ParseTokens(Tokenize(expression)), typeof(T));
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to evaluate an expression string.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="expression">The expression string to be evaluated.</param>
        /// <returns>Whether the expression evaluated successfully.</returns>
        public static bool TryEvaluate<T>(string expression, out T result)
        {
            try
            {
                result = Evaluate<T>(expression);
                return true;
            }
            catch (ExpressionException)
            {
                result = default;
                return false;
            }
        }




        #region Lexer

        /// <summary>
        /// Splits a given expression string into tokens.
        /// </summary>
        /// <param name="expression">The expression string to be evaluated.</param>
        /// <returns>An enumerable object that returns the tokens of the expression string.</returns>
        internal static IEnumerable<ExpressionToken> Tokenize(IEnumerable<char> expression)
        {
            TokenMode currentMode = TokenMode.None;
            StringBuilder currentText = new();

            ExpressionToken ToToken()
            {
                if (currentMode == TokenMode.Number)
                {
                    return new ConstantExpressionToken
                    {
                        Value = double.Parse(currentText.ToString(), CultureInfo.InvariantCulture)
                    };
                }
                else
                {
                    return new OperatorExpressionToken
                    {
                        Operator = currentText.ToString()
                    };
                }
            }

            foreach (char character in expression)
            {
                if (character == ' ')
                {
                    if (currentText.Length > 0) yield return ToToken();
                    currentText = new();
                    continue;
                }
                if (character == '(')
                {
                    if (currentText.Length > 0) yield return ToToken();
                    currentText = new();
                    yield return new StartExpressionToken();
                    continue;
                }
                if (character == ')')
                {
                    if (currentText.Length > 0) yield return ToToken();
                    currentText = new();
                    yield return new EndExpressionToken();
                    continue;
                }

                TokenMode targetMode = character switch
                {
                    >= '0' and <= '9' or '.' => TokenMode.Number,
                    >= 'a' and <= 'z' => TokenMode.Letter,
                    _ => TokenMode.Punctuation,
                };

                if (targetMode != currentMode)
                {
                    if (currentText.Length > 0) yield return ToToken();
                    currentText = new();
                    currentMode = targetMode;
                }

                // Attempt to split continuos punctuation operators
                if (targetMode == TokenMode.Punctuation)
                {
                    string currentOp = currentText.ToString() + character;
                    bool hasExtraOperators = Operator.Operators.Keys.Any(op => op.StartsWith(currentOp));
                    if (!hasExtraOperators)
                    {
                        if (currentText.Length > 0) yield return ToToken();
                        currentText = new();
                    }
                }

                currentText.Append(character);

            }
            if (currentText.Length > 0) yield return ToToken();
            yield return new EndExpressionToken();
        }

        enum TokenMode
        {
            None,
            Number,
            Letter,
            Punctuation,
        }

        #endregion



        #region Parser

        /// <summary>
        /// Parses and evaluates a token enumerable.
        /// </summary>
        /// <param name="tokens">A token enumerable.</param>
        /// <returns>The result.</returns>
        internal static double ParseTokens(IEnumerable<ExpressionToken> tokens)
        {
            var enumerator = tokens.GetEnumerator();
            enumerator.MoveNext();
            Expression result = ParseTokens(enumerator, true);
            if (result is ConstantExpression constant) return constant.Value;
            else throw new ExpressionException("");
        }

        /// <summary>
        /// Parses and evaluates a token enumerator.
        /// </summary>
        /// <param name="tokens">A token enumerator.</param>
        /// <param name="evaluate">Whether the parser also evaluates the expression.</param>
        /// <returns>The result.</returns>
        internal static Expression ParseTokens(IEnumerator<ExpressionToken> tokens, bool evaluate = false, int rightBindingPower = 0)
        {
            Expression leftSide = ProcessFirstToken(tokens, evaluate);

            ProcessInfixTokens(tokens, evaluate, rightBindingPower, ref leftSide);

            return evaluate ? new ConstantExpression { Value = leftSide.Evaluate() } : leftSide;
        }

        static Expression ProcessFirstToken(IEnumerator<ExpressionToken> tokens, bool evaluate)
        {
            ExpressionToken token = tokens.Current;
            tokens.MoveNext();

            switch (token)
            {
                case StartExpressionToken:
                    {
                        var exp = ParseTokens(tokens, evaluate);
                        if (tokens.Current is not EndExpressionToken)
                        {
                            throw new ExpressionException($"Unexpected {token}");
                        }
                        tokens.MoveNext();
                        return exp;
                    }

                case ConstantExpressionToken leftConstant:
                    return new ConstantExpression
                    {
                        Value = leftConstant.Value
                    };

                case OperatorExpressionToken prefixOperator:
                    {
                        Operator op = GetOperatorFromToken(prefixOperator);
                        if (op.PrefixFunction == null)
                        {
                            throw new ExpressionException($"Operator '{prefixOperator.Operator}' can't be used in prefix position");
                        }
                        return new PrefixOperatorExpression
                        {
                            Operator = op,
                            RightExpression = ParseTokens(tokens, evaluate, (int)BindingPower.Prefix)
                        };
                    }

                default:
                    throw new ExpressionException($"Unexpected {token}");
            }
        }

        static void ProcessInfixTokens(IEnumerator<ExpressionToken> tokens, bool evaluate, int rightBindingPower, ref Expression leftSide)
        {
            while (true)
            {
                ExpressionToken token = tokens.Current;

                if (token is OperatorExpressionToken infixOperator)
                {
                    Operator op = GetOperatorFromToken(infixOperator);
                    if (op.InfixFunction == null)
                    {
                        throw new ExpressionException($"Operator '{infixOperator.Operator}' can't be used in infix position");
                    }
                    if (rightBindingPower >= op.LeftBindingPower)
                    {
                        break;
                    }
                    tokens.MoveNext();

                    leftSide = new InfixOperatorExpression
                    {
                        Operator = op,
                        LeftExpression = leftSide,
                        RightExpression = ParseTokens(tokens, evaluate, op.LeftBindingPower + (int)op.Associativity)
                    };
                }
                else
                {
                    break;
                }
            }
        }

        static Operator GetOperatorFromToken(OperatorExpressionToken token)
        {
            if (Operator.Operators.TryGetValue(token.Operator, out Operator op))
            {
                return op;
            }
            else
            {
                throw new ExpressionException($"Unknown operator '{token.Operator}'");
            }
        }
        
        #endregion
    }
}