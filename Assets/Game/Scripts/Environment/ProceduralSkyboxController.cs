using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Environment
{
    /// <summary>
    /// 程序化天空盒日夜循环控制器（适配 Custom/ProceduralSkybox_URP）
    /// </summary>
    [ExecuteAlways] // 编辑模式下也可运行
    public class ProceduralSkyboxController : MonoBehaviour
    {
        [Header("天空盒材质")]
        public Material skyboxMaterial;

        [Header("时间控制")]
        [Range(0f, 24f)] public float currentTime = 12f; // 0-24小时
        public float timeSpeed = 0.1f;                   // 时间流速
        public bool autoUpdate = true;                   // 是否自动推进时间

        [Header("光源（可选）")]
        public Light sunLight;  // 主光源（太阳）
        public Light moonLight; // 月光

        [Header("颜色渐变曲线")]
        public Gradient dayTopGradient;
        public Gradient dayBottomGradient;
        public Gradient nightTopGradient;
        public Gradient nightBottomGradient;
        public Gradient sunColorGradient;
        public Gradient moonColorGradient;
        public Gradient horizonColorGradient;
        public Gradient horizonNightTintGradient;
        public Gradient cloudColorGradient;

        [Header("云层参数")]
        public float cloudDensity = 0.8f;
        public float cloudScale = 2.0f;
        public Vector2 cloudSpeed = new Vector2(0.05f, 0.02f);
        public float cloudHeight = 0.3f;

        [Header("星星参数")]
        public float starsIntensity = 0.5f;
        public float starsTwinkleSpeed = 2.0f;

        [Header("性能优化")]
        public float slowUpdateInterval = 0.2f; // 慢速更新间隔（秒）

        // 缓存上次的值，避免重复设置材质属性
        private float lastTime = -1f;
        private Vector3 lastSunDir = Vector3.zero;
        private Vector3 lastMoonDir = Vector3.zero;
        private float lastBlend = -1f;
        private float lastSunIntensity = -1f;
        private float lastMoonIntensity = -1f;
        private float lastCloudDensity = -1f;
        private float lastCloudScale = -1f;
        private Vector2 lastCloudSpeed = Vector2.zero;
        private float lastCloudHeight = -1f;
        private float lastStarsIntensity = -1f;
        private float lastStarsTwinkleSpeed = -1f;

        // 颜色缓存
        private Color lastDayTop, lastDayBottom, lastNightTop, lastNightBottom;
        private Color lastSunColor, lastMoonColor, lastHorizonColor, lastHorizonNightTint, lastCloudColor;

        private WaitForSeconds slowWait;


        private void Reset()
        {
            // 如果材质未指定，尝试获取当前天空盒材质
            if (skyboxMaterial == null)
                skyboxMaterial = RenderSettings.skybox;

            // 初始化所有渐变曲线
            InitDayTopGradient();
            InitDayBottomGradient();
            InitNightTopGradient();
            InitNightBottomGradient();
            InitSunColorGradient();
            InitMoonColorGradient();
            InitHorizonColorGradient();
            InitHorizonNightTintGradient();
            InitCloudColorGradient();

            // 云参数默认值
            cloudDensity = 0.8f;
            cloudScale = 2.0f;
            cloudSpeed = new Vector2(0.05f, 0.02f);
            cloudHeight = 0.3f;

            // 星星参数
            starsIntensity = 0.5f;
            starsTwinkleSpeed = 2.0f;

            // 时间参数
            currentTime = 12f;
            timeSpeed = 0.1f;
            autoUpdate = true;
            slowUpdateInterval = 0.2f;
        }

        // ----- 渐变曲线初始化函数 -----
        void InitDayTopGradient()
        {
            dayTopGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.04f, 0.10f, 0.16f), 0.0f), // #0A1A2A
                new GradientColorKey(new Color(0.16f, 0.29f, 0.42f), 0.2f), // #2A4A6A
                new GradientColorKey(new Color(0.29f, 0.48f, 0.60f), 0.3f), // #4A7A9A
                new GradientColorKey(new Color(0.42f, 0.71f, 0.93f), 0.5f), // #6CB4EE
                new GradientColorKey(new Color(0.29f, 0.48f, 0.60f), 0.7f), // #4A7A9A
                new GradientColorKey(new Color(0.16f, 0.29f, 0.42f), 0.8f), // #2A4A6A
                new GradientColorKey(new Color(0.04f, 0.10f, 0.16f), 1.0f)  // #0A1A2A
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            dayTopGradient.SetKeys(colorKeys, alphaKeys);
        }

        void InitDayBottomGradient()
        {
            dayBottomGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.10f, 0.16f, 0.23f), 0.0f), // #1A2A3A
                new GradientColorKey(new Color(1.00f, 0.85f, 0.73f), 0.2f), // #FFDAB9
                new GradientColorKey(new Color(0.79f, 0.91f, 1.00f), 0.3f), // #CAE9FF
                new GradientColorKey(new Color(0.88f, 0.94f, 1.00f), 0.5f), // #E0F0FF
                new GradientColorKey(new Color(0.79f, 0.91f, 1.00f), 0.7f), // #CAE9FF
                new GradientColorKey(new Color(1.00f, 0.69f, 0.49f), 0.8f), // #FFB07C
                new GradientColorKey(new Color(0.10f, 0.16f, 0.23f), 1.0f)  // #1A2A3A
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            dayBottomGradient.SetKeys(colorKeys, alphaKeys);
        }

        void InitNightTopGradient()
        {
            nightTopGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.04f, 0.04f, 0.10f), 0.0f), // #0A0A1A
                new GradientColorKey(new Color(0.08f, 0.08f, 0.16f), 0.2f), // #14142A
                new GradientColorKey(new Color(0.04f, 0.04f, 0.10f), 0.5f), // #0A0A1A
                new GradientColorKey(new Color(0.08f, 0.08f, 0.16f), 0.8f), // #14142A
                new GradientColorKey(new Color(0.04f, 0.04f, 0.10f), 1.0f)  // #0A0A1A
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            nightTopGradient.SetKeys(colorKeys, alphaKeys);
        }

        void InitNightBottomGradient()
        {
            nightBottomGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.10f, 0.10f, 0.18f), 0.0f), // #1A1A2E
                new GradientColorKey(new Color(0.18f, 0.11f, 0.24f), 0.2f), // #2D1B3C
                new GradientColorKey(new Color(0.10f, 0.10f, 0.18f), 0.5f), // #1A1A2E
                new GradientColorKey(new Color(0.18f, 0.11f, 0.24f), 0.8f), // #2D1B3C
                new GradientColorKey(new Color(0.10f, 0.10f, 0.18f), 1.0f)  // #1A1A2E
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            nightBottomGradient.SetKeys(colorKeys, alphaKeys);
        }

        void InitSunColorGradient()
        {
            sunColorGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.00f, 0.88f, 0.69f), 0.0f), // #FFE0B0
                new GradientColorKey(new Color(1.00f, 0.60f, 0.40f), 0.2f), // #FF9966
                new GradientColorKey(new Color(1.00f, 0.96f, 0.90f), 0.3f), // #FFF5E6
                new GradientColorKey(new Color(1.00f, 1.00f, 0.94f), 0.5f), // #FFFFF0
                new GradientColorKey(new Color(1.00f, 0.96f, 0.90f), 0.7f), // #FFF5E6
                new GradientColorKey(new Color(1.00f, 0.60f, 0.40f), 0.8f), // #FF9966
                new GradientColorKey(new Color(1.00f, 0.88f, 0.69f), 1.0f)  // #FFE0B0
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            sunColorGradient.SetKeys(colorKeys, alphaKeys);
        }

        void InitMoonColorGradient()
        {
            moonColorGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.88f, 0.88f, 1.00f), 0.0f), // #E0E0FF
                new GradientColorKey(new Color(0.75f, 0.75f, 0.88f), 0.2f), // #C0C0E0
                new GradientColorKey(new Color(0.63f, 0.63f, 0.75f), 0.5f), // #A0A0C0
                new GradientColorKey(new Color(0.75f, 0.75f, 0.88f), 0.8f), // #C0C0E0
                new GradientColorKey(new Color(0.88f, 0.88f, 1.00f), 1.0f)  // #E0E0FF
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            moonColorGradient.SetKeys(colorKeys, alphaKeys);
        }

        void InitHorizonColorGradient()
        {
            horizonColorGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.23f, 0.23f, 0.35f), 0.0f), // #3A3A5A
                new GradientColorKey(new Color(1.00f, 0.69f, 0.49f), 0.2f), // #FFB07C
                new GradientColorKey(new Color(0.69f, 0.88f, 1.00f), 0.3f), // #B0E0FF
                new GradientColorKey(new Color(0.79f, 0.91f, 1.00f), 0.5f), // #CAE9FF
                new GradientColorKey(new Color(0.69f, 0.88f, 1.00f), 0.7f), // #B0E0FF
                new GradientColorKey(new Color(1.00f, 0.69f, 0.49f), 0.8f), // #FFB07C
                new GradientColorKey(new Color(0.23f, 0.23f, 0.35f), 1.0f)  // #3A3A5A
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            horizonColorGradient.SetKeys(colorKeys, alphaKeys);
        }

        void InitHorizonNightTintGradient()
        {
            horizonNightTintGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.16f, 0.16f, 0.29f), 0.0f), // #2A2A4A
                new GradientColorKey(new Color(0.23f, 0.23f, 0.35f), 0.2f), // #3A3A5A
                new GradientColorKey(new Color(0.16f, 0.16f, 0.29f), 0.5f), // #2A2A4A
                new GradientColorKey(new Color(0.23f, 0.23f, 0.35f), 0.8f), // #3A3A5A
                new GradientColorKey(new Color(0.16f, 0.16f, 0.29f), 1.0f)  // #2A2A4A
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            horizonNightTintGradient.SetKeys(colorKeys, alphaKeys);
        }

        void InitCloudColorGradient()
        {
            cloudColorGradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.29f, 0.29f, 0.42f), 0.0f), // #4A4A6A
                new GradientColorKey(new Color(1.00f, 0.75f, 0.63f), 0.2f), // #FFC0A0
                new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.3f), // #FFFFFF
                new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.5f), // #FFFFFF
                new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.7f), // #FFFFFF
                new GradientColorKey(new Color(1.00f, 0.69f, 0.56f), 0.8f), // #FFB090
                new GradientColorKey(new Color(0.29f, 0.29f, 0.42f), 1.0f)  // #4A4A6A
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
            };
            cloudColorGradient.SetKeys(colorKeys, alphaKeys);
        }
        void OnEnable()
        {
            if (skyboxMaterial == null)
                skyboxMaterial = RenderSettings.skybox; // 自动获取当前天空盒材质

            slowWait = new WaitForSeconds(slowUpdateInterval);
            StartCoroutine(SlowUpdateRoutine());
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        void Update()
        {
            if (!autoUpdate || !Application.isPlaying) return;

            // 更新时间
            currentTime += Time.deltaTime * timeSpeed;
            if (currentTime >= 24f) currentTime -= 24f;
            if (currentTime < 0f) currentTime += 24f;

            // 快速更新：方向、强度、混合因子（每帧）
            UpdateFastParams();
        }

        /// <summary>
        /// 快速变化参数（每帧更新，带缓存比较）
        /// </summary>
        void UpdateFastParams()
        {
            if (skyboxMaterial == null) return;

            float t = currentTime / 24f;
            float sunAngle = t * 360f;
            float sunHeight = Mathf.Sin(sunAngle * Mathf.Deg2Rad);
            float sunHorizontal = Mathf.Cos(sunAngle * Mathf.Deg2Rad);

            // 太阳方向（东升西落）
            Vector3 sunDir = new Vector3(sunHorizontal, sunHeight, 0f).normalized;
            // 月亮方向与太阳相反（简单实现）
            Vector3 moonDir = new Vector3(-sunHorizontal, -sunHeight, 0f).normalized;

            // 昼夜混合因子：根据太阳高度映射 0~1（夜晚→白天）
            float blend = Mathf.InverseLerp(-0.3f, 0.7f, sunHeight);
            blend = Mathf.Clamp01(blend);

            // 太阳/月亮强度
            float sunIntensity = Mathf.Lerp(0.2f, 1.2f, blend);
            float moonIntensity = Mathf.Lerp(1.0f, 0.1f, blend);

            // ---------- 设置材质属性（仅当变化时）----------
            // 太阳方向
            if (Vector3.Distance(sunDir, lastSunDir) > 0.001f)
            {
                skyboxMaterial.SetVector("_SunDirection", sunDir);
                lastSunDir = sunDir;
                if (sunLight != null)
                    sunLight.transform.rotation = Quaternion.LookRotation(-sunDir);
            }

            // 月亮方向
            if (Vector3.Distance(moonDir, lastMoonDir) > 0.001f)
            {
                skyboxMaterial.SetVector("_MoonDirection", moonDir);
                lastMoonDir = moonDir;
                if (moonLight != null)
                    moonLight.transform.rotation = Quaternion.LookRotation(-moonDir);
            }

            // 昼夜混合因子
            if (Mathf.Abs(blend - lastBlend) > 0.001f)
            {
                skyboxMaterial.SetFloat("_DayNightBlend", blend);
                // 同时关闭自动混合，由脚本控制
                skyboxMaterial.SetFloat("_AutoBlend", 0f);
                lastBlend = blend;
            }

            // 太阳强度
            if (Mathf.Abs(sunIntensity - lastSunIntensity) > 0.01f)
            {
                skyboxMaterial.SetFloat("_SunIntensity", sunIntensity);
                lastSunIntensity = sunIntensity;
                if (sunLight != null) sunLight.intensity = sunIntensity;
            }

            // 月亮强度
            if (Mathf.Abs(moonIntensity - lastMoonIntensity) > 0.01f)
            {
                skyboxMaterial.SetFloat("_MoonIntensity", moonIntensity);
                lastMoonIntensity = moonIntensity;
                if (moonLight != null) moonLight.intensity = moonIntensity * 0.5f;
            }

            // 星星种子（每帧变化产生闪烁）
            float starsSeed = Time.time * 0.1f;
            skyboxMaterial.SetFloat("_StarsSeed", starsSeed);
        }

        /// <summary>
        /// 慢速变化参数（颜色渐变、云参数等，通过协程降频）
        /// </summary>
        IEnumerator SlowUpdateRoutine()
        {
            while (true)
            {
                if (skyboxMaterial != null && autoUpdate)
                {
                    UpdateSlowParams();
                }
                yield return slowWait;
            }
        }

        void UpdateSlowParams()
        {
            float t = currentTime / 24f;

            // 采样渐变曲线
            Color dayTop = dayTopGradient.Evaluate(t);
            Color dayBottom = dayBottomGradient.Evaluate(t);
            Color nightTop = nightTopGradient.Evaluate(t);
            Color nightBottom = nightBottomGradient.Evaluate(t);
            Color sunColor = sunColorGradient.Evaluate(t);
            Color moonColor = moonColorGradient.Evaluate(t);
            Color horizonColor = horizonColorGradient.Evaluate(t);
            Color horizonNightTint = horizonNightTintGradient.Evaluate(t);
            Color cloudColor = cloudColorGradient.Evaluate(t);

            // 月亮相位（简单映射：满月→新月→满月）
            float phase = Mathf.Sin(t * Mathf.PI * 2f);
            phase = Mathf.Clamp(phase, -1f, 1f);

            // 批量设置颜色（带缓存）
            SetColorIfChanged("_DayColorTop", dayTop, ref lastDayTop);
            SetColorIfChanged("_DayColorBottom", dayBottom, ref lastDayBottom);
            SetColorIfChanged("_NightColorTop", nightTop, ref lastNightTop);
            SetColorIfChanged("_NightColorBottom", nightBottom, ref lastNightBottom);
            SetColorIfChanged("_SunColor", sunColor, ref lastSunColor);
            SetColorIfChanged("_MoonColor", moonColor, ref lastMoonColor);
            SetColorIfChanged("_HorizonColor", horizonColor, ref lastHorizonColor);
            SetColorIfChanged("_HorizonNightTint", horizonNightTint, ref lastHorizonNightTint);
            SetColorIfChanged("_CloudColor", cloudColor, ref lastCloudColor);

            // 月亮相位
            skyboxMaterial.SetFloat("_MoonPhase", phase);

            // 云参数
            SetFloatIfChanged("_CloudDensity", cloudDensity, ref lastCloudDensity);
            SetFloatIfChanged("_CloudScale", cloudScale, ref lastCloudScale);
            SetVectorIfChanged("_CloudSpeed", new Vector4(cloudSpeed.x, cloudSpeed.y, 0, 0), ref lastCloudSpeed);
            SetFloatIfChanged("_CloudHeight", cloudHeight, ref lastCloudHeight);

            // 星星参数
            SetFloatIfChanged("_StarsIntensity", starsIntensity, ref lastStarsIntensity);
            SetFloatIfChanged("_StarsTwinkleSpeed", starsTwinkleSpeed, ref lastStarsTwinkleSpeed);
        }

        // ---------- 辅助方法：仅当值变化时才设置材质属性 ----------
        void SetColorIfChanged(string property, Color newValue, ref Color cached)
        {
            if (newValue != cached)
            {
                skyboxMaterial.SetColor(property, newValue);
                cached = newValue;
            }
        }

        void SetFloatIfChanged(string property, float newValue, ref float cached)
        {
            if (Mathf.Abs(newValue - cached) > 0.001f)
            {
                skyboxMaterial.SetFloat(property, newValue);
                cached = newValue;
            }
        }

        void SetVectorIfChanged(string property, Vector4 newValue, ref Vector2 cached)
        {
            if (Vector4.Distance(newValue, new Vector4(cached.x, cached.y, 0, 0)) > 0.001f)
            {
                skyboxMaterial.SetVector(property, newValue);
                cached = new Vector2(newValue.x, newValue.y);
            }
        }

        // ---------- 公开方法：供外部调用（如UI滑块）----------
        public void SetTime(float time01)
        {
            currentTime = Mathf.Lerp(0f, 24f, time01);
            if (!autoUpdate)
            {
                UpdateFastParams();
                UpdateSlowParams();
            }
        }

        public void SetTimeSpeed(float speed)
        {
            timeSpeed = speed;
        }

        public void SetCloudDensity(float density)
        {
            cloudDensity = density;
            SetFloatIfChanged("_CloudDensity", cloudDensity, ref lastCloudDensity);
        }

        public void SetStarsIntensity(float intensity)
        {
            starsIntensity = intensity;
            SetFloatIfChanged("_StarsIntensity", starsIntensity, ref lastStarsIntensity);
        }
    }
}
