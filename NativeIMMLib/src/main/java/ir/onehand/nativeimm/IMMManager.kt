@file:Suppress("unused")

package ir.onehand.nativeimm

import android.annotation.SuppressLint
import android.view.ViewGroup
import android.widget.RelativeLayout
import com.unity3d.player.UnityPlayer

class IMMManager(private val parentGroup: ViewGroup) {
    private val layout = RelativeLayout(UnityPlayer.currentActivity)

    @SuppressLint("UseSparseArrays")
    private val views = HashMap<Int, InputReceiverView>()
    private var nextID = 0
    private val lock = Object()

    val relativeLayout: RelativeLayout
        get() = layout

    init {
        parentGroup.addView(
            layout,
            ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
        )
    }

    // interop method
    fun createInputReceiver(params: InputReceiverParams): Int {
        val id: Int
        synchronized(lock) {
            id = ++nextID
        }

        UnityInterop.executePluginFunction {
            val view = InputReceiverView(this, id, params)
            views[id] = view
        }

        return id
    }

    // interop method
    fun getView(id: Int) = views[id] ?: throw Exception("Unknown view ID $id")

    fun getFocusedView() = views.values.firstOrNull { it.isFocused }

    fun destroy() {
        for (view in views.values)
            view.destroy()

        parentGroup.removeView(layout)
    }

    fun onViewDestroyed(view: InputReceiverView) = views.remove(view.id)
}