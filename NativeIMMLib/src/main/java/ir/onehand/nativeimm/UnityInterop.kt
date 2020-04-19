@file:Suppress("unused")

package ir.onehand.nativeimm

import android.annotation.SuppressLint
import android.view.View
import android.view.ViewGroup
import com.unity3d.player.UnityPlayer
import org.json.JSONException
import org.json.JSONObject
import java.util.*

@Suppress("SameParameterValue")
@SuppressLint("StaticFieldLeak")
class UnityInterop {
    enum class ErrorCode(val code: String) {
        ExceptionInPluginMessage("EXCEPTION")
    }

    enum class MessageName(val messageName: String) {
        VisibleHeightChanged("KB_HEIGHT_CHANGED"),
        InitDone("INIT_DONE"),
        ViewCreated("VIEW_CREATED"),
        EnterPressed("ENTER_PRESSED"),
        TextChanged("TEXT_CHANGED")
    }

    companion object {
        private const val gameObjectName = "NativeIMM"
        private const val dataMethodName = "ReceiveData"
        private const val errorMethodName = "ReceiveError"

        private var viewGroup: ViewGroup? = null

        private var heightChangeTracker: KeyboardHeightChangeTracker? = null
        private var immManager: IMMManager? = null

        private var debugMode = false
        val isDebugMode get() = debugMode

        // interop method
        @JvmStatic
        fun initialize(includeNotchInHeight: Boolean) =
            initInternal(includeNotchInHeight)

        private fun initInternal(includeNotchInHeight: Boolean): Boolean {
            if (immManager != null)
                return true

            executePluginFunction {
                val activity = UnityPlayer.currentActivity
                if (viewGroup == null) {
                    val rootView = activity.findViewById<ViewGroup>(android.R.id.content)
                    viewGroup =
                        getFirstNonGroupView(rootView)!!.parent as ViewGroup
                }

                immManager = IMMManager(viewGroup!!)

                heightChangeTracker =
                    KeyboardHeightChangeTracker(
                        viewGroup!!,
                        includeNotchInHeight
                    ) { visible ->
                        val args = JSONObject()
                        args.put("visibleHeight", visible)
                        sendMessage(
                            MessageName.VisibleHeightChanged,
                            args
                        )
                    }

                sendMessage(
                    MessageName.InitDone,
                    JSONObject()
                )
            }

            return false
        }

        // interop method
        @JvmStatic
        fun destroy() {
            val imm = immManager ?: return

            executePluginFunction {
                heightChangeTracker!!.destroy()
                heightChangeTracker = null

                imm.destroy()
                immManager = null
            }
        }

        // interop method
        @JvmStatic
        fun getIMMManager() = immManager

        // interop method
        @JvmStatic
        fun setDebugModeEnabled(enabled: Boolean) {
            debugMode = enabled
        }

        private fun getFirstNonGroupView(view: View): View? {
            if (view is ViewGroup) {
                for (i in 0 until view.childCount) {
                    val result =
                        getFirstNonGroupView(view.getChildAt(i))
                    if (result != null) {
                        return result
                    }
                }
                return null
            } else {
                return view
            }
        }

        fun sendMessage(name: MessageName, args: JSONObject) {
            try {
                val messageObj = JSONObject()
                messageObj.put("name", name.messageName)
                messageObj.put("args", args)
                val dataString = messageObj.toString(0)
                UnityPlayer.UnitySendMessage(
                    gameObjectName,
                    dataMethodName, dataString
                )
            } catch (e: JSONException) {
                e.printStackTrace()
            }
        }

        private fun sendError(code: ErrorCode, message: String, data: JSONObject) {
            try {
                val errorObj = JSONObject()
                errorObj.put("code", code.code)
                errorObj.put("message", message)
                errorObj.put("data", data)
                val dataString = errorObj.toString(0)
                UnityPlayer.UnitySendMessage(
                    gameObjectName,
                    errorMethodName, dataString
                )
            } catch (e: JSONException) {
                e.printStackTrace()
            }
        }

        fun executePluginFunction(func: () -> Unit) {
            try {
                UnityPlayer.currentActivity.runOnUiThread {
                    func()
                }
            } catch (e: Exception) {
                val stackTrace = Arrays.toString(e.stackTrace)
                val message = "$e at: $stackTrace"
                sendError(
                    ErrorCode.ExceptionInPluginMessage,
                    message,
                    JSONObject()
                )
            }
        }
    }
}