#nullable enable

using System.Globalization;

namespace Tiedragon.Graph;

/// <summary>
/// Shared readable graph-number formatting for 2D and 3D axes.
/// </summary>
public static class GraphNumberFormatter
{
    private const double LightYearMeters = 9_460_730_472_580_800d;
    private const double PlanckLengthMeters = 1.616255e-35;

    public static string FormatDisplayNumber(double value)
    {
        return FormatTick(value, 0d);
    }

    public static string FormatTick(double value, double step)
    {
        var abs = Math.Abs(value);
        var absStep = Math.Abs(step);
        if (abs < 1e-300 && absStep >= 1e-30d)
            return "0";

        if (TryFormatPlanckTick(value, step, absStep > 0d ? absStep : abs, out var planckTick))
            return planckTick;

        if (TryFormatSmallSiTick(value, step, Math.Max(abs, absStep), out var smallTick))
            return smallTick;

        if (TryFormatLightYearTick(value, step, Math.Max(abs, absStep), out var lightYearTick))
            return lightYearTick;

        if (TryFormatLargeSiTick(value, step, Math.Max(abs, absStep), out var largeTick))
            return largeTick;

        return value.ToString(FormatForStep(step, abs), CultureInfo.CurrentCulture);
    }

    private static bool TryFormatPlanckTick(double value, double step, double scaleValue, out string text)
    {
        if (scaleValue >= 1e-30d)
        {
            text = string.Empty;
            return false;
        }

        var planckValue = value / PlanckLengthMeters;
        var planckStep = step / PlanckLengthMeters;
        if (Math.Abs(planckValue) < Math.Max(0.001d, Math.Abs(planckStep) / 1000d))
        {
            text = "0";
            return true;
        }

        text = FormatCompactTick(planckValue, planckStep, " lP");
        return true;
    }

    private static bool TryFormatLightYearTick(double value, double step, double scaleValue, out string text)
    {
        if (scaleValue < LightYearMeters)
        {
            text = string.Empty;
            return false;
        }

        var lyValue = value / LightYearMeters;
        var lyStep = step / LightYearMeters;
        var lyScale = scaleValue / LightYearMeters;

        if (lyScale >= 1e30d)
        {
            text = FormatScientificTick(lyValue) + " lj";
            return true;
        }
        if (lyScale >= 1e27d)
        {
            text = FormatCompactTick(lyValue / 1e27d, lyStep / 1e27d, "R lj");
            return true;
        }
        if (lyScale >= 1e24d)
        {
            text = FormatCompactTick(lyValue / 1e24d, lyStep / 1e24d, "Y lj");
            return true;
        }
        if (lyScale >= 1e21d)
        {
            text = FormatCompactTick(lyValue / 1e21d, lyStep / 1e21d, "Z lj");
            return true;
        }
        if (lyScale >= 1e18d)
        {
            text = FormatCompactTick(lyValue / 1e18d, lyStep / 1e18d, "E lj");
            return true;
        }
        if (lyScale >= 1e15d)
        {
            text = FormatCompactTick(lyValue / 1e15d, lyStep / 1e15d, "P lj");
            return true;
        }
        if (lyScale >= 1e12d)
        {
            text = FormatCompactTick(lyValue / 1e12d, lyStep / 1e12d, "T lj");
            return true;
        }
        if (lyScale >= 1e9d)
        {
            text = FormatCompactTick(lyValue / 1e9d, lyStep / 1e9d, "G lj");
            return true;
        }
        if (lyScale >= 1e6d)
        {
            text = FormatCompactTick(lyValue / 1e6d, lyStep / 1e6d, "M lj");
            return true;
        }
        if (lyScale >= 1e3d)
        {
            text = FormatCompactTick(lyValue / 1e3d, lyStep / 1e3d, "K lj");
            return true;
        }

        text = FormatCompactTick(lyValue, lyStep, " lj");
        return true;
    }

    private static bool TryFormatLargeSiTick(double value, double step, double scaleValue, out string text)
    {
        if (scaleValue >= 1e33d)
        {
            text = FormatScientificTick(value);
            return true;
        }
        if (scaleValue >= 1e30d)
        {
            text = FormatCompactTick(value / 1e30d, step / 1e30d, "Q");
            return true;
        }
        if (scaleValue >= 1e27d)
        {
            text = FormatCompactTick(value / 1e27d, step / 1e27d, "R");
            return true;
        }
        if (scaleValue >= 1e24d)
        {
            text = FormatCompactTick(value / 1e24d, step / 1e24d, "Y");
            return true;
        }
        if (scaleValue >= 1e21d)
        {
            text = FormatCompactTick(value / 1e21d, step / 1e21d, "Z");
            return true;
        }
        if (scaleValue >= 1e18d)
        {
            text = FormatCompactTick(value / 1e18d, step / 1e18d, "E");
            return true;
        }
        if (scaleValue >= 1e15d)
        {
            text = FormatCompactTick(value / 1e15d, step / 1e15d, "P");
            return true;
        }
        if (scaleValue >= 1e12d)
        {
            text = FormatCompactTick(value / 1e12d, step / 1e12d, "T");
            return true;
        }
        if (scaleValue >= 1e9d)
        {
            text = FormatCompactTick(value / 1e9d, step / 1e9d, "G");
            return true;
        }
        if (scaleValue >= 1e6d)
        {
            text = FormatCompactTick(value / 1e6d, step / 1e6d, "M");
            return true;
        }
        if (scaleValue >= 1e3d)
        {
            text = FormatCompactTick(value / 1e3d, step / 1e3d, "K");
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool TryFormatSmallSiTick(double value, double step, double scaleValue, out string text)
    {
        if (scaleValue < 1e-30d)
        {
            text = FormatScientificTick(value);
            return true;
        }
        if (scaleValue < 1e-27d)
        {
            text = FormatCompactTick(value * 1e30d, step * 1e30d, "q");
            return true;
        }
        if (scaleValue < 1e-24d)
        {
            text = FormatCompactTick(value * 1e27d, step * 1e27d, "r");
            return true;
        }
        if (scaleValue < 1e-21d)
        {
            text = FormatCompactTick(value * 1e24d, step * 1e24d, "y");
            return true;
        }
        if (scaleValue < 1e-18d)
        {
            text = FormatCompactTick(value * 1e21d, step * 1e21d, "z");
            return true;
        }
        if (scaleValue < 1e-15d)
        {
            text = FormatCompactTick(value * 1e18d, step * 1e18d, "a");
            return true;
        }
        if (scaleValue < 1e-12d)
        {
            text = FormatCompactTick(value * 1e15d, step * 1e15d, "f");
            return true;
        }
        if (scaleValue < 1e-9d)
        {
            text = FormatCompactTick(value * 1e12d, step * 1e12d, "p");
            return true;
        }
        if (scaleValue < 1e-6d)
        {
            text = FormatCompactTick(value * 1e9d, step * 1e9d, "n");
            return true;
        }
        if (scaleValue < 1e-3d)
        {
            text = FormatCompactTick(value * 1e6d, step * 1e6d, "u");
            return true;
        }
        if (scaleValue < 1d)
        {
            text = FormatCompactTick(value * 1e3d, step * 1e3d, "m");
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static string FormatCompactTick(double scaledValue, double scaledStep, string suffix)
    {
        var format = FormatForStep(scaledStep, Math.Abs(scaledValue));
        return scaledValue.ToString(format, CultureInfo.CurrentCulture) + suffix;
    }

    private static string FormatScientificTick(double value)
    {
        return value.ToString("0.###E+0", CultureInfo.CurrentCulture);
    }

    private static string FormatForStep(double step, double absValue)
    {
        if (step > 0d && step < 1d)
        {
            var decimals = Math.Clamp((int)Math.Ceiling(-Math.Log10(step)) + 1, 1, 4);
            return "0." + new string('#', decimals);
        }

        return absValue >= 100d ? "0" : "0.##";
    }
}
