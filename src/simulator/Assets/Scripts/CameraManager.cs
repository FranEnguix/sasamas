using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour {
    [SerializeField] Vector2Int[] captureDimensions;
    [SerializeField] GameObject[] cameras;
    private Camera[] entityCamera;
    private IEnumerator[] coroutineTakeImage;
    private float[] imageTimer;

    void Start() {
        entityCamera = new Camera[cameras.Length];
        coroutineTakeImage = new IEnumerator[cameras.Length];
        imageTimer = new float[cameras.Length];
        for(int i = 0; i < cameras.Length; i++)
            entityCamera[i] = cameras[i].GetComponent<Camera>();
    }

    private void TakePicture(int cameraIndex) {
        if (ImageQueue != null && cameraIndex < cameras.Length) {
            string image = ImageToBase64(ScreenShot(cameraIndex));
            string dateTime = DateToIsoFormat(DateTime.Now.ToUniversalTime());
            ImageData imageData = new ImageData {
                imageBase64 = image,
                cameraIndex = cameraIndex,
                dateTimeUTC = dateTime
            };
            ImageQueue.Enqueue(imageData);
            Debug.Log("Image queue length: " + ImageQueue.Count);
        }
    }

    private string DateToIsoFormat(DateTime date) {
        return date.ToString(@"yyyy-MM-dd" + "T" + @"HH:mm:ss" + "Z");
    }
    private string ImageToBase64(byte[] rawImage) {
        Debug.Log(rawImage);
        return Convert.ToBase64String(rawImage);
    }

    public byte[] ScreenShot(int cameraIndex) {
        int captureWidth = captureDimensions[cameraIndex].x;
        int captureHeight = captureDimensions[cameraIndex].y;
        var rectangle = new Rect(0, 0, captureWidth, captureHeight);
        var renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
        var screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        entityCamera[cameraIndex].targetTexture = renderTexture;
        entityCamera[cameraIndex].Render();
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(rectangle, 0, 0);
        entityCamera[cameraIndex].targetTexture = null;
        RenderTexture.active = null;
        return screenShot.EncodeToJPG();
    }

    public void CameraFov(int cameraIndex, float fov) {
        if (cameraIndex < cameras.Length) {
            var camera = cameras[cameraIndex];
            camera.GetComponent<Camera>().fieldOfView = fov;
        }
    }

    public void CameraMove(int cameraIndex, Vector3 axis, float units) {
        if (cameraIndex < cameras.Length) {
            var camera = cameras[cameraIndex];
            var movement = camera.transform.localPosition;
            if (axis.x > 0)
                movement.x += units;
            if (axis.y > 0)
                movement.y += units;
            if (axis.z > 0)
                movement.z += units;
            camera.transform.localPosition = movement;
        }
    }

    public void CameraRotate(int cameraIndex, Vector3 rotation) {
        if (cameraIndex < cameras.Length) {
            var camera = entityCamera[cameraIndex];
            camera.transform.rotation = Quaternion.Euler(rotation);
        }
    }

    private IEnumerator TakePictureCoroutine(int cameraIndex, float seconds) {
        yield return new WaitForSeconds(seconds);
        TakePicture(cameraIndex);
    }

    public void LaunchTakePicture(int cameraIndex) {
        if (cameraIndex < cameras.Length) {
            if (coroutineTakeImage[cameraIndex] != null)
                StopCoroutine(coroutineTakeImage[cameraIndex]);
            if (imageTimer[cameraIndex] == 0) {
                TakePicture(cameraIndex);
            } else if (imageTimer[cameraIndex] > 0) {
                coroutineTakeImage[cameraIndex] = TakePictureCoroutine(cameraIndex, imageTimer[cameraIndex]);
                StartCoroutine(coroutineTakeImage[cameraIndex]);
            }
        }
    }

    public void SetFrequency(int cameraIndex, float frequency) {
        if (cameraIndex < cameras.Length)
            imageTimer[cameraIndex] = frequency;
    }

    public ConcurrentQueue<ImageData> ImageQueue {
        get;
        set;
    }

    /*
    new System.Threading.Thread(() => {
            var file = System.IO.File.Create("prueba.jpg");
            file.Write(image, 0, image.Length);
            file.Close();
            Debug.Log("File prueba.jpg created");
        }).Start();
    */
}
