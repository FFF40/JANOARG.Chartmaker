using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Shared.Utils.Animation;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.MultiEdit
{
    public class ChartmakerMultiHandlerBeatPosition : ChartmakerMultiHandler<BeatPosition>
    {
        public     BeatPosition From = BeatPosition.NaN;
        public new BeatPosition To   = new(0);

        public BeatPositionOperation Operation;

        public    string         LerpSource = "Offset";
        protected FieldInfo      LerpField;
        public    IEaseDirective LerpEasing = new BasicEaseDirective(EaseFunction.Linear, EaseMode.In);

        public float LerpFrom;
        public float LerpTo;

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
                float value = LerpField.FieldType == typeof(BeatPosition) ? (BeatPosition)LerpField.GetValue(item) : (float)LerpField.GetValue(item);
                LerpFrom = Mathf.Min(LerpFrom, value);
                LerpTo = Mathf.Max(LerpTo, value);
            }
        }

        public override BeatPosition Get(BeatPosition from, object source, int index = 0) 
        {
            float to = LerpField == null 
                ? LerpTo : Mathf.InverseLerp(LerpFrom, LerpTo, 
                
                    LerpField.FieldType == typeof(BeatPosition)
                        ? (BeatPosition)LerpField.GetValue(source) : (float)LerpField.GetValue(source));
        
            BeatPosition toBeat = float.IsFinite(From) ? (BeatPosition)Mathf.Lerp(From, To, LerpEasing.Get(to)) : To;
        
            return BeatPositionOperations.Get[Operation](from, toBeat);
        }
    }

    public enum BeatPositionOperation 
    {
        Set, Add, Snap
    }
    public static class BeatPositionOperations 
    {
        public static Dictionary<BeatPositionOperation, Func<BeatPosition, BeatPosition, BeatPosition>> Get = new Dictionary<BeatPositionOperation, Func<BeatPosition, BeatPosition, BeatPosition>> 
        {
            { BeatPositionOperation.Set,        (from, to) =>        to },
            { BeatPositionOperation.Add,        (from, to) => from + to },
            { BeatPositionOperation.Snap,       (from, to) => new BeatPosition(from.Number, Mathf.RoundToInt(from.Numerator * (float)to.Number / from.Denominator), to.Number) },
        };
    }
}