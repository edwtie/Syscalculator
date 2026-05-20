#nullable enable
using Tiedragon.Graph;
using Tiedragon.Graph.G3D;

namespace Syscalculator.UI.WinForms;

internal readonly record struct Graph3DPreviewSyncState(
    GraphPlotView3D View,
    GraphPlotView MarkerView,
    GraphCamera3D Camera,
    double Step,
    double GridStep,
    bool ShowGrid,
    bool ShowLines);
