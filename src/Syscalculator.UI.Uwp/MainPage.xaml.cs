using Tiedragon.NodSystem.Core;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Syscalculator.UI.Uwp;

public sealed partial class MainPage : Page
{
    private readonly TextBox _inputBox;
    private readonly TextBox _outputBox;

    public MainPage()
    {
        var root = new Grid
        {
            Padding = new Thickness(24),
            RowSpacing = 16,
            Background = new SolidColorBrush(Colors.White)
        };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titlePanel = new StackPanel { Spacing = 4 };
        titlePanel.Children.Add(new TextBlock
        {
            Text = "Syscalculator 2.1",
            FontSize = 28,
            FontWeight = FontWeights.SemiBold
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = "UWP .NET 10 Native AOT fork",
            Opacity = 0.72
        });

        _inputBox = new TextBox
        {
            Header = "Input",
            Text = "22",
            InputScope = new Windows.UI.Xaml.Input.InputScope
            {
                Names =
                {
                    new Windows.UI.Xaml.Input.InputScopeName(Windows.UI.Xaml.Input.InputScopeNameValue.Number)
                }
            }
        };
        Grid.SetRow(_inputBox, 1);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        Grid.SetRow(buttons, 2);

        var convertButton = new Button { Content = "Celsius naar Fahrenheit" };
        convertButton.Click += ConvertCelsius_Click;
        buttons.Children.Add(convertButton);

        var traceButton = new Button { Content = "Trace demo" };
        traceButton.Click += TraceDemo_Click;
        buttons.Children.Add(traceButton);

        _outputBox = new TextBox
        {
            Header = "Output",
            AcceptsReturn = true,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetRow(_outputBox, 3);

        root.Children.Add(titlePanel);
        root.Children.Add(_inputBox);
        root.Children.Add(buttons);
        root.Children.Add(_outputBox);

        Content = root;
        ConvertCelsius_Click(this, new RoutedEventArgs());
    }

    private void ConvertCelsius_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var doc = NodParser.Parse("""
            Name Celsius naar Fahrenheit
            format ##.00
            math ans * 1,8
            math ans + 32
            end
            """);

            var result = NodEngine.ConvertForward(doc, _inputBox.Text);
            _outputBox.Text = result.Text;
        }
        catch (Exception ex)
        {
            _outputBox.Text = ex.Message;
        }
    }

    private void TraceDemo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var doc = NodParser.Parse("""
            Name Trace demo
            format ##.00
            math ans * e^2
            math ans + 10
            math sqrt(ans)
            end
            """);

            var result = NodEngine.ConvertForwardWithTrace(doc, _inputBox.Text);
            var lines = new List<string>
            {
                "Output: " + result.Text,
                ""
            };

            foreach (var step in result.Trace.Steps)
                lines.Add($"{step.InputValue} -> {step.Expression} -> {step.OutputValue}");

            _outputBox.Text = string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            _outputBox.Text = ex.Message;
        }
    }
}

