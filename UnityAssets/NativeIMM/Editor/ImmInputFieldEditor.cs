using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;
using UnityEditor.AnimatedValues;
using TMPro.EditorUtilities;

[CanEditMultipleObjects]
[CustomEditor(typeof(ImmInputField), true)]
public class ImmInputFieldEditor : SelectableEditor
{
    private struct Foldout
    { // Track Inspector foldout panel states, globally.
        public static bool textInput = true;
        public static bool fontSettings = true;
        public static bool extraSettings = true;
        //public static bool shadowSetting = false;
        //public static bool materialEditor = true;
    }

    SerializedProperty m_TextViewport;
    SerializedProperty m_TextComponent;
    SerializedProperty m_Text;
    SerializedProperty m_ContentType;
    SerializedProperty m_LineType;
    SerializedProperty m_LineLimit;
    SerializedProperty m_InputType;
    SerializedProperty m_ReturnKeyType;
    SerializedProperty m_CharacterValidation;
    SerializedProperty m_InputValidator;
    SerializedProperty m_RegexValue;
    SerializedProperty m_KeyboardType;
    SerializedProperty m_CharacterLimit;
    SerializedProperty m_CaretBlinkRate;
    SerializedProperty m_CaretWidth;
    SerializedProperty m_CaretColor;
    SerializedProperty m_CustomCaretColor;
    SerializedProperty m_SelectionColor;
    SerializedProperty m_Placeholder;
    SerializedProperty m_VerticalScrollbar;
    SerializedProperty m_ScrollbarScrollSensitivity;
    SerializedProperty m_OnValueChanged;
    SerializedProperty m_OnEndEdit;
    SerializedProperty m_OnSelect;
    SerializedProperty m_OnDeselect;
    SerializedProperty m_ReadOnly;
    SerializedProperty m_HideOnSubmit;
    SerializedProperty m_CloseWithBackButton;
    SerializedProperty m_KeepFocus;
    SerializedProperty m_RichText;
    SerializedProperty m_RichTextEditingAllowed;
    SerializedProperty m_ResetOnDeActivation;
    SerializedProperty m_RestoreOriginalTextOnEscape;

    SerializedProperty m_OnFocusSelectAll;
    SerializedProperty m_GlobalPointSize;
    SerializedProperty m_GlobalFontAsset;

    AnimBool m_CustomColor;

    //TMP_InputValidator m_ValidationScript;

    protected override void OnEnable()
    {
        base.OnEnable();

        m_TextViewport = serializedObject.FindProperty("m_TextViewport");
        m_TextComponent = serializedObject.FindProperty("m_TextComponent");
        m_Text = serializedObject.FindProperty("m_Text");
        m_ContentType = serializedObject.FindProperty("m_ContentType");
        m_LineType = serializedObject.FindProperty("m_LineType");
        m_LineLimit = serializedObject.FindProperty("m_LineLimit");
        m_InputType = serializedObject.FindProperty("m_InputType");
        m_ReturnKeyType = serializedObject.FindProperty("m_ReturnKeyType");
        m_CharacterValidation = serializedObject.FindProperty("m_CharacterValidation");
        m_InputValidator = serializedObject.FindProperty("m_InputValidator");
        m_RegexValue = serializedObject.FindProperty("m_RegexValue");
        m_KeyboardType = serializedObject.FindProperty("m_KeyboardType");
        m_CharacterLimit = serializedObject.FindProperty("m_CharacterLimit");
        m_CaretBlinkRate = serializedObject.FindProperty("m_CaretBlinkRate");
        m_CaretWidth = serializedObject.FindProperty("m_CaretWidth");
        m_CaretColor = serializedObject.FindProperty("m_CaretColor");
        m_CustomCaretColor = serializedObject.FindProperty("m_CustomCaretColor");
        m_SelectionColor = serializedObject.FindProperty("m_SelectionColor");

        m_Placeholder = serializedObject.FindProperty("m_Placeholder");
        m_VerticalScrollbar = serializedObject.FindProperty("m_VerticalScrollbar");
        m_ScrollbarScrollSensitivity = serializedObject.FindProperty("m_ScrollSensitivity");

        m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");
        m_OnEndEdit = serializedObject.FindProperty("m_OnEndEdit");
        m_OnSelect = serializedObject.FindProperty("m_OnSelect");
        m_OnDeselect = serializedObject.FindProperty("m_OnDeselect");
        m_ReadOnly = serializedObject.FindProperty("m_ReadOnly");
        m_HideOnSubmit = serializedObject.FindProperty("hideOnSubmit");
        m_CloseWithBackButton = serializedObject.FindProperty("closeWithBackButton");
        m_KeepFocus = serializedObject.FindProperty("keepFocus");
        m_RichText = serializedObject.FindProperty("m_RichText");
        m_RichTextEditingAllowed = serializedObject.FindProperty("m_isRichTextEditingAllowed");
        m_ResetOnDeActivation = serializedObject.FindProperty("m_ResetOnDeActivation");
        m_RestoreOriginalTextOnEscape = serializedObject.FindProperty("m_RestoreOriginalTextOnEscape");

        m_OnFocusSelectAll = serializedObject.FindProperty("m_OnFocusSelectAll");
        m_GlobalPointSize = serializedObject.FindProperty("m_GlobalPointSize");
        m_GlobalFontAsset = serializedObject.FindProperty("m_GlobalFontAsset");

        m_CustomColor = new AnimBool(m_CustomCaretColor.boolValue);
        m_CustomColor.valueChanged.AddListener(Repaint);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        m_CustomColor.valueChanged.RemoveListener(Repaint);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.OnInspectorGUI();

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_TextViewport);

        EditorGUILayout.PropertyField(m_TextComponent);

        TMPro.TextMeshProUGUI text = null;
        if (m_TextComponent != null && m_TextComponent.objectReferenceValue != null)
        {
            text = m_TextComponent.objectReferenceValue as TMPro.TextMeshProUGUI;
            //if (text.supportRichText)
            //{
            //    EditorGUILayout.HelpBox("Using Rich Text with input is unsupported.", MessageType.Warning);
            //}
        }

        EditorGUI.BeginDisabledGroup(m_TextComponent == null || m_TextComponent.objectReferenceValue == null);

        // TEXT INPUT BOX
        EditorGUILayout.PropertyField(m_Text);

        // INPUT FIELD SETTINGS
        #region INPUT FIELD SETTINGS

        Foldout.fontSettings = EditorGUILayout.Foldout(Foldout.fontSettings, "Input Field Settings", true, TMP_UIStyleManager.boldFoldout);

        if (Foldout.fontSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_GlobalFontAsset, new GUIContent("Font Asset", "Set the Font Asset for both Placeholder and Input Field text object."));
            if (EditorGUI.EndChangeCheck())
            {
                ImmInputField inputField = target as ImmInputField;
                inputField.SetGlobalFontAsset(m_GlobalFontAsset.objectReferenceValue as TMPro.TMP_FontAsset);
            }


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_GlobalPointSize, new GUIContent("Point Size", "Set the point size of both Placeholder and Input Field text object."));
            if (EditorGUI.EndChangeCheck())
            {
                ImmInputField inputField = target as ImmInputField;
                inputField.SetGlobalPointSize(m_GlobalPointSize.floatValue);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CharacterLimit);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_ReturnKeyType);
            EditorGUILayout.PropertyField(m_ContentType);
            if (!m_ContentType.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;

                if (m_ContentType.enumValueIndex == (int)TMPro.TMP_InputField.ContentType.Standard ||
                    m_ContentType.enumValueIndex == (int)TMPro.TMP_InputField.ContentType.Autocorrected ||
                    m_ContentType.enumValueIndex == (int)TMPro.TMP_InputField.ContentType.Custom)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_LineType);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (text != null)
                        {
                            if (m_LineType.enumValueIndex == (int)TMPro.TMP_InputField.LineType.SingleLine)
                                text.enableWordWrapping = false;
                            else
                            {
                                text.enableWordWrapping = true;
                            }
                        }
                    }

                    if (m_LineType.enumValueIndex != (int)TMPro.TMP_InputField.LineType.SingleLine)
                    {
                        EditorGUILayout.PropertyField(m_LineLimit);
                    }
                }

                if (m_ContentType.enumValueIndex == (int)TMPro.TMP_InputField.ContentType.Custom)
                {
                    EditorGUILayout.PropertyField(m_InputType);
                    EditorGUILayout.PropertyField(m_KeyboardType);
                    EditorGUILayout.PropertyField(m_CharacterValidation);
                    if (m_CharacterValidation.enumValueIndex == (int)TMPro.TMP_InputField.CharacterValidation.Regex)
                    {
                        EditorGUILayout.PropertyField(m_RegexValue);
                    }
                    else if (m_CharacterValidation.enumValueIndex == (int)TMPro.TMP_InputField.CharacterValidation.CustomValidator)
                    {
                        EditorGUILayout.PropertyField(m_InputValidator);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Placeholder);
            EditorGUILayout.PropertyField(m_VerticalScrollbar);

            if (m_VerticalScrollbar.objectReferenceValue != null)
                EditorGUILayout.PropertyField(m_ScrollbarScrollSensitivity);

            EditorGUILayout.PropertyField(m_CaretBlinkRate);
            EditorGUILayout.PropertyField(m_CaretWidth);

            EditorGUILayout.PropertyField(m_CustomCaretColor);

            m_CustomColor.target = m_CustomCaretColor.boolValue;

            if (EditorGUILayout.BeginFadeGroup(m_CustomColor.faded))
            {
                EditorGUILayout.PropertyField(m_CaretColor);
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_SelectionColor);

            EditorGUI.indentLevel--;
        }
        #endregion


        // CONTROL SETTINGS
        #region CONTROL SETTINGS
        Foldout.extraSettings = EditorGUILayout.Foldout(Foldout.extraSettings, "Control Settings", true, TMP_UIStyleManager.boldFoldout);

        if (Foldout.extraSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_OnFocusSelectAll, new GUIContent("OnFocus - Select All", "Should all the text be selected when the Input Field is selected."));
            EditorGUILayout.PropertyField(m_ResetOnDeActivation, new GUIContent("Reset On DeActivation", "Should the Text and Caret position be reset when Input Field is DeActivated."));
            EditorGUILayout.PropertyField(m_RestoreOriginalTextOnEscape, new GUIContent("Restore On ESC Key", "Should the original text be restored when pressing ESC."));
            EditorGUILayout.PropertyField(m_ReadOnly);
            EditorGUILayout.PropertyField(m_HideOnSubmit);
            EditorGUILayout.PropertyField(m_CloseWithBackButton);
            EditorGUILayout.PropertyField(m_KeepFocus);
            EditorGUILayout.PropertyField(m_RichText);
            EditorGUILayout.PropertyField(m_RichTextEditingAllowed, new GUIContent("Allow Rich Text Editing"));

            EditorGUI.indentLevel--;
        }
        #endregion


        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(m_OnValueChanged);
        EditorGUILayout.PropertyField(m_OnEndEdit);
        EditorGUILayout.PropertyField(m_OnSelect);
        EditorGUILayout.PropertyField(m_OnDeselect);

        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
