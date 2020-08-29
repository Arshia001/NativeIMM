package ir.onehand.nativeimm

import android.content.Context
import android.os.Bundle
import android.os.Handler
import android.text.*
import android.text.method.MetaKeyKeyListener
import android.util.Log
import android.view.KeyEvent
import android.view.View
import android.view.inputmethod.*
import kotlin.math.max
import kotlin.math.min

class UnityInputConnection(
    private val eventReceiver: UnityInputConnectionEventReceiver,
    private val editable: Editable,
    private val targetView: View,
    private val multiline: Boolean
) : InputConnection {
    private class ComposingSpan : NoCopySpan

    companion object {
        private val Composing = ComposingSpan()

        fun getComposingStart(editable: Editable) = editable.getSpanStart(Composing)

        fun getComposingEnd(editable: Editable) = editable.getSpanEnd(Composing)

        fun removeComposingSpans(text: Spannable) {
            text.removeSpan(Composing)
            val sps = text.getSpans(0, text.length, Any::class.java)
            if (sps != null) {
                for (i in sps.indices.reversed()) {
                    val o = sps[i]
                    if (text.getSpanFlags(o) and Spanned.SPAN_COMPOSING != 0) {
                        text.removeSpan(o)
                    }
                }
            }
        }

        private fun setComposingSpans(text: Spannable) {
            setComposingSpans(text, 0, text.length)
        }

        @Suppress("SameParameterValue")
        private fun setComposingSpans(text: Spannable, start: Int, end: Int) {
            val spans = text.getSpans(start, end, Any::class.java)
            if (spans != null) {
                for (i in spans.indices.reversed()) {
                    val o = spans[i]
                    if (o === Composing) {
                        text.removeSpan(o)
                        continue
                    }
                    val fl = text.getSpanFlags(o)
                    if (fl and (Spanned.SPAN_COMPOSING or Spanned.SPAN_POINT_MARK_MASK)
                        != Spanned.SPAN_COMPOSING or Spanned.SPAN_EXCLUSIVE_EXCLUSIVE
                    ) {
                        text.setSpan(
                            o, text.getSpanStart(o), text.getSpanEnd(o),
                            fl and Spanned.SPAN_POINT_MARK_MASK.inv()
                                    or Spanned.SPAN_COMPOSING
                                    or Spanned.SPAN_EXCLUSIVE_EXCLUSIVE
                        )
                    }
                }
            }

            text.setSpan(
                Composing,
                start,
                end,
                Spanned.SPAN_EXCLUSIVE_EXCLUSIVE or Spanned.SPAN_COMPOSING
            )
        }
    }

    private val imm =
        targetView.context.getSystemService(Context.INPUT_METHOD_SERVICE) as InputMethodManager

    private var batchEditCount = 0
    private var textChanged = false

    private fun sendChanged() {
        if (batchEditCount > 0)
            return

        val composingStart = getComposingStart(editable)
        val composingEnd = getComposingEnd(editable)

        val selectionStart = Selection.getSelectionStart(editable)
        val selectionEnd = Selection.getSelectionEnd(editable)

        val info = EditableTextInfo(
            //if (textChanged) editable.toString() else null,
            editable.toString(),
            min(composingStart, composingEnd),
            max(composingStart, composingEnd),
            min(selectionStart, selectionEnd),
            max(selectionStart, selectionEnd)
        )

        textChanged = false

        eventReceiver.onEdit(info)
    }

    // Will replace the composing region; will replace the selection if there is none
    private fun replaceText(text: CharSequence?, newCursorPosition: Int, composing: Boolean) {
        beginBatchEdit()

        var start = getComposingStart(editable)
        var end = getComposingEnd(editable)

        if (end < start) {
            val temp = start
            start = end
            end = temp
        }

        if (start != -1 && end != -1) {
            removeComposingSpans(editable)
        } else {
            start = Selection.getSelectionStart(editable)
            end = Selection.getSelectionEnd(editable)
            if (start < 0) start = 0
            if (end < 0) end = 0
            if (end < start) {
                val temp = start
                start = end
                end = temp
            }
        }

        val composingText: CharSequence
        if (composing) {
            val spannable: Spannable?
            if (text !is Spannable) {
                spannable = SpannableStringBuilder(text)
                composingText = spannable
            } else {
                composingText = text
                spannable = text
            }

            setComposingSpans(spannable)
        } else {
            composingText = text!!
        }

        var cursorPosition = newCursorPosition +
                if (newCursorPosition > 0) {
                    end - 1
                } else {
                    start
                }

        if (cursorPosition < 0)
            cursorPosition = 0
        if (cursorPosition > editable.length)
            cursorPosition = editable.length

        Selection.setSelection(editable, cursorPosition)

        editable.replace(start, end, composingText)

        endBatchEdit()
    }

    override fun commitText(text: CharSequence?, newCursorPosition: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "commitText $text $newCursorPosition")

        replaceText(text, newCursorPosition, false)

        textChanged = true
        sendChanged()

        return true
    }

    override fun closeConnection() {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "closeConnection")

        finishComposingText()
    }

    override fun commitCompletion(text: CompletionInfo?): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "commitCompletion")

        // Nothing to do
        return false
    }

    override fun setComposingRegion(_start: Int, _end: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "setComposingRegion $_start $_end")

        var start = min(_start, _end)
        var end = max(_start, _end)

        val len = editable.length
        if (start < 0)
            start = 0
        if (end < 0)
            end = 0
        if (start > len)
            start = len
        if (end > len)
            end = len

        if (start != end)
            editable.setSpan(
                Composing,
                start,
                end,
                Spanned.SPAN_EXCLUSIVE_EXCLUSIVE or Spanned.SPAN_COMPOSING
            )

        sendChanged()
        return true
    }

    override fun performContextMenuAction(id: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "performContextMenuAction $id")

        if (id == android.R.id.selectAll) {
            Selection.selectAll(editable)
            sendChanged()
        }

        return false
    }

    override fun setSelection(start: Int, end: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "setSelection $start $end")

        val len = editable.length
        if (start < 0 || end < 0 || start > len || end > len)
            return true

        if (start == end && MetaKeyKeyListener.getMetaState(
                editable,
                MetaKeyKeyListener.META_SHIFT_ON
            ) != 0
        )
            Selection.extendSelection(editable, start)
        else
            Selection.setSelection(editable, start, end)

        sendChanged()
        return true
    }

    override fun requestCursorUpdates(cursorUpdateMode: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "requestCursorUpdates $cursorUpdateMode")

        // No position info to return to input method since we're not running in an android view
        return true
    }

    override fun getTextBeforeCursor(n: Int, flags: Int): CharSequence {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getTextBeforeCursor $n $flags")

        var start = Selection.getSelectionStart(editable)
        val end = Selection.getSelectionEnd(editable)

        if (end < start)
            start = end

        if (start <= 0)
            return ""

        val len = if (n > start) start else n

        val result = if (flags and InputConnection.GET_TEXT_WITH_STYLES != 0)
            editable.subSequence(start - len, start)
        else
            TextUtils.substring(editable, start - len, start)

        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getTextBeforeCursor result: '$result'")
        return result
    }

    override fun getSelectedText(flags: Int): CharSequence? {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getSelectedText $flags")

        var start = Selection.getSelectionStart(editable)
        var end = Selection.getSelectionEnd(editable)

        if (end < start) {
            val temp = start
            start = end
            end = temp
        }

        if (start == end || start < 0)
            return null

        val result = if (flags and InputConnection.GET_TEXT_WITH_STYLES != 0)
            editable.subSequence(start, end)
        else
            TextUtils.substring(editable, start, end)

        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getSelectedText result: '$result'")
        return result
    }

    override fun getTextAfterCursor(n: Int, flags: Int): CharSequence {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getTextAfterCursor $n $flags")

        val start = Selection.getSelectionStart(editable)
        var end = Selection.getSelectionEnd(editable)

        if (end < start)
            end = start

        if (end < 0)
            end = 0

        val len = if (end + n > editable.length) editable.length - end else n

        val result = if (flags and InputConnection.GET_TEXT_WITH_STYLES != 0)
            editable.subSequence(end, end + len)
        else
            TextUtils.substring(editable, end, end + len)

        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getTextAfterCursor result: '$result'")
        return result
    }

    override fun getHandler(): Handler? {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getHandler")

        // Nothing to return
        return null
    }

    override fun getExtractedText(request: ExtractedTextRequest?, flags: Int): ExtractedText? {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getExtractedText")

        // No support for extracted text
        return null
    }

    override fun beginBatchEdit(): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "beginBatchEdit")

        ++batchEditCount
        return true
    }

    override fun endBatchEdit(): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "endBatchEdit")

        if (batchEditCount > 0) {
            --batchEditCount
            if (batchEditCount == 0)
                sendChanged()
        }

        return true
    }

    override fun setComposingText(text: CharSequence?, newCursorPosition: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "setComposingText '$text' $newCursorPosition")

        replaceText(text, newCursorPosition, true)

        textChanged = true
        sendChanged()

        return true
    }

    override fun clearMetaKeyStates(states: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "clearMetaKeyStates $states")

        MetaKeyKeyListener.clearMetaKeyState(editable, states)
        return true
    }

    override fun reportFullscreenMode(enabled: Boolean): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "reportFullscreenMode $enabled")

        // Go to hell, we don't support full-screen mode
        return false
    }

    override fun getCursorCapsMode(reqModes: Int): Int {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "getCursorUpdateMode $reqModes")

        var start = Selection.getSelectionStart(editable)
        val end = Selection.getSelectionEnd(editable)

        if (start > end) {
            start = end
        }

        return TextUtils.getCapsMode(editable, start, reqModes)
    }

    override fun performPrivateCommand(action: String?, data: Bundle?): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "performPrivateCommand $action $data")

        // No private commands
        return false
    }

    private fun isPrintable(codePoint: Int): Boolean {
        val block = Character.UnicodeBlock.of(codePoint)
        return !Character.isISOControl(codePoint) && block != null && block !== Character.UnicodeBlock.SPECIALS
    }

    override fun sendKeyEvent(event: KeyEvent): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "sendKeyEvent $event")

        if (event.action == KeyEvent.ACTION_UP) {
            val codePoint = event.unicodeChar
            if (isPrintable(codePoint)) {
                if (UnityInterop.isDebugMode)
                    Log.i(LOG_TAG, "received alphanumeric character $codePoint in key event")

                replaceText(codePoint.toChar().toString(), 1, false)
                return true
            } else if (event.keyCode == KeyEvent.KEYCODE_DEL) {
                deleteSurroundingText(1, 0)
            }
        }

        return false
    }

    override fun finishComposingText(): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "finishComposingText")

        removeComposingSpans(editable)
        sendChanged()

        return true
    }

    override fun commitCorrection(correctionInfo: CorrectionInfo?): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "commitCorrection $correctionInfo")

        // Nothing to do, this is for UI animations and stuff
        return true
    }

    override fun commitContent(
        inputContentInfo: InputContentInfo,
        flags: Int,
        opts: Bundle?
    ): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "commitContent $inputContentInfo $flags $opts")

        // Can't accept any odd stuff such as GIFs
        return false
    }

    override fun performEditorAction(editorAction: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "performEditorAction $editorAction")

        if (multiline) {
            removeComposingSpans(editable)
            replaceText("\n", 1, false)
            imm.restartInput(targetView)

            textChanged = true
            sendChanged()
        } else
            eventReceiver.onAccept()

        return true
    }

    private val invalidIndex = -1
    private fun findIndexBackward(
        cs: CharSequence, from: Int,
        numCodePoints: Int
    ): Int {
        var currentIndex = from
        var waitingHighSurrogate = false
        val n = cs.length
        if (currentIndex < 0 || n < currentIndex) {
            return invalidIndex // The starting point is out of range.
        }
        if (numCodePoints < 0) {
            return invalidIndex // Basically this should not happen.
        }
        var remainingCodePoints = numCodePoints
        while (true) {
            if (remainingCodePoints == 0) {
                return currentIndex // Reached to the requested length in code points.
            }
            --currentIndex
            if (currentIndex < 0) {
                return if (waitingHighSurrogate) {
                    invalidIndex // An invalid surrogate pair is found.
                } else 0
                // Reached to the beginning of the text w/o any invalid surrogate pair.
            }
            val c = cs[currentIndex]
            if (waitingHighSurrogate) {
                if (!Character.isHighSurrogate(c)) {
                    return invalidIndex // An invalid surrogate pair is found.
                }
                waitingHighSurrogate = false
                --remainingCodePoints
                continue
            }
            if (!Character.isHighSurrogate(c) && !Character.isLowSurrogate(c)) {
                --remainingCodePoints
                continue
            }
            if (Character.isHighSurrogate(c)) {
                return invalidIndex // A invalid surrogate pair is found.
            }
            waitingHighSurrogate = true
        }
    }

    private fun findIndexForward(
        cs: CharSequence, from: Int,
        numCodePoints: Int
    ): Int {
        var currentIndex = from
        var waitingLowSurrogate = false
        val n = cs.length
        if (currentIndex < 0 || n < currentIndex) {
            return invalidIndex // The starting point is out of range.
        }
        if (numCodePoints < 0) {
            return invalidIndex // Basically this should not happen.
        }
        var remainingCodePoints = numCodePoints
        while (true) {
            if (remainingCodePoints == 0) {
                return currentIndex // Reached to the requested length in code points.
            }
            if (currentIndex >= n) {
                return if (waitingLowSurrogate) {
                    invalidIndex // An invalid surrogate pair is found.
                } else n
                // Reached to the end of the text w/o any invalid surrogate pair.
            }
            val c = cs[currentIndex]
            if (waitingLowSurrogate) {
                if (!Character.isLowSurrogate(c)) {
                    return invalidIndex // An invalid surrogate pair is found.
                }
                --remainingCodePoints
                waitingLowSurrogate = false
                ++currentIndex
                continue
            }
            if (!Character.isHighSurrogate(c) && !Character.isLowSurrogate(c)) {
                --remainingCodePoints
                ++currentIndex
                continue
            }
            if (Character.isLowSurrogate(c)) {
                return invalidIndex // A invalid surrogate pair is found.
            }
            waitingLowSurrogate = true
            ++currentIndex
        }
    }

    // Deleting should be straight-forward enough, I didn't take care to study the code
    override fun deleteSurroundingTextInCodePoints(beforeLength: Int, afterLength: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "deleteSurroundingTextInCodePoints $beforeLength $afterLength")

        beginBatchEdit()
        var a = Selection.getSelectionStart(editable)
        var b = Selection.getSelectionEnd(editable)
        if (a > b) {
            val tmp = a
            a = b
            b = tmp
        }
        // Ignore the composing text.
        var ca = BaseInputConnection.getComposingSpanStart(editable)
        var cb = BaseInputConnection.getComposingSpanEnd(editable)
        if (cb < ca) {
            val tmp = ca
            ca = cb
            cb = tmp
        }
        if (ca != -1 && cb != -1) {
            if (ca < a) a = ca
            if (cb > b) b = cb
        }
        if (a >= 0 && b >= 0) {
            val start = findIndexBackward(editable, a, beforeLength.coerceAtLeast(0))
            if (start != invalidIndex) {
                val end = findIndexForward(editable, b, afterLength.coerceAtLeast(0))
                if (end != invalidIndex) {
                    val numDeleteBefore = a - start
                    if (numDeleteBefore > 0) {
                        editable.delete(start, a)
                    }
                    val numDeleteAfter = end - b
                    if (numDeleteAfter > 0) {
                        editable.delete(b - numDeleteBefore, end - numDeleteBefore)
                    }
                }
            }
        }

        textChanged = true
        endBatchEdit()

        return true
    }

    // Deleting should be straight-forward enough, I didn't take care to study the code
    override fun deleteSurroundingText(beforeLength: Int, afterLength: Int): Boolean {
        if (UnityInterop.isDebugMode)
            Log.i(LOG_TAG, "deleteSurroundingText $beforeLength $afterLength")

        beginBatchEdit()

        var start = Selection.getSelectionStart(editable)
        var end = Selection.getSelectionEnd(editable)

        if (start > end) {
            val tmp = start
            start = end
            end = tmp
        }

        // Ignore the composing text.
        var cStart = BaseInputConnection.getComposingSpanStart(editable)
        var cEnd = BaseInputConnection.getComposingSpanEnd(editable)
        if (cEnd < cStart) {
            val tmp = cStart
            cStart = cEnd
            cEnd = tmp
        }
        if (cStart != -1 && cEnd != -1) {
            if (cStart < start) start = cStart
            if (cEnd > end) end = cEnd
        }

        var deleted = 0

        if (beforeLength > 0) {
            var dStart = start - beforeLength
            if (dStart < 0) dStart = 0
            editable.delete(dStart, start)
            deleted = start - dStart
        }

        if (afterLength > 0) {
            end -= deleted
            var dEnd = end + afterLength
            if (dEnd > editable.length)
                dEnd = editable.length
            editable.delete(end, dEnd)
        }

        textChanged = true
        endBatchEdit()

        return true
    }
}