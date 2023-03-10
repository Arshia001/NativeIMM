@file:Suppress("unused", "MemberVisibilityCanBePrivate")

package ir.onehand.nativeimm

import android.annotation.SuppressLint
import android.content.Context
import android.text.Editable
import android.text.InputType
import android.text.Selection
import android.util.Log
import android.view.KeyEvent
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import android.view.inputmethod.InputConnection
import android.view.inputmethod.InputMethodManager
import android.widget.RelativeLayout
import com.unity3d.player.UnityPlayer
import org.json.JSONObject

@SuppressLint("ViewConstructor")
class InputReceiverView(parentGroup: ViewGroup) :
    View(parentGroup.context) {

    private val relativeLayout = RelativeLayout(UnityPlayer.currentActivity)

    init {
        parentGroup.addView(
            relativeLayout,
            ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
        )
    }

    private val imm = context.getSystemService(Context.INPUT_METHOD_SERVICE) as InputMethodManager

    private val editable = Editable.Factory.getInstance().newEditable("")

    private var imeOptions = 0
    private var inputType = 0
    private var multiline = false
    private var closeWithBackButton = false

    private val eventReceiver = object : UnityInputConnectionEventReceiver {
        override fun onEdit(contents: EditableTextInfo) {
            sendMessage(UnityInterop.MessageName.TextChanged) {
                it.put("text", contents.text)
                it.put("selStart", contents.selectionStart)
                it.put("selEnd", contents.selectionEnd)
                it.put("compStart", contents.composingStart)
                it.put("compEnd", contents.composingEnd)
            }
        }

        override fun onAccept() {
            sendMessage(UnityInterop.MessageName.EnterPressed)
        }
    }

    init {
        layoutParams = RelativeLayout.LayoutParams(1, 1).apply {
            setMargins(-1, -1, 0, 0)
        }

        setBackgroundColor(0)
        isFocusable = true
        isFocusableInTouchMode = true

        relativeLayout.addView(this)
    }

    // interop method
    fun open(params: InputReceiverParams) =
        UnityInterop.executePluginFunction {
            imeOptions = getImeOptions(params)
            inputType = getInputType(params)
            multiline = params.multiline
            closeWithBackButton = params.closeWithBackButton

            if (!requestFocus()) {
                Log.e(LOG_TAG, "InputReceiverView failed to take focus")
            }
            showKeyboard()
        }

    // interop method
    fun close() =
        UnityInterop.executePluginFunction {
            hideKeyboard()
        }

    // interop method
    fun updateStatus(setText: Boolean, text: String, setSelection: Boolean, selectionStart: Int, selectionEnd: Int) =
        UnityInterop.executePluginFunction {
            if (setText) {
                editable.replace(0, editable.length, text)

                if (!setSelection) {
                    Selection.setSelection(editable, editable.length, editable.length)
                    imm.restartInput(this)
                }
            }

            if (setSelection) {
                var start = selectionStart
                var end = selectionEnd
                if (start > end) {
                    val temp = start
                    start = end
                    end = temp
                }

                UnityInputConnection.removeComposingSpans(editable)

                Selection.setSelection(
                    editable,
                    if (start < 0 || start > editable.length) editable.length else start,
                    if (end < 0 || end > editable.length) editable.length else end
                )

                imm.restartInput(this)
            }
        }

    // interop method
    fun destroy() =
        UnityInterop.executePluginFunction {
            if (isFocused)
                hideKeyboard()

            relativeLayout.removeView(this)
        }

    override fun onKeyPreIme(keyCode: Int, event: KeyEvent?): Boolean {
        if (!closeWithBackButton && keyCode == KeyEvent.KEYCODE_BACK)
            return true

        return super.onKeyPreIme(keyCode, event)
    }

    private fun showKeyboard() {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "Opening keyboard")

        imm.showSoftInput(this, InputMethodManager.SHOW_FORCED)
    }

    fun hideKeyboard() {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "Closing keyboard")

        clearFocus()
        UnityPlayer.currentActivity.window.decorView.clearFocus()
        imm.hideSoftInputFromWindow(windowToken, 0)
    }

    private fun sendMessage(name: UnityInterop.MessageName, putArgs: ((JSONObject) -> Unit)? = null) {
        val args = makeMessageArgs()
        if (putArgs != null)
            putArgs(args)
        UnityInterop.sendMessage(name, args)
    }

    private fun makeMessageArgs() = JSONObject()

    override fun onCheckIsTextEditor(): Boolean = true

    override fun onCreateInputConnection(outAttrs: EditorInfo): InputConnection {
        val connection =
            UnityInputConnection(eventReceiver, editable, this, multiline) // BaseInputConnection(this, false)

        outAttrs.imeOptions =
            imeOptions or
                    EditorInfo.IME_FLAG_NO_FULLSCREEN or // Don't enter fullscreen edit mode
                    EditorInfo.IME_FLAG_NO_EXTRACT_UI // Don't enter extracted UI mode (the same thing as fullscreen?)

        outAttrs.inputType = inputType

        var start = Selection.getSelectionStart(editable)
        var end = Selection.getSelectionEnd(editable)
        if (end < start) {
            val temp = end
            end = start
            start = temp
        }
        outAttrs.initialSelStart = if (start < 0) editable.length else start
        outAttrs.initialSelEnd = if (end < 0) editable.length else end

        return connection
    }

    private fun getImeOptions(params: InputReceiverParams): Int {
        var imeOptions = EditorInfo.IME_FLAG_NO_EXTRACT_UI
        when (params.returnKeyType) {
            "Next" -> imeOptions = imeOptions or EditorInfo.IME_ACTION_NEXT
            "Done" -> imeOptions = imeOptions or EditorInfo.IME_ACTION_DONE
            "Search" -> imeOptions = imeOptions or EditorInfo.IME_ACTION_SEARCH
        }
        return imeOptions
    }

    private fun getInputType(params: InputReceiverParams): Int {
        var editInputType = 0
        when (params.contentType) {
            "Standard" -> editInputType = editInputType or
                    (InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_SENTENCES)
            "Autocorrected" -> editInputType = editInputType or
                    (InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_SENTENCES or InputType.TYPE_TEXT_FLAG_AUTO_CORRECT)
            "IntegerNumber" -> editInputType = editInputType or InputType.TYPE_CLASS_NUMBER
            "DecimalNumber" -> editInputType = editInputType or
                    (InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_FLAG_DECIMAL)
            "Alphanumeric" -> editInputType = editInputType or
                    (InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_CAP_SENTENCES)
            "Name" -> editInputType = editInputType or
                    (InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PERSON_NAME)
            "EmailAddress" -> editInputType = editInputType or
                    (InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS)
            "Password" -> editInputType = editInputType or
                    (InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PASSWORD)
            "Pin" -> editInputType = editInputType or InputType.TYPE_CLASS_PHONE
            "Custom" // We need more details
            -> {
                when (params.keyboardType) {
                    "ASCIICapable" -> editInputType = InputType.TYPE_CLASS_TEXT or
                            InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS
                    "NumbersAndPunctuation" -> editInputType = InputType.TYPE_CLASS_NUMBER or
                            InputType.TYPE_NUMBER_FLAG_DECIMAL or InputType.TYPE_NUMBER_FLAG_SIGNED
                    "URL" -> editInputType = InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS or
                            InputType.TYPE_TEXT_VARIATION_URI
                    "NumberPad" -> editInputType = InputType.TYPE_CLASS_NUMBER
                    "PhonePad" -> editInputType = InputType.TYPE_CLASS_PHONE
                    "NamePhonePad" -> editInputType = InputType.TYPE_CLASS_TEXT or
                            InputType.TYPE_TEXT_VARIATION_PERSON_NAME
                    "EmailAddress" -> editInputType = InputType.TYPE_CLASS_TEXT or
                            InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS
                    "Social" -> editInputType = InputType.TYPE_TEXT_VARIATION_URI or
                            InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS
                    "Search" -> {
                        editInputType = InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS or
                                InputType.TYPE_NUMBER_FLAG_DECIMAL or InputType.TYPE_NUMBER_FLAG_SIGNED
                    }
                    else -> editInputType = InputType.TYPE_CLASS_TEXT
                }
                when (params.inputType) {
                    "Standard" -> {
                    }
                    "AutoCorrect" -> editInputType = editInputType or InputType.TYPE_TEXT_FLAG_AUTO_CORRECT
                    "Password" ->
                        editInputType =
                            if (params.keyboardType !== "NumbersAndPunctuation" && params.keyboardType !== "NumberPad" &&
                                params.keyboardType !== "PhonePad"
                            )
                                editInputType or (InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PASSWORD)
                            else
                                editInputType or InputType.TYPE_NUMBER_VARIATION_PASSWORD
                }
            }
            else -> editInputType = editInputType or InputType.TYPE_CLASS_TEXT
        }// This is default behaviour
        if (params.multiline) {
            editInputType = editInputType or
                    (InputType.TYPE_TEXT_FLAG_MULTI_LINE or InputType.TYPE_TEXT_FLAG_CAP_SENTENCES)
        }
        return editInputType
    }
}