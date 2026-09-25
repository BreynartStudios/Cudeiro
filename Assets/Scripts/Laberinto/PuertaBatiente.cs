using UnityEngine;

/// <summary>
/// Puerta batiente estilo "cantina del oeste".
/// Se abre (alejándose de quien la empuja) cuando un CharacterController o un Rigidbody dinámico
/// entra en la zona de la puerta, y al quedar libre vuelve a cerrarse oscilando a un lado y a otro
/// hasta detenerse, como un muelle amortiguado.
///
/// El pivote del objeto debe estar en la bisagra y el eje de giro es su eje Y local.
/// La simulación es local en cada cliente (no necesita red): todos ven a los jugadores
/// sincronizados por la NetworkTransform, así que todas las puertas reaccionan igual.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PuertaBatiente : MonoBehaviour
{
    [Header("Apertura")]
    [Tooltip("Ángulo máximo de apertura en grados, hacia cualquiera de los dos lados.")]
    [Range(10f, 170f)] public float anguloMaximo = 100f;
    [Tooltip("Frecuencia (Hz) del muelle mientras alguien empuja la puerta. Más alto = se abre más rápido.")]
    public float frecuenciaApertura = 2.5f;
    [Tooltip("Amortiguación mientras se abre (1 = sin rebote).")]
    [Range(0f, 1.5f)] public float amortiguacionApertura = 0.8f;

    [Header("Cierre (vaivén)")]
    [Tooltip("Frecuencia (Hz) del vaivén al cerrarse. Más bajo = balanceo más lento.")]
    public float frecuenciaCierre = 0.9f;
    [Tooltip("Amortiguación del cierre. Valores bajos hacen que vaya y venga más veces.")]
    [Range(0f, 1f)] public float amortiguacionCierre = 0.14f;
    [Tooltip("Fracción de velocidad que conserva la puerta al rebotar contra alguien.")]
    [Range(0f, 1f)] public float rebote = 0.3f;

    [Header("Detección")]
    [Tooltip("Distancia (metros) a cada lado de la puerta a la que empieza a abrirse.")]
    public float distanciaDeteccion = 0.8f;
    public LayerMask capasDetectables = ~0;

    const float k_AnguloReposo = 0.05f;
    const float k_VelocidadReposo = 0.5f;

    static readonly Collider[] s_Resultados = new Collider[16];

    Rigidbody m_Rigidbody;
    BoxCollider m_Hoja;
    Quaternion m_RotacionLocalCerrada;
    Vector3 m_CentroZona;
    Vector3 m_MitadZona;
    Quaternion m_RotacionZona;
    Vector3 m_MitadHoja;
    float m_SignoHoja;

    float m_Angulo;
    float m_Velocidad;
    float m_Objetivo;
    bool m_Ocupada;
    bool m_EnReposo = true;

    void Awake()
    {
        // Por si quedaba la configuración antigua con HingeJoint: la puerta se mueve por script.
        if (TryGetComponent(out HingeJoint bisagra)) Destroy(bisagra);

        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.isKinematic = true;
        m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        foreach (var box in GetComponents<BoxCollider>())
        {
            if (!box.isTrigger) { m_Hoja = box; break; }
        }
        if (m_Hoja == null)
        {
            Debug.LogWarning($"{name}: PuertaBatiente necesita un BoxCollider sólido.", this);
            enabled = false;
            return;
        }

        m_RotacionLocalCerrada = transform.localRotation;
        m_SignoHoja = Mathf.Sign(m_Hoja.center.x);

        // Zona de detección fija (no gira con la puerta): la hoja cerrada ensanchada hacia ambos lados.
        Vector3 escala = Abs(transform.lossyScale);
        m_MitadHoja = Vector3.Scale(m_Hoja.size * 0.5f, escala);
        m_MitadZona = m_MitadHoja + new Vector3(0f, 0f, distanciaDeteccion);
        m_CentroZona = transform.TransformPoint(m_Hoja.center);
        m_RotacionZona = transform.rotation;
    }

    void FixedUpdate()
    {
        DetectarOcupacion();

        if (m_EnReposo && !m_Ocupada) return;
        m_EnReposo = false;

        float frecuencia = m_Ocupada ? frecuenciaApertura : frecuenciaCierre;
        float amortiguacion = m_Ocupada ? amortiguacionApertura : amortiguacionCierre;
        float w = 2f * Mathf.PI * frecuencia;
        float dt = Time.fixedDeltaTime;

        // Muelle amortiguado (Euler semi-implícito).
        float aceleracion = w * w * (m_Objetivo - m_Angulo) - 2f * amortiguacion * w * m_Velocidad;
        float velocidad = m_Velocidad + aceleracion * dt;
        float angulo = Mathf.Clamp(m_Angulo + velocidad * dt, -anguloMaximo, anguloMaximo);

        if (HojaChocaria(angulo))
        {
            // Golpea a alguien que está en su recorrido: rebota suavemente en lugar de atravesarlo.
            m_Velocidad = -m_Velocidad * rebote;
            return;
        }

        if (Mathf.Abs(angulo) >= anguloMaximo) velocidad = 0f;
        m_Angulo = angulo;
        m_Velocidad = velocidad;

        if (!m_Ocupada && Mathf.Abs(m_Angulo) < k_AnguloReposo && Mathf.Abs(m_Velocidad) < k_VelocidadReposo)
        {
            m_Angulo = 0f;
            m_Velocidad = 0f;
            m_EnReposo = true;
        }

        m_Rigidbody.MoveRotation(RotacionMundo(m_Angulo));
    }

    void DetectarOcupacion()
    {
        int n = Physics.OverlapBoxNonAlloc(m_CentroZona, m_MitadZona, s_Resultados, m_RotacionZona,
            capasDetectables, QueryTriggerInteraction.Ignore);

        bool ocupadaAntes = m_Ocupada;
        m_Ocupada = false;

        for (int i = 0; i < n; i++)
        {
            if (!EsEmpujador(s_Resultados[i])) continue;

            if (!ocupadaAntes)
            {
                // Se abre hacia el lado contrario al que llega el cuerpo.
                Vector3 local = Quaternion.Inverse(m_RotacionZona) * (s_Resultados[i].bounds.center - m_CentroZona);
                float lado = local.z >= 0f ? 1f : -1f;
                m_Objetivo = lado * m_SignoHoja * anguloMaximo;
            }
            m_Ocupada = true;
            return;
        }

        m_Objetivo = 0f;
    }

    bool HojaChocaria(float angulo)
    {
        Quaternion rotacion = RotacionMundo(angulo);
        Vector3 centro = transform.position + rotacion * Vector3.Scale(m_Hoja.center, Abs(transform.lossyScale));
        int n = Physics.OverlapBoxNonAlloc(centro, m_MitadHoja * 0.95f, s_Resultados, rotacion,
            capasDetectables, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            if (EsEmpujador(s_Resultados[i])) return true;
        }
        return false;
    }

    bool EsEmpujador(Collider c)
    {
        if (c.transform.IsChildOf(transform)) return false;
        if (c is CharacterController) return true;
        Rigidbody rb = c.attachedRigidbody;
        return rb != null && !rb.isKinematic;
    }

    Quaternion RotacionMundo(float angulo)
    {
        Quaternion padre = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        return padre * m_RotacionLocalCerrada * Quaternion.AngleAxis(angulo, Vector3.up);
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Vector3 escala = Abs(transform.lossyScale);
            m_MitadZona = Vector3.Scale(box.size * 0.5f, escala) + new Vector3(0f, 0f, distanciaDeteccion);
            m_CentroZona = transform.TransformPoint(box.center);
            m_RotacionZona = transform.rotation;
        }
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.matrix = Matrix4x4.TRS(m_CentroZona, m_RotacionZona, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, m_MitadZona * 2f);
    }
}
