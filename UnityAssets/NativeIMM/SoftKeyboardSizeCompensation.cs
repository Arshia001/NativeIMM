using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftKeyboardSizeCompensation : MonoBehaviour
{
    RectTransform rectTransform, referenceTransform;

    void Start()
    {
        rectTransform = transform as RectTransform;
        referenceTransform = rectTransform.parent as RectTransform;

        rectTransform.pivot = new Vector2(0.5f, 1.0f);

        NativeImm.Instance.VisibleHeightChanged += UpdateScreenHeight;
    }

    void OnDestroy()
    {
        NativeImm.Instance.VisibleHeightChanged -= UpdateScreenHeight;
    }

    private void UpdateScreenHeight(float ratio)
    {
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, referenceTransform.rect.height * ratio);
    }
}
