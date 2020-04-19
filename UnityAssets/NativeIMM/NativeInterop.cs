using NotSoSimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ReturnKeyType
{
    Default,
    Next,
    Done,
    Search
}

public class InputReceiverParams
{
    public TMPro.TMP_InputField.ContentType ContentType { get; set; }
    public TMPro.TMP_InputField.InputType InputType { get; set; }
    public TouchScreenKeyboardType KeyboardType { get; set; }
    public ReturnKeyType ReturnKeyType { get; set; }
    public bool Multiline { get; set; }
    public bool CloseWithBackButton { get; set; }

    string TranslateToJavaName(string name) => char.ToLower(name[0]) + name.Substring(1);

    public AndroidJavaObject ToJavaObject()
    {
        var obj = new AndroidJavaObject("ir.onehand.nativeimm.InputReceiverParams");
        obj.Set(TranslateToJavaName(nameof(ContentType)), ContentType.ToString());
        obj.Set(TranslateToJavaName(nameof(InputType)), InputType.ToString());
        obj.Set(TranslateToJavaName(nameof(KeyboardType)), KeyboardType.ToString());
        obj.Set(TranslateToJavaName(nameof(ReturnKeyType)), ReturnKeyType.ToString());
        obj.Set(TranslateToJavaName(nameof(Multiline)), Multiline);
        obj.Set(TranslateToJavaName(nameof(CloseWithBackButton)), CloseWithBackButton);
        return obj;
    }
}

public class EditableTextInfo
{
    public EditableTextInfo(JSONObject jObj)
    {
        Text = jObj["text"].AsString;
        SelectionStart = jObj["selStart"].AsInt.Value;
        SelectionEnd = jObj["selEnd"].AsInt.Value;
        ComposingStart = jObj["compStart"].AsInt.Value;
        ComposingEnd = jObj["compEnd"].AsInt.Value;
    }

    public string Text { get; }
    public int ComposingStart { get; }
    public int ComposingEnd { get; }
    public int SelectionStart { get; }
    public int SelectionEnd { get; }

    public override string ToString() => 
        $"EditableTextInfo: '{Text}', selection {SelectionStart} - {SelectionEnd}, composing {ComposingStart} - {ComposingEnd}";
}

class NativeInterop : MonoBehaviour
{
    public static NativeInterop instance;
    public static NativeInterop Instance
    {
        get
        {
            if (instance == null)
            {
                // Name used by native code, do not rename
                var go = new GameObject("NativeIMM");
                DontDestroyOnLoad(go);

                instance = go.AddComponent<NativeInterop>();
            }

            return instance;
        }
    }

    public delegate void VisibleHeightChangedDelegate(int visibleHeight);
    public event VisibleHeightChangedDelegate VisibleHeightChanged;

    public delegate void InitDoneDelegate();
    public event InitDoneDelegate InitDone;

    public delegate void ViewCreatedDelegate(int id);
    public event ViewCreatedDelegate ViewCreated;

    public delegate void EnterPressedDelegate(int id);
    public event EnterPressedDelegate EnterPressed;

    public delegate void TextChangedDelegate(int id, EditableTextInfo textInfo);
    public event TextChangedDelegate TextChanged;

    public delegate void ErrorReceivedDelegate(string code, string message, JSONObject data);
    public event ErrorReceivedDelegate ErrorReceived;

    readonly AndroidJavaClass nativeClass = new AndroidJavaClass("ir.onehand.nativeimm.UnityInterop");

    void OnDestroy() => Destroy();

    [Obsolete("For native interop, do not access directly")]
    public void ReceiveData(string dataString)
    {
        var json = JSON.Parse(dataString);
        var name = json["name"].AsString;
        var args = json["args"].AsObject;
        var id = args["id"].AsInt;

        switch (name)
        {
            case "KB_HEIGHT_CHANGED":
                VisibleHeightChanged?.Invoke(args["visibleHeight"].AsInt.Value);
                break;

            case "INIT_DONE":
                InitDone?.Invoke();
                break;

            case "VIEW_CREATED":
                ViewCreated?.Invoke(id.Value);
                break;

            case "ENTER_PRESSED":
                EnterPressed?.Invoke(id.Value);
                break;

            case "TEXT_CHANGED":
                TextChanged?.Invoke(id.Value, new EditableTextInfo(args));
                break;

            default:
                Debug.LogError($"Unknown event {name} from native module");
                break;
        }
    }

    [Obsolete("For native interop, do not access directly")]
    public void ReceiveError(string dataString)
    {
        var json = JSON.Parse(dataString);
        var code = json["code"].AsString;
        var message = json["message"].AsString;
        var data = json["data"].AsObject;

        Debug.LogError($"NativeKeyboard native error '{code}': {message}");

        ErrorReceived?.Invoke(code, message, data);
    }

    public bool Initialize(bool includeNotchInHeight) =>
        nativeClass.CallStatic<bool>("initialize", includeNotchInHeight);

    public void Destroy() =>
        nativeClass.CallStatic("destroy");

    public AndroidJavaObject GetIMMManager() =>
        nativeClass.CallStatic<AndroidJavaObject>("getIMMManager");

    public void SetDebugMode(bool enabled) =>
        nativeClass.CallStatic("setDebugModeEnabled", enabled);

    public int CreateInputReceiverView(AndroidJavaObject immManager, InputReceiverParams inputReceiverParams) =>
        immManager.Call<int>("createInputReceiver", inputReceiverParams.ToJavaObject());

    public AndroidJavaObject GetInputReceiverView(AndroidJavaObject immManager, int id) =>
        immManager.Call<AndroidJavaObject>("getView", id);

    public void SetText(AndroidJavaObject inputReceiverView, string text) =>
        inputReceiverView.Call("setText", text);

    public void SetActive(AndroidJavaObject inputReceiverView, bool focus) =>
        inputReceiverView.Call("setActive", focus);

    public void SetSelection(AndroidJavaObject inputReceiverView, int selectionStart, int selectionEnd) =>
        inputReceiverView.Call("setSelection", selectionStart, selectionEnd);

    public void DestroyReceiver(AndroidJavaObject inputReceiverView) =>
        inputReceiverView.Call("destroy");
}
