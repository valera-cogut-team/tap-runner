using TapRunner.Domain;
using TapRunner.Facade;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace TapRunner.Presentation
{
    /// <summary>Reactive HUD for Tap Runner (Chapter 11 / 14 — UI updates on streams, not every Update).</summary>
    public sealed class TapRunnerHud : MonoBehaviour
    {
        private ITapRunnerFacade _facade;
        private CompositeDisposable _bindings;
        private bool _uiBuilt;

        private TMP_Text _mainLine;
        private TMP_Text _hintLine;

        public void Initialize(ITapRunnerFacade facade)
        {
            if (!_uiBuilt)
            {
                BuildUi();
                _uiBuilt = true;
            }

            _bindings?.Dispose();
            _bindings = new CompositeDisposable();
            _facade = facade;
            if (_facade == null)
                return;

            Observable.CombineLatest(
                _facade.PhaseRx,
                _facade.ScoreRx,
                _facade.BestScoreRx,
                _facade.DistanceRx,
                (phase, score, best, dist) => (phase, score, best, dist)
            ).Subscribe(t => RefreshMain(t.phase, t.score, t.best, t.dist)).AddTo(_bindings);
        }

        private void OnDestroy()
        {
            _bindings?.Dispose();
            _bindings = null;
            _facade = null;
        }

        private void RefreshMain(TapRunnerGamePhase phase, int score, int best, float dist)
        {
            if (_mainLine == null)
                return;

            if (phase == TapRunnerGamePhase.GameOver)
            {
                _mainLine.text =
                    $"<b>TAP TO RETRY</b>   <color=#FF8A65>score {score}</color>   <color=#6ED9BE>best {best}</color>";
            }
            else
            {
                _mainLine.text =
                    $"<b>RUN</b>  <color=#FFC94A>{score}</color>   " +
                    $"<b>BEST</b>  <color=#6ED9BE>{best}</color>   " +
                    $"<color=#8899AA>m {dist:0}</color>";
            }

            if (_hintLine != null)
            {
                _hintLine.text = phase == TapRunnerGamePhase.GameOver
                    ? "<size=90%>Space / tap — restart · avoid blocks</size>"
                    : "<size=90%>Space / tap — jump</size>";
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("TapRunnerHud_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.layer = 5;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2600;
            canvas.overrideSorting = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.55f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var safeGo = new GameObject("SafeArea", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            safeGo.layer = 5;
            var safeRt = safeGo.GetComponent<RectTransform>();
            StretchFullCanvas(safeRt);
            ApplyScreenSafeArea(safeRt);

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(safeGo.transform, false);
            panelGo.layer = 5;

            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(28f, -24f);
            panelRt.sizeDelta = new Vector2(820f, 0f);

            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.16f, 0.86f);
            bg.raycastTarget = false;

            var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(22, 22, 18, 18);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperLeft;

            var fitter = panelGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _mainLine = CreateTmpLine(panelGo.transform, "MainLine", 30f, FontStyles.Bold, new Color(0.96f, 0.97f, 1f));
            _hintLine = CreateTmpLine(panelGo.transform, "HintLine", 18f, FontStyles.Normal, new Color(0.7f, 0.76f, 0.86f));
        }

        private static void StretchFullCanvas(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void ApplyScreenSafeArea(RectTransform rt)
        {
            var sa = Screen.safeArea;
            if (sa.width <= 1f || sa.height <= 1f)
                return;

            var anchorMin = sa.position;
            var anchorMax = sa.position + sa.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TMP_Text CreateTmpLine(Transform parent, string name, float fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null)
            {
                tmp.font = font;
                tmp.fontSharedMaterial = font.material;
            }

            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;
            tmp.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, fontSize + 12f);

            return tmp;
        }
    }
}
