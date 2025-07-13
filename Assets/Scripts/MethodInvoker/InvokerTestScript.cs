using UnityEngine;

public class InvokerTestScript : MonoBehaviour
{
    private void TestOne()
    {
        print("Private");
    }
    private void TestTwo()
    {
        print("Private");
    }
    public void TestThree(string value)
    {
        print(value);
    }
}
