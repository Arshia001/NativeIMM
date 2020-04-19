#pragma warning disable CS0162 // Unreachable code detected

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class NativeInputReceiver
{
    internal int ID { get; private set; }

    internal AndroidJavaObject NativeObject { get; private set; }

    bool active;
    public bool Active
    {
        get => active;
        set
        {
            if (NativeImm.DebugMode)
                Debug.Log($"Setting {nameof(active)}: {value}");

            active = value;
            NativeInterop.Instance.SetActive(NativeObject, value);
        }
    }

    string text = "";
    public string Text
    {
        get => text;
        set
        {
            if (NativeImm.DebugMode)
                Debug.Log($"Setting {nameof(text)}: {value}");

            text = value;
            NativeInterop.Instance.SetText(NativeObject, value);
        }
    }

    RangeInt selection;
    public RangeInt Selection
    {
        get => selection;
        set
        {
            if (NativeImm.DebugMode)
                Debug.Log($"Setting {nameof(selection)}: {value.start} - {value.end}");

            selection = value;
            var (start, end) = UnityRangeToAndroidRange(value);
            NativeInterop.Instance.SetSelection(NativeObject, start, end);
        }
    }

    public RangeInt? composition;

    public event Action Submit;

    public event Action Closed;

    public event Action<NativeInputReceiver> TextChanged;

    private NativeInputReceiver() { }

    public static async Task<NativeInputReceiver> Open(string text, TMPro.TMP_InputField.ContentType contentType,
        TMPro.TMP_InputField.InputType inputType, TouchScreenKeyboardType keyboardType, ReturnKeyType returnKeyType,
        bool multiline, bool closeWithBackButton)
    {
        var receiver = new NativeInputReceiver();

        await receiver.InitializeNativeField(new InputReceiverParams
        {
            ContentType = contentType,
            Multiline = multiline,
            KeyboardType = keyboardType,
            InputType = inputType,
            ReturnKeyType = returnKeyType,
            CloseWithBackButton = closeWithBackButton 
        }, text);

        receiver.Active = true;

        NativeImm.KeyboardStateChanged += receiver.OnKeyboardStatusChanged;

        return receiver;
    }

    (int start, int end) UnityRangeToAndroidRange(RangeInt range)
    {
        var start = range.start;
        var end = range.end;
        return (Math.Min(start, end), Math.Max(start, end));
    }

    RangeInt AndroidRangeToUnityRange(int start, int end) => new RangeInt(start, end - start);

    async Task InitializeNativeField(InputReceiverParams receiverParams, string initialText)
    {
        await NativeImm.Initialize();

        (ID, NativeObject) = await NativeImm.CreateView(this, receiverParams);

        NativeInterop.Instance.SetText(NativeObject, initialText);
    }

    public void Destroy()
    {
        NativeImm.KeyboardStateChanged -= OnKeyboardStatusChanged;

        if (NativeObject != null)
            NativeImm.DestroyReceiver(this);

        NativeObject = null;
    }

    internal void OnTextChanged(EditableTextInfo textInfo)
    {
        if (NativeImm.DebugMode)
            Debug.Log($"Received text update: {textInfo}");

        text = textInfo.Text ?? "";

        selection = AndroidRangeToUnityRange(textInfo.SelectionStart, textInfo.SelectionEnd);

        composition = textInfo.ComposingStart == textInfo.ComposingEnd ? default(RangeInt?) : AndroidRangeToUnityRange(textInfo.ComposingStart, textInfo.ComposingEnd);

        if (NativeImm.DebugMode)
            Debug.Log($"Text: '{text}', selection: {selection.start} - {selection.end}, composition: {composition?.start.ToString() ?? "NONE"} - {composition?.end.ToString() ?? "NONE"}");

        TextChanged?.Invoke(this);
    }

    internal void OnEnterPressed()
    {
        if (NativeImm.DebugMode)
            Debug.Log($"Enter pressed");

        Submit?.Invoke();
    }

    internal void OnKeyboardStatusChanged(bool shown)
    {
        if (!shown && active)
        {
            if (NativeImm.DebugMode)
                Debug.Log($"Soft keyboard closed");

            Closed?.Invoke();
        }
    }
}
