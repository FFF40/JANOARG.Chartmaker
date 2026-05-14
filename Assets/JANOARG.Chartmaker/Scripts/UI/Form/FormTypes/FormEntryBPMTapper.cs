using System;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryBPMTapper : FormEntry<float>
    {
        public UnityEvent OnStartTap;

        public Button TapButton;
        public TMP_Text TapButtonLabel;
        public Button ResetButton;

        int TapCount;
        DateTime StartTapTime;
        DateTime EndTapTime;

        const int TAP_COUNT_THRESHOLD = 8;

        public new void Start()
        {
            base.Start();
            UpdateLabels();
        }

        public void UpdateLabels()
        {
            if (TapCount == 0)
            {
                TapButtonLabel.text = "Tap here to set BPM";
            }
            else if (TapCount < TAP_COUNT_THRESHOLD)
            {
                TapButtonLabel.text = $"Keep going... ({TapCount}/{TAP_COUNT_THRESHOLD})";
            }
            else 
            {
                TapButtonLabel.text = $"BPM = {CurrentValue:F2}";
            }
        }

        public void Tap()
        {
            TapCount++;

            DateTime now = DateTime.Now;
            if (TapCount == 1)
            {
                StartTapTime = now;
                OnStartTap.Invoke();
            }
            EndTapTime = now;

            if (TapCount >= TAP_COUNT_THRESHOLD)
            {
                float bpm = (TapCount - 1) / (float)(EndTapTime - StartTapTime).TotalSeconds * 60;
                SetValue(bpm);
            }

            UpdateLabels();
        }

        public void Reset()
        {
            TapCount = 0;
            UpdateLabels();
        }
    }
}
