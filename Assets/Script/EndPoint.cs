using UnityEngine;

public class EndPoint : MonoBehaviour
{
    public bool isEndPointReached = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isEndPointReached = true;
            Debug.Log("End Point Reached!");
        }
    }
}
