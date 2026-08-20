using Proto.Core;
using UnityEngine;

namespace Proto.Input
{
    /// <summary>
    ///     入力設定を管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "ProtoInputSettings", menuName = "Proto/Input Settings")]
    public class ProtoInputSettings : ScriptableObject
    {
        [Header("Joystick Settings")] [SerializeField]
        private float joystickDeadZone = ProtoConstants.Input.DefaultDeadZone;

        [SerializeField] private float joystickMaxDistance = ProtoConstants.Input.DefaultMaxDistance;

        [Header("Mouse Settings")] [SerializeField]
        private float mouseSensitivity = ProtoConstants.Input.DefaultMouseSensitivity;

        // Properties
        public float JoystickDeadZone => joystickDeadZone;
        public float JoystickMaxDistance => joystickMaxDistance;
        public float MouseSensitivity => mouseSensitivity;

        /// <summary>
        ///     設定をデフォルト値にリセット
        /// </summary>
        public void ResetToDefaults()
        {
            joystickDeadZone = ProtoConstants.Input.DefaultDeadZone;
            joystickMaxDistance = ProtoConstants.Input.DefaultMaxDistance;
            mouseSensitivity = ProtoConstants.Input.DefaultMouseSensitivity;
        }

        /// <summary>
        ///     設定値を検証
        /// </summary>
        public void ValidateSettings()
        {
            joystickDeadZone = Mathf.Clamp01(joystickDeadZone);
            joystickMaxDistance = Mathf.Max(0.1f, joystickMaxDistance);
            mouseSensitivity = Mathf.Max(0.1f, mouseSensitivity);
        }

        /// <summary>
        ///     他の設定から値をコピー
        /// </summary>
        public void CopyFrom(ProtoInputSettings other)
        {
            if (other == null) return;

            joystickDeadZone = other.joystickDeadZone;
            joystickMaxDistance = other.joystickMaxDistance;
            mouseSensitivity = other.mouseSensitivity;
        }
    }
}