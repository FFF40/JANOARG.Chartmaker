using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JANOARG.Chartmaker.Utils.Math;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Shared.Utils.Animation;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.MultiEdit
{
    public abstract class ChartmakerLerpableMultiHandler<T> : ChartmakerMultiHandler<T>
    {
        public     float From = float.NaN;
        public new float To;

        public LerpableOperation Operation;
        public string CustomExpressionString;
        public Expression CustomExpression;
        public ExpressionContext CustomExpressionContext;

        public    string         LerpSource = "Offset";
        protected FieldInfo      LerpField;
        public    IEaseDirective LerpEasing = new BasicEaseDirective(EaseFunction.Linear, EaseMode.In);

        public float LerpFrom;
        public float LerpTo;

        public abstract ExpressionContext GetExpressionContext();

        public void SetLerp(IList list)
        {
            LerpFrom = float.PositiveInfinity;
            LerpTo = float.NegativeInfinity;
            LerpField = list.GetType().GetGenericArguments()[0].GetField(LerpSource);
       
            if (LerpField == null) 
            {
                UnityEngine.Debug.LogWarning($"SetLerp: LerpSource '{LerpSource}' not found on type {list.GetType().GetGenericArguments()[0].Name}. Select a valid scalar field in the Lerp Source dropdown.");
                return;
            }
     
            foreach (object item in list)
            {
                float value = LerpField.FieldType == typeof(BeatPosition) 
                    ? (BeatPosition)LerpField.GetValue(item) : (float)LerpField.GetValue(item);
            
                LerpFrom = Mathf.Min(LerpFrom, value);
                LerpTo = Mathf.Max(LerpTo, value);
            }
        }

        public void PrepareCustomExpression()
        {
            CustomExpression = string.IsNullOrEmpty(CustomExpressionString) ? null : ExpressionUtils.ParseTokens(ExpressionUtils.Tokenize(CustomExpressionString), false);
            CustomExpressionContext = GetExpressionContext();
        }
    }

    public enum LerpableOperation {
        Set, Add, Multiply, Min, Max, Mirror, Expression
    }
    public static class LerpableOperations {
        public static Dictionary<LerpableOperation, Func<float, float, float>> Get = new Dictionary<LerpableOperation, Func<float, float, float>> 
        {
            { LerpableOperation.Set,        (from, to) =>                  to },
            { LerpableOperation.Add,        (from, to) =>           from + to },
            { LerpableOperation.Multiply,   (from, to) =>           from * to },
            { LerpableOperation.Min,        (from, to) => Mathf.Min(from, to) },
            { LerpableOperation.Max,        (from, to) => Mathf.Max(from, to) },
            { LerpableOperation.Mirror,     (from, to) =>    to - (from - to) },
        };
    }
}