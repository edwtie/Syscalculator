#nullable enable
using System.ComponentModel;
using Tiedragon.Graph;
using Tiedragon.Graph.G2D;
using Tiedragon.NodSystem.Core;

namespace Syscalculator.UI.WinForms;

internal sealed class SolverStepsForm : Form
{
    private sealed class StepMotionPanel : Panel
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string StepText { get; set; } = "";
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int StepIndex { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int StepCount { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float? DisplayProgress { get; set; }

        public StepMotionPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.FromArgb(239, 246, 255);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var rect = Rectangle.Inflate(ClientRectangle, -1, -1);
            using var background = new SolidBrush(Color.FromArgb(239, 246, 255));
            using var border = new Pen(Color.FromArgb(169, 202, 245));
            using var path = UiGeometry.CreateRoundedRectangle(rect, 8);
            e.Graphics.FillPath(background, path);
            e.Graphics.DrawPath(border, path);

            var count = Math.Max(1, StepCount);
            var current = Math.Clamp(StepIndex, 0, count - 1);
            var progressLeft = rect.Left + 18;
            var progressRight = rect.Right - 18;
            var progressY = rect.Top + 18;
            using var trackPen = new Pen(Color.FromArgb(191, 219, 254), 5) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            using var activePen = new Pen(Color.FromArgb(37, 99, 235), 5) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            e.Graphics.DrawLine(trackPen, progressLeft, progressY, progressRight, progressY);

            var baseProgress = count == 1 ? 1f : current / (float)(count - 1);
            var progress = Math.Clamp(DisplayProgress ?? baseProgress, 0f, 1f);
            var activeX = progressLeft + (progressRight - progressLeft) * progress;
            e.Graphics.DrawLine(activePen, progressLeft, progressY, activeX, progressY);

            using var dotBrush = new SolidBrush(Color.FromArgb(220, 38, 38));
            using var dotGlow = new SolidBrush(Color.FromArgb(58, 220, 38, 38));
            const float pulse = 7f;
            e.Graphics.FillEllipse(dotGlow, activeX - pulse, progressY - pulse, pulse * 2, pulse * 2);
            e.Graphics.FillEllipse(dotBrush, activeX - 5, progressY - 5, 10, 10);

            using var stepFont = new Font("Segoe UI", 10, FontStyle.Bold);
            using var textFont = new Font("Segoe UI", 10);
            using var titleBrush = new SolidBrush(Color.FromArgb(0, 74, 173));
            using var textBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
            e.Graphics.DrawString($"Stap {current + 1} van {count}", stepFont, titleBrush, rect.Left + 18, rect.Top + 30);

            var textRect = new RectangleF(rect.Left + 112, rect.Top + 28, rect.Width - 128, rect.Height - 34);
            e.Graphics.DrawString(StepText, textFont, textBrush, textRect);
        }

    }

    private sealed class GraphDrawingPanel : Panel
    {
        public GraphDrawingPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }
    }

    private readonly string _nodText;
    private readonly LanguageCatalog _language;
    private readonly TextBox _inputBox;
    private readonly TextBox _stepsBox;
    private readonly Label _titleLabel;
    private readonly Label _resultLabel;
    private readonly Label _stepCountLabel;
    private readonly Panel _ruleCardPanel;
    private readonly Label _ruleCardTitle;
    private readonly Label _ruleCardSubtitle;
    private readonly Label _ruleCardExample;
    private readonly HtmlMathPreviewControl _ruleCardFormulaPreview;
    private readonly StepMotionPanel _motionPanel;
    private readonly HtmlMathPreviewControl _currentStepPreview;
    private readonly Panel _graphPanel;
    private readonly Button _refreshButton;
    private readonly Button _previousStepButton;
    private readonly Button _playStepsButton;
    private readonly Button _nextStepButton;
    private readonly Button _closeButton;
    private readonly System.Windows.Forms.Timer _stepTimer;
    private readonly System.Windows.Forms.Timer _ballTimer;
    private readonly System.Windows.Forms.Timer _graphRevealTimer;
    private SolverStepReport? _report;
    private int _currentStepIndex;
    private float _ballFromProgress;
    private float _ballToProgress;
    private DateTime _ballAnimationStartedAt;
    private int _ballAnimationDurationMs;
    private bool _ballAnimationIsPlay;
    private readonly List<GraphLineSeries> _graphLines = new();
    private readonly List<PointF> _graphHighlights = new();
    private GraphPlotView _graphView;
    private GraphPlotView _graphPanStartView;
    private bool _hasGraphView;
    private bool _graphPanning;
    private DateTime _graphRevealStartedAt;
    private float _graphRevealProgress = 1f;
    private bool _resumePlaybackAfterGraphReveal;
    private const int AutoStepIntervalMs = 6200;
    private const int GraphRevealDurationMs = 5200;
    private Point _graphPanStart;
    private float _requestedMinX = -10;
    private float _requestedMaxX = 10;
    private float _requestedStep = 1;
    private string _graphMessage = "";
    private string _graphCaption = "";

    public SolverStepsForm(string nodText, string defaultInput, LanguageCatalog language)
    {
        _nodText = nodText;
        _language = language;

        Text = T("editor.solver_steps.title", "Solver stappen");
        AppWindowIcon.ApplyTo(this);
        Width = 820;
        Height = 610;
        MinimumSize = new Size(620, 470);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(246, 249, 253);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(16),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        _titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(12, 36, 66),
            Margin = new Padding(0, 0, 0, 8)
        };
        root.Controls.Add(_titleLabel, 0, 0);

        var inputPanel = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 5,
            Margin = new Padding(0, 0, 0, 12)
        };
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(inputPanel, 0, 1);

        inputPanel.Controls.Add(new Label
        {
            Text = T("editor.solver_steps.input", "Startwaarde"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 0)
        }, 0, 0);

        _inputBox = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(defaultInput) ? "0" : defaultInput,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 12, 0)
        };
        inputPanel.Controls.Add(_inputBox, 1, 0);

        _refreshButton = new Button
        {
            Text = T("editor.solver_steps.refresh", "Vernieuw"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 12, 0)
        };
        _refreshButton.Click += (_, _) => Render();
        inputPanel.Controls.Add(_refreshButton, 2, 0);

        _resultLabel = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 74, 173)
        };
        inputPanel.Controls.Add(_resultLabel, 3, 0);

        _stepCountLabel = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            Margin = new Padding(12, 0, 0, 0)
        };
        inputPanel.Controls.Add(_stepCountLabel, 4, 0);

        _ruleCardPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 98,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 10)
        };
        root.Controls.Add(_ruleCardPanel, 0, 2);

        var ruleLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.White
        };
        ruleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        ruleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        ruleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        ruleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ruleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _ruleCardPanel.Controls.Add(ruleLayout);

        _ruleCardTitle = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 74, 173),
            Margin = new Padding(0, 0, 8, 2)
        };
        ruleLayout.Controls.Add(_ruleCardTitle, 0, 0);

        _ruleCardFormulaPreview = new HtmlMathPreviewControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0)
        };
        ruleLayout.Controls.Add(_ruleCardFormulaPreview, 1, 0);
        ruleLayout.SetRowSpan(_ruleCardFormulaPreview, 3);

        _ruleCardSubtitle = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(51, 65, 85),
            Margin = new Padding(0, 0, 8, 2)
        };
        ruleLayout.Controls.Add(_ruleCardSubtitle, 0, 1);

        _ruleCardExample = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0, 0, 8, 0)
        };
        ruleLayout.Controls.Add(_ruleCardExample, 0, 2);

        _motionPanel = new StepMotionPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10)
        };
        root.Controls.Add(_motionPanel, 0, 3);

        _currentStepPreview = new HtmlMathPreviewControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12)
        };
        root.Controls.Add(_currentStepPreview, 0, 4);

        _graphPanel = new GraphDrawingPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 12)
        };
        _graphPanel.Paint += GraphPanel_Paint;
        _graphPanel.Resize += (_, _) => _graphPanel.Invalidate();
        _graphPanel.MouseWheel += GraphPanel_MouseWheel;
        _graphPanel.MouseDown += GraphPanel_MouseDown;
        _graphPanel.MouseMove += GraphPanel_MouseMove;
        _graphPanel.MouseUp += GraphPanel_MouseUp;
        root.Controls.Add(_graphPanel, 0, 5);

        _stepsBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = true,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 12)
        };
        _stepsBox.MouseDown += StepsBox_MouseDown;
        root.Controls.Add(_stepsBox, 0, 6);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        root.Controls.Add(bottom, 0, 7);

        _closeButton = new Button
        {
            Text = T("common.close", "Close"),
            AutoSize = true,
            DialogResult = DialogResult.OK
        };
        bottom.Controls.Add(_closeButton);

        _nextStepButton = new Button
        {
            Text = T("editor.solver_steps.next", "Volgende"),
            AutoSize = true
        };
        _nextStepButton.Click += (_, _) => MoveStep(1);
        bottom.Controls.Add(_nextStepButton);

        _playStepsButton = new Button
        {
            Text = T("editor.solver_steps.play", "Play"),
            AutoSize = true
        };
        _playStepsButton.Click += (_, _) => ToggleStepPlayback();
        bottom.Controls.Add(_playStepsButton);

        _previousStepButton = new Button
        {
            Text = T("editor.solver_steps.previous", "Vorige"),
            AutoSize = true
        };
        _previousStepButton.Click += (_, _) => MoveStep(-1);
        bottom.Controls.Add(_previousStepButton);

        AcceptButton = _refreshButton;
        CancelButton = _closeButton;

        _stepTimer = new System.Windows.Forms.Timer { Interval = AutoStepIntervalMs };
        _stepTimer.Tick += (_, _) => MoveStep(1, wrap: false);
        _ballTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _ballTimer.Tick += (_, _) => AdvanceBallAnimation();
        _graphRevealTimer = new System.Windows.Forms.Timer { Interval = 33 };
        _graphRevealTimer.Tick += (_, _) => AdvanceGraphReveal();
        Render();
    }

    private void Render()
    {
        try
        {
            var normalized = NodTextNormalizer.Normalize(_nodText, repairConcatenated: true);
            var doc = NodParser.Parse(normalized);
            var report = SolverStepBuilder.Build(doc, _inputBox.Text);
            _report = report;
            BuildGraphData(report);
            _titleLabel.Text = report.Title;
            _resultLabel.Text = T("editor.solver_steps.result", "Resultaat") + ": " + report.ResultText;
            RenderRuleCard(report.RuleCard);
            StopBallAnimation();
            _resumePlaybackAfterGraphReveal = false;
            _graphRevealTimer.Stop();
            _graphRevealProgress = 1f;
            _currentStepIndex = 0;
            RenderStepList();
            UpdateCurrentStepPreview();
            _graphPanel.Invalidate();

            var isCalculus = doc.CalculusSteps.Count > 0;
            _inputBox.Enabled = isCalculus;
            _refreshButton.Enabled = isCalculus;
        }
        catch (Exception ex)
        {
            _titleLabel.Text = T("editor.solver_steps.title", "Solver stappen");
            _resultLabel.Text = T("status.error", "Error: {0}").Replace("{0}", ex.Message);
            _stepsBox.Text = ex.Message;
            _report = null;
            _stepCountLabel.Text = "";
            _motionPanel.StepText = "";
            _motionPanel.StepCount = 0;
            _motionPanel.DisplayProgress = null;
            _motionPanel.Invalidate();
            RenderRuleCard(null);
            _currentStepPreview.MathMarkup = BuildTextMathMarkup(ex.Message);
            ClearGraphData(ex.Message);
            _resumePlaybackAfterGraphReveal = false;
            _graphRevealTimer.Stop();
            _graphRevealProgress = 1f;
            _graphPanel.Invalidate();
        }
    }

    private void RenderStepList()
    {
        if (_report is null)
        {
            _stepsBox.Text = "";
            return;
        }

        _stepsBox.Text = string.Join(
            Environment.NewLine + Environment.NewLine,
            _report.Steps.Select((step, index) =>
            {
                var marker = index == _currentStepIndex ? ">> " : "   ";
                return $"{marker}{index + 1}. {step}";
            }));
    }

    private void StepsBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_report is null || _report.Steps.Count == 0 || e.Button != MouseButtons.Left)
            return;

        var charIndex = _stepsBox.GetCharIndexFromPosition(e.Location);
        var clickedLine = _stepsBox.GetLineFromCharIndex(charIndex);
        var stepIndex = MapStepTextLineToStepIndex(clickedLine);
        if (stepIndex < 0 || stepIndex >= _report.Steps.Count || stepIndex == _currentStepIndex)
            return;

        var previousStep = _currentStepIndex;
        StopBallAnimation();
        _stepTimer.Stop();
        _resumePlaybackAfterGraphReveal = false;
        _playStepsButton.Text = T("editor.solver_steps.play", "Play");
        _currentStepIndex = stepIndex;
        StartGraphRevealIfNeeded(previousStep, stepIndex);
        RenderStepList();
        UpdateCurrentStepPreview();
    }

    private static int MapStepTextLineToStepIndex(int clickedLine)
    {
        // RenderStepList zet een lege regel tussen elke stap.
        return clickedLine / 2;
    }

    private void RenderRuleCard(FormulaRuleCard? card)
    {
        _ruleCardPanel.Visible = card is not null;
        if (card is null)
        {
            _ruleCardTitle.Text = "";
            _ruleCardSubtitle.Text = "";
            _ruleCardExample.Text = "";
            _ruleCardFormulaPreview.MathMarkup = "";
            return;
        }

        _ruleCardTitle.Text = card.Title;
        _ruleCardSubtitle.Text = card.Subtitle;
        _ruleCardExample.Text = card.ExampleText;
        _ruleCardFormulaPreview.MathMarkup = $$"""
        <div class="rule-card-formula">
          {{card.FormulaMathMl}}
        </div>
        """;
    }


    private void UpdateCurrentStepPreview()
    {
        if (_report is null || _report.Steps.Count == 0)
        {
            _stepCountLabel.Text = T("editor.solver_steps.no_steps", "0 stappen");
            _currentStepPreview.MathMarkup = BuildTextMathMarkup("");
            _previousStepButton.Enabled = false;
            _nextStepButton.Enabled = false;
            _playStepsButton.Enabled = false;
            return;
        }

        _currentStepIndex = Math.Clamp(_currentStepIndex, 0, _report.Steps.Count - 1);
        _stepCountLabel.Text = string.Format(T("editor.solver_steps.count", "Stap {0} van {1}"), _currentStepIndex + 1, _report.Steps.Count);
        _currentStepPreview.MathMarkup = BuildStepMathMarkup(_report, _currentStepIndex);
        _motionPanel.StepText = _report.Steps[_currentStepIndex];
        _motionPanel.StepIndex = _currentStepIndex;
        _motionPanel.StepCount = _report.Steps.Count;
        _motionPanel.Invalidate();
        _graphPanel.Invalidate();
        _previousStepButton.Enabled = _currentStepIndex > 0;
        _nextStepButton.Enabled = _currentStepIndex < _report.Steps.Count - 1;
        _playStepsButton.Enabled = _report.Steps.Count > 1;
    }

    private void MoveStep(int delta, bool wrap = true)
    {
        if (_report is null || _report.Steps.Count == 0)
            return;

        var previousStep = _currentStepIndex;
        var next = _currentStepIndex + delta;
        if (next >= _report.Steps.Count)
        {
            if (!wrap)
            {
                _stepTimer.Stop();
                _resumePlaybackAfterGraphReveal = false;
                _playStepsButton.Text = T("editor.solver_steps.play", "Play");
                StopBallAnimation();
                _motionPanel.Invalidate();
                _graphPanel.Invalidate();
                return;
            }

            next = 0;
        }
        else if (next < 0)
        {
            next = wrap ? _report.Steps.Count - 1 : 0;
        }

        StartBallAnimation(_currentStepIndex, next);
        _currentStepIndex = next;
        StartGraphRevealIfNeeded(previousStep, next);
        RenderStepList();
        UpdateCurrentStepPreview();
    }

    private void StartGraphRevealIfNeeded(int previousStep, int nextStep)
    {
        if (_report?.GraphRevealStepIndex is not int revealStep)
            return;

        if (previousStep < revealStep && nextStep >= revealStep)
        {
            _resumePlaybackAfterGraphReveal = _stepTimer.Enabled;
            if (_resumePlaybackAfterGraphReveal)
                _stepTimer.Stop();

            _graphRevealStartedAt = DateTime.UtcNow;
            _graphRevealProgress = 0f;
            _graphRevealTimer.Stop();
            _graphRevealTimer.Start();
        }
        else if (nextStep < revealStep)
        {
            _resumePlaybackAfterGraphReveal = false;
            _graphRevealTimer.Stop();
            _graphRevealProgress = 1f;
        }
    }

    private void AdvanceGraphReveal()
    {
        var elapsed = (float)(DateTime.UtcNow - _graphRevealStartedAt).TotalMilliseconds;
        _graphRevealProgress = Math.Clamp(elapsed / GraphRevealDurationMs, 0f, 1f);
        _graphPanel.Invalidate();

        if (_graphRevealProgress < 1f)
            return;

        _graphRevealTimer.Stop();
        if (_resumePlaybackAfterGraphReveal)
        {
            _resumePlaybackAfterGraphReveal = false;
            _stepTimer.Start();
        }
    }

    private void StartBallAnimation(int fromIndex, int toIndex)
    {
        if (_report is null || _report.Steps.Count <= 1 || fromIndex == toIndex)
        {
            StopBallAnimation();
            return;
        }

        var max = _report.Steps.Count - 1;
        _ballFromProgress = Math.Clamp(fromIndex, 0, max) / (float)max;
        _ballToProgress = Math.Clamp(toIndex, 0, max) / (float)max;
        _ballAnimationIsPlay = _stepTimer.Enabled;
        var distance = Math.Abs(_ballToProgress - _ballFromProgress);
        var speedPerSecond = _ballAnimationIsPlay
            ? 1f / Math.Max(0.2f, (_report.Steps.Count - 1) * (_stepTimer.Interval / 1000f))
            : 1.35f;
        _ballAnimationDurationMs = Math.Clamp((int)MathF.Round(distance / speedPerSecond * 1000f), 120, _ballAnimationIsPlay ? _stepTimer.Interval - 8 : 420);
        _ballAnimationStartedAt = DateTime.UtcNow;
        _motionPanel.DisplayProgress = _ballFromProgress;
        _ballTimer.Stop();
        _ballTimer.Start();
    }

    private void AdvanceBallAnimation()
    {
        var elapsedMs = (float)(DateTime.UtcNow - _ballAnimationStartedAt).TotalMilliseconds;
        var t = Math.Clamp(elapsedMs / Math.Max(1, _ballAnimationDurationMs), 0f, 1f);
        var eased = _ballAnimationIsPlay ? t : t * t * (3f - 2f * t);
        _motionPanel.DisplayProgress = _ballFromProgress + (_ballToProgress - _ballFromProgress) * eased;
        _motionPanel.Invalidate();

        if (t < 1f)
            return;

        StopBallAnimation();
    }

    private void StopBallAnimation()
    {
        _ballTimer.Stop();
        _motionPanel.DisplayProgress = null;
    }

    private void ToggleStepPlayback()
    {
        if (_stepTimer.Enabled)
        {
            _stepTimer.Stop();
            _resumePlaybackAfterGraphReveal = false;
            _playStepsButton.Text = T("editor.solver_steps.play", "Play");
            StopBallAnimation();
            _motionPanel.Invalidate();
            return;
        }

        if (_graphRevealTimer.Enabled && _resumePlaybackAfterGraphReveal)
        {
            _resumePlaybackAfterGraphReveal = false;
            _playStepsButton.Text = T("editor.solver_steps.play", "Play");
            return;
        }

        if (_report is not null && _currentStepIndex >= _report.Steps.Count - 1)
        {
            StopBallAnimation();
            _currentStepIndex = 0;
            _resumePlaybackAfterGraphReveal = false;
            _graphRevealTimer.Stop();
            _graphRevealProgress = 1f;
        }

        _stepTimer.Start();
        _playStepsButton.Text = T("editor.solver_steps.pause", "Pauze");
        RenderStepList();
        UpdateCurrentStepPreview();
    }

    private void GraphPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (_report?.GraphRevealStepIndex is int revealStep && _currentStepIndex < revealStep)
        {
            _resumePlaybackAfterGraphReveal = false;
            _graphRevealTimer.Stop();
            _graphRevealProgress = 1f;
            GraphSurfaceApi.DrawMulti(
                e.Graphics,
                _graphPanel,
                [],
                [],
                new GraphPlotView(-10, 10, -10, 10),
                -10,
                10,
                1,
                "",
                $"Grafiek verschijnt bij stap {revealStep + 1}.",
                GraphPlotDensity.Normal);
            return;
        }

        if (!_hasGraphView)
        {
            GraphSurfaceApi.DrawMulti(
                e.Graphics,
                _graphPanel,
                [],
                [],
                new GraphPlotView(-10, 10, -10, 10),
                -10,
                10,
                1,
                _graphMessage,
                T("editor.solver_steps.no_graph", "Geen grafiek beschikbaar."),
                GraphPlotDensity.Normal);
            return;
        }

        var graphLines = GetRevealedGraphLines();

        GraphSurfaceApi.DrawMulti(
            e.Graphics,
            _graphPanel,
            graphLines,
            [],
            _graphView,
            _requestedMinX,
            _requestedMaxX,
            _requestedStep,
            "",
            T("editor.solver_steps.no_graph", "Geen grafiek beschikbaar."),
            GraphPlotDensity.Normal);

        if (!string.IsNullOrWhiteSpace(_graphCaption))
        {
            using var brush = new SolidBrush(Color.FromArgb(15, 63, 143));
            e.Graphics.DrawString(_graphCaption, Font, brush, 12, 10);
        }

        DrawGraphPulse(e.Graphics);
    }

    private IReadOnlyList<GraphLineSeries> GetRevealedGraphLines()
    {
        if (_graphRevealProgress >= 0.999f)
            return _graphLines;

        var progress = Math.Clamp((_graphRevealProgress - 0.35f) / 0.65f, 0f, 1f);
        return _graphLines
            .Select(line =>
            {
                var count = Math.Clamp((int)MathF.Ceiling(line.Points.Count * progress), 1, line.Points.Count);
                return new GraphLineSeries(line.Points.Take(count).ToArray(), line.Color, line.Width);
            })
            .ToArray();
    }

    private void DrawGraphPulse(Graphics graphics)
    {
        if (!_hasGraphView || _graphHighlights.Count == 0)
            return;

        var plot = GraphSurfaceApi.GetPlotRectangle(_graphPanel);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        PointF Map(PointF point)
        {
            var x = plot.Left + ((point.X - _graphView.MinX) / (_graphView.MaxX - _graphView.MinX)) * plot.Width;
            var y = plot.Bottom - ((point.Y - _graphView.MinY) / (_graphView.MaxY - _graphView.MinY)) * plot.Height;
            return new PointF((float)x, (float)y);
        }

        const float glowRadius = 11f;
        const float pointRadius = 6f;
        using var glow = new SolidBrush(Color.FromArgb(58, 220, 38, 38));
        using var pointBrush = new SolidBrush(Color.FromArgb(220, 38, 38));
        using var outline = new Pen(Color.White, 2.2f);
        foreach (var point in _graphHighlights.Select(Map))
        {
            graphics.FillEllipse(glow, point.X - glowRadius, point.Y - glowRadius, glowRadius * 2, glowRadius * 2);
            graphics.FillEllipse(pointBrush, point.X - pointRadius, point.Y - pointRadius, pointRadius * 2, pointRadius * 2);
            graphics.DrawEllipse(outline, point.X - pointRadius, point.Y - pointRadius, pointRadius * 2, pointRadius * 2);
        }
    }

    private void BuildGraphData(SolverStepReport report)
    {
        ClearGraphData("");

        if (report.EquationGraph is not null)
        {
            BuildEquationGraphData(report.EquationGraph);
            return;
        }

        if (report.CalculusGraph is not null)
            BuildCalculusGraphData(report.CalculusGraph);
    }

    private void BuildEquationGraphData(EquationGraphInfo graph)
    {
        var center = (float)graph.Solution;
        var minX = center - 10;
        var maxX = center + 10;
        if (Math.Abs(maxX - minX) < 0.01f)
        {
            minX = -10;
            maxX = 10;
        }

        var leftPoints = BuildEquationPoints(graph.LeftExpression, graph.SolveVariable, graph.GivenValues, minX, maxX);
        var rightPoints = BuildEquationPoints(graph.RightExpression, graph.SolveVariable, graph.GivenValues, minX, maxX);
        var allPoints = leftPoints.Concat(rightPoints).ToArray();
        if (allPoints.Length == 0)
            return;

        var intersectionY = EvaluateEquationExpression(graph.LeftExpression, graph.SolveVariable, graph.GivenValues, graph.Solution);
        _graphHighlights.Add(new PointF((float)graph.Solution, (float)intersectionY));
        _graphLines.Add(new GraphLineSeries(leftPoints, Color.FromArgb(37, 99, 235), 2.4f));
        _graphLines.Add(new GraphLineSeries(rightPoints, Color.FromArgb(234, 88, 12), 2.4f));
        _graphCaption = $"snijpunt: {graph.SolveVariable} = {Format(graph.Solution)}";
        SetGraphView(minX, maxX, allPoints.Concat(_graphHighlights).ToArray());
    }

    private void BuildCalculusGraphData(CalculusGraphInfo graph)
    {
        var minX = (float)(graph.A ?? graph.InputValue - 5);
        var maxX = (float)(graph.B ?? graph.InputValue + 5);
        if (Math.Abs(maxX - minX) < 0.01f)
        {
            minX = (float)graph.InputValue - 5;
            maxX = (float)graph.InputValue + 5;
        }

        var points = BuildFunctionPoints(graph.Expression, minX, maxX);
        if (points.Count == 0)
            return;

        _graphLines.Add(new GraphLineSeries(points, Color.FromArgb(37, 99, 235), 2.4f));
        if (graph.Operation == CalculusOperation.Integrate && graph.A is not null && graph.B is not null)
        {
            _graphHighlights.Add(new PointF((float)graph.A.Value, 0));
            _graphHighlights.Add(new PointF((float)graph.B.Value, 0));
            _graphCaption = $"integraal van {Format(graph.A.Value)} tot {Format(graph.B.Value)}";
        }
        else
        {
            var y = NodExpressionEvaluator.Evaluate(graph.Expression, graph.InputValue);
            _graphHighlights.Add(new PointF((float)graph.InputValue, (float)y));
            _graphCaption = $"hellingpunt bij x = {Format(graph.InputValue)}";
        }

        SetGraphView(minX, maxX, points.Concat(_graphHighlights).ToArray());
    }

    private static List<PointF> BuildEquationPoints(string expression, string solveVariable, IReadOnlyDictionary<string, decimal> givens, float minX, float maxX)
    {
        var points = new List<PointF>();
        const int count = 160;
        for (var i = 0; i <= count; i++)
        {
            var x = minX + (maxX - minX) * i / count;
            try
            {
                var y = EvaluateEquationExpression(expression, solveVariable, givens, (decimal)x);
                points.Add(new PointF(x, (float)y));
            }
            catch
            {
                // Skip undefined graph points.
            }
        }

        return points;
    }

    private static List<PointF> BuildFunctionPoints(string expression, float minX, float maxX)
    {
        var points = new List<PointF>();
        const int count = 180;
        for (var i = 0; i <= count; i++)
        {
            var x = minX + (maxX - minX) * i / count;
            try
            {
                var y = NodExpressionEvaluator.Evaluate(expression, (decimal)x);
                points.Add(new PointF(x, (float)y));
            }
            catch
            {
                // Skip undefined graph points.
            }
        }

        return points;
    }

    private static decimal EvaluateEquationExpression(string expression, string solveVariable, IReadOnlyDictionary<string, decimal> givens, decimal x)
    {
        var variables = new Dictionary<string, decimal>(givens, StringComparer.OrdinalIgnoreCase)
        {
            [solveVariable] = x
        };
        return NodExpressionEvaluator.Evaluate(expression, x, variables);
    }

    private void SetGraphView(float minX, float maxX, IReadOnlyList<PointF> points)
    {
        if (points.Count == 0)
            return;

        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);
        if (Math.Abs(maxX - minX) < 0.0001f) { minX -= 1; maxX += 1; }
        if (Math.Abs(maxY - minY) < 0.0001f) { minY -= 1; maxY += 1; }
        GraphSurfaceApi.NormalizeRange(ref minX, ref maxX, ref minY, ref maxY, padX: false);
        _graphView = new GraphPlotView(minX, maxX, minY, maxY);
        _requestedMinX = minX;
        _requestedMaxX = maxX;
        _requestedStep = Math.Max(1, (maxX - minX) / 8f);
        _hasGraphView = true;
    }

    private void ClearGraphData(string message)
    {
        _graphLines.Clear();
        _graphHighlights.Clear();
        _hasGraphView = false;
        _graphMessage = message;
        _graphCaption = "";
    }

    private void GraphPanel_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (!_hasGraphView)
            return;

        ZoomGraph(e.Delta > 0 ? 0.85f : 1.18f, e.Location);
    }

    private void GraphPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_hasGraphView || e.Button != MouseButtons.Left)
            return;

        _graphPanning = true;
        _graphPanStart = e.Location;
        _graphPanStartView = _graphView;
        _graphPanel.Cursor = Cursors.Hand;
    }

    private void GraphPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_hasGraphView || !_graphPanning)
            return;

        var plot = GraphSurfaceApi.GetPlotRectangle(_graphPanel);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var start = GraphSurfaceApi.ScreenToGraph(_graphPanStart, plot, _graphPanStartView);
        var current = GraphSurfaceApi.ScreenToGraph(e.Location, plot, _graphPanStartView);
        var dx = start.X - current.X;
        var dy = start.Y - current.Y;
        _graphView = new GraphPlotView(
            _graphPanStartView.MinX + dx,
            _graphPanStartView.MaxX + dx,
            _graphPanStartView.MinY + dy,
            _graphPanStartView.MaxY + dy);
        _graphPanel.Invalidate();
    }

    private void GraphPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        _graphPanning = false;
        _graphPanel.Cursor = Cursors.Default;
    }

    private void ZoomGraph(float factor, Point screenPoint)
    {
        var plot = GraphSurfaceApi.GetPlotRectangle(_graphPanel);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var anchor = GraphSurfaceApi.ScreenToGraph(screenPoint, plot, _graphView);
        var newWidth = (_graphView.MaxX - _graphView.MinX) * factor;
        var newHeight = (_graphView.MaxY - _graphView.MinY) * factor;
        if (newWidth < GraphSurfaceApi.MinimumViewSpan || newHeight < GraphSurfaceApi.MinimumViewSpan)
            return;

        var xRatio = (anchor.X - _graphView.MinX) / (_graphView.MaxX - _graphView.MinX);
        var yRatio = (anchor.Y - _graphView.MinY) / (_graphView.MaxY - _graphView.MinY);
        var minX = anchor.X - newWidth * xRatio;
        var maxX = minX + newWidth;
        var minY = anchor.Y - newHeight * yRatio;
        var maxY = minY + newHeight;
        _graphView = new GraphPlotView(minX, maxX, minY, maxY);
        _graphPanel.Invalidate();
    }

    private static string Format(decimal value)
        => value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

    private static string BuildStepMathMarkup(SolverStepReport report, int stepIndex)
    {
        var math = stepIndex >= 0 && stepIndex < report.StepMathMl.Count
            ? report.StepMathMl[stepIndex]
            : BuildTextMathMarkup(report.Steps[Math.Clamp(stepIndex, 0, report.Steps.Count - 1)]);

        return $$"""
        <div class="math-step-sequence">
          <div class="math-step-row">
            <span class="math-step-arrow">&#9654;</span>
            {{math}}
          </div>
        </div>
        """;
    }

    private static string BuildTextMathMarkup(string text)
    {
        return $"""<span>{System.Net.WebUtility.HtmlEncode(text)}</span>""";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stepTimer?.Dispose();
            _ballTimer?.Dispose();
            _graphRevealTimer?.Dispose();
        }

        base.Dispose(disposing);
    }

    private string T(string key, string fallback) => _language.Text(key, fallback);
}

