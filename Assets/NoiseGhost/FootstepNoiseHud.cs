using UnityEngine;
using UnityEngine.UI;

namespace StealthGame
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerFootstepNoise))]
    public class FootstepNoiseHud : MonoBehaviour
    {
        [Header("Right-side meter, 1920 x 1080 reference")]
        public Vector2 offset = new Vector2(34, 0);
        public Vector2 size = new Vector2(26, 280);
        [Range(5,40)] public int segmentCount = 20;
        [Min(0)] public float segmentGap = 3f;
        public Color quietColor = new Color(.28f, .66f, .30f);
        public Color loudColor = new Color(1, .25f, .12f);
        public Color frameColor = new Color(.83f, .83f, .77f, .9f);
        PlayerFootstepNoise noise;
        GameObject hud;
        RectTransform frame, fill;
        Image fillImage, border;
        Image[] segments;
        Texture2D iconTexture;
        Sprite iconSprite;
        static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var o = new GameObject(name, typeof(RectTransform), typeof(Image)); o.transform.SetParent(parent, false);
            var r = (RectTransform)o.transform; r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var image = o.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        void Awake()
        {
            noise = GetComponent<PlayerFootstepNoise>();
            hud = new GameObject("Footstep Noise HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 60;
            var scaler = hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            border = Panel("Noise meter frame", hud.transform, new Vector2(1,.5f), new Vector2(1,.5f), frameColor);
            frame = border.rectTransform; frame.pivot = new Vector2(1,.5f);
            var background = Panel("Background", frame, Vector2.zero, Vector2.one, new Color(.04f,.04f,.04f,.9f));
            background.rectTransform.offsetMin = new Vector2(3,3); background.rectTransform.offsetMax = new Vector2(-3,-3);
            fillImage = Panel("Noise level", background.transform, Vector2.zero, new Vector2(1,0), quietColor); fill = fillImage.rectTransform;
            fillImage.enabled = false;
            segments = new Image[Mathf.Clamp(segmentCount,5,40)];
            for(int i=0;i<segments.Length;i++)segments[i]=Panel("Noise segment "+(i+1),background.transform,new Vector2(0,i/(float)segments.Length),new Vector2(1,(i+1)/(float)segments.Length),Color.clear);
            var icon = Panel("Footstep sound icon", frame, new Vector2(.5f,0), new Vector2(.5f,0), frameColor);
            icon.rectTransform.sizeDelta = new Vector2(32,36); icon.rectTransform.anchoredPosition = new Vector2(0,-22);
            iconTexture = new Texture2D(32,36,TextureFormat.RGBA32,false); iconTexture.filterMode = FilterMode.Bilinear;
            // A microphone-shaped sound meter icon; this reads game footsteps, not the microphone.
            for(int y=0;y<36;y++) for(int x=0;x<32;x++)
            {
                bool capsule = (x>=12 && x<=19 && y>=16 && y<=27) || new Vector2(x-15.5f,y-27).sqrMagnitude<=16 || new Vector2(x-15.5f,y-16).sqrMagnitude<=16;
                float ring = new Vector2(x-15.5f,y-16).magnitude;
                bool bracket = (y>=16 && y<=23 && (x==7 || x==8 || x==23 || x==24)) || (y<16 && ring>=7 && ring<=9);
                bool stand = (x>=14 && x<=17 && y>=5 && y<=9) || (x>=9 && x<=22 && y>=4 && y<=6);
                iconTexture.SetPixel(x,y,capsule||bracket||stand ? new Color(.07f,.07f,.07f) : Color.clear);
            }
            iconTexture.Apply(); iconSprite = Sprite.Create(iconTexture,new Rect(0,0,32,36),new Vector2(.5f,.5f));
            var mark = Panel("Sound symbol",icon.transform,Vector2.zero,Vector2.one,Color.white); mark.sprite = iconSprite;
        }
        void OnEnable(){if(hud!=null)hud.SetActive(true);}
        void OnDisable(){if(hud!=null)hud.SetActive(false);}
        void Update()
        {
            frame.anchoredPosition=new Vector2(-offset.x,offset.y);frame.sizeDelta=size;border.color=frameColor;
            if(segments.Length!=Mathf.Clamp(segmentCount,5,40))
            {
                foreach(var item in segments)Destroy(item.gameObject);
                segments=new Image[Mathf.Clamp(segmentCount,5,40)];
                for(int i=0;i<segments.Length;i++)segments[i]=Panel("Noise segment "+(i+1),fill.parent,new Vector2(0,i/(float)segments.Length),new Vector2(1,(i+1)/(float)segments.Length),Color.clear);
            }
            int lit=noise.Level<=.001f?0:Mathf.CeilToInt(noise.Level*segments.Length);
            float gap=Mathf.Min(segmentGap,(size.y-6)/segments.Length*.7f);
            for(int i=0;i<segments.Length;i++)
            {
                segments[i].rectTransform.offsetMin=new Vector2(1,gap*.5f);segments[i].rectTransform.offsetMax=new Vector2(-1,-gap*.5f);
                segments[i].color=i<lit?Color.Lerp(quietColor,loudColor,Mathf.InverseLerp(.65f,1,noise.Level)):Color.clear;
            }
        }
        void OnDestroy(){if(hud!=null)Destroy(hud);if(iconSprite!=null)Destroy(iconSprite);if(iconTexture!=null)Destroy(iconTexture);}
    }
}
