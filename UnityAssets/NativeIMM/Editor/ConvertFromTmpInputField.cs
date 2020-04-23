using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using System;
using System.Reflection;

public class ConvertFromTmpInputField : EditorWindow
{
    [MenuItem("Window/Convert TMP input field to IMM input field")]
    public static void ShowWindow()
    {
        GetWindow(typeof(ConvertFromTmpInputField));
    }

    public void OnGUI()
    {
        if (GUILayout.Button("Convert selected"))
        {
            var selection = Selection.gameObjects;

            foreach (var go in selection)
                if (go.GetComponent<TMP_InputField>() != null)
                    Convert(go);

            Debug.Log("Done");
        }
    }

    void Convert(GameObject go)
    {
        Undo.RegisterCompleteObjectUndo(go, "Convert TMP input field to IMM input field");

        var tmp = go.GetComponent<TMP_InputField>();

        DestroyImmediate(tmp);

        var imm = ObjectFactory.AddComponent<ImmInputField>(go);

        typeof(ImmInputField).GetField("m_TextComponent", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_TextComponent", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_TextViewport", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_TextViewport", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_Text", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_Text", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_ContentType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_ContentType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_LineType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_LineType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_LineLimit", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_LineLimit", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_InputType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_InputType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_CharacterValidation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_CharacterValidation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_InputValidator", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_InputValidator", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_RegexValue", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_RegexValue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_KeyboardType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_KeyboardType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_CharacterLimit", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_CharacterLimit", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_CaretBlinkRate", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_CaretBlinkRate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_CaretWidth", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_CaretWidth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_CaretColor", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_CaretColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_CustomCaretColor", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_CustomCaretColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_SelectionColor", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_SelectionColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_Placeholder", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_Placeholder", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_VerticalScrollbar", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_VerticalScrollbar", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_ScrollSensitivity", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_ScrollSensitivity", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_OnValueChanged", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_OnValueChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_OnEndEdit", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_OnEndEdit", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_OnSelect", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_OnSelect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_OnDeselect", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_OnDeselect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_ReadOnly", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_ReadOnly", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_RichText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_RichText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_isRichTextEditingAllowed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_isRichTextEditingAllowed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_ResetOnDeActivation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_ResetOnDeActivation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_RestoreOriginalTextOnEscape", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_RestoreOriginalTextOnEscape", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_OnFocusSelectAll", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_OnFocusSelectAll", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_GlobalPointSize", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_GlobalPointSize", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));
        typeof(ImmInputField).GetField("m_GlobalFontAsset", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(imm, typeof(TMP_InputField).GetField("m_GlobalFontAsset", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tmp));

        Debug.Log($"Converted {go.name}", go);
    }
}
