using Windows.UI.Xaml;

namespace Syscalculator.UI.Uwp;

public static class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        Application.Start(_ => new App());
    }
}
