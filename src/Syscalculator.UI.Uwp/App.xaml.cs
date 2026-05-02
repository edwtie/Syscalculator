using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Syscalculator.UI.Uwp;

public sealed partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var rootFrame = Window.Current.Content as Frame;
        if (rootFrame is null)
        {
            rootFrame = new Frame();
            Window.Current.Content = rootFrame;
        }

        if (rootFrame.Content is null)
            rootFrame.Navigate(typeof(MainPage), args.Arguments);

        Window.Current.Activate();
    }
}
