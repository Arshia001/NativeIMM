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
    public bool Active
    {
        get => NativeImm.Instance.NextActiveInputReceiver == this;
        set
        {
            if (NativeImm.DebugMode)
                Debug.Log($"Setting {nameof(value)}: {value}");

            if (value)
                NativeImm.Instance.Activate(this);
            else
                NativeImm.Instance.Deactivate(this);
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

            if (text != value)
            {
                text = value;
                NativeImm.Instance.TextUpdated(this);
            }
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

            var range = UnityRangeToAndroidRange(value);
            if (range != NativeSelectionRange)
            {
                NativeSelectionRange = range;
                NativeImm.Instance.SelectionUpdated(this);
            }
        }
    }

    internal (int start, int end) NativeSelectionRange { get; private set; }

    public RangeInt? composition;

    public event Action Submit;

    public event Action Closed;

    public event Action<NativeInputReceiver> TextChanged;

    public InputReceiverParams ReceiverParams { get; private set; }

    private NativeInputReceiver() { }

    ~NativeInputReceiver() => Destroy();

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

        NativeImm.Instance.KeyboardStateChanged += receiver.OnKeyboardStatusChanged;

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
        await NativeImm.Instance.Initialize();

        ReceiverParams = receiverParams;
        text = initialText;

        selection = new RangeInt(initialText.Length, 0);
        NativeSelectionRange = (initialText.Length, initialText.Length);
    }

    public void Destroy() => NativeImm.Instance.KeyboardStateChanged -= OnKeyboardStatusChanged;

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

    internal void OnDeactivated()
    {
        if (NativeImm.DebugMode)
            Debug.Log($"Input receiver lost focus");

        Closed?.Invoke();
    }

    internal void OnKeyboardStatusChanged(bool shown)
    {
        if (!shown && Active)
        {
            if (NativeImm.DebugMode)
                Debug.Log($"Soft keyboard closed");

            Active = false;
            Closed?.Invoke();
        }
    }
}
