#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// Kleine eigen WinForms MathML-renderer voor Syscalculator/NOD.
/// 
/// Ondersteunt een bruikbare Presentation MathML-subset:
/// math, mrow, mi, mn, mo, mtext, span,
/// mfrac, msup, msub, msubsup,
/// msqrt, mroot,
/// mover, munder, munderover.
/// 
/// Daardoor kunnen ook integralen en limieten worden getekend:
/// - integraal met grenzen: msubsup + mo integral
/// - limiet: munder + mo lim
/// </summary>
public sealed class MathMlPreviewControl : UserControl
{
    private string _mathMarkup = "";

    public string MathMarkup
    {
        get => _mathMarkup;
        set
        {
            _mathMarkup = value ?? "";
            LastRenderError = null;
            Invalidate();
        }
    }

    public string? LastRenderError { get; private set; }

    // Zoek/commentaar: Constructor: maakt en initialiseert MathMlPreviewControl.
    public MathMlPreviewControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        BackColor = Color.FromArgb(250, 250, 250);
        ForeColor = Color.FromArgb(20, 20, 20);
        Font = CreatePreferredFont(13f);
        MinimumSize = new Size(80, 34);
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreatePreferredFont.
    private static Font CreatePreferredFont(float size)
    {
        // Cambria Math is standaard op moderne Windows en is goed voor integral, sqrt, lim, super/subscript.
        try
        {
            return new Font("Cambria Math", size, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            return new Font("Segoe UI Symbol", size, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    // Zoek/commentaar: Methode OnPaint: centrale logica voor deze stap.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.Clear(BackColor);

        var box = BuildBox(_mathMarkup);
        box.Measure(e.Graphics, Font);

        const float padding = 8f;
        var x = padding;
        var y = Math.Max(padding, (ClientSize.Height - box.Height) / 2f);

        using var brush = new SolidBrush(ForeColor);
        using var pen = new Pen(ForeColor, Math.Max(1f, Font.Size / 12f));
        box.Draw(e.Graphics, Font, brush, pen, x, y);
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildBox.
    private MathBox BuildBox(string markup)
    {
        if (string.IsNullOrWhiteSpace(markup))
            return new GlyphBox("");

        try
        {
            // MathML uit HTML bevat soms named entities die XML niet kent.
            // Eerst normaliseren, anders valt XDocument.Parse stil terug naar plain text.
            var safeMarkup = NormalizeEntities(markup);

            // MathML-fragmenten hebben soms meerdere root-elementen.
            // Daarom altijd verpakken in <root>.
            var xml = XDocument.Parse("<root>" + safeMarkup + "</root>", LoadOptions.PreserveWhitespace);
            return ParseContainer(xml.Root!);
        }
        catch (Exception ex)
        {
            LastRenderError = ex.GetType().Name + ": " + ex.Message;
            return new GlyphBox(MarkupToPlainText(markup));
        }
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseContainer.
    private static MathBox ParseContainer(XElement element)
    {
        var boxes = element.Nodes()
            .Select(ParseNode)
            .Where(b => b is not null && !b.IsEmpty)
            .Cast<MathBox>()
            .ToList();

        return boxes.Count switch
        {
            0 => new GlyphBox(""),
            1 => boxes[0],
            _ => new RowBox(boxes)
        };
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseNode.
    private static MathBox? ParseNode(XNode node)
    {
        if (node is XText text)
        {
            var value = NormalizeText(WebUtility.HtmlDecode(text.Value));
            return string.IsNullOrWhiteSpace(value) ? null : new GlyphBox(value);
        }

        if (node is not XElement element)
            return null;

        var name = element.Name.LocalName.ToLowerInvariant();

        switch (name)
        {
            case "root":
            case "math":
            case "mrow":
                return StyleBox(element, ParseContainer(element));

            case "semantics":
            case "mstyle":
            case "mpadded":
            case "menclose":
            case "maction":
                return StyleBox(element, ParseContainer(element));

            case "annotation":
            case "annotation-xml":
                return new GlyphBox("");

            case "mspace":
            {
                var width = element.Attribute("width")?.Value ?? "8";
                return StyleBox(element, new SpaceBox(ParseLength(width, 8f)));
            }

            case "mi":
            case "mn":
            case "mtext":
            case "ms":
            case "span":
                if (element.Elements().Any())
                    return StyleBox(element, ParseContainer(element));

                return StyleBox(element, new GlyphBox(NormalizeText(WebUtility.HtmlDecode(element.Value))));

            case "mo":
                if (element.Elements().Any())
                    return StyleBox(element, ParseContainer(element));

                var opText = NormalizeText(WebUtility.HtmlDecode(element.Value));
                if (IsFunctionName(opText))
                    return StyleBox(element, new FunctionBox(opText));

                if (IsLargeOperator(opText))
                    return StyleBox(element, new LargeOperatorBox(opText));

                return StyleBox(element, IsMathAxisOperator(opText)
                    ? new OperatorBox(opText)
                    : new GlyphBox(opText));

            case "mfrac":
            {
                var children = element.Elements().ToList();
                if (children.Count >= 2)
                    return StyleBox(element, new FractionBox(ParseElement(children[0]), ParseElement(children[1])));

                return StyleBox(element, new GlyphBox(NormalizeText(WebUtility.HtmlDecode(element.Value))));
            }

            case "msup":
            {
                var children = element.Elements().ToList();
                if (children.Count >= 2)
                    return StyleBox(element, new SuperscriptBox(ParseElement(children[0]), ParseElement(children[1])));

                return StyleBox(element, new GlyphBox(NormalizeText(WebUtility.HtmlDecode(element.Value))));
            }

            case "msub":
            {
                var children = element.Elements().ToList();
                if (children.Count >= 2)
                    return StyleBox(element, new SubscriptBox(ParseElement(children[0]), ParseElement(children[1])));

                return StyleBox(element, new GlyphBox(NormalizeText(WebUtility.HtmlDecode(element.Value))));
            }

            case "msubsup":
            {
                var children = element.Elements().ToList();
                if (children.Count >= 3)
                    return StyleBox(element, new SubSupBox(ParseElement(children[0]), ParseElement(children[1]), ParseElement(children[2])));

                return StyleBox(element, new GlyphBox(NormalizeText(WebUtility.HtmlDecode(element.Value))));
            }

            case "msqrt":
                return StyleBox(element, new SqrtBox(ParseContainer(element)));

            case "mroot":
            {
                var children = element.Elements().ToList();
                if (children.Count >= 2)
                    return StyleBox(element, new RootBox(ParseElement(children[0]), ParseElement(children[1])));

                return StyleBox(element, new SqrtBox(ParseContainer(element)));
            }

            case "mover":
            {
                var children = element.Elements().ToList();
                if (children.Count >= 2)
                    return StyleBox(element, new OverBox(ParseElement(children[0]), ParseElement(children[1])));

                return StyleBox(element, ParseContainer(element));
            }

            case "munder":
            {
                var children = element.Elements().ToList();
                if (children.Count >= 2)
                    return StyleBox(element, new UnderBox(ParseElement(children[0]), ParseElement(children[1])));

                return StyleBox(element, ParseContainer(element));
            }

            case "munderover":
            {
                var children = element.Elements().ToList();
                if (children.Count >= 3)
                    return StyleBox(element, new UnderOverBox(ParseElement(children[0]), ParseElement(children[1]), ParseElement(children[2])));

                return StyleBox(element, ParseContainer(element));
            }

            case "mfenced":
            {
                var open = element.Attribute("open")?.Value ?? "(";
                var close = element.Attribute("close")?.Value ?? ")";
                return StyleBox(element, new RowBox(new MathBox[]
                {
                    new GlyphBox(open),
                    ParseContainer(element),
                    new GlyphBox(close)
                }));
            }

            default:
                return element.Elements().Any()
                    ? ParseContainer(element)
                    : new GlyphBox(NormalizeText(WebUtility.HtmlDecode(element.Value)));
        }
    }

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseElement.
    private static MathBox ParseElement(XElement element)
        => ParseNode(element) ?? new GlyphBox("");

    // Zoek/commentaar: Methode StyleBox: centrale logica voor deze stap.
    private static MathBox StyleBox(XElement element, MathBox box)
    {
        if (TryReadColor(element, out var color))
            return new ColorBox(box, color);

        return box;
    }

    // Zoek/commentaar: Probeert deze actie uit te voeren en geeft succes/mislukking terug voor TryReadColor.
    private static bool TryReadColor(XElement element, out Color color)
    {
        color = Color.Empty;

        var value =
            element.Attribute("mathcolor")?.Value ??
            element.Attribute("color")?.Value ??
            element.Attribute("foreground")?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            var style = element.Attribute("style")?.Value;
            if (!string.IsNullOrWhiteSpace(style))
            {
                var match = Regex.Match(style, @"(?:^|;)\s*color\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                    value = match.Groups[1].Value.Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();

        try
        {
            if (value.StartsWith("#", StringComparison.Ordinal))
            {
                color = ColorTranslator.FromHtml(value);
                return true;
            }

            color = Color.FromName(value);
            return color.IsKnownColor || color.IsNamedColor;
        }
        catch
        {
            return false;
        }
    }

    // Zoek/commentaar: Normaliseert invoertekst naar een vaste vorm voor NormalizeEntities.
    private static string NormalizeEntities(string markup)
    {
        return markup
            .Replace("&InvisibleTimes;", "\u00B7")
            .Replace("&ApplyFunction;", "")
            .Replace("&DifferentialD;", "d")
            .Replace("&dd;", "d")
            .Replace("&nbsp;", " ")
            .Replace("&thinsp;", " ")
            .Replace("&ThickSpace;", " ")
            .Replace("&MediumSpace;", " ")
            .Replace("&pi;", "\u03C0")
            .Replace("&infin;", "\u221E")
            .Replace("&Integral;", "\u222B")
            .Replace("&sum;", "\u2211")
            .Replace("&prod;", "\u220F")
            .Replace("&minus;", "\u2212")
            .Replace("&times;", "\u00D7")
            .Replace("&divide;", "\u00F7")
            .Replace("&rarr;", "\u2192")
            .Replace("&RightArrow;", "\u2192")
            .Replace("&larr;", "\u2190")
            .Replace("&LeftArrow;", "\u2190")
            .Replace("&le;", "\u2264")
            .Replace("&ge;", "\u2265")
            .Replace("&ne;", "\u2260")
            .Replace("&alpha;", "\u03B1")
            .Replace("&beta;", "\u03B2")
            .Replace("&gamma;", "\u03B3")
            .Replace("&delta;", "\u03B4")
            .Replace("&theta;", "\u03B8")
            .Replace("&lambda;", "\u03BB")
            .Replace("&mu;", "\u03BC")
            .Replace("&sigma;", "\u03C3")
            .Replace("&omega;", "\u03C9");
    }
    // Zoek/commentaar: Normaliseert invoertekst naar een vaste vorm voor NormalizeText.
    private static string NormalizeText(string text)
    {
        return text
            .Replace("&#x21D2;", "\u21D2")
            .Replace("&#x2192;", "\u2192")
            .Replace("&#xD7;", "\u00D7")
            .Replace("&InvisibleTimes;", "\u00B7")
            .Trim();
    }
    // Zoek/commentaar: Maakt MathML/HTML-markup leesbaar als platte tekst.
    private static string MarkupToPlainText(string markup)
    {
        var text = markup;
        text = text.Replace("&#x21D2;", "\u21D2");
        text = text.Replace("&#x2192;", "\u2192");
        text = text.Replace("&#xD7;", "\u00D7");
        text = Regex.Replace(text, "<[^>]+>", "");
        return WebUtility.HtmlDecode(text).Trim();
    }
    // Zoek/commentaar: Herkent grote operatoren zoals integraal, som en product.
    private static bool IsLargeOperator(string text)
        => text is "\u222B" or "\u2211" or "\u220F";
    // Zoek/commentaar: Herkent operatoren die visueel op de wiskundige as moeten staan.
    private static bool IsMathAxisOperator(string text)
        => text is "=" or "+" or "\u2212" or "-" or "\u00B1" or "\u2213" or "\u00D7" or "\u00F7" or "\u2192" or "\u2190" or "\u2264" or "\u2265" or "\u2260";
    // Zoek/commentaar: Methode IsFunctionName: centrale logica voor deze stap.
    private static bool IsFunctionName(string text)
        => text is "sin" or "cos" or "tan" or "asin" or "acos" or "atan"
            or "log" or "ln" or "sqrt" or "abs" or "exp";

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseLength.
    private static float ParseLength(string value, float fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var match = Regex.Match(value, @"-?\d+(\.\d+)?");
        if (!match.Success)
            return fallback;

        return float.TryParse(
            match.Value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result)
            ? Math.Max(0, result)
            : fallback;
    }

    // Zoek/commentaar: Type-overzicht: class MathBox bevat de hoofdlogica/data voor dit onderdeel.
    private abstract class MathBox
    {
        public float Width { get; protected set; }
        public float Height { get; protected set; }
        public float Baseline { get; protected set; }
        public virtual bool IsEmpty => false;

        public abstract void Measure(Graphics graphics, Font font);
        public abstract void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y);

        // Zoek/commentaar: Methode FontBaseline: centrale logica voor deze stap.
        protected static float FontBaseline(Graphics graphics, Font font)
        {
            var family = font.FontFamily;
            var ascent = family.GetCellAscent(font.Style);
            var lineSpacing = family.GetLineSpacing(font.Style);
            return font.GetHeight(graphics) * ascent / lineSpacing;
        }

        // Zoek/commentaar: Methode SmallFont: centrale logica voor deze stap.
        protected static Font SmallFont(Font font)
            => new(font.FontFamily, Math.Max(7f, font.Size * 0.68f), font.Style, font.Unit);
    }

    // Zoek/commentaar: Type-overzicht: class GlyphBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class GlyphBox : MathBox
    {
        private readonly string _text;

        // Zoek/commentaar: Constructor: maakt en initialiseert GlyphBox.
        public GlyphBox(string text)
        {
            _text = text ?? "";
        }

        public override bool IsEmpty => string.IsNullOrWhiteSpace(_text);

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            if (string.IsNullOrEmpty(_text))
            {
                Width = 0;
                Height = font.GetHeight(graphics);
                Baseline = FontBaseline(graphics, font);
                return;
            }

            var size = graphics.MeasureString(
                _text,
                font,
                int.MaxValue,
                StringFormat.GenericTypographic);

            Width = Math.Max(1, size.Width);
            Height = Math.Max(font.GetHeight(graphics), size.Height);
            Baseline = FontBaseline(graphics, font);
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            graphics.DrawString(_text, font, brush, x, y, StringFormat.GenericTypographic);
        }
    }

    // Zoek/commentaar: Type-overzicht: class ColorBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class ColorBox : MathBox
    {
        private readonly MathBox _inner;
        private readonly Color _color;

        // Zoek/commentaar: Constructor: maakt en initialiseert ColorBox.
        public ColorBox(MathBox inner, Color color)
        {
            _inner = inner;
            _color = color;
        }

        public override bool IsEmpty => _inner.IsEmpty;

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            _inner.Measure(graphics, font);
            Width = _inner.Width;
            Height = _inner.Height;
            Baseline = _inner.Baseline;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var localBrush = new SolidBrush(_color);
            using var localPen = new Pen(_color, pen.Width);
            _inner.Draw(graphics, font, localBrush, localPen, x, y);
        }
    }

    // Zoek/commentaar: Type-overzicht: class FunctionBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class FunctionBox : MathBox
    {
        private readonly string _text;

        // Zoek/commentaar: Constructor: maakt en initialiseert FunctionBox.
        public FunctionBox(string text)
        {
            _text = text ?? "";
        }

        public override bool IsEmpty => string.IsNullOrWhiteSpace(_text);

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            // Functienamen zoals sin, log en ln horen rechtop en op de gewone baseline.
            using var functionFont = new Font(font.FontFamily, font.Size, FontStyle.Regular, font.Unit);

            var size = graphics.MeasureString(
                _text,
                functionFont,
                int.MaxValue,
                StringFormat.GenericTypographic);

            Width = Math.Max(1, size.Width);
            Height = Math.Max(functionFont.GetHeight(graphics), size.Height);
            Baseline = FontBaseline(graphics, functionFont);
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var functionFont = new Font(font.FontFamily, font.Size, FontStyle.Regular, font.Unit);
            graphics.DrawString(_text, functionFont, brush, x, y, StringFormat.GenericTypographic);
        }
    }

    // Zoek/commentaar: Type-overzicht: class OperatorBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class OperatorBox : MathBox
    {
        private readonly string _text;

        // Zoek/commentaar: Constructor: maakt en initialiseert OperatorBox.
        public OperatorBox(string text)
        {
            _text = text ?? "";
        }

        public override bool IsEmpty => string.IsNullOrWhiteSpace(_text);

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            var size = graphics.MeasureString(
                _text,
                font,
                int.MaxValue,
                StringFormat.GenericTypographic);

            Width = Math.Max(1, size.Width);
            Height = Math.Max(font.GetHeight(graphics), size.Height);

            // Voor +, =, pijl enz. is de "wiskundige as" belangrijker dan de tekst-baseline.
            // Hiermee lijnen operators visueel beter uit met breukstrepen.
            Baseline = Height * 0.53f;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            graphics.DrawString(_text, font, brush, x, y, StringFormat.GenericTypographic);
        }
    }

    // Zoek/commentaar: Type-overzicht: class LargeOperatorBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class LargeOperatorBox : MathBox
    {
        private readonly string _text;

        // Zoek/commentaar: Constructor: maakt en initialiseert LargeOperatorBox.
        public LargeOperatorBox(string text)
        {
            _text = text;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            using var largeFont = new Font(
                font.FontFamily,
                Math.Max(font.Size * 1.72f, font.Size + 7f),
                font.Style,
                font.Unit);

            var size = graphics.MeasureString(
                _text,
                largeFont,
                int.MaxValue,
                StringFormat.GenericTypographic);

            Width = Math.Max(1, size.Width);
            Height = Math.Max(largeFont.GetHeight(graphics), size.Height);
            Baseline = FontBaseline(graphics, largeFont) - 1f;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var largeFont = new Font(
                font.FontFamily,
                Math.Max(font.Size * 1.72f, font.Size + 7f),
                font.Style,
                font.Unit);

            graphics.DrawString(_text, largeFont, brush, x, y, StringFormat.GenericTypographic);
        }
    }

    // Zoek/commentaar: Type-overzicht: class SpaceBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class SpaceBox : MathBox
    {
        private readonly float _width;

        // Zoek/commentaar: Constructor: maakt en initialiseert SpaceBox.
        public SpaceBox(float width)
        {
            _width = Math.Max(1f, width);
        }

        public override bool IsEmpty => false;

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            Width = _width;
            Height = font.GetHeight(graphics);
            Baseline = FontBaseline(graphics, font);
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            // bewust leeg: dit is alleen witruimte
        }
    }

    // Zoek/commentaar: Type-overzicht: class RowBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class RowBox : MathBox
    {
        private readonly List<MathBox> _children;

        // Zoek/commentaar: Constructor: maakt en initialiseert RowBox.
        public RowBox(IEnumerable<MathBox> children)
        {
            _children = children.ToList();
        }

        public override bool IsEmpty => _children.Count == 0 || _children.All(c => c.IsEmpty);

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            Width = 0;
            var above = 0f;
            var below = 0f;

            for (var i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                child.Measure(graphics, font);

                Width += child.Width;
                if (i < _children.Count - 1)
                    Width += GapBetween(child, _children[i + 1]);

                above = Math.Max(above, child.Baseline);
                below = Math.Max(below, child.Height - child.Baseline);
            }

            Baseline = above;
            Height = above + below;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            var currentX = x;

            for (var i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                var childY = y + Baseline - child.Baseline;

                child.Draw(graphics, font, brush, pen, currentX, childY);
                currentX += child.Width;

                if (i < _children.Count - 1)
                    currentX += GapBetween(child, _children[i + 1]);
            }
        }

        // Zoek/commentaar: Methode GapBetween: centrale logica voor deze stap.
        private static float GapBetween(MathBox left, MathBox right)
        {
            // Meer natuurlijke formule-spatiering:
            // - rond =, +, -, pijl iets meer ruimte
            // - tussen x^2 en dx iets meer ruimte
            // - na een grote operator zoals integral een beetje ademruimte
            if (left is LargeOperatorBox)
                return 5f;

            if (left is FractionBox || right is FractionBox)
                return 5f;

            if (left is OperatorBox || right is OperatorBox)
                return 5f;

            if (left is SuperscriptBox && right is GlyphBox)
                return 4f;

            if (left is FunctionBox)
                return 2f;

            return 2f;
        }
    }

    // Zoek/commentaar: Type-overzicht: class FractionBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class FractionBox : MathBox
    {
        private readonly MathBox _numerator;
        private readonly MathBox _denominator;

        // Zoek/commentaar: Constructor: maakt en initialiseert FractionBox.
        public FractionBox(MathBox numerator, MathBox denominator)
        {
            _numerator = numerator;
            _denominator = denominator;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            _numerator.Measure(graphics, font);
            _denominator.Measure(graphics, font);

            Width = Math.Max(_numerator.Width, _denominator.Width) + 16f;

            // De wiskundige as/baseline van een breuk ligt bij de breukstreep,
            // niet bij de noemer. Daardoor lijnen "lim", "=" en "+/-" veel beter uit.
            var gapAboveLine = 4f;
            var gapBelowLine = 5f;

            Baseline = _numerator.Height + gapAboveLine;
            Height = Baseline + gapBelowLine + _denominator.Height;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            var numX = x + (Width - _numerator.Width) / 2f;
            var denX = x + (Width - _denominator.Width) / 2f;

            _numerator.Draw(graphics, font, brush, pen, numX, y);

            // Pixel-snap: horizontale lijnen worden dan echt strak en niet half wazig/scheef.
            var lineY = (float)Math.Round(y + Baseline) + 0.5f;
            graphics.DrawLine(pen, x + 3f, lineY, x + Width - 3f, lineY);

            _denominator.Draw(graphics, font, brush, pen, denX, lineY + 5f);
        }
    }

    // Zoek/commentaar: Type-overzicht: class SuperscriptBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class SuperscriptBox : MathBox
    {
        private readonly MathBox _baseBox;
        private readonly MathBox _supBox;

        // Zoek/commentaar: Constructor: maakt en initialiseert SuperscriptBox.
        public SuperscriptBox(MathBox baseBox, MathBox supBox)
        {
            _baseBox = baseBox;
            _supBox = supBox;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Measure(graphics, font);
            _supBox.Measure(graphics, smallFont);

            Width = _baseBox.Width + _supBox.Width + 1f;
            Baseline = Math.Max(_baseBox.Baseline, _supBox.Height + 2f);
            Height = Baseline + Math.Max(_baseBox.Height - _baseBox.Baseline, 0f);
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Draw(graphics, font, brush, pen, x, y + Baseline - _baseBox.Baseline);
            _supBox.Draw(graphics, smallFont, brush, pen, x + _baseBox.Width + 1f, y);
        }
    }

    // Zoek/commentaar: Type-overzicht: class SubscriptBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class SubscriptBox : MathBox
    {
        private readonly MathBox _baseBox;
        private readonly MathBox _subBox;

        // Zoek/commentaar: Constructor: maakt en initialiseert SubscriptBox.
        public SubscriptBox(MathBox baseBox, MathBox subBox)
        {
            _baseBox = baseBox;
            _subBox = subBox;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Measure(graphics, font);
            _subBox.Measure(graphics, smallFont);

            Width = _baseBox.Width + _subBox.Width + 1f;
            Baseline = _baseBox.Baseline;
            Height = Math.Max(_baseBox.Height, _baseBox.Baseline + _subBox.Height + 2f);
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Draw(graphics, font, brush, pen, x, y);
            _subBox.Draw(graphics, smallFont, brush, pen, x + _baseBox.Width + 1f, y + _baseBox.Baseline + 1f);
        }
    }

    // Zoek/commentaar: Type-overzicht: class SubSupBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class SubSupBox : MathBox
    {
        private readonly MathBox _baseBox;
        private readonly MathBox _subBox;
        private readonly MathBox _supBox;

        // Zoek/commentaar: Constructor: maakt en initialiseert SubSupBox.
        public SubSupBox(MathBox baseBox, MathBox subBox, MathBox supBox)
        {
            _baseBox = baseBox;
            _subBox = subBox;
            _supBox = supBox;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Measure(graphics, font);
            _subBox.Measure(graphics, smallFont);
            _supBox.Measure(graphics, smallFont);

            var scriptWidth = Math.Max(_subBox.Width, _supBox.Width);
            var scriptGap = 1f;

            Width = _baseBox.Width + scriptWidth + scriptGap;

            // Bovenste script iets boven de operator, onderste script iets eronder.
            var above = Math.Max(_baseBox.Baseline, _supBox.Height + 2f);
            var below = Math.Max(_baseBox.Height - _baseBox.Baseline, _subBox.Height + 3f);

            Baseline = above;
            Height = above + below;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var smallFont = SmallFont(font);

            var baseY = y + Baseline - _baseBox.Baseline;
            _baseBox.Draw(graphics, font, brush, pen, x, baseY);

            var scriptX = x + _baseBox.Width + 1f;

            // Compacte positie zoals bij integraalgrenzen.
            var supY = y;
            var subY = y + Baseline + 1f;

            _supBox.Draw(graphics, smallFont, brush, pen, scriptX, supY);
            _subBox.Draw(graphics, smallFont, brush, pen, scriptX, subY);
        }
    }

    // Zoek/commentaar: Type-overzicht: class SqrtBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class SqrtBox : MathBox
    {
        private readonly MathBox _content;

        // Zoek/commentaar: Constructor: maakt en initialiseert SqrtBox.
        public SqrtBox(MathBox content)
        {
            _content = content;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            _content.Measure(graphics, font);
            Width = _content.Width + 18f;
            Height = _content.Height + 6f;
            Baseline = _content.Baseline + 6f;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            var top = y + 3f;
            var baseY = y + Height - 5f;

            var points = new[]
            {
                new PointF(x + 2f, y + Height * 0.62f),
                new PointF(x + 7f, baseY),
                new PointF(x + 13f, top),
                new PointF(x + Width - 2f, top)
            };

            graphics.DrawLines(pen, points);
            _content.Draw(graphics, font, brush, pen, x + 16f, y + 6f);
        }
    }

    // Zoek/commentaar: Type-overzicht: class RootBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class RootBox : MathBox
    {
        private readonly MathBox _content;
        private readonly MathBox _index;

        // Zoek/commentaar: Constructor: maakt en initialiseert RootBox.
        public RootBox(MathBox content, MathBox index)
        {
            _content = content;
            _index = index;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            using var smallFont = SmallFont(font);

            _content.Measure(graphics, font);
            _index.Measure(graphics, smallFont);

            Width = _content.Width + 22f + _index.Width;
            Height = _content.Height + 10f;
            Baseline = _content.Baseline + 8f;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var smallFont = SmallFont(font);

            _index.Draw(graphics, smallFont, brush, pen, x, y);
            var rootX = x + _index.Width + 2f;

            var top = y + 6f;
            var baseY = y + Height - 5f;
            var points = new[]
            {
                new PointF(rootX + 2f, y + Height * 0.62f),
                new PointF(rootX + 7f, baseY),
                new PointF(rootX + 13f, top),
                new PointF(rootX + Width - _index.Width - 2f, top)
            };

            graphics.DrawLines(pen, points);
            _content.Draw(graphics, font, brush, pen, rootX + 16f, y + 8f);
        }
    }

    // Zoek/commentaar: Type-overzicht: class UnderBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class UnderBox : MathBox
    {
        private readonly MathBox _baseBox;
        private readonly MathBox _underBox;

        // Zoek/commentaar: Constructor: maakt en initialiseert UnderBox.
        public UnderBox(MathBox baseBox, MathBox underBox)
        {
            _baseBox = baseBox;
            _underBox = underBox;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Measure(graphics, font);
            _underBox.Measure(graphics, smallFont);

            Width = Math.Max(_baseBox.Width, _underBox.Width);

            // Baseline blijft de baseline van "lim".
            // De rij kan deze dan correct uitlijnen met de breukstreep.
            Baseline = _baseBox.Baseline;
            Height = _baseBox.Height + _underBox.Height + 1f;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Draw(graphics, font, brush, pen, x + (Width - _baseBox.Width) / 2f, y);

            // Ondertekst dichter onder "lim", zoals normale limietnotatie.
            _underBox.Draw(
                graphics,
                smallFont,
                brush,
                pen,
                x + (Width - _underBox.Width) / 2f,
                y + _baseBox.Height - 1f);
        }
    }

    // Zoek/commentaar: Type-overzicht: class OverBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class OverBox : MathBox
    {
        private readonly MathBox _baseBox;
        private readonly MathBox _overBox;

        // Zoek/commentaar: Constructor: maakt en initialiseert OverBox.
        public OverBox(MathBox baseBox, MathBox overBox)
        {
            _baseBox = baseBox;
            _overBox = overBox;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Measure(graphics, font);
            _overBox.Measure(graphics, smallFont);

            Width = Math.Max(_baseBox.Width, _overBox.Width);
            Baseline = _overBox.Height + 3f + _baseBox.Baseline;
            Height = _overBox.Height + 3f + _baseBox.Height;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var smallFont = SmallFont(font);

            _overBox.Draw(graphics, smallFont, brush, pen, x + (Width - _overBox.Width) / 2f, y);
            _baseBox.Draw(graphics, font, brush, pen, x + (Width - _baseBox.Width) / 2f, y + _overBox.Height + 3f);
        }
    }

    // Zoek/commentaar: Type-overzicht: class UnderOverBox bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class UnderOverBox : MathBox
    {
        private readonly MathBox _baseBox;
        private readonly MathBox _underBox;
        private readonly MathBox _overBox;

        // Zoek/commentaar: Constructor: maakt en initialiseert UnderOverBox.
        public UnderOverBox(MathBox baseBox, MathBox underBox, MathBox overBox)
        {
            _baseBox = baseBox;
            _underBox = underBox;
            _overBox = overBox;
        }

        // Zoek/commentaar: Meet tekst of UI-afmetingen voor Measure.
        public override void Measure(Graphics graphics, Font font)
        {
            using var smallFont = SmallFont(font);

            _baseBox.Measure(graphics, font);
            _underBox.Measure(graphics, smallFont);
            _overBox.Measure(graphics, smallFont);

            Width = Math.Max(_baseBox.Width, Math.Max(_underBox.Width, _overBox.Width));
            Baseline = _overBox.Height + 3f + _baseBox.Baseline;
            Height = _overBox.Height + 3f + _baseBox.Height + _underBox.Height + 3f;
        }

        // Zoek/commentaar: Tekent een visueel onderdeel voor Draw.
        public override void Draw(Graphics graphics, Font font, Brush brush, Pen pen, float x, float y)
        {
            using var smallFont = SmallFont(font);

            _overBox.Draw(graphics, smallFont, brush, pen, x + (Width - _overBox.Width) / 2f, y);

            var baseY = y + _overBox.Height + 3f;
            _baseBox.Draw(graphics, font, brush, pen, x + (Width - _baseBox.Width) / 2f, baseY);

            _underBox.Draw(graphics, smallFont, brush, pen, x + (Width - _underBox.Width) / 2f, baseY + _baseBox.Height + 1f);
        }
    }
}
