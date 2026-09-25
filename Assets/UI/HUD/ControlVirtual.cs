using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

/// <summary>
/// Control táctil que escribe en un control de un mando virtual del Input System
/// (por ejemplo "&lt;Gamepad&gt;/leftStick"). Así el HUD de UI Toolkit mueve al jugador
/// a través de las mismas acciones que un mando real, sin tocar el código de la plantilla.
/// </summary>
public class ControlVirtual : OnScreenControl
{
    [InputControl]
    [SerializeField] string m_RutaControl = "<Gamepad>/leftStick";

    protected override string controlPathInternal
    {
        get => m_RutaControl;
        set => m_RutaControl = value;
    }

    public void EnviarVector(Vector2 valor) => SendValueToControl(valor);

    public void EnviarBoton(bool pulsado) => SendValueToControl(pulsado ? 1f : 0f);
}
