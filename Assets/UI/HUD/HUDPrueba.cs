using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// HUD de una prueba: cartel de fase, progreso por etapas, cronómetro,
/// joystick táctil con botón de sprint y cartel de objetivo.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HUDPrueba : MonoBehaviour
{
    public enum ModoJoystick { Automatico, Siempre, Nunca }

    [Header("Prueba")]
    [Min(1)] public int numeroFase = 1;
    public string nombrePrueba = "EL PANTANO";
    public string textoObjetivo = "LLEGA A LA";
    public string textoObjetivoDestacado = "META";

    [Header("Progreso")]
    [Min(1)] public int totalEtapas = 5;
    [Min(0)] public int etapaActual;

    [Header("Cronómetro")]
    public bool iniciarAlComenzar = true;

    [Header("Controles táctiles")]
    [Tooltip("Automático: solo se muestra en dispositivos con pantalla táctil.")]
    public ModoJoystick mostrarJoystick = ModoJoystick.Automatico;
    public ControlVirtual joystickMovimiento;
    public ControlVirtual botonSprint;

    [Header("HUD de la plantilla")]
    [Tooltip("Oculta las barras de vida y estamina del HUD del jugador (CoreHUD).")]
    public bool ocultarBarrasPlantilla = true;

    const string k_ClasePunto = "progreso__punto";
    const string k_ClasePuntoSuperado = "progreso__punto--superado";
    const string k_ClasePuntoActual = "progreso__punto--actual";
    const string k_ClasePuntoBorde = "progreso__punto-borde";
    const string k_ClaseJoystickOculto = "joystick--oculto";
    const string k_ClaseSprintPulsado = "joystick__sprint--pulsado";
    const string k_BarrasPlantilla = "health-info-container";
    const float k_IntervaloBusquedaBarras = 0.5f;

    Label m_FaseLabel;
    Label m_PruebaLabel;
    Label m_TiempoLabel;
    Label m_CentesimasLabel;
    Label m_ObjetivoLabel;
    Label m_ObjetivoDestacadoLabel;
    VisualElement m_Puntos;
    VisualElement m_Joystick;
    VisualElement m_JoystickBase;
    VisualElement m_JoystickMando;
    VisualElement m_SprintBoton;

    float m_Tiempo;
    bool m_Corriendo;
    int m_CentesimasMostradas = -1;
    int m_PunteroJoystick = -1;
    float m_SiguienteBusquedaBarras;

    public float Tiempo => m_Tiempo;

    void OnEnable()
    {
        var raiz = GetComponent<UIDocument>().rootVisualElement;
        if (raiz == null) return;
        m_FaseLabel = raiz.Q<Label>("faseLabel");
        m_PruebaLabel = raiz.Q<Label>("pruebaLabel");
        m_TiempoLabel = raiz.Q<Label>("tiempoLabel");
        m_CentesimasLabel = raiz.Q<Label>("centesimasLabel");
        m_ObjetivoLabel = raiz.Q<Label>("objetivoLabel");
        m_ObjetivoDestacadoLabel = raiz.Q<Label>("objetivoDestacadoLabel");
        m_Puntos = raiz.Q("progresoPuntos");
        if (m_Puntos == null)
        {
            Debug.LogError("HUDPrueba: el UIDocument no usa HUDPrueba.uxml.", this);
            return;
        }
        m_Joystick = raiz.Q("joystick");
        m_JoystickBase = raiz.Q("joystickBase");
        m_JoystickMando = raiz.Q("joystickMando");
        m_SprintBoton = raiz.Q("sprintBoton");

        m_JoystickBase.RegisterCallback<PointerDownEvent>(AlPulsarJoystick);
        m_JoystickBase.RegisterCallback<PointerMoveEvent>(AlMoverJoystick);
        m_JoystickBase.RegisterCallback<PointerUpEvent>(AlSoltarJoystick);
        m_JoystickBase.RegisterCallback<PointerCaptureOutEvent>(AlPerderJoystick);
        m_SprintBoton.RegisterCallback<PointerDownEvent>(AlPulsarSprint);
        m_SprintBoton.RegisterCallback<PointerUpEvent>(AlSoltarSprint);
        m_SprintBoton.RegisterCallback<PointerCaptureOutEvent>(AlPerderSprint);

        Refrescar();
        ActualizarJoystickVisible();
    }

    void OnDisable()
    {
        m_JoystickBase?.UnregisterCallback<PointerDownEvent>(AlPulsarJoystick);
        m_JoystickBase?.UnregisterCallback<PointerMoveEvent>(AlMoverJoystick);
        m_JoystickBase?.UnregisterCallback<PointerUpEvent>(AlSoltarJoystick);
        m_JoystickBase?.UnregisterCallback<PointerCaptureOutEvent>(AlPerderJoystick);
        m_SprintBoton?.UnregisterCallback<PointerDownEvent>(AlPulsarSprint);
        m_SprintBoton?.UnregisterCallback<PointerUpEvent>(AlSoltarSprint);
        m_SprintBoton?.UnregisterCallback<PointerCaptureOutEvent>(AlPerderSprint);
    }

    void Start()
    {
        if (m_Puntos == null) return;
        if (iniciarAlComenzar) IniciarCronometro();
        MostrarTiempo();
    }

    void Update()
    {
        if (ocultarBarrasPlantilla && Time.unscaledTime >= m_SiguienteBusquedaBarras)
        {
            m_SiguienteBusquedaBarras = Time.unscaledTime + k_IntervaloBusquedaBarras;
            OcultarBarrasPlantilla();
        }

        if (!m_Corriendo || m_TiempoLabel == null) return;
        m_Tiempo += Time.deltaTime;
        MostrarTiempo();
    }

    void OnValidate()
    {
        etapaActual = Mathf.Clamp(etapaActual, 0, totalEtapas - 1);
        if (m_Puntos == null) return;
        Refrescar();
        ActualizarJoystickVisible();
    }

    // ---------- API pública ----------

    public void IniciarCronometro() => m_Corriendo = true;

    public void DetenerCronometro() => m_Corriendo = false;

    public void ReiniciarCronometro()
    {
        m_Tiempo = 0f;
        m_CentesimasMostradas = -1;
        MostrarTiempo();
    }

    public void ConfigurarPrueba(int fase, string nombre, int etapas)
    {
        numeroFase = fase;
        nombrePrueba = nombre;
        totalEtapas = Mathf.Max(1, etapas);
        etapaActual = 0;
        Refrescar();
    }

    public void EstablecerEtapa(int etapa)
    {
        etapaActual = Mathf.Clamp(etapa, 0, totalEtapas - 1);
        ConstruirProgreso();
    }

    public void EstablecerObjetivo(string texto, string destacado)
    {
        textoObjetivo = texto;
        textoObjetivoDestacado = destacado;
        Refrescar();
    }

    // ---------- Presentación ----------

    void Refrescar()
    {
        m_FaseLabel.text = $"FASE {numeroFase}";
        m_PruebaLabel.text = nombrePrueba;
        m_ObjetivoLabel.text = textoObjetivo;
        m_ObjetivoDestacadoLabel.text = textoObjetivoDestacado;
        ConstruirProgreso();
    }

    void ConstruirProgreso()
    {
        m_Puntos.Clear();
        for (int i = 0; i < totalEtapas; i++)
        {
            var punto = new VisualElement { pickingMode = PickingMode.Ignore };
            punto.AddToClassList(k_ClasePunto);
            if (i < etapaActual) punto.AddToClassList(k_ClasePuntoSuperado);
            if (i == etapaActual)
            {
                punto.AddToClassList(k_ClasePuntoActual);
                var borde = new VisualElement { pickingMode = PickingMode.Ignore };
                borde.AddToClassList(k_ClasePuntoBorde);
                punto.Add(borde);
            }
            m_Puntos.Add(punto);
        }
    }

    void MostrarTiempo()
    {
        int centesimasTotales = Mathf.FloorToInt(m_Tiempo * 100f);
        if (centesimasTotales == m_CentesimasMostradas) return;
        m_CentesimasMostradas = centesimasTotales;

        int minutos = centesimasTotales / 6000;
        int segundos = centesimasTotales / 100 % 60;
        m_TiempoLabel.text = $"{minutos:00}:{segundos:00}";
        m_CentesimasLabel.text = $".{centesimasTotales % 100:00}";
    }

    void ActualizarJoystickVisible()
    {
        bool visible = mostrarJoystick == ModoJoystick.Siempre ||
                       (mostrarJoystick == ModoJoystick.Automatico &&
                        (Application.isMobilePlatform || Touchscreen.current != null));

        m_Joystick.EnableInClassList(k_ClaseJoystickOculto, !visible);
        // Sin joystick visible no hace falta el mando virtual.
        if (joystickMovimiento != null) joystickMovimiento.enabled = visible;
        if (botonSprint != null) botonSprint.enabled = visible;
    }

    // El HUD del jugador llega con él al conectarse (y al reaparecer), así que se busca periódicamente.
    void OcultarBarrasPlantilla()
    {
        var documentos = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var documento in documentos)
        {
            var barras = documento.rootVisualElement?.Q(k_BarrasPlantilla);
            if (barras != null) barras.style.display = DisplayStyle.None;
        }
    }

    // ---------- Joystick ----------

    void AlPulsarJoystick(PointerDownEvent e)
    {
        if (m_PunteroJoystick != -1) return;
        m_PunteroJoystick = e.pointerId;
        m_JoystickBase.CapturePointer(e.pointerId);
        MoverMando(e.localPosition);
    }

    void AlMoverJoystick(PointerMoveEvent e)
    {
        if (e.pointerId != m_PunteroJoystick) return;
        MoverMando(e.localPosition);
    }

    void AlSoltarJoystick(PointerUpEvent e)
    {
        if (e.pointerId != m_PunteroJoystick) return;
        m_JoystickBase.ReleasePointer(e.pointerId);
    }

    void AlPerderJoystick(PointerCaptureOutEvent e)
    {
        m_PunteroJoystick = -1;
        m_JoystickMando.style.translate = new Translate(0, 0);
        if (joystickMovimiento != null) joystickMovimiento.EnviarVector(Vector2.zero);
    }

    void MoverMando(Vector2 posicionLocal)
    {
        Rect r = m_JoystickBase.contentRect;
        float radio = Mathf.Min(r.width, r.height) * 0.5f;
        Vector2 desplazamiento = Vector2.ClampMagnitude(posicionLocal - r.center, radio);

        m_JoystickMando.style.translate = new Translate(desplazamiento.x, desplazamiento.y);

        // En UI Toolkit la Y crece hacia abajo; en el stick, hacia arriba.
        Vector2 valor = new Vector2(desplazamiento.x, -desplazamiento.y) / radio;
        if (joystickMovimiento != null) joystickMovimiento.EnviarVector(valor);
    }

    // ---------- Sprint ----------

    void AlPulsarSprint(PointerDownEvent e)
    {
        m_SprintBoton.CapturePointer(e.pointerId);
        m_SprintBoton.AddToClassList(k_ClaseSprintPulsado);
        if (botonSprint != null) botonSprint.EnviarBoton(true);
    }

    void AlSoltarSprint(PointerUpEvent e) => m_SprintBoton.ReleasePointer(e.pointerId);

    void AlPerderSprint(PointerCaptureOutEvent e)
    {
        m_SprintBoton.RemoveFromClassList(k_ClaseSprintPulsado);
        if (botonSprint != null) botonSprint.EnviarBoton(false);
    }
}
