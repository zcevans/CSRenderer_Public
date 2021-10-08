using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowFinalFrameBuffer : MonoBehaviour
{

    // Update is called once per frame
    public void OnUpdate(RenderTexture rt)
    {
        this.GetComponent<RawImage>().texture = rt;
    }
}
