package ir.onehand.nativeimm

interface UnityInputConnectionEventReceiver {
    fun onEdit(contents: EditableTextInfo)
    fun onAccept()
}