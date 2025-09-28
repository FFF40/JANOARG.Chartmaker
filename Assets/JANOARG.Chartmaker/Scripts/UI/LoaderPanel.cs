using System.Collections;
using JANOARG.Chartmaker.UI.Themeable;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace JANOARG.Chartmaker.UI
{
    public class LoaderPanel : MonoBehaviour
    {
        public Image    SongCoverBackground;
        public TMP_Text SongTitleLabel;
        public TMP_Text SongArtistLabel;
        public Slider   ProgressBar;
        public TMP_Text ActionLabel;
        public TMP_Text ProgressLabel;
        public Button   CancelButton;

        bool isAnimating;

        public void OnEnable()
        {
            StartCoroutine(Intro());
        }

        IEnumerator Intro()
        {
            isAnimating = true;
            StartCoroutine(IntroNudge());

            void ease1(float x)
            {
                float ease1 = Ease.Get(x, EaseFunction.Exponential, EaseMode.Out);

                SongCoverBackground.rectTransform.anchorMin = new Vector2(1 - ease1, 0);
                SongCoverBackground.rectTransform.anchorMax = new Vector2(2 - ease1, 1);
            }

            for (float a = 0; a < 1; a += Time.deltaTime / .5f)
            {
                ease1(a);
                yield return null;
            }

            ease1(1);

            isAnimating = false;
        }

        IEnumerator IntroNudge()
        {
            RectTransform rt = (RectTransform)transform;
            rt.anchoredPosition -= new Vector2(-2, 2);

            yield return new WaitForSecondsRealtime(0.05f);

            rt.anchoredPosition += new Vector2(-2, 2);
        }

        public void SetSong(PlayableSong song)
        {
            SongCoverBackground.color = song.BackgroundColor;

            SongTitleLabel.color = SongArtistLabel.color = song.InterfaceColor + new Color(0, 0, 0, 1);
            SongTitleLabel.text = song.SongName;

            SongArtistLabel.text = song.SongArtist;
        }

        public void SetSong(string name, string artist)
        {
            SongCoverBackground.color = Themer.main.Keys["Background1"];

            SongTitleLabel.color = SongArtistLabel.color = Themer.main.Keys["Content0"];
            SongTitleLabel.text = name;

            SongArtistLabel.text = artist;
        }

        public void SetSong(string name, string artist, Color bg, Color fg)
        {
            SongCoverBackground.color = bg;

            SongTitleLabel.color = SongArtistLabel.color = fg;
            SongTitleLabel.text = name;

            SongArtistLabel.text = artist;
        }

        public void SetNoSong()
        {
            SongCoverBackground.color = Themer.main.Keys["Background1"];

            SongTitleLabel.color = SongArtistLabel.color = Themer.main.Keys["Content0"];
            SongTitleLabel.text = "";

            SongArtistLabel.text = "";
        }

        public void SetCancelButton(Action action)
        {
            CancelButton.gameObject.SetActive(true);
            CancelButton.onClick.RemoveAllListeners();
            CancelButton.onClick.AddListener(() => action());
        }

        public void SetNoCancelButton()
        {
            CancelButton.onClick.RemoveAllListeners();
            CancelButton.gameObject.SetActive(false);
        }
    }
}
