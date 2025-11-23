using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class XRTeleportManager : MonoBehaviour
{
    [Header("XR Origin")]
    public XROrigin xrOrigin;

    [System.Serializable]
    public class TeleportRule
    {
        public string ruleName;

        [Header("Input")]
        public InputActionReference triggerAction;

        [Header("Destination (Teleport To)")]
        public Transform toPoint;
    }

    [Header("Teleport Rules")]
    public List<TeleportRule> teleportRules = new List<TeleportRule>();

    private void OnEnable()
    {
        foreach (var rule in teleportRules)
        {
            if (rule?.triggerAction?.action != null)
                rule.triggerAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        foreach (var rule in teleportRules)
        {
            if (rule?.triggerAction?.action != null)
                rule.triggerAction.action.Disable();
        }
    }

    private void Update()
    {
        foreach (var rule in teleportRules)
        {
            if (rule?.triggerAction?.action == null || rule.toPoint == null)
                continue;

            // 버튼이 눌리면
            if (rule.triggerAction.action.triggered)
            {
                TeleportTo(rule.toPoint);
            }
        }
    }

    private void TeleportTo(Transform target)
    {
        if (xrOrigin == null || target == null)
            return;

        // 1. 먼저 Origin의 회전을 목표의 Y축과 맞춰준다
        var originTransform = xrOrigin.transform;
        Vector3 euler = originTransform.eulerAngles;
        euler.y = target.eulerAngles.y;
        originTransform.eulerAngles = euler;

        // 2. 그 다음, 카메라가 target.position에 오도록 이동
        xrOrigin.MoveCameraToWorldLocation(target.position);

        Debug.Log($"Teleport → {target.name}");
    }

}
