package ir.onehand.nativeimm

import android.graphics.Rect
import android.graphics.drawable.ColorDrawable
import android.util.Log
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.view.WindowManager
import android.widget.LinearLayout
import android.widget.PopupWindow
import com.unity3d.player.UnityPlayer

class KeyboardHeightChangeTracker(
    parentGroup: ViewGroup,
    private val includeNotch: Boolean,
    private val onHeightChanged: (visibleHeight: Int) -> Unit
) :
    PopupWindow(UnityPlayer.currentActivity) {

    private val popupView: View

    init {
        popupView = LinearLayout(UnityPlayer.currentActivity)
        popupView.layoutParams =
            ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
        popupView.background = ColorDrawable(0)
        contentView = popupView
        softInputMode = WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE or
                WindowManager.LayoutParams.SOFT_INPUT_STATE_ALWAYS_VISIBLE
        inputMethodMode = INPUT_METHOD_NEEDED

        width = 0
        height = WindowManager.LayoutParams.MATCH_PARENT
        setBackgroundDrawable(ColorDrawable(0))
        showAtLocation(parentGroup, Gravity.NO_GRAVITY, 0, 0)
        popupView.viewTreeObserver.addOnGlobalLayoutListener {
            onGlobalLayout()
        }
    }

    fun destroy() = dismiss()

    private fun onGlobalLayout() {
        val rect = Rect()
        popupView.getWindowVisibleDisplayFrame(rect)
        onHeightChanged(if (includeNotch) rect.height() + rect.top else rect.height())
    }
}