using UnityEngine;
using UnityEngine.UI;

public class CameraFeed : MonoBehaviour
{
    public RawImage rawImage;
    private WebCamTexture webCamTexture;

    void Start()
    {
        webCamTexture = new WebCamTexture();
        rawImage.texture = webCamTexture;
        webCamTexture.Play();
    }
}
