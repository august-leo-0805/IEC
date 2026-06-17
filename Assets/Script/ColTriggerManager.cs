using UnityEngine;

public class ColTriggerManager : MonoBehaviour
{
    public GameObject colTrigger;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            colTrigger.SetActive(false);
        }
    }
}