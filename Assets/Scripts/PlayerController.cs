using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float m_speed = 4.0f;
    [SerializeField] float m_jumpForce = 7.5f;
    [SerializeField] float m_rollForce = 6.0f;
    [SerializeField] bool m_noBlood = false;
    [SerializeField] GameObject m_slideDust;

    private Animator m_animator;
    private Rigidbody2D m_body2d;
    private Sensor_HeroKnight m_groundSensor;
    private Sensor_HeroKnight m_wallSensorR1;
    private Sensor_HeroKnight m_wallSensorR2;
    private Sensor_HeroKnight m_wallSensorL1;
    private Sensor_HeroKnight m_wallSensorL2;
    private bool m_isWallSliding = false;
    private bool m_grounded = false;
    private bool m_rolling = false;
    private int m_facingDirection = 1;
    private int m_currentAttack = 0;
    private float m_timeSinceAttack = 0.0f;
    private float m_delayToIdle = 0.0f;
    private float m_rollDuration = 8.0f / 14.0f;
    private float m_rollCurrentTime;

    // Nueva variable para la vida del héroe
    private int m_health = 100;

    void Start()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();
    }

    void Update()
    {
        // Aumentar el temporizador que controla el combo de ataque
        m_timeSinceAttack += Time.deltaTime;

        // Aumentar el temporizador que verifica la duración del rodado
        if (m_rolling)
            m_rollCurrentTime += Time.deltaTime;

        // Deshabilitar rodado si el temporizador excede la duración
        if (m_rollCurrentTime > m_rollDuration)
            m_rolling = false;

        // Verificar si el personaje acaba de aterrizar en el suelo
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
        }

        // Verificar si el personaje acaba de comenzar a caer
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        // -- Manejar entrada y movimiento --
        float inputX = Input.GetAxis("Horizontal");

        // Cambiar la dirección del sprite según la dirección de movimiento
        if (inputX > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            m_facingDirection = 1;
        }
        else if (inputX < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            m_facingDirection = -1;
        }

        // Movimiento
        if (!m_rolling && m_timeSinceAttack > 0.25f) // Evitar movimiento durante el ataque
            m_body2d.velocity = new Vector2(inputX * m_speed, m_body2d.velocity.y);

        // Establecer AirSpeed en el animador
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        // -- Manejar Animaciones --
        // Deslizamiento por la pared
        m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State());
        m_animator.SetBool("WallSlide", m_isWallSliding);

        // Muerte
        // Si la vida llega a 0, ejecutar animación de muerte
        if (m_health <= 0)
        {
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");
        }

        // Ataque
        else if (Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f && !m_rolling)
        {
            m_currentAttack++;

            // Volver al ataque uno después del tercer ataque
            if (m_currentAttack > 3)
                m_currentAttack = 1;

            // Restablecer el combo de ataque si el tiempo desde el último ataque es demasiado grande
            if (m_timeSinceAttack > 1.0f)
                m_currentAttack = 1;

            // Llamar a una de las tres animaciones de ataque "Attack1", "Attack2", "Attack3"
            m_animator.SetTrigger("Attack" + m_currentAttack);

            // Restablecer temporizador
            m_timeSinceAttack = 0.0f;
        }

        // Bloquear
        else if (Input.GetMouseButtonDown(1) && !m_rolling)
        {
            m_animator.SetTrigger("Block");
            m_animator.SetBool("IdleBlock", true);
        }
        else if (Input.GetMouseButtonUp(1))
            m_animator.SetBool("IdleBlock", false);

        // Rodar
        else if (Input.GetKeyDown("left shift") && !m_rolling && !m_isWallSliding)
        {
            m_rolling = true;
            m_animator.SetTrigger("Roll");
            m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);
        }

        // Saltar
        else if (Input.GetKeyDown("space") && m_grounded && !m_rolling)
        {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
        }

        // Correr
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
        {
            // Restablecer temporizador
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
        }

        // Idle
        else
        {
            // Previene transiciones parpadeantes a idle
            m_delayToIdle -= Time.deltaTime;
            if (m_delayToIdle < 0)
                m_animator.SetInteger("AnimState", 0);
        }
    }

    // Método para detectar colisiones
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Mostrar en la consola con qué objeto está chocando
        Debug.Log("Chocando con: " + collision.gameObject.name);
    }

    // Eventos de animación
    // Llamado en la animación de deslizamiento.
    void AE_SlideDust()
    {
        Vector3 spawnPosition;

        if (m_facingDirection == 1)
            spawnPosition = m_wallSensorR2.transform.position;
        else
            spawnPosition = m_wallSensorL2.transform.position;

        if (m_slideDust != null)
        {
            // Establecer la posición de aparición correcta del polvo
            GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
            // Girar polvo en la dirección correcta
            dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
        }
    }
}
