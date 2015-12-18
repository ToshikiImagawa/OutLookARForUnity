/**
 * 
 *  You can not modify and use this source freely
 *  only for the development of application related OutLookAR.
 * 
 * (c) OutLookAR All rights reserved.
 * by Toshiki Imagawa
**/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace OutLookAR.Test
{
    public class CaptureTest : UIBehaviour
    {
        [SerializeField]
        Image PanelModel;
        AspectRatioFitter arf;
        AspectRatioFitter ARF { get { return arf ?? (arf = PanelModel.GetComponent<AspectRatioFitter>()); } }

        // Use this for initialization
        protected override void Start()
        {
            CaptureManager.Instance.Play();
            ARF.aspectRatio = ((float)CaptureManager.Instance.Width) / CaptureManager.Instance.Height;
        }

        void UpdateTexture(Texture2D texture)
        {
            ARF.aspectRatio = ((float)texture.width) / texture.height;
            PanelModel.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(texture.width, texture.height), 1.0f);
        }
        protected override void OnEnable()
        {
            CaptureManager.Instance.OnTextur += UpdateTexture;
        }
        protected override void OnDisable()
        {
            CaptureManager.Instance.OnTextur -= UpdateTexture;
        }
    }
}