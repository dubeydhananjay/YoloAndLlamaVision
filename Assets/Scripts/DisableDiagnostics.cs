using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Diagnostics;
using UnityEngine;

public class DisableDiagnostics : MonoBehaviour
{
    void Start()
    {
        // Get the Diagnostics system
        var diagnosticsSystem = CoreServices.DiagnosticsSystem as IMixedRealityDiagnosticsSystem;

        if (diagnosticsSystem != null)
        {
            // Disable the diagnostics system
            diagnosticsSystem.ShowDiagnostics = false;
            diagnosticsSystem.ShowProfiler = false;  // This specifically disables the profiler
        }
    }
}
