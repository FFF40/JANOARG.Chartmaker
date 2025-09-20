using System.Collections;
using TMPro;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Modal.ModalTypes
{
    public class AboutModal : Modal
    {
        public static AboutModal main;
    
        public TMP_Text VersionLabel;

        public TMP_Text FlavorTextLabel;

        public RectTransform Content;

        public RectTransform TwitterIcon;
        public RectTransform XIcon;

        public void Awake()
        {
            if (main) Close();
            else main = this;
        }

        public new void Start()
        {
            base.Start();
            VersionLabel.text = "Version " + Application.version;
            NewFlavorText();
        }

        public void NewFlavorText()
        {
            do
            {
                FlavorTextLabel.text = FlavorTexts[Random.Range(0, FlavorTexts.Length)]();
            }
            while (string.IsNullOrEmpty(FlavorTextLabel.text));
        }

        public void Update()
        {
            Content.anchoredPosition += 15 * Time.deltaTime * Vector2.up;
            if (Content.anchoredPosition.y > Content.rect.height - 400) Content.anchoredPosition -= Vector2.up * Content.rect.height;
        }

        public void OpenGitHub()
        {
            Application.OpenURL("https://github.com/ducdat0507/JANOARG-Chartmaker");
        }

        public void OpenDiscord()
        {
            Application.OpenURL("https://discord.gg/vXJTPFQBHm");
        }

        public void OpenGuilded()
        {
            Application.OpenURL("https://guilded.gg/FFF40");
        }

        public void OpenSubreddit()
        {
            Application.OpenURL("https://reddit.com/r/fff40");
        }

        public void OpenTwitter()
        {
            StopAllCoroutines();
            StartCoroutine(OpenTwitterRoutine());
        }
        public IEnumerator OpenTwitterRoutine()
        {
            for (float a = 0; a < 1; a += Time.deltaTime)
            {
                XIcon.gameObject.SetActive(a < .5f);
                TwitterIcon.gameObject.SetActive(a >= .5f);
                XIcon.anchoredPosition = TwitterIcon.anchoredPosition = Vector2.right * Mathf.Cos(a * 20) * (1 - a) * (1 - a) * 5;
                yield return null;
            }
            XIcon.gameObject.SetActive(false);
            TwitterIcon.gameObject.SetActive(true);
            XIcon.anchoredPosition = TwitterIcon.anchoredPosition = Vector2.zero;
        
            // Trolled, open it anyways!
            Application.OpenURL("https://twitter.com/FFF40_Studios");
        }

        private const string FUN_HEADER   = "<i>Did you know:</i>\n";
        private const string CHART_HEADER = "<i>Charting tip:</i>\n";
        public static readonly System.Func<string>[] FlavorTexts = new System.Func<string>[] {
        
        
            // Game facts
            () => $"{FUN_HEADER}<b>JANOARG stands for \"Just Another Normal, Ordinary, Acceptable Rhythm Game\".",
            () => $"{FUN_HEADER}<b>Our team consists of a nursery of snails which are current developing this game at record snail speed.",
            () => $"{FUN_HEADER}<b>We chose the \"developing at snail speed\" subtitle because we believe that by the time this game officially releases it will become true to its name.",
            () => $"{FUN_HEADER}<b>Don't try to press that Twitter button below, we actually don't have a Twitter account.\n Future note: We do now!",
        
            // Chartmaker facts
            () => $"{FUN_HEADER}<b>You can click on the visualizer (metronome) on the top right corner to switch between different visualizers.",
            () => $"{FUN_HEADER}<b>You can drag the Timeline view with the middle mouse button and select with the right mouse button.",
            () => $"{FUN_HEADER}<b>Making messages for this message box takes somewhere about one half of our development time (or less. I'm pretty sure it's less.)",
            () => $"{FUN_HEADER}<b>The text info fields support <color=#800>r<color=#550>i<color=#070>c<color=#066>h <color=#009>t<color=#606>e<color=#800>x<color=#550>t<color=#070>!<color=#000><br>Check out TextMesh Pro rich text tags for how to use them!",

            // Meta facts
            () => $"{FUN_HEADER}<b>This randomly displayed text box may not always give 100% factual statements.",
            () => $"{FUN_HEADER}<b>There are " + FlavorTexts.Length + " total possible outcomes that this message box gives. Seriously.",
            () => $"{FUN_HEADER}<b>We don't have that many tips, unfortunately.",
            () => $"{FUN_HEADER}<b>Instead of closing and reopening the about window, you can click on this text box to reroll this message and obtain a new random one.",
            () => $"{FUN_HEADER}<b>There is no canonical term to specify this message box/text box/tip box/...what is it called again?",
            () => {
                var calendar = new System.Globalization.ChineseLunisolarCalendar();
                var now = System.DateTime.Now;
                return $"{FUN_HEADER}<b>Some messages here are dynamically generated. For example, today is " + calendar.GetYear(now) + "-" + calendar.GetMonth(now) + "-" + calendar.GetDayOfMonth(now) + " in East Asian lunisolar calendar.";      
            },
        
            // Snail facts
            () => $"{FUN_HEADER}<b>A nursery of snails is called an \"escargatoire\".",
            () => $"{FUN_HEADER}<b>A snail can live somewhere for 1~10 years in the wild, but in here they can live for 3. Just 3.",

            // Charting tips
            () => $"{CHART_HEADER}<b>Missing something? Want more features? Have message ideas to put here? Suggest them in our GitHub issues page and we <i>may</i> make them real!",
            () => $"{CHART_HEADER}<b>Be sure to make use of the keybindings, mastering them can be proven to be extremely helpful!",
            () => $"{CHART_HEADER}<b>Experiencing a chart block? Join our Discord server, it's where charters like you and us exchange tips and ideas!",
            () => $"{CHART_HEADER}<b>Make sure to test play your charts, playability is just as important as those lane-twisting effects!",
            () => $"{CHART_HEADER}<b>Sync is very important, make sure your Timing data is in sync with the song before starting working on anything else.",
            () => $"{CHART_HEADER}<b>Make sure to read these charting tips coming out of this text box, they are very helpful!",
            () => $"{CHART_HEADER}<b>Randomness is cool and all, but don't let this message box get in your way of doing the actual charting.",
            () => $"{CHART_HEADER}<b>Experiment with the preferences in the Options > Preferences window to find out which settings work best with your charting workflow.",
            () => $"{CHART_HEADER}<b>Make sure to respect the song artist's copyright guidelines before charting their song. If you're not sure you can distribute the chart without the song file!",
            () => $"{CHART_HEADER}<b>Watching the song's MV (if one exists) is the best way to get inspirations for your charts. Unless, it's a corny one, which you should maybe reconsider your choice of song.",
            () => $"{CHART_HEADER}<b>Want more tips? Our folks on our Discord server have plenty of them!",
        
            // Quotes
            () => "<b>🐌</b>\n<i><align=right>- 🐌",

            // Super secret easter egg
            () => "<i>Missing snail:</i>\n<b>We're trying to find a lost snail which is currently hiding somewhere inside the Picker. Can you find it?",
        };
    }
}
