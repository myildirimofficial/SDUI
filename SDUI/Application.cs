using SDUI.Controls;
using SDUI.Native.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using static SDUI.Native.Windows.Methods;

namespace SDUI;

public class Application
{
    private static readonly List<UIWindowBase> _openForms = new();
    private static UIWindowBase _activeForm;
    private static bool _dpiAwarenessSet;

    public static IReadOnlyList<UIWindowBase> OpenForms => _openForms.AsReadOnly();

    public static UIWindowBase ActiveForm
    {
        get => _activeForm;
        internal set
        {
            if (_activeForm == value) return;
            _activeForm = value;
        }
    }

    /// <summary>
    /// Sets the process DPI awareness to Per-Monitor V2 (Win10 1703+),
    /// falling back to Per-Monitor (Win8.1+).
    /// Call before creating any windows.
    /// </summary>
    public static void EnableDpiAwareness()
    {
        if (_dpiAwarenessSet)
            return;

        _dpiAwarenessSet = true;

        if (Environment.OSVersion.Version >= new Version(10, 0, 15063))
        {
            try
            {
                SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                return;
            }
            catch (EntryPointNotFoundException) { }
        }

        try
        {
            SetProcessDpiAwareness(2); // PROCESS_PER_MONITOR_DPI_AWARE
        }
        catch (EntryPointNotFoundException) { }
    }

    internal static void RegisterForm(UIWindowBase form)
    {
        if (form == null || _openForms.Contains(form))
            return;

        _openForms.Add(form);
        _activeForm = form;
    }

    internal static void UnregisterForm(UIWindowBase form)
    {
        if (form == null)
            return;

        _openForms.Remove(form);

        if (_activeForm == form)
            _activeForm = _openForms.LastOrDefault();
    }

    internal static void SetActiveForm(UIWindowBase form)
    {
        if (form == null || !_openForms.Contains(form))
            return;

        _activeForm = form;
    }

    public static void Run(UIWindowBase window)
    {
		try
		{
            EnableDpiAwareness();

            if (!window.IsHandleCreated)
                window.CreateHandle();

            window.Show();

            MSG msg;
            while (GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
            {
				try
				{
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
				catch (Exception e)
				{
                    Debug.WriteLine("Exception in message loop: " + e.ToString());
				}
            }
        }
		catch (Exception ex)
		{
            DefWindowProc(IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero);
            Debug.WriteLine("Exception in Application.Run: " + ex.ToString());
		}
    }
}
