using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

namespace JANOARG.Chartmaker.UI
{
    public class MathableInputField : TMP_InputField
    {
        [SerializeField] private bool evaluateOnEndEdit = true;
        private string _PreEditString = String.Empty;

        // For outside reference
        public static readonly string[] OpList =
        {
            "~",  
            "^",  
            "*",  
            "/",  
            "%",  
            "+",  
            "-",  
            "<<", 
            ">>", 
            "&",  
            "XOR",
            "|",  
            "OR", 
            "AND",
        };
        
        private static readonly Dictionary<string, (int precedence, bool rightAssoc)> Operators = new() 
        {
            { "~",   (5, true)  },  // unary NOT
            { "^",   (4, true)  },  // exponentiation
            { "*",   (3, false) },  // mult/div
            { "/",   (3, false) },
            { "%",   (3, false) },
            { "+",   (2, false) },
            { "-",   (2, false) },
            { "<<",  (1, false) }, // shifts
            { ">>",  (1, false) },
            { "&",   (0, false) }, // bitwise AND
            { "XOR", (0, false) },
            { "|",   (0, false) }, // bitwise OR
            { "OR",  (0, false) },
            { "AND", (0, false) } 
        };

        protected override void Start()
        {
            base.Start();

            if (_PreEditString == String.Empty)
                _PreEditString = text;

            onEndEdit.AddListener(EvaluateExpression);
        }

        public T Evaluate<T>(string input)
        {
            EvaluateExpression(input);

            try
            {
                return (T)Convert.ChangeType(text, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Conversion failed: {ex.Message}");
                return default;
            }
        }


        private void EvaluateExpression(string input)
        {
            if (evaluateOnEndEdit && !string.IsNullOrEmpty(input))
            {
                try
                {
                    var tokens = Tokenize(input).ToList();
                    var postfix = ToPostfix(tokens);
                    var result = EvalPostfix(postfix);

                    text = result.ToString(CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Invalid expression: {ex.Message}");
                    text = _PreEditString; // revert if invalid
                }
                finally
                {
                    _PreEditString = string.Empty;
                }
            }
        }

        private static IEnumerable<string> Tokenize(string input)
        {
            var number = "";
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsWhiteSpace(c)) continue;

                if (char.IsDigit(c) || c == '.')
                {
                    number += c;
                }
                else
                {
                    if (number.Length > 0)
                    {
                        yield return number;
                        number = "";
                    }

                    // Handle multi-char operators (<<, >>, XOR, AND, OR)
                    string op = null;

                    // Look ahead for multi-char
                    if (i + 1 < input.Length)
                    {
                        string twoChar = input.Substring(i, Math.Min(2, input.Length - i));
                        if (Operators.ContainsKey(twoChar))
                        {
                            op = twoChar;
                            i += 1;
                        }
                    }
                    if (op == null && i + 2 < input.Length)
                    {
                        string threeChar = input.Substring(i, Math.Min(3, input.Length - i)).ToUpper();
                        if (Operators.ContainsKey(threeChar))
                        {
                            op = threeChar;
                            i += 2;
                        }
                    }

                    if (op == null)
                        op = c.ToString();

                    yield return op;
                }
            }
            if (number.Length > 0)
                yield return number;
        }

        private static Queue<string> ToPostfix(IEnumerable<string> tokens)
        {
            var output = new Queue<string>();
            var opStack = new Stack<string>();

            foreach (string token in tokens)
            {
                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    output.Enqueue(token);
                }
                else if (Operators.ContainsKey(token))
                {
                    var (prec, rightAssoc) = Operators[token];
                    while (opStack.Count > 0 && Operators.ContainsKey(opStack.Peek()))
                    {
                        var (topPrec, topRightAssoc) = Operators[opStack.Peek()];
                        if ((!rightAssoc && prec <= topPrec) || (rightAssoc && prec < topPrec))
                            output.Enqueue(opStack.Pop());
                        else break;
                    }
                    opStack.Push(token);
                }
                else if (token == "(") 
                    opStack.Push(token);
                else if (token == ")")
                {
                    while (opStack.Count > 0 && opStack.Peek() != "(")
                        output.Enqueue(opStack.Pop());
                    if (opStack.Count == 0 || opStack.Pop() != "(")
                        throw new Exception("Mismatched parentheses");
                }
            }

            while (opStack.Count > 0)
            {
                var op = opStack.Pop();
                
                if (op == "(" || op == ")") 
                    throw new Exception("Mismatched parentheses");
                
                output.Enqueue(op);
            }

            return output;
        }

        private static double EvalPostfix(Queue<string> postfix)
        {
            var stack = new Stack<double>();

            foreach (string token in postfix)
            {
                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                {
                    stack.Push(num);
                }
                else
                {
                    switch (token)
                    {
                        case "~":
                            stack.Push(~Convert.ToInt32(stack.Pop()));
                            break;
                        case "^":
                            {
                                var r = stack.Pop();
                                var l = stack.Pop();
                                stack.Push(Math.Pow(l, r));
                                break;
                            }
                        case "*":
                            stack.Push(stack.Pop() * stack.Pop());
                            break;
                        case "/":
                            {
                                var r = stack.Pop();
                                var l = stack.Pop();
                                stack.Push(l / r);
                                break;
                            }
                        case "%":
                            {
                                var r = stack.Pop();
                                var l = stack.Pop();
                                stack.Push(l % r);
                                break;
                            }
                        case "+":
                            stack.Push(stack.Pop() + stack.Pop());
                            break;
                        case "-":
                            {
                                var r = stack.Pop();
                                var l = stack.Pop();
                                stack.Push(l - r);
                                break;
                            }
                        case "<<":
                            {
                                var r = (int)stack.Pop();
                                var l = (int)stack.Pop();
                                stack.Push(l << r);
                                break;
                            }
                        case ">>":
                            {
                                var r = (int)stack.Pop();
                                var l = (int)stack.Pop();
                                stack.Push(l >> r);
                                break;
                            }
                        case "&":
                        case "AND":
                            {
                                var r = (int)stack.Pop();
                                var l = (int)stack.Pop();
                                stack.Push(l & r);
                                break;
                            }
                        case "XOR":
                            {
                                var r = (int)stack.Pop();
                                var l = (int)stack.Pop();
                                stack.Push(l ^ r);
                                break;
                            }
                        case "|":
                        case "OR":
                            {
                                var r = (int)stack.Pop();
                                var l = (int)stack.Pop();
                                stack.Push(l | r);
                                break;
                            }
                        default:
                            throw new Exception($"Unknown operator {token}");
                    }
                }
            }

            if (stack.Count != 1) throw new Exception("Invalid expression");
            return stack.Pop();
        }
    }
}