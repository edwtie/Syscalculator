using Tiedragon.Graph;

namespace Syscalculator.UI.WinForms;

internal readonly record struct GraphPreviewSyncState(
    GraphPlotView View,
    GraphPlotView MarkerView,
    decimal Step,
    bool ShowRangeLines);
