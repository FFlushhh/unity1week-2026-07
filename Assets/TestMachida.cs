using UnityEngine;

public class TestMachida : MonoBehaviour
{
    const int age = 5;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TestFunc();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void TestFunc()
    {
        for (int i = 0; i < age; ++i)
        {
            Debug.Log("" + i);
        }
    }
}
