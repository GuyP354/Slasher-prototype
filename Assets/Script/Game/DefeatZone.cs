using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class DefeatZone : MonoBehaviour
{
    private Health health;

    [Header("Lose UI")]
    [SerializeField] private GameObject looseGameCanvas;
    [SerializeField] private bool pauseOnDefeat = true;

    private bool isDefeated;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (looseGameCanvas == null)
        {
            GameObject found = GameObject.Find("Loose Game Canvas");
            if (found != null)
                looseGameCanvas = found;
        }

        if (looseGameCanvas != null)
            looseGameCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health != null)
            health.OnDeath += OnGateDestroy;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= OnGateDestroy;
    }

    private void OnTriggerEnter (Collider col)
    {
        Debug.Log(col.name);
        if(col.tag == "Enemy")
        {
            LevelManager.Instance.EnemyCrossed();
            //SpawnManager.Instance.DestroyEnemy(col.gameObject);
        }
    }

    public void OnGateDestroy()
    {
        if (isDefeated) return;
        isDefeated = true;

        if (looseGameCanvas != null)
            looseGameCanvas.SetActive(true);

        if (pauseOnDefeat)
            Time.timeScale = 0f;
    }
}
