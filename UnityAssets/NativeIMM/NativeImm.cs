// #define NATIVE_IMM_DEBUG_MODE

#pragma warning disable CS0162 // Unreachable code detected

using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class NativeImm : MonoBehaviour
{
#if NATIVE_IMM_DEBUG_MODE
    public const bool DebugMode = true;
#else
    public const bool DebugMode = false;
#endif

    static NativeImm instance;

    public static NativeImm Instance =>
        instance ?? (instance = NativeInterop.Instance.gameObject.AddComponent<NativeImm>());

    TaskCompletionSource<object> initializeTCS;

    AndroidJavaObject inputReceiverView;

    NativeInputReceiver activeInputReceiver;

    NativeInputReceiver receiverToActivate;
    int deactivateCurrentlyActiveReceiver;
    bool textUpdated;
    bool selectionUpdated;

    internal NativeInputReceiver NextActiveInputReceiver =>
        receiverToActivate ?? (deactivateCurrentlyActiveReceiver > 0 ? null : activeInputReceiver);

    public delegate void VisibleHeightChangedDelegate(float visibleHeightRatio);
    public event VisibleHeightChangedDelegate VisibleHeightChanged;

    public delegate void InputReceiverEventDelegate(NativeInputReceiver receiver);
    public event InputReceiverEventDelegate EnterPressed;

    public delegate void InputReceiverTextChangedDelegate(NativeInputReceiver receiver, EditableTextInfo textInfo);
    public event InputReceiverTextChangedDelegate TextChanged;

    public delegate void KeyboardStateChangedDelegate(bool shown);
    public event KeyboardStateChangedDelegate KeyboardStateChanged;

    public bool KeyboardVisible { get; private set; }

    public Task Initialize()
    {
        if (initializeTCS != null)
            return initializeTCS.Task;

        var ni = NativeInterop.Instance;

        ni.SetDebugMode(DebugMode);

        ni.TextChanged += OnTextChanged; ;
        ni.EnterPressed += OnEnterPressed;
        ni.VisibleHeightChanged += OnVisibleHeightChanged;
        ni.InitDone += OnInitDone;

        initializeTCS = new TaskCompletionSource<object>();

        if (ni.Initialize(Screen.cutouts.Any()))
            CompleteInit();

        return initializeTCS.Task;
    }

    void CompleteInit()
    {
        try
        {
            inputReceiverView = NativeInterop.Instance.GetInputReceiverView();
            initializeTCS?.TrySetResult(null);
        }
        catch (Exception ex)
        {
            initializeTCS?.TrySetException(ex);
        }
    }

    void OnInitDone() => CompleteInit();

    void OnDestroy() => Destroy();

    public void Destroy()
    {
        var ni = NativeInterop.Instance;

        ni.TextChanged -= OnTextChanged;
        ni.EnterPressed -= OnEnterPressed;
        ni.VisibleHeightChanged -= OnVisibleHeightChanged;
        ni.InitDone -= OnInitDone;

        ni.DestroyReceiver(inputReceiverView);

        initializeTCS = null;
    }

    void OnVisibleHeightChanged(int visibleHeight)
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

    void OnEnterPressed()
    {
        if (activeInputReceiver != null)
        {
            EnterPressed?.Invoke(activeInputReceiver);
            activeInputReceiver.OnEnterPressed();
        }
    }

    private void OnTextChanged(EditableTextInfo textInfo)
    {
        if (activeInputReceiver != null)
        {
            TextChanged?.Invoke(activeInputReceiver, textInfo);
            activeInputReceiver.OnTextChanged(textInfo);
        }
    }

    public void Activate(NativeInputReceiver receiver)
    {
        if (activeInputReceiver != receiver)
            receiverToActivate = receiver;
        else
            receiverToActivate = null;
    }

    public void Deactivate(NativeInputReceiver receiver)
    {
        if (activeInputReceiver == receiver)
            deactivateCurrentlyActiveReceiver = 2;
    }

    public void TextUpdated(NativeInputReceiver receiver)
    {
        if (activeInputReceiver == receiver)
            textUpdated = true;
    }

    public void SelectionUpdated(NativeInputReceiver receiver)
    {
        if (activeInputReceiver == receiver)
            selectionUpdated = true;
    }

    void LateUpdate()
    {
        var ni = NativeInterop.Instance;

        if (receiverToActivate != null)
        {
            if (DebugMode)
                Debug.Log($"Activating new input receiver with text = '{receiverToActivate.Text}'");

            if (activeInputReceiver != null)
                activeInputReceiver.OnDeactivated();

            ni.Open(inputReceiverView, receiverToActivate.ReceiverParams);
            ni.UpdateStatus(inputReceiverView, true, receiverToActivate.Text, true, receiverToActivate.Selection.start, receiverToActivate.Selection.end);

            deactivateCurrentlyActiveReceiver = 0;
            textUpdated = false;
            selectionUpdated = false;
            activeInputReceiver = receiverToActivate;
            receiverToActivate = null;
        }

        if (deactivateCurrentlyActiveReceiver > 0)
        {
            --deactivateCurrentlyActiveReceiver;

            if (deactivateCurrentlyActiveReceiver == 0)
            {
                if (DebugMode)
                    Debug.Log("Deactivating input receiver");

                activeInputReceiver = null;
                ni.Close(inputReceiverView);
            }
            else
                if (DebugMode)
                    Debug.Log($"Will deactivate input receiver in {deactivateCurrentlyActiveReceiver} frames");
        }

        if (textUpdated || selectionUpdated)
        {
            if (DebugMode)
                Debug.Log($"Updating status: text = '{(textUpdated ? activeInputReceiver.Text : "NO CHANGE")}', " +
                    $"selection = {(selectionUpdated ? activeInputReceiver.Selection.start.ToString() : "NO CHANGE")} - " +
                    $"{(selectionUpdated ? activeInputReceiver.Selection.end.ToString() : "NO CHANGE")}");

            if (activeInputReceiver != null)
                ni.UpdateStatus(
                    inputReceiverView,
                    textUpdated,
                    textUpdated ? activeInputReceiver.Text : "",
                    selectionUpdated,
                    selectionUpdated ? activeInputReceiver.Selection.start : 0,
                    selectionUpdated ? activeInputReceiver.Selection.end : 0
                    );

            textUpdated = false;
            selectionUpdated = false;
        }
    }
}
