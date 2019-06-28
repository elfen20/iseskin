using System;
using System.Diagnostics;
using Cave.Media;
using Cave.Media.Video;

namespace IseSkin
{
    public class IseSprite
    {        
        public IRenderSprite Sprite { get; private set; }
        public ResizeMode AspectCorrection = ResizeMode.None;
        public BoxAlignment Alignment = BoxAlignment.Center;
        public Vector3 Position = Vector3.Create(0.5f,0.5f,0);
        public Vector3 MaxSize = Vector3.Create(1f, 1f, 1f);
        public ARGB TagColor { get; private set; }

        string imageFileName;
        string text = string.Empty;

        public void LoadImage(string fileName)
        {
            if (fileName != imageFileName)
            {
                try
                {
                    using (Bitmap32 image = Bitmap32.FromFile(fileName))
                    {
                        Sprite.LoadTexture(image);
                    }
                    imageFileName = fileName;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error on loading image: " + ex.Message);
                }
            }
        }

        public void AlignSprite()
        {
            Vector3 translate = Vector3.Empty;
            switch (Alignment & BoxAlignment.XFlags)
            {
                case BoxAlignment.Left:
                    translate.X = (Sprite.Scale.X - MaxSize.X) / 2f;
                    break;
                case BoxAlignment.Center:
                    translate.X = 0;
                    break;
                case BoxAlignment.Right:
                    translate.X = (MaxSize.X - Sprite.Scale.X) / 2f;
                    break;
                default:
                    throw new Exception(string.Format("Invalid alignment '{0}'!", Alignment & BoxAlignment.XFlags));
            }
            switch (Alignment & BoxAlignment.YFlags)
            {
                case BoxAlignment.Top:
                    translate.Y = (Sprite.Scale.Y - MaxSize.Y) / 2f;
                    break;
                case BoxAlignment.Center:
                    translate.Y = 0;
                    break;
                case BoxAlignment.Bottom:
                    translate.Y = (MaxSize.Y - Sprite.Scale.Y) / 2f;
                    break;
                default:
                    throw new Exception(string.Format("Invalid alignment '{0}'!", Alignment & BoxAlignment.YFlags));
            }
            Sprite.Position = IseSkin.PositionTransform(Position + translate);
        }

        public virtual string Text
        {
            get { return text; }
            set { text = value; }
        }

        public virtual void Fit()
        {
            Sprite.Scale = Sprite.ScaleFromSizeNorm(MaxSize.X, MaxSize.Y, AspectCorrection);
            AlignSprite();
        }

        public IseSprite(IRenderSprite sprite)
        {
            Sprite = sprite;
            ARGB col = ARGB.Random;
            col.Alpha = 100;
            TagColor = col;
        }
    }

    public class IseText : IseSprite
    {
        public IRenderText RenderText { get; private set; }
        public float FontSize = 0.1f;
        public float FontCorrection = 1f;

        public IseText(IRenderSprite sprite, float fontCorrection = 1f) : base(sprite)
        {
            AspectCorrection = ResizeMode.TouchFromInside;
            RenderText = new RenderText(sprite);
            RenderText.FontSize = 16f;
            FontCorrection = fontCorrection;
        }

        public override string Text
        {
            get { return RenderText.Text; }
            set
            {
                RenderText.Text = value;
            }
        }

        public override void Fit()
        {
            Update();
            base.Fit();
        }

        public void Update()
        {
            //RenderText.BackColor = Color.Wheat;
            int w = (int)(Sprite.Renderer.Resolution.X * MaxSize.X);
            int h = (int)(Sprite.Renderer.Resolution.Y * MaxSize.Y);
            RenderText.FontSize = Sprite.Renderer.Resolution.Y * FontSize * FontCorrection;
            RenderText.Update(w,h);
        }
    }
}
