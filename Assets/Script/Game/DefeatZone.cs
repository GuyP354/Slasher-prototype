using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class DefeatZone : MonoBehaviour
{
    private Health health;

    [Header("Lose UI")]
    [SerializeField] private GameObject loseGameCanvas;
    [SerializeField] private bool pauseOnDefeat = true;

    private bool isDefeated;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (loseGameCanvas == null)
        {
            GameObject found = GameObject.Find("Lose Game Canvas");
            if (found != null)
                loseGameCanvas = found;
        }

        if (loseGameCanvas != null)
            loseGameCanvas.SetActive(false);
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

        if (loseGameCanvas != null)
            loseGameCanvas.SetActive(true);

        if (pauseOnDefeat)
            Time.timeScale = 0f;
    }
}
