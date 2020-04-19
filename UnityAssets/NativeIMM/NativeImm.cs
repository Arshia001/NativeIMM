// #define NATIVE_IMM_DEBUG_MODE

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class NativeImm
{
#if NATIVE_IMM_DEBUG_MODE
    public const bool DebugMode = true;
#else
    public const bool DebugMode = false;
#endif

    static TaskCompletionSource<object> initializeTCS;

    static AndroidJavaObject immManager;

    static readonly Dictionary<int, NativeInputReceiver> receivers = new Dictionary<int, NativeInputReceiver>();
    static readonly Dictionary<int, TaskCompletionSource<(int id, AndroidJavaObject nativeObj)>> receiverTCSs =
        new Dictionary<int, TaskCompletionSource<(int id, AndroidJavaObject nativeObj)>>();

    public delegate void VisibleHeightChangedDelegate(float visibleHeightRatio);
    public static event VisibleHeightChangedDelegate VisibleHeightChanged;

    public delegate void InputReceiverEventDelegate(NativeInputReceiver receiver);
    public static event InputReceiverEventDelegate EnterPressed;

    public delegate void InputReceiverTextChangedDelegate(NativeInputReceiver receiver, EditableTextInfo textInfo);
    public static event InputReceiverTextChangedDelegate TextChanged;

    public delegate void KeyboardStateChangedDelegate(bool shown);
    public static event KeyboardStateChangedDelegate KeyboardStateChanged;

    public static bool KeyboardVisible { get; private set; }

    public static Task Initialize()
    {
        if (initializeTCS != null)
            return initializeTCS.Task;

        var ni = NativeInterop.Instance;

        ni.SetDebugMode(DebugMode);

        ni.TextChanged += OnTextChanged; ;
        ni.EnterPressed += OnEnterPressed;
        ni.ViewCreated += OnViewCreated;
        ni.VisibleHeightChanged += OnVisibleHeightChanged;
        ni.InitDone += OnInitDone;

        initializeTCS = new TaskCompletionSource<object>();

        if (ni.Initialize(Screen.cutouts.Any()))
            CompleteInit();

        return initializeTCS.Task;
    }

    static void CompleteInit()
    {
        try
        {
            immManager = NativeInterop.Instance.GetIMMManager();
            initializeTCS?.TrySetResult(null);
        }
        catch (Exception ex)
        {
            initializeTCS?.TrySetException(ex);
        }
    }

    static void OnInitDone() => CompleteInit();

    public static void Destroy()
    {
        var ni = NativeInterop.Instance;

        ni.TextChanged -= OnTextChanged;
        ni.EnterPressed -= OnEnterPressed;
        ni.ViewCreated -= OnViewCreated;
        ni.VisibleHeightChanged -= OnVisibleHeightChanged;
        ni.InitDone -= OnInitDone;

        receivers.Clear();
        receiverTCSs.Clear();
        initializeTCS = null;
    }

    internal static Task<(int id, AndroidJavaObject nativeObj)> CreateView(NativeInputReceiver receiver, InputReceiverParams inputReceiverParams)
    {
        var tcs = new TaskCompletionSource<(int id, AndroidJavaObject nativeObj)>();
        var id = NativeInterop.Instance.CreateInputReceiverView(immManager, inputReceiverParams);
        receivers[id] = receiver;
        receiverTCSs[id] = tcs;
        return tcs.Task;
    }

    static void OnViewCreated(int id)
    {
        if (receiverTCSs.TryGetValue(id, out var tcs))
        {
            tcs.SetResult((id, NativeInterop.Instance.GetInputReceiverView(immManager, id)));
            receiverTCSs.Remove(id);
        }
    }

    internal static void DestroyReceiver(NativeInputReceiver receiver)
    {
        receivers.Remove(receiver.ID);
        NativeInterop.instance.DestroyReceiver(receiver.NativeObject);
    }

    static void OnVisibleHeightChanged(int visibleHeight)
    {
        var ratio = visibleHeight / (float)Screen.height;
        if (ratio >= 0.9f)
            ratio = 1.0f;

        VisibleHeightChanged?.Invoke(ratio);

        var visible = ratio < 0.999f;

        if (KeyboardVisible != visible)
            KeyboardStateChanged?.Invoke(visible);

        KeyboardVisible = visible;
    }

    static void OnEnterPressed(int id)
    {
        if (receivers.TryGetValue(id, out var receiver))
        {
            EnterPressed?.Invoke(receiver);
            receiver.OnEnterPressed();
        }
    }

    private static void OnTextChanged(int id, EditableTextInfo textInfo)
    {
        if (receivers.TryGetValue(id, out var receiver))
        {
            TextChanged?.Invoke(receiver, textInfo);
            receiver.OnTextChanged(textInfo);
        }
    }
}
