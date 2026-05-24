#nullable enable

using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace Tiedragon.ToolEditor;

// Copyright (c) Tiedragon. All rights reserved.
//
// Shared work-in-progress banner used by preview and visual HTML editing.
// It stays outside package HTML, so concept warnings never become document data.
internal sealed class ToolEditorConceptBanner : ToolEditorWorkBoardBanner
{
    private readonly Color _conceptBackColor = Color.FromArgb(255, 212, 0);
    private readonly Color _signedBackColor = Color.FromArgb(220, 252, 231);
    private readonly Color _signedBorderColor = Color.FromArgb(34, 197, 94);
    private readonly Color _signedTextColor = Color.FromArgb(20, 83, 45);
    private readonly Color _signedIconBackColor = Color.FromArgb(240, 253, 244);
    private readonly Color _signedIconBorderColor = Color.FromArgb(74, 222, 128);
    private readonly Label _message;
    private readonly ToolEditorWorkBoardSvgIcon _conceptIcon;
    private readonly ToolEditorSignedLockIcon _signedIcon;
    private readonly Panel _closeSlot;
    private ToolEditorBannerKind _kind = ToolEditorBannerKind.Concept;

    public ToolEditorConceptBanner()
    {
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        Padding = new Padding(8);
        BackColor = _conceptBackColor;

        _message = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            BackColor = _conceptBackColor,
            ForeColor = Color.FromArgb(31, 41, 55),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0),
            Margin = Padding.Empty
        };

        _conceptIcon = new ToolEditorWorkBoardSvgIcon
        {
            Dock = DockStyle.Left,
            Width = 62,
            BackColor = _conceptBackColor,
            Margin = Padding.Empty
        };

        _signedIcon = new ToolEditorSignedLockIcon
        {
            Dock = DockStyle.Left,
            Width = 62,
            BackColor = _signedBackColor,
            ForeColor = _signedTextColor,
            Margin = Padding.Empty,
            Visible = false
        };
        _signedIcon.CircleBackColor = _signedIconBackColor;
        _signedIcon.CircleBorderColor = _signedIconBorderColor;

        _closeSlot = new Panel
        {
            Dock = DockStyle.Right,
            Width = 48,
            BackColor = _conceptBackColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var close = new Button
        {
            Size = new Size(24, 24),
            Text = "×",
            BackColor = Color.FromArgb(255, 249, 220),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Margin = Padding.Empty
        };
        close.FlatAppearance.BorderColor = Color.FromArgb(214, 163, 24);
        close.FlatAppearance.BorderSize = 1;
        close.MouseEnter += (_, _) => close.BackColor = Color.FromArgb(255, 239, 165);
        close.MouseLeave += (_, _) => close.BackColor = Color.FromArgb(255, 249, 220);
        close.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        _closeSlot.Resize += (_, _) =>
        {
            close.Location = new Point(
                Math.Max(0, (_closeSlot.Width - close.Width) / 2),
                Math.Max(0, (_closeSlot.Height - close.Height) / 2));
        };
        _closeSlot.Controls.Add(close);

        Controls.Add(_message);
        Controls.Add(_closeSlot);
        Controls.Add(_signedIcon);
        Controls.Add(_conceptIcon);
        ApplyKind();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolEditorBannerKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value)
                return;

            _kind = value;
            BannerKind = value;
            ApplyKind();
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Message
    {
        get => _message.Text;
        set => _message.Text = value;
    }

    public event EventHandler? CloseRequested;

    private void ApplyKind()
    {
        var signed = _kind == ToolEditorBannerKind.Signed;
        var backColor = signed ? _signedBackColor : _conceptBackColor;
        BackColor = backColor;
        _message.BackColor = backColor;
        _message.ForeColor = signed ? _signedTextColor : Color.FromArgb(31, 41, 55);
        _conceptIcon.BackColor = backColor;
        _conceptIcon.Visible = !signed;
        _signedIcon.BackColor = backColor;
        _signedIcon.Visible = signed;
        _closeSlot.BackColor = backColor;
        _closeSlot.Visible = !signed;
        _closeSlot.Width = signed ? 0 : 48;
        SignedBackColor = _signedBackColor;
        SignedBorderColor = _signedBorderColor;
    }
}

internal enum ToolEditorBannerKind
{
    Concept,
    Signed
}

internal class ToolEditorWorkBoardBanner : Panel
{
    private const int Border = 8;
    private const int Stripe = 18;
    private int _stripeOffset;
    private ToolEditorBannerKind _bannerKind = ToolEditorBannerKind.Concept;

    public ToolEditorWorkBoardBanner()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolEditorBannerKind BannerKind
    {
        get => _bannerKind;
        set
        {
            if (_bannerKind == value)
                return;

            _bannerKind = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SignedBackColor { get; set; } = Color.FromArgb(220, 252, 231);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SignedBorderColor { get; set; } = Color.FromArgb(34, 197, 94);

    public void AdvanceStripe()
    {
        if (BannerKind == ToolEditorBannerKind.Signed)
            return;

        _stripeOffset = (_stripeOffset + 3) % (Stripe * 2);
        InvalidateBorder();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var width = ClientSize.Width;
        var height = ClientSize.Height;
        if (width <= 0 || height <= 0)
            return;

        if (BannerKind == ToolEditorBannerKind.Signed)
        {
            using var brush = new SolidBrush(SignedBackColor);
            e.Graphics.FillRectangle(brush, 0, 0, width, height);
            using var pen = new Pen(SignedBorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, width - 1), Math.Max(0, height - 1));
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.None;
        var yellow = Color.FromArgb(255, 212, 0);
        using var blackBrush = new SolidBrush(Color.Black);
        for (var x = -height + _stripeOffset; x < width + height; x += Stripe * 2)
        {
            var points = new[]
            {
                new Point(x, 0),
                new Point(x + Stripe, 0),
                new Point(x + Stripe - height, height),
                new Point(x - height, height)
            };
            e.Graphics.FillPolygon(blackBrush, points);
        }

        using var yellowBrush = new SolidBrush(yellow);
        e.Graphics.FillRectangle(yellowBrush, Border, Border, Math.Max(0, width - Border * 2), Math.Max(0, height - Border * 2));
        using var edgePen = new Pen(Color.FromArgb(190, 152, 0));
        e.Graphics.DrawRectangle(edgePen, 0, 0, Math.Max(0, width - 1), Math.Max(0, height - 1));
    }

    private void InvalidateBorder()
    {
        var width = ClientSize.Width;
        var height = ClientSize.Height;
        if (width <= 0 || height <= 0)
            return;

        Invalidate(new Rectangle(0, 0, width, Border), false);
        Invalidate(new Rectangle(0, Math.Max(0, height - Border), width, Border), false);
        Invalidate(new Rectangle(0, 0, Border, height), false);
        Invalidate(new Rectangle(Math.Max(0, width - Border), 0, Border, height), false);
    }
}

internal sealed class ToolEditorWorkBoardSvgIcon : Control
{
    private const float SvgWidth = 350f;
    private const float SvgHeight = 300f;
    private static readonly Regex SvgPathTokenRegex = new(@"[A-Za-z]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly SvgPathPart[] SourceSvgPaths =
    [
        new(Color.FromArgb(193, 18, 28), "M 11,300 C 11,300 27.820331,300 11,300 C -3.9165388,300 3.7804912,283.85305 3.7804912,283.85305 L 164.90679,8.074496 C 164.90679,8.074496 161.40064,14.190767 164.90679,8.074496 C 171.16011,-2.85324 179.31089,-2.372093 185.11873,7.601505 L 346.21203,283.85305 C 346.21203,283.85305 353.91732,300 339,300 C 322.17219,300 339,300 339,300 L 11,300"),
        new(Color.FromArgb(247, 251, 245), "M 175.483,73.698 L 283.20852,259.57553 L 66.297261,259.57553 L 175.483,73.698"),
        new(Color.FromArgb(42, 45, 47), "M 261.23527,250.91189 L 220.82909,202.89056 C 216.02385,196.73996 210.26599,195.30649 204.00231,206.24089 L 195.84183,221.4507 C 195.84183,221.4507 193.43078,225.2594 189.57816,225.70944 C 189.57816,225.70944 186.7203,222.39246 180.45662,233.81857 L 171.30137,250.91189 L 261.23527,250.91189"),
        new(Color.FromArgb(42, 45, 47), "M 169.39615,251.17025 L 171.30137,219.30049 C 171.30137,219.30049 171.07376,214.09165 169.39615,210.02459 L 165.04614,197.90674 L 197.2834,215.97516 L 198.0084,214.55003 L 177.32901,202.17382 L 177.32901,168.90392 C 177.32901,156.79441 171.79876,153.44409 158.32723,154.41918 L 146.04436,155.81932 C 146.04436,155.81932 142.68912,156.53605 140.77545,159.62802 L 128.03735,178.41318 L 140.05045,185.08049 L 116.01581,251.39527 L 122.49868,251.39527 L 149.66936,205.26579 L 160.73828,219.07547 L 163.36852,251.17025 L 169.39615,251.17025"),
        new(Color.FromArgb(42, 45, 47), "M 179.24268,136.74246 C 173.64499,136.74246 169.10108,141.2679 169.10108,146.80177 C 169.10108,152.33564 173.64499,156.86108 179.24268,156.86108 C 184.87408,156.86108 189.41798,152.33564 189.41798,146.80177 C 189.41798,141.2679 184.87408,136.74246 179.24268,136.74246"),
        new(Color.FromArgb(247, 251, 245), "M 168.44353,197.19834 L 160.96589,192.6729 L 167.94615,183.38866 L 168.44353,197.19834"),
        new(Color.FromArgb(247, 251, 245), "M 141.7365,182.44691 L 136.19783,179.12992 L 144.86412,166.52869 L 151.34699,166.29533 L 141.7365,182.44691")
    ];
    private static readonly Lazy<IReadOnlyList<SvgPathShape>> SvgShapes = new(() => SourceSvgPaths
        .Select(part => new SvgPathShape(part.Fill, BuildSvgPath(part.Data)))
        .ToArray());

    public ToolEditorWorkBoardSvgIcon()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var availableWidth = ClientSize.Width - 10;
        var availableHeight = ClientSize.Height - 10;
        if (availableWidth <= 0 || availableHeight <= 0)
            return;

        var scale = Math.Min(availableWidth / SvgWidth, availableHeight / SvgHeight);
        var left = (ClientSize.Width - SvgWidth * scale) / 2f;
        var top = (ClientSize.Height - SvgHeight * scale) / 2f;
        e.Graphics.TranslateTransform(left, top);
        e.Graphics.ScaleTransform(scale, scale);

        foreach (var shape in SvgShapes.Value)
        {
            using var brush = new SolidBrush(shape.Fill);
            e.Graphics.FillPath(brush, shape.Path);
        }

        e.Graphics.ResetTransform();
    }

    private static GraphicsPath BuildSvgPath(string data)
    {
        var tokens = SvgPathTokenRegex.Matches(data).Select(match => match.Value).ToArray();
        var path = new GraphicsPath(FillMode.Winding);
        var index = 0;
        var command = '\0';
        var current = PointF.Empty;
        var figureStart = PointF.Empty;

        while (index < tokens.Length)
        {
            if (IsCommand(tokens[index]))
                command = tokens[index++][0];
            if (command == '\0')
                break;

            var relative = char.IsLower(command);
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    current = ReadPoint(tokens, ref index, current, relative);
                    figureStart = current;
                    path.StartFigure();
                    command = relative ? 'l' : 'L';
                    break;
                case 'L':
                    while (CanReadNumbers(tokens, index, 2))
                    {
                        var next = ReadPoint(tokens, ref index, current, relative);
                        path.AddLine(current, next);
                        current = next;
                    }
                    break;
                case 'C':
                    while (CanReadNumbers(tokens, index, 6))
                    {
                        var control1 = ReadPoint(tokens, ref index, current, relative);
                        var control2 = ReadPoint(tokens, ref index, current, relative);
                        var end = ReadPoint(tokens, ref index, current, relative);
                        path.AddBezier(current, control1, control2, end);
                        current = end;
                    }
                    break;
                case 'Z':
                    path.CloseFigure();
                    current = figureStart;
                    command = '\0';
                    break;
                default:
                    index++;
                    break;
            }
        }

        return path;
    }

    private static PointF ReadPoint(string[] tokens, ref int index, PointF current, bool relative)
    {
        var x = float.Parse(tokens[index++], System.Globalization.CultureInfo.InvariantCulture);
        var y = float.Parse(tokens[index++], System.Globalization.CultureInfo.InvariantCulture);
        return relative ? new PointF(current.X + x, current.Y + y) : new PointF(x, y);
    }

    private static bool CanReadNumbers(string[] tokens, int index, int count)
    {
        if (index + count > tokens.Length)
            return false;

        for (var i = 0; i < count; i++)
        {
            if (IsCommand(tokens[index + i]))
                return false;
        }

        return true;
    }

    private static bool IsCommand(string token)
    {
        return token.Length == 1 && char.IsLetter(token[0]);
    }

    private sealed record SvgPathPart(Color Fill, string Data);

    private sealed record SvgPathShape(Color Fill, GraphicsPath Path);
}

internal sealed class ToolEditorSignedLockIcon : Control
{
    public ToolEditorSignedLockIcon()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        TabStop = false;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CircleBackColor { get; set; } = Color.FromArgb(240, 253, 244);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CircleBorderColor { get; set; } = Color.FromArgb(74, 222, 128);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var circleSize = Math.Min(ClientSize.Width, ClientSize.Height) - 18;
        if (circleSize <= 0)
            return;

        var circleLeft = (ClientSize.Width - circleSize) / 2;
        var circleTop = (ClientSize.Height - circleSize) / 2;
        using (var brush = new SolidBrush(CircleBackColor))
        using (var pen = new Pen(CircleBorderColor, 1.4f))
        {
            e.Graphics.FillEllipse(brush, circleLeft, circleTop, circleSize, circleSize);
            e.Graphics.DrawEllipse(pen, circleLeft, circleTop, circleSize, circleSize);
        }

        var scale = circleSize / 34f;
        var lockWidth = 16f * scale;
        var bodyHeight = 12f * scale;
        var shackleHeight = 10f * scale;
        var stroke = Math.Max(1.6f, 2.2f * scale);
        var left = ClientSize.Width / 2f - lockWidth / 2f;
        var bodyTop = ClientSize.Height / 2f - bodyHeight / 2f + 3f * scale;
        var radius = 2.5f * scale;
        var shackleRect = new RectangleF(
            ClientSize.Width / 2f - lockWidth * .34f,
            bodyTop - shackleHeight + 2f * scale,
            lockWidth * .68f,
            shackleHeight * 1.15f);

        using var penLock = new Pen(ForeColor, stroke)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        e.Graphics.DrawArc(penLock, shackleRect, 200, 140);

        using var bodyBrush = new SolidBrush(ForeColor);
        using var bodyPath = RoundedRectangle(new RectangleF(left, bodyTop, lockWidth, bodyHeight), radius);
        e.Graphics.FillPath(bodyBrush, bodyPath);

        using var keyBrush = new SolidBrush(CircleBackColor);
        var keySize = Math.Max(2.2f, 3.2f * scale);
        e.Graphics.FillEllipse(
            keyBrush,
            ClientSize.Width / 2f - keySize / 2f,
            bodyTop + bodyHeight * .32f,
            keySize,
            keySize);
        using var keyPen = new Pen(CircleBackColor, Math.Max(1.2f, 1.7f * scale));
        e.Graphics.DrawLine(
            keyPen,
            ClientSize.Width / 2f,
            bodyTop + bodyHeight * .52f,
            ClientSize.Width / 2f,
            bodyTop + bodyHeight * .76f);
    }

    private static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
