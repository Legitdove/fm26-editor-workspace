using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace FM26PlayerExport.Handlers
{
    public static class UIUtils
    {
        public static string GameLang = "pt";

        public static string GetTrans(string key)
        {
            if (GameLang == "en") {
                if (key == "Amarelo") return "Yellow";
                if (key == "Vermelho") return "Red";
                if (key == "Sub In") return "Sub In";
                if (key == "Sub Out") return "Sub Out";
                if (key == "Lesão") return "Injured";
            } else if (GameLang == "es") {
                if (key == "Amarelo") return "Amarilla";
                if (key == "Vermelho") return "Roja";
                if (key == "Sub In") return "Entra";
                if (key == "Sub Out") return "Sale";
                if (key == "Lesão") return "Lesión";
            } else { // default as PT or fallback
                if (key == "Sub In") return "Entra";
                if (key == "Sub Out") return "Sai";
            }
            return key;
        }

        public static string GetText(
            VisualElement el,
            bool allowRenderedTextFallback = true,
            bool allowTooltipFallback = true)
        {
            if (el == null) return null;
            try { var te = el.TryCast<TextElement>(); if (te != null && !string.IsNullOrWhiteSpace(te.text)) return StripHtml(te.text.Trim()); } catch { }
            try { var lb = el.TryCast<Label>();       if (lb != null && !string.IsNullOrWhiteSpace(lb.text)) return StripHtml(lb.text.Trim()); } catch { }
            if (allowTooltipFallback)
            {
                try { var tip = el.tooltip; if (!string.IsNullOrWhiteSpace(tip)) return StripHtml(tip.Trim()); } catch { }
            }
            return null;
        }

        public static string StripHtml(string s)
            => string.IsNullOrEmpty(s) ? s : Regex.Replace(s, "<[^>]+>", string.Empty).Trim();

        public static string CollectFirstText(
            VisualElement el,
            int d = 0,
            bool allowRenderedTextFallback = true,
            bool allowTooltipFallback = true)
        {
            if (el == null || d > 20) return null;
            var t = GetText(el, allowRenderedTextFallback, allowTooltipFallback);
            if (t != null) return t;
            for (int i = 0; i < el.childCount; i++)
            {
                var r = CollectFirstText(el.ElementAt(i), d + 1, allowRenderedTextFallback, allowTooltipFallback);
                if (r != null) return r;
            }
            return null;
        }
        
        public static string CollectFirstTooltip(VisualElement el, int d = 0)
        {
            if (el == null || d > 20) return null;
            try { var tip = el.tooltip; if (!string.IsNullOrWhiteSpace(tip)) return StripHtml(tip.Trim()); } catch { }
            for (int i = 0; i < el.childCount; i++) { var r = CollectFirstTooltip(el.ElementAt(i), d+1); if (r != null) return r; }
            return null;
        }

        public static string CollectAllTextsJoined(
            VisualElement el,
            int d = 0,
            bool allowRenderedTextFallback = true,
            bool allowTooltipFallback = true)
        {
            if (el == null || d > 20) return "";
            var list = new List<string>();
            CollectAllTexts(el, list, 0, allowRenderedTextFallback, allowTooltipFallback);
            return string.Join(" ", list).Trim();
        }

        public static void CollectAllTexts(
            VisualElement el,
            List<string> out_,
            int d = 0,
            bool allowRenderedTextFallback = true,
            bool allowTooltipFallback = true)
        {
            if (el == null || d > 20) return;
            var t = GetText(el, allowRenderedTextFallback, allowTooltipFallback);
            if (t != null && !out_.Contains(t)) out_.Add(t);
            if (allowTooltipFallback)
            {
                try
                {
                    var tip = el.tooltip;
                    if (!string.IsNullOrWhiteSpace(tip))
                    {
                        var ts = StripHtml(tip.Trim());
                        if (!out_.Contains(ts)) out_.Add(ts);
                    }
                }
                catch { }
            }
            for (int i = 0; i < el.childCount; i++)
                CollectAllTexts(el.ElementAt(i), out_, d + 1, allowRenderedTextFallback, allowTooltipFallback);
        }

        public static StarRatingResult TryReadStarRating(VisualElement cell)
        {
            var starClassLists = new List<List<string>>();
            CollectStarClassLists(cell, cell, starClassLists, 0);
            if (starClassLists.Count == 0) return null;

            if (!StarRatingParser.TryParseRating(starClassLists, out StarRatingResult rating))
            {
                Plugin.Log.LogWarning($"[FM26Export] Unrecognised star cell structure; exporting blank. Classes: {string.Join(" | ", starClassLists.ConvertAll(classes => string.Join(",", classes)))}");
                return null;
            }

            return rating;
        }

        public static string TryReadStars(VisualElement cell)
        {
            var rating = TryReadStarRating(cell);
            return rating == null ? null : FormatStarRating(rating.DisplayedStars, true);
        }

        public static string FormatStarRating(float rating, bool blankWhenZero)
        {
            if (blankWhenZero && rating <= 0f) return string.Empty;
            return rating.ToString("0.#", CultureInfo.InvariantCulture).Replace(".", ",");
        }

        private static bool IsVisibleForStarRead(VisualElement element, VisualElement cell)
        {
            VisualElement current = element;
            while (current != null)
            {
                try
                {
                    if (current.resolvedStyle.display == DisplayStyle.None
                        || current.resolvedStyle.visibility == Visibility.Hidden
                        || current.resolvedStyle.opacity <= 0f)
                    {
                        return false;
                    }
                }
                catch
                {
                }

                if (current == cell)
                    break;

                current = current.parent;
            }

            return true;
        }

        private static void CollectStarClassLists(
            VisualElement element,
            VisualElement cell,
            List<List<string>> starClassLists,
            int depth)
        {
            if (element == null || depth > 12) return;

            if (!IsVisibleForStarRead(element, cell))
                return;

            try
            {
                var classes = new List<string>();
                for (int i = 0; i < element.classList.Count; i++)
                    classes.Add(element.classList[i]);

                if (element.childCount == 0
                    && StarRatingParser.Classify(classes) != StarVisualState.NotStar)
                {
                    starClassLists.Add(classes);
                }
            }
            catch { }

            for (int i = 0; i < element.childCount; i++)
                CollectStarClassLists(element.ElementAt(i), cell, starClassLists, depth + 1);
        }

        public static string RowKey(List<string> vals)
        {
            if (vals == null || vals.Count == 0) return string.Empty;
            return string.Join("|", vals);
        }

        public static VisualElement FindByName(VisualElement el, string name)
        {
            if (el == null) return null;
            if (el.name == name) return el;
            for (int i = 0; i < el.childCount; i++) { var r = FindByName(el.ElementAt(i), name); if (r != null) return r; }
            return null;
        }

        public static string Esc(string v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            v = v.Replace("\r", " ").Replace("\n", " ");
            string q = new string(new char[]{ (char)34 });
            if (v.Contains(";") || v.Contains(q)) v = q + v.Replace(q, q + q) + q;
            return v;
        }

        public static string LerIconesComoTexto(VisualElement el, int d = 0)
        {
            if (el == null || d > 6) return "";
            var res = new List<string>();
            try {
                for (int c = 0; c < el.classList.Count; c++)
                {
                    string cls = el.classList[c].ToLower();
                    if (cls.Contains("yellow")) res.Add(GetTrans("Amarelo"));
                    if (cls.Contains("red")) res.Add(GetTrans("Vermelho"));
                    if (cls.Contains("sub") && !cls.Contains("subject")) {
                        if (cls.Contains("on") || cls.Contains("in")) res.Add(GetTrans("Sub In"));
                        else if (cls.Contains("off") || cls.Contains("out")) res.Add(GetTrans("Sub Out"));
                        else res.Add("Sub");
                    }
                    if (cls.Contains("injur")) res.Add(GetTrans("Lesão"));
                    if (cls.Contains("condition") || cls.Contains("heart") || cls.Contains("sharpness")) res.Add("Coração");
                    if (cls.Contains("fatigue") || cls.Contains("tired")) res.Add("Fadigado");
                }
                string tip = el.tooltip;
                if (!string.IsNullOrWhiteSpace(tip) && res.Count > 0) {
                     string t = StripHtml(tip);
                     if (t.Length < 40) {
                         // Evita sobrescrever uma substituição validada com um tooltip genérico ("Entra")
                         string lastRes = res[res.Count - 1];
                         if (lastRes != GetTrans("Sub In") && lastRes != GetTrans("Sub Out")) 
                         {
                             res[res.Count - 1] = t;
                         }
                     }
                }
            } catch {}
            for(int i=0; i<el.childCount;i++){
                string sub = LerIconesComoTexto(el.ElementAt(i), d+1);
                if (!string.IsNullOrEmpty(sub)) {
                    foreach(var s in sub.Split(new string[]{" | "}, System.StringSplitOptions.RemoveEmptyEntries)) 
                        if (!res.Contains(s)) res.Add(s);
                }
            }
            return string.Join(" | ", res);
        }

        public static string DiagCell(VisualElement el, int d = 0)
        {
            if (el == null || d > 6) return string.Empty;
            var clsSb = new StringBuilder();
            try { for (int c = 0; c < el.classList.Count; c++) { if (c>0) clsSb.Append(','); clsSb.Append(el.classList[c]); } } catch { }
            string t = GetText(el) ?? string.Empty;
            var sb = new StringBuilder();
            sb.Append($"{new string('-',d)}{el.GetType().Name}[cls={clsSb},ch={el.childCount},txt={t}] ");
            for (int i = 0; i < el.childCount; i++) sb.Append(DiagCell(el.ElementAt(i), d+1));
            return sb.ToString();
        }
    }
}
