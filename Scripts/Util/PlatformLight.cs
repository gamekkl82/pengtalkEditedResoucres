using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlatformLight : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
//        Light light = GetComponent<Light>();

//#if UNITY_ANDROID
//        light.intensity = 2f;
//        light.color = Color.white;
//#elif UNITY_IOS
//        light.intensity = 1.16f;
//        light.color = Color.white;
//#elif UNITY_STANDALONE_WIN
//        light.intensity = 1.16f;
//        light.color = Color.white;
//#endif
        //StartCoroutine(callSceneName(gameObject.name));
    }

    IEnumerator callSceneName (string _lightName)
    {
        Debug.Log(_lightName);

        yield return new WaitForSeconds(1);
        StartCoroutine(callSceneName(_lightName));
    }
}
