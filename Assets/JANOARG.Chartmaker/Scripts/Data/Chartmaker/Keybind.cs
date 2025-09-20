using System.Collections.Generic;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker
{
    [System.Serializable]
    public class Keybind
    {
        public KeyCode        KeyCode   = KeyCode.Space;
        public EventModifiers Modifiers = EventModifiers.None;

        public Keybind () {}

        public Keybind (KeyCode keyCode, EventModifiers modifiers = EventModifiers.None)
        {
            KeyCode = keyCode;
            Modifiers = modifiers;
        }

        public Keybind (Event ev)
        {
            KeyCode = ev.keyCode;
            Modifiers = ev.modifiers & (
                EventModifiers.Shift 
                | EventModifiers.Alt 
                | EventModifiers.Control 
                | EventModifiers.Command
            );
        }

        public bool Matches(Event ev)
        {
            const EventModifiers cas = EventModifiers.Shift | EventModifiers.Alt | EventModifiers.Control | EventModifiers.Command;
        
            if (Application.platform != RuntimePlatform.OSXEditor && ev.control) 
                ev.modifiers = ev.modifiers ^ EventModifiers.Control | EventModifiers.Command;
        
            return ev.keyCode == KeyCode && (ev.modifiers & cas) == Modifiers;
        }

        public static bool operator == (Event ev, Keybind keybind) => 
            keybind != null && keybind.Matches(ev);

        public static bool operator != (Event ev, Keybind keybind) => 
            keybind != null && !keybind.Matches(ev);

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString() 
        {
            string keyCode = KeyCode.ToString();

            switch (KeyCode)
            {
                case >= KeyCode.Alpha0 and <= KeyCode.Alpha9: 
                    keyCode = ((int)KeyCode - 48).ToString(); break;
            
                case >= KeyCode.A and <= KeyCode.Z:
                    break;
            
                case >= KeyCode.Exclaim and <= KeyCode.Tilde: 
                    keyCode = (char)KeyCode + ""; break;
            
                case KeyCode.UpArrow:    keyCode = "↑"; break;
                case KeyCode.DownArrow:  keyCode = "↓"; break;
                case KeyCode.LeftArrow:  keyCode = "←"; break;
                case KeyCode.RightArrow: keyCode = "→"; break;

            }

            if (Application.platform is RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor)
            {
                if ((Modifiers & EventModifiers.Shift) > 0)   keyCode = "⇧" + keyCode;
                if ((Modifiers & EventModifiers.Alt) > 0)     keyCode = "⌥" + keyCode;
                if ((Modifiers & EventModifiers.Command) > 0) keyCode = "⌘" + keyCode;
                if ((Modifiers & EventModifiers.Control) > 0) keyCode = "⌃" + keyCode;
            }
            else 
            {
                if ((Modifiers & EventModifiers.Shift) > 0) keyCode = "Shift+" + keyCode;
                if ((Modifiers & EventModifiers.Alt) > 0)   keyCode = "Alt+" +   keyCode;
                if ((Modifiers & (EventModifiers.Command | EventModifiers.Control)) > 0) keyCode = "Ctrl+" + keyCode;
            }

            return keyCode;
        }

        public string ToUnityHotkeyString() 
        {
            string key = KeyCode.ToString();
            switch (KeyCode)
            {
                case KeyCode.UpArrow:    key = "UP";    break;
                case KeyCode.DownArrow:  key = "DOWN";  break;
                case KeyCode.LeftArrow:  key = "LEFT";  break;
                case KeyCode.RightArrow: key = "RIGHT"; break;
                case KeyCode.Home:       key = "HOME";  break;
                case KeyCode.End:        key = "END";   break;
                case KeyCode.PageUp:     key = "PGUP";  break;
                case KeyCode.PageDown:   key = "PGDN";  break;
                case KeyCode.Insert:     key = "INS";   break;
                case KeyCode.Delete:     key = "DEL";   break;
                case KeyCode.Tab:        key = "TAB";   break;
                case KeyCode.Space:      key = "SPACE"; break;

                case >= KeyCode.A and <= KeyCode.Z: break;
            
                case >= KeyCode.Exclaim and <= KeyCode.Tilde: key = (char)KeyCode + ""; break;
            }

            if ((Modifiers & EventModifiers.Shift) > 0)   key = "#" + key;
            if ((Modifiers & EventModifiers.Alt) > 0)     key = "&" + key;
            if ((Modifiers & EventModifiers.Control) > 0) key = "^" + key;
            if ((Modifiers & EventModifiers.Command) > 0) key = "%" + key;
            if (Modifiers == 0)                           key = "_" + key;
        
            return key;
        }

        public string ToSaveString()
        {
            string key = ((int)KeyCode).ToString();

            if ((Modifiers & EventModifiers.Shift) > 0)   key = "#" + key;
            if ((Modifiers & EventModifiers.Alt) > 0)     key = "&" + key;
            if ((Modifiers & EventModifiers.Control) > 0) key = "^" + key;
            if ((Modifiers & EventModifiers.Command) > 0) key = "%" + key;

            return key;
        }

        public static Keybind FromSaveString(string str)
        {
            EventModifiers mod = EventModifiers.None;
        
            if (str.StartsWith("%")) { mod |= EventModifiers.Command; str = str[1..]; }
            if (str.StartsWith("^")) { mod |= EventModifiers.Control; str = str[1..]; }
            if (str.StartsWith("&")) { mod |= EventModifiers.Alt;     str = str[1..]; }
            if (str.StartsWith("#")) { mod |= EventModifiers.Shift;   str = str[1..]; }

            return new Keybind((KeyCode)int.Parse(str), mod);
        }
    }

    public class KeybindAction
    {
        public string Name;
        public string Category;
 
        public Keybind       Keybind;
        public System.Action Invoke;
    }

    public class KeybindActionList: Dictionary<string, KeybindAction>
    {

        public void LoadKeys()
        {
            foreach (KeyValuePair<string, KeybindAction> action in this) 
            {
                string str = Behaviors.Chartmaker.Chartmaker.main.KeybindingsStorage.Get(action.Key, (string)null);
            
                if (!string.IsNullOrWhiteSpace(str)) 
                    action.Value.Keybind = Keybind.FromSaveString(str);
            }
        }

        public void HandleEvent(Event ev)
        {
            foreach (KeybindAction action in this.Values)
            {
                if (!action.Keybind.Matches(ev)) 
                    continue;

                action.Invoke();
           
                ev.Use();
                break;
            }
        }


        public Dictionary<string, Dictionary<string, KeybindAction>> MakeCategoryGroups()
        {
            Dictionary<string, Dictionary<string, KeybindAction>> dict = new();
            foreach (KeyValuePair<string, KeybindAction> action in this) 
            {
                if (!dict.ContainsKey(action.Value.Category))
                    dict.Add(action.Value.Category, new Dictionary<string, KeybindAction>());
            
                dict[action.Value.Category].Add(action.Key, action.Value);
            }
            return dict;
        }
    }
}