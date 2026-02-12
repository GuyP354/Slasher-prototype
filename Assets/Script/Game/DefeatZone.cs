using UnityEngine;

public class DefeatZone : MonoBehaviour
{
    private void OnTriggerEnter (Collider col)
    {
        Debug.Log(col.name);
        if(col.tag == "Enemy")
        {
            LevelManager.Instance.EnemyCrossed();
            //Destroy(col.gameObject);
        }
    }
}
