// Spotify Overlay - funny.games
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Kleiner JSON-Leser (Objekte -> Dictionary, Listen -> List, Zahlen -> double)
static class Json
{
    public static object Parse(string s)
    {
        int i = 0;
        return Value(s, ref i);
    }

    public static object Get(object o, string key)
    {
        Dictionary<string, object> d = o as Dictionary<string, object>;
        object v;
        if (d != null && d.TryGetValue(key, out v)) return v;
        return null;
    }

    public static string Str(object o, string key)
    {
        string s = Get(o, key) as string;
        return s ?? "";
    }

    static void Skip(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }

    static object Value(string s, ref int i)
    {
        Skip(s, ref i);
        char c = s[i];
        if (c == '{')
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            i++;
            Skip(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true)
            {
                Skip(s, ref i);
                string key = Text(s, ref i);
                Skip(s, ref i);
                i++;   // ':'
                d[key] = Value(s, ref i);
                Skip(s, ref i);
                if (s[i] == ',') { i++; continue; }
                i++;   // '}'
                return d;
            }
        }
        if (c == '[')
        {
            List<object> l = new List<object>();
            i++;
            Skip(s, ref i);
            if (s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Value(s, ref i));
                Skip(s, ref i);
                if (s[i] == ',') { i++; continue; }
                i++;   // ']'
                return l;
            }
        }
        if (c == '"') return Text(s, ref i);

        int st = i;
        while (i < s.Length && ",}] \r\n\t".IndexOf(s[i]) < 0) i++;
        string lit = s.Substring(st, i - st);
        if (lit == "true") return true;
        if (lit == "false") return false;
        if (lit == "null") return null;
        double n;
        double.TryParse(lit, NumberStyles.Float, CultureInfo.InvariantCulture, out n);
        return n;
    }

    static string Text(string s, ref int i)
    {
        StringBuilder sb = new StringBuilder();
        i++;   // oeffnendes "
        while (s[i] != '"')
        {
            if (s[i] == '\\')
            {
                i++;
                char e = s[i];
                if (e == 'n') sb.Append('\n');
                else if (e == 't') sb.Append('\t');
                else if (e == 'r') sb.Append('\r');
                else if (e == 'b') sb.Append('\b');
                else if (e == 'f') sb.Append('\f');
                else if (e == 'u')
                {
                    sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16));
                    i += 4;
                }
                else sb.Append(e);
            }
            else sb.Append(s[i]);
            i++;
        }
        i++;   // schliessendes "
        return sb.ToString();
    }
}
