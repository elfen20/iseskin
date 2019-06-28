using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Cave;
using Cave.Media;
using Cave.Media.Video;

namespace IseSkin
{
    public class IseSkin
    {
        IRenderer renderer;
        string assetDirectory = "";
        IniReader config;
        Dictionary<string, Size> sizes = new Dictionary<string, Size>();
        bool usePlaceHolders = false;
        float globalFontCorrection = 1f;

        public Dictionary<string, IseSprite> Sprites { get; private set; } = new Dictionary<string, IseSprite>();
        public ARGB BGColor { get; private set; } = ARGB.Transparent;

        public IseSkin(IRenderer newRenderer)
        {
            renderer = newRenderer;
        }

        public void Render()
        {
            renderer.Render(Sprites.Select(C => C.Value.Sprite).ToArray());
        }

        public void Clear()
        {
            foreach (var s in Sprites.Values)
            {
                s.Sprite.DeleteTexture();
            }
            Sprites.Clear();
            sizes.Clear();
        }

        public void LoadFromConfigFile(IniReader newConfig)
        {
            config = newConfig;
            Trace.TraceInformation("reloading config...");
            Clear();
            // get window config
            assetDirectory = newConfig.ReadString("Init", "AssetDir", "");
            renderer.AspectCorrection = newConfig.ReadEnum<ResizeMode>("Init", "GlobalAspectCorrection", ResizeMode.None);
            if (readARGB(newConfig, "Init", "BGColor", out ARGB vBGColor)) BGColor = vBGColor;
            usePlaceHolders = newConfig.ReadBool("Init", "UsePlaceHolders", false);
            if (readFloat(config, "Init", "GlobalFontCorrection", out float vCorrection)) globalFontCorrection = vCorrection;
            // parse ini data
            ReadSizes();            
            ReadSprites();        
            ReadSpriteValues();
        }

        public void ReadSpriteValues()
        {
            string currentSizeName = GetCurrentSizeName();
            foreach (KeyValuePair<string, IseSprite> item in Sprites)
            {
                // read default section
                ReadSpriteValuesFromSection(item.Key, item.Value);
                // read window size dependent section
                string sectionName = item.Key + "@" + currentSizeName;
                ReadSpriteValuesFromSection(sectionName, item.Value);
                item.Value.Fit();
            }
        }

        private void ReadSpriteValuesFromSection(string section, IseSprite iseSprite)
        {
            if (config.HasSection(section))
            {
                Trace.TraceInformation($"reading sprite data from section [{section}]");
                if (readVector3(config, section, "Position", out Vector3 vPos)) iseSprite.Position = vPos;
                if (readVector3(config, section, "Center", out Vector3 vCenter)) iseSprite.Sprite.CenterPoint = vCenter;
                if (readFloat(config, section, "Alpha", out float vAlpha)) iseSprite.Sprite.Alpha = vAlpha;
                if (readBool(config, section, "Visible", out bool vVisible)) iseSprite.Sprite.Visible = vVisible;

                if (readVector3(config, section, "MaxSize", out Vector3 vSize)) iseSprite.MaxSize = vSize;
                if (ReadEnum(config, section, "Alignment", out BoxAlignment aMode)) iseSprite.Alignment = aMode;

                if (usePlaceHolders)
                {
                    iseSprite.Sprite.Tint = iseSprite.TagColor;
                    iseSprite.Text = section;
                    iseSprite.AspectCorrection = ResizeMode.None;
                }
                else
                {
                    if (readARGB(config, section, "Tint", out ARGB vTint)) iseSprite.Sprite.Tint = vTint;
                    if (ReadEnum(config, section, "AspectCorrection", out ResizeMode rMode)) iseSprite.AspectCorrection = rMode;
                    if (iseSprite is IseText)
                    {
                        var iText = (iseSprite as IseText);
                        if (readFloat(config, section, "FontSize", out float fSize)) iText.FontSize = fSize;
                        if (readString(config, section, "FontName", out string vFontName)) iText.RenderText.FontName = vFontName;
                        if (readString(config, section, "Text", out string vText)) iText.Text = vText;
                        if (readARGB(config, section, "TextFColor", out ARGB vTFColor)) iText.RenderText.ForeColor = vTFColor;
                        if (readARGB(config, section, "TextBColor", out ARGB vTBColor)) iText.RenderText.BackColor = vTBColor;
                    }
                    else
                    {
                        string imageFileName = config.ReadString(section, "Image", string.Empty);
                        if (!string.IsNullOrEmpty(imageFileName))
                        {
                            iseSprite.LoadImage(Path.Combine(assetDirectory, imageFileName));
                        }
                    }
                }
            }
        }

        private void ReadSprites()
        {
            string[] newSprites = config.ReadSection("Sprites", true);
            foreach (string nSprite in newSprites)
            {
                var s = GetKeyValue(nSprite, "=");
                IRenderSprite sprite = renderer.CreateSprite(s.Key);
                IseSprite iseSprite;
                if (usePlaceHolders)
                {
                    iseSprite = new IseText(sprite, globalFontCorrection);
                }
                else
                {
                    switch (s.Value.ToLower())
                    {
                        case "text":
                            iseSprite = new IseText(sprite, globalFontCorrection);
                            break;
                        default:
                            iseSprite = new IseSprite(sprite);
                            break;
                    }
                }
                Sprites.Add(s.Key, iseSprite);
            }
        }

        private void ReadSizes()
        {
            string[] newSizes = config.ReadSection("Sizes", true);
            foreach (string nSize in newSizes)
            {
                var s = GetKeyValue(nSize, "=");
                if (readSize(config, "Sizes", s.Key, out Size size))
                {
                    sizes.Add(s.Key, size);
                }
            }
        }

        public string GetCurrentSizeName()
        {
            foreach (KeyValuePair<string, Size> item in sizes)
            {
                if ((renderer.Resolution.X < item.Value.Width) || (renderer.Resolution.Y < item.Value.Height))
                {
                    Trace.TraceInformation($"selected size: [{item.Key}]");
                    return item.Key;
                }
            }
            Trace.TraceInformation($"selected default size");
            return string.Empty;
        }

        public void FitSprites()
        {
            foreach (var iseSprite in Sprites.Values)
            {
                iseSprite.Fit();
            }
        }

        static KeyValuePair<string, string> GetKeyValue(string data, string seperator)
        {
            int position = data.IndexOf(seperator);
            if (position < 0)
            {
                return new KeyValuePair<string, string>(data, string.Empty);
            }
            else
            {
                return new KeyValuePair<string, string>(data.Substring(0, position), data.Substring(position + 1));
            }
        }

        public void SetText(string spriteName, string text)
        {
            if (usePlaceHolders) return;
            if (Sprites.ContainsKey(spriteName))
            {
                var sprite = Sprites[spriteName];
                if (sprite is IseText)
                {
                    (sprite as IseText).Text = text;
                    (sprite as IseText).Update();
                }
            }
        }

        public static Vector3 PositionTransform(Vector3 v)
        {
            return Vector3.Create(
                    v.X * 2f - 1f,
                    1f - v.Y * 2f,
                    v.Z
                );
        }

        public static bool ReadEnum<TEnum>(IniReader reader, string section, string name, out TEnum value) where TEnum : struct
        {
            string sValue = reader.ReadString(section, name, string.Empty);
            return Enum.TryParse(sValue, out value);
        }

        public static bool readFloat(IniReader reader, string section, string name, out float value)
        {
            string sValue = reader.ReadString(section, name, string.Empty);
            return float.TryParse(sValue, out value);
        }

        public static bool readString(IniReader reader, string section, string name, out string value)
        {
            value = string.Empty;
            return reader.GetValue(section, name, ref value);
        }

        public static bool readBool(IniReader reader, string section, string name, out bool value)
        {
            value = false;
            return reader.GetValue(section, name, ref value);
        }


        public static bool readVector3(IniReader reader, string section, string name, out Vector3 vector)
        {
            vector = Vector3.Empty;
            string[] parts = new string[3] { "0", "0", "0" };
            string sizeStr = reader.ReadString(section, name, "");
            string[] splitParts = sizeStr.Split(';');
            Array.Copy(splitParts, parts, splitParts.Length);
            if (parts.Length == 3)
            {
                float x, y, z;
                if (float.TryParse(parts[0], out x) && float.TryParse(parts[1], out y) && float.TryParse(parts[2], out z))
                {
                    vector = Vector3.Create(x, y, z);
                    return true;
                }
            }
            return false;
        }

        public static bool readARGB(IniReader reader, string section, string name, out ARGB color)
        {
            color = ARGB.Transparent;
            string value = "";
            if (reader.GetValue(section, name, ref value))
            {
                color = ARGB.FromString(value);
                return true;
            }
            return false;
        }

        public static bool readSize(IniReader reader, string section, string name, out Size size)
        {
            size = Size.Empty;
            string sizeStr = reader.ReadString(section, name, "");
            string[] parts = sizeStr.Split(';');
            if (parts.Length == 2)
            {
                int w, h;
                if (int.TryParse(parts[0], out w) && int.TryParse(parts[1], out h))
                {
                    size = new Size(w, h);
                    return true;
                }
            }
            return false;
        }

    }
}
