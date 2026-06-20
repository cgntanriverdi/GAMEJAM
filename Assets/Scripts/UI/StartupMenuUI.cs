using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StartupMenuUI : MonoBehaviour
{
    private sealed class LevelButtonWidget
    {
        public int LevelIndex;
        public Button Button;
        public Image NodeImage;
        public Image NodeRimImage;
        public Image NodeShadeImage;
        public Image NodeHighlightImage;
        public Image NodePedestalImage;
        public Image NodeGlowImage;
        public TextMeshProUGUI NumberText;
        public StarRowWidget Stars;
    }

    private sealed class StarRowWidget
    {
        public RectTransform Rect;
        public Image[] Stars;
    }

    private enum OverlayState
    {
        Hidden,
        Intro,
        Map,
        Complete,
        CharacterSelect
    }

    private static StartupMenuUI _instance;
    private static bool _shouldBlockAutoStart = true;

    private readonly List<LevelButtonWidget> _levelButtons = new();

    private Canvas _canvas;
    private RectTransform _playAreaRoot;
    private GridManager _gridManager;
    private PlayerToken _playerToken;
    private SwipeInputController _swipeInput;
    private GameManager _gameManager;
    private GameManager _subscribedGameManager;

    private GameObject _overlayRoot;
    private GameObject _introPanel;
    private GameObject _mapPanel;
    private GameObject _completePanel;
    private GameObject _characterPanel;
    private RectTransform _introPanelRect;
    private RectTransform _mapPanelRect;
    private RectTransform _completePanelRect;
    private RectTransform _characterPanelRect;
    private RectTransform _introContentRect;
    private RectTransform _characterContentRect;
    private RectTransform _mapCardRect;
    private RectTransform _mapRouteWindowRect;   // görünür pencere (ScrollRect burada)
    private RectTransform _mapRouteViewportRect; // mask + drag yakalayıcı
    private RectTransform _mapRouteRect;         // kaydırılan içerik (node'lar burada)
    private UnityEngine.UI.ScrollRect _mapRouteScroll;
    private RectTransform _completeCardRect;
    private RectTransform _completeContentRect;
    private RectTransform _completeTitleRect;
    private RectTransform _completeStarsRect;
    private RectTransform _completeTimeRect;
    private RectTransform _completeBestRect;
    private RectTransform _completeRewardIconRect;
    private RectTransform _completeMapButtonRect;
    private RectTransform _completeReplayButtonRect;
    private RectTransform _completeNextButtonRect;
    private TextMeshProUGUI _textTemplate;
    private Sprite _panelSprite;
    private Sprite _roundButtonSprite;
    private Sprite _mapBackgroundSprite;

    private TextMeshProUGUI _completeTitleText;
    private TextMeshProUGUI _completeTimeText;
    private TextMeshProUGUI _completeBestText;
    private TextMeshProUGUI _completeBestTimeText;
    private TextMeshProUGUI _completeRewardIconText;
    private TextMeshProUGUI _completeMapButtonText;
    private TextMeshProUGUI _completeReplayButtonText;
    private TextMeshProUGUI _completeNextButtonText;
    private Button _nextButton;
    private Sprite _starSprite;
    private Sprite _circleSprite;
    private Sprite _softCircleSprite;
    private StarRowWidget _completeStarsWidget;
    private StarRowWidget _completeBestStarsWidget;
    private Vector2 _lastCanvasSize = Vector2.negativeInfinity;
    private Vector2 _lastRouteSize = Vector2.negativeInfinity;
    private Coroutine _completePanelAnimation;

    private GameObject _settingsPanel;
    private Button     _settingsButton;
    private RectTransform _settingsButtonRect;

    private OverlayState _overlayState = OverlayState.Intro;
    private LevelCompletionResult _lastResult;

    public static StartupMenuUI Instance => _instance;
    public static bool ShouldBlockAutoStart => _shouldBlockAutoStart;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        _shouldBlockAutoStart = true;

        if (_instance != null)
            return;

        _instance = FindObjectOfType<StartupMenuUI>();
        if (_instance != null)
            return;

        var go = new GameObject(nameof(StartupMenuUI));
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<StartupMenuUI>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        DetachGameManagerEvents();
    }

    private void Update()
    {
        if (_overlayRoot != null)
        {
            RefreshResponsiveLayoutIfNeeded();
            return;
        }

        if (!_shouldBlockAutoStart || GameManager.Instance == null)
            return;

        CacheSceneReferences();
        if (_canvas == null)
            return;

        BuildOverlay();
        ShowIntro();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager.Instance == null)
        {
            TeardownOverlay();
            return;
        }

        CacheSceneReferences();
        if (_canvas == null)
            return;

        BuildOverlay();
        ShowIntro();
    }

    private void CacheSceneReferences()
    {
        _canvas = FindObjectOfType<Canvas>();
        _gridManager = FindObjectOfType<GridManager>();
        _playerToken = FindObjectOfType<PlayerToken>();
        _swipeInput = FindObjectOfType<SwipeInputController>();
        _gameManager = GameManager.Instance;
        AttachGameManagerEvents();

        GameObject playAreaRootObject = GameObject.Find("PlayAreaRoot");
        _playAreaRoot = playAreaRootObject != null
            ? playAreaRootObject.GetComponent<RectTransform>()
            : null;

        if (_textTemplate == null)
            _textTemplate = FindObjectOfType<TextMeshProUGUI>();

        if (_panelSprite == null)
        {
            GameObject bannerBackground = GameObject.Find("BannerBackground");
            Image bannerImage = bannerBackground != null ? bannerBackground.GetComponent<Image>() : null;
            if (bannerImage != null)
                _panelSprite = bannerImage.sprite;
        }

        if (_roundButtonSprite == null)
        {
            GameObject undoButton = GameObject.Find("UndoButton");
            Image undoImage = undoButton != null ? undoButton.GetComponent<Image>() : null;
            if (undoImage != null)
                _roundButtonSprite = undoImage.sprite;
        }

        if (_mapBackgroundSprite == null)
        {
            Sprite[] mapBackgroundSprites = Resources.LoadAll<Sprite>("Generated/MapBackground");
            if (mapBackgroundSprites != null && mapBackgroundSprites.Length > 0)
                _mapBackgroundSprite = mapBackgroundSprites[0];
            if (_mapBackgroundSprite == null)
            {
                GameObject backgroundObject = GameObject.Find("Background");
                SpriteRenderer backgroundRenderer = backgroundObject != null ? backgroundObject.GetComponent<SpriteRenderer>() : null;
                if (backgroundRenderer != null)
                    _mapBackgroundSprite = backgroundRenderer.sprite;
            }
        }
    }

    private void AttachGameManagerEvents()
    {
        if (_subscribedGameManager == _gameManager)
            return;

        DetachGameManagerEvents();

        _subscribedGameManager = _gameManager;
        if (_subscribedGameManager != null)
            _subscribedGameManager.OnLevelResultReady += HandleLevelResult;
    }

    private void DetachGameManagerEvents()
    {
        if (_subscribedGameManager != null)
            _subscribedGameManager.OnLevelResultReady -= HandleLevelResult;

        _subscribedGameManager = null;
    }

    private void BuildOverlay()
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            RefreshMapPanel();
            return;
        }

        _overlayRoot = CreateUiObject("StartupOverlay", _canvas.transform);
        RectTransform overlayRect = _overlayRoot.GetComponent<RectTransform>();
        StretchToParent(overlayRect);

        Image overlayImage = _overlayRoot.AddComponent<Image>();
        overlayImage.color = new Color(0.03f, 0.05f, 0.14f, 0.28f);
        overlayImage.raycastTarget = true;

        BuildIntroPanel();
        BuildMapPanel();
        BuildCompletePanel();
        BuildCharacterPanel();
        BuildSettingsButton();
        BuildSettingsPanel();
        RefreshResponsiveLayout(forceRouteRebuild: true);

        SetOverlayState(OverlayState.Intro);
    }

    private void BuildIntroPanel()
    {
        _introPanel = CreateUiObject("IntroPanel", _overlayRoot.transform);
        _introPanelRect = _introPanel.GetComponent<RectTransform>();

        _introContentRect = CreateRect(
            "IntroContent",
            _introPanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(404f, 724f));

        RectTransform ropeRect = CreateRect(
            "Rope",
            _introContentRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -28f),
            new Vector2(10f, 156f));
        Image ropeImage = ropeRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(ropeImage, new Color(0.48f, 0.33f, 0.21f, 0.92f));
        ropeImage.raycastTarget = false;

        RectTransform cardRect = CreateRect(
            "IntroCard",
            _introContentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 88f),
            new Vector2(360f, 300f));
        Image cardImage = cardRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(cardImage, new Color(0.99f, 0.94f, 0.82f, 0.98f));
        cardImage.raycastTarget = false;

        Shadow cardShadow = cardRect.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        cardShadow.effectDistance = new Vector2(0f, -10f);

        Outline cardOutline = cardRect.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.72f, 0.54f, 0.24f, 0.8f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform holeRect = CreateRect(
            "CardHole",
            cardRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -24f),
            new Vector2(28f, 28f));
        Image holeImage = holeRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(holeImage, new Color(0.45f, 0.32f, 0.2f, 0.96f));
        holeImage.raycastTarget = false;

        RectTransform titleRect = CreateRect(
            "Title",
            cardRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 38f),
            new Vector2(280f, 120f));
        TextMeshProUGUI titleText = CreateText(titleRect, "Acıkmış\nPatiler", 54f, new Color(0.73f, 0.48f, 0.12f));
        titleText.fontStyle = FontStyles.Bold;
        titleText.lineSpacing = -12f;

        RectTransform subtitleRect = CreateRect(
            "Subtitle",
            cardRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -52f),
            new Vector2(280f, 40f));
        TextMeshProUGUI subtitleText = CreateText(subtitleRect, "Rotanı seç, hızlı bitir, yıldızları kazan.", 22f, new Color(0.42f, 0.29f, 0.16f));
        subtitleText.enableWordWrapping = true;

        RectTransform buttonRect = CreateRect(
            "PlayButton",
            _introContentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -118f),
            new Vector2(248f, 74f));
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(buttonImage, new Color(0.9f, 0.24f, 0.56f, 1f));

        Shadow buttonShadow = buttonRect.gameObject.AddComponent<Shadow>();
        buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
        buttonShadow.effectDistance = new Vector2(0f, -6f);

        Outline buttonOutline = buttonRect.gameObject.AddComponent<Outline>();
        buttonOutline.effectColor = new Color(0.76f, 0.12f, 0.41f, 0.8f);
        buttonOutline.effectDistance = new Vector2(1f, -1f);

        Button playButton = buttonRect.gameObject.AddComponent<Button>();
        playButton.targetGraphic = buttonImage;
        playButton.onClick.AddListener(HandlePlayPressed);

        ColorBlock colors = playButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.95f, 0.98f, 1f);
        colors.pressedColor = new Color(0.92f, 0.84f, 0.9f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.6f);
        playButton.colors = colors;

        RectTransform playLabelRect = CreateRect(
            "Label",
            buttonRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(180f, 44f));
        TextMeshProUGUI playLabel = CreateText(playLabelRect, "Oyna", 34f, Color.white);
        playLabel.fontStyle = FontStyles.Bold;

        // Karakter Seç butonu — Play ile aynı stil, hemen altında
        AddIntroButton(_introContentRect, "ChooseCharacterButton", "Karakter Seç",
            new Vector2(0f, -210f), HandleChooseCharacterPressed);
    }

    private Button AddIntroButton(RectTransform parent, string name, string label,
        Vector2 position, UnityEngine.Events.UnityAction action, Vector2 btnSize = default)
    {
        Vector2 size      = (btnSize == Vector2.zero) ? new Vector2(248f, 74f) : btnSize;
        float   fontScale = size.x / 248f;

        RectTransform rect = CreateRect(name, parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            position, size);
        Image img = rect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(img, new Color(0.9f, 0.24f, 0.56f, 1f));

        Shadow sh = rect.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.24f);
        sh.effectDistance = new Vector2(0f, -6f);

        Outline ol = rect.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.76f, 0.12f, 0.41f, 0.8f);
        ol.effectDistance = new Vector2(1f, -1f);

        Button btn = rect.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.95f, 0.98f, 1f);
        cb.pressedColor = new Color(0.92f, 0.84f, 0.9f, 1f);
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = new Color(1f, 1f, 1f, 0.6f);
        btn.colors = cb;

        // Label her yönden buton içinde boşluklu kalsın; uzun metinler otomatik küçülsün
        RectTransform lblRect = CreateRect("Label", rect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(size.x * 0.76f, size.y * 0.52f));
        TextMeshProUGUI lbl = CreateText(lblRect, label, 28f * fontScale, Color.white);
        lbl.fontStyle           = FontStyles.Bold;
        lbl.enableAutoSizing    = true;
        lbl.fontSizeMax         = 28f * fontScale;
        lbl.fontSizeMin         = 10f;
        lbl.enableWordWrapping  = false;

        return btn;
    }

    private void BuildMapPanel()
    {
        _mapPanel = CreateUiObject("MapPanel", _overlayRoot.transform);
        _mapPanelRect = _mapPanel.GetComponent<RectTransform>();

        _mapCardRect = CreateRect(
            "MapCard",
            _mapPanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -10f),
            new Vector2(680f, 1080f));
        Image mapCardImage = _mapCardRect.gameObject.AddComponent<Image>();
        if (_mapBackgroundSprite != null)
        {
            mapCardImage.sprite = _mapBackgroundSprite;
            mapCardImage.type = Image.Type.Simple;
            mapCardImage.preserveAspect = false;
        }
        mapCardImage.color = Color.white;

        // Görünür pencere — ScrollRect bunun üzerinde. Boyutu LayoutMapPanel ayarlar.
        _mapRouteWindowRect = CreateRect(
            "RouteScroll",
            _mapCardRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 24f),
            new Vector2(580f, 860f));

        // Viewport — pencereyi doldurur, RectMask2D ile içeriği kırpar, drag yakalar.
        _mapRouteViewportRect = CreateRect(
            "RouteViewport",
            _mapRouteWindowRect,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            Vector2.zero,
            Vector2.zero);
        StretchToParent(_mapRouteViewportRect);
        Image viewportCatcher = _mapRouteViewportRect.gameObject.AddComponent<Image>();
        viewportCatcher.color = new Color(1f, 1f, 1f, 0.001f); // görünmez ama raycast hedefi
        viewportCatcher.raycastTarget = true;
        _mapRouteViewportRect.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

        // İçerik — node'lar buraya kurulur; yüksekliği level sayısına göre RebuildMapRoute belirler.
        _mapRouteRect = CreateRect(
            "RouteContent",
            _mapRouteViewportRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(580f, 860f));

        _mapRouteScroll = _mapRouteWindowRect.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        _mapRouteScroll.viewport = _mapRouteViewportRect;
        _mapRouteScroll.content = _mapRouteRect;
        _mapRouteScroll.horizontal = false;
        _mapRouteScroll.vertical = true;
        _mapRouteScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
        _mapRouteScroll.elasticity = 0.08f;
        _mapRouteScroll.inertia = true;
        _mapRouteScroll.decelerationRate = 0.135f;
        _mapRouteScroll.scrollSensitivity = 28f;
    }

    private void BuildMapRoute(RectTransform routeRect, Vector2 viewportSize)
    {
        int levelCount = LevelManager.Instance != null
            ? LevelManager.Instance.SessionLevelCount
            : 1;

        // Node boyutu görünür pencerenin GENİŞLİĞİNE göre sabit (yüksekliğe sıkıştırılmaz).
        float nodeSize = CalculateNodeSize(viewportSize, levelCount);
        float starSize = Mathf.Clamp(nodeSize * 0.46f, 32f, 52f);
        float starSpacing = Mathf.Clamp(nodeSize * 0.055f, 5f, 9f);
        float starOffset = Mathf.Clamp((nodeSize * 0.5f) + (starSize * 0.5f) + (nodeSize * 0.2f), 50f, 86f);
        float ribbonThickness = Mathf.Clamp(nodeSize * 0.18f, 16f, 28f);

        // İçerik yüksekliği level sayısıyla büyür → kaydırılır.
        float verticalStep = nodeSize * 1.85f;
        float topMargin    = nodeSize * 1.0f;
        float bottomMargin = starOffset + (starSize * 0.5f) + (nodeSize * 0.35f);
        float contentHeight = Mathf.Max(
            viewportSize.y,
            topMargin + bottomMargin + Mathf.Max(0, levelCount - 1) * verticalStep);

        Vector2 contentSize = new Vector2(viewportSize.x, contentHeight);
        routeRect.sizeDelta = contentSize;
        routeRect.anchoredPosition = Vector2.zero;

        Vector2[] positions = BuildNodePositions(levelCount, contentSize, nodeSize, verticalStep, topMargin);

        for (int i = 1; i < positions.Length; i++)
            CreatePathRibbon(routeRect, positions[i - 1], positions[i], ribbonThickness);

        for (int i = 0; i < levelCount; i++)
        {
            Vector2 position = positions[i];
            RectTransform nodeRect = CreateRect(
                $"Level_{i + 1}",
                routeRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                new Vector2(nodeSize * 1.12f, nodeSize * 1.18f));

            Image hitArea = nodeRect.gameObject.AddComponent<Image>();
            hitArea.sprite = GetCircleSprite();
            hitArea.color = new Color(1f, 1f, 1f, 0.001f);
            hitArea.raycastTarget = true;

            Button nodeButton = nodeRect.gameObject.AddComponent<Button>();
            nodeButton.targetGraphic = hitArea;

            int capturedIndex = i;
            nodeButton.onClick.AddListener(() => HandleLevelSelected(capturedIndex));

            Image glowImage = CreateNodeLayer(nodeRect, "Glow", Vector2.zero, new Vector2(nodeSize * 1.36f, nodeSize * 1.36f), GetSoftCircleSprite(), new Color(1f, 1f, 1f, 0f));
            CreateNodeLayer(nodeRect, "Shadow", new Vector2(0f, -nodeSize * 0.18f), new Vector2(nodeSize * 0.92f, nodeSize * 0.34f), GetSoftCircleSprite(), new Color(0f, 0f, 0f, 0.22f));
            Image pedestalImage = CreateNodeLayer(nodeRect, "Pedestal", new Vector2(0f, -nodeSize * 0.11f), new Vector2(nodeSize * 0.98f, nodeSize * 0.34f), GetCircleSprite(), new Color(1f, 0.94f, 0.82f, 1f));
            Image rimImage = CreateNodeLayer(nodeRect, "Rim", new Vector2(0f, -nodeSize * 0.04f), new Vector2(nodeSize, nodeSize), GetCircleSprite(), Color.white);
            Image nodeImage = CreateNodeLayer(nodeRect, "Body", new Vector2(0f, 0f), new Vector2(nodeSize * 0.84f, nodeSize * 0.84f), GetCircleSprite(), GetNodeColor(i));
            Image shadeImage = CreateNodeLayer(nodeRect, "Shade", new Vector2(0f, -nodeSize * 0.09f), new Vector2(nodeSize * 0.74f, nodeSize * 0.5f), GetCircleSprite(), new Color(0f, 0f, 0f, 0.16f));
            Image highlightImage = CreateNodeLayer(nodeRect, "Highlight", new Vector2(-nodeSize * 0.14f, nodeSize * 0.12f), new Vector2(nodeSize * 0.34f, nodeSize * 0.24f), GetCircleSprite(), new Color(1f, 1f, 1f, 0.62f));

            RectTransform numberRect = CreateRect(
                "Number",
                nodeRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, nodeSize * 0.02f),
                new Vector2(nodeSize * 0.82f, nodeSize * 0.36f));
            TextMeshProUGUI numberText = CreateText(numberRect, (i + 1).ToString(), Mathf.Clamp(nodeSize * 0.32f, 25f, 44f), Color.white);
            numberText.fontStyle = FontStyles.Bold;

            Shadow numberShadow = numberRect.gameObject.AddComponent<Shadow>();
            numberShadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
            numberShadow.effectDistance = new Vector2(0f, -2f);

            StarRowWidget stars = CreateStarRow(
                routeRect,
                "Stars",
                position + new Vector2(0f, -starOffset),
                starSize,
                starSpacing);

            _levelButtons.Add(new LevelButtonWidget
            {
                LevelIndex = i,
                Button = nodeButton,
                NodeImage = nodeImage,
                NodeRimImage = rimImage,
                NodeShadeImage = shadeImage,
                NodeHighlightImage = highlightImage,
                NodePedestalImage = pedestalImage,
                NodeGlowImage = glowImage,
                NumberText = numberText,
                Stars = stars
            });
        }
    }

    private void BuildCharacterPanel()
    {
        _characterPanel = CreateUiObject("CharacterPanel", _overlayRoot.transform);
        _characterPanelRect = _characterPanel.GetComponent<RectTransform>();

        // contentRect kart + back butonunu barındıracak kadar geniş/uzun
        _characterContentRect = CreateRect(
            "CharacterContent",
            _characterPanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(760f, 1120f));

        RectTransform ropeRect = CreateRect(
            "Rope", _characterContentRect,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -34f), new Vector2(14f, 200f));
        Image ropeImage = ropeRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(ropeImage, new Color(0.48f, 0.33f, 0.21f, 0.92f));
        ropeImage.raycastTarget = false;

        // Kart ~%10 daha büyük: 540→600, 600→660; merkez (0,60) aynı
        RectTransform cardRect = CreateRect(
            "CharacterCard", _characterContentRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 72f), new Vector2(720f, 800f));
        Image cardImage = cardRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(cardImage, new Color(0.99f, 0.94f, 0.82f, 0.98f));
        cardImage.raycastTarget = false;

        Shadow cardShadow = cardRect.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        cardShadow.effectDistance = new Vector2(0f, -10f);

        Outline cardOutline = cardRect.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.72f, 0.54f, 0.24f, 0.8f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform holeRect = CreateRect(
            "CardHole", cardRect,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -34f), new Vector2(38f, 38f));
        Image holeImage = holeRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(holeImage, new Color(0.45f, 0.32f, 0.2f, 0.96f));
        holeImage.raycastTarget = false;

        // Title: kart genişliğine uygun
        RectTransform titleRect = CreateRect(
            "Title", cardRect,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(640f, 74f));
        TextMeshProUGUI titleText = CreateText(titleRect, "Karakter Seç", 58f, new Color(0.73f, 0.48f, 0.12f));
        titleText.fontStyle = FontStyles.Bold;

        Vector2 charBtnSize  = new Vector2(390f, 116f);
        Vector2 characterImagePosition = new Vector2(210f, 0f);
        Vector2 characterImageSize = new Vector2(190f, 190f);
        string[] characters  = { "Dog", "Cat", "Rabbit" };
        string[] characterLabels = { "Köpek", "Kedi", "Tavşan" };
        float[] yPositions   = { 206f, 24f, -158f };
        for (int i = 0; i < characters.Length; i++)
        {
            string captured = characters[i];
            AddIntroButton(_characterContentRect, $"{captured}Button", characterLabels[i],
                new Vector2(-120f, yPositions[i]),
                () => HandleCharacterSelected(captured),
                charBtnSize);

            if (captured == "Rabbit")
            {
                RectTransform rabbitImgRect = CreateRect(
                    "RabbitImage", _characterContentRect,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(characterImagePosition.x, yPositions[i]), characterImageSize);
                Image rabbitImg = rabbitImgRect.gameObject.AddComponent<Image>();
                rabbitImg.preserveAspect = true;
                var sprites = Resources.LoadAll<Sprite>("rabbit_final");
                if (sprites != null && sprites.Length > 0)
                {
                    rabbitImg.sprite = sprites[0];
                }
            }
            else if (captured == "Cat")
            {
                RectTransform catImgRect = CreateRect(
                    "CatImage", _characterContentRect,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(characterImagePosition.x, yPositions[i]), characterImageSize);
                Image catImg = catImgRect.gameObject.AddComponent<Image>();
                catImg.preserveAspect = true;
                var sprites = Resources.LoadAll<Sprite>("kedi_final");
                if (sprites != null && sprites.Length > 0)
                {
                    catImg.sprite = sprites[0];
                }
            }
            else if (captured == "Dog")
            {
                RectTransform dogImgRect = CreateRect(
                    "DogImage", _characterContentRect,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(characterImagePosition.x, yPositions[i]), characterImageSize);
                Image dogImg = dogImgRect.gameObject.AddComponent<Image>();
                dogImg.preserveAspect = true;
                var sprites = Resources.LoadAll<Sprite>("dog_greenscreen-removebg-preview");
                if (sprites != null && sprites.Length > 0)
                {
                    dogImg.sprite = sprites[0];
                }
            }
        }

        // Geri butonu kartın altında, büyük dokunma alanıyla durur.
        RectTransform backRect = CreateRect(
            "BackButton", _characterContentRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -402f), new Vector2(330f, 92f));
        Image backImg = backRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(backImg, new Color(0.55f, 0.55f, 0.6f, 0.9f));
        Button backBtn = backRect.gameObject.AddComponent<Button>();
        backBtn.targetGraphic = backImg;
        backBtn.onClick.AddListener(() => SetOverlayState(OverlayState.Intro));
        RectTransform backLblRect = CreateRect("Label", backRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(280f, 54f));
        TextMeshProUGUI backLbl = CreateText(backLblRect, "Geri", 44f, Color.white);
        backLbl.fontStyle = FontStyles.Bold;
    }

    private void BuildCompletePanel()
    {
        _completePanel = CreateUiObject("CompletePanel", _overlayRoot.transform);
        _completePanelRect = _completePanel.GetComponent<RectTransform>();
        Image completeBackdropImage = _completePanel.AddComponent<Image>();
        completeBackdropImage.color = new Color(0.03f, 0.04f, 0.08f, 0.34f);
        completeBackdropImage.raycastTarget = true;

        _completeCardRect = CreateRect(
            "CompleteCard",
            _completePanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(420f, 360f));
        Image cardImage = _completeCardRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(cardImage, new Color(1f, 0.99f, 0.95f, 0.98f));

        Shadow cardShadow = _completeCardRect.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0.2f, 0.18f, 0.12f, 0.2f);
        cardShadow.effectDistance = new Vector2(0f, -12f);

        _completeContentRect = CreateRect(
            "CompleteContent",
            _completeCardRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(420f, 360f));

        _completeTitleRect = CreateRect(
            "Title",
            _completeContentRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -48f),
            new Vector2(320f, 48f));
        _completeTitleText = CreateText(_completeTitleRect, "KİŞİSEL REKOR: 00:00", 44f, new Color(0.53f, 0.3f, 0f));
        _completeTitleText.fontStyle = FontStyles.Bold;
        _completeTitleText.enableAutoSizing = true;
        _completeTitleText.fontSizeMax = 44f;
        _completeTitleText.fontSizeMin = 28f;
        _completeTitleText.enableWordWrapping = false;
        _completeTitleText.characterSpacing = 0f;

        _completeStarsRect = CreateRect(
            "Stars",
            _completeCardRect,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 48f),
            new Vector2(300f, 120f));
        _completeStarsWidget = CreateCompleteStarCluster(_completeStarsRect);

        _completeTimeRect = CreateRect(
            "PersonalBestLabel",
            _completeContentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -18f),
            new Vector2(320f, 32f));
        _completeTimeText = CreateText(_completeTimeRect, "Şu Anki Süre: 00:00", 34f, new Color(0.38f, 0.35f, 0.26f));
        _completeTimeText.fontStyle = FontStyles.Bold;
        _completeTimeText.enableAutoSizing = true;
        _completeTimeText.fontSizeMax = 34f;
        _completeTimeText.fontSizeMin = 22f;
        _completeTimeText.enableWordWrapping = false;
        _completeTimeText.characterSpacing = 0f;
        _completeTimeRect.gameObject.SetActive(false);

        _completeBestRect = CreateRect(
            "PersonalBest",
            _completeContentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -58f),
            new Vector2(340f, 48f));

        _completeRewardIconRect = CreateRect(
            "BestIcon",
            _completeBestRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-52f, 0f),
            new Vector2(54f, 54f));
        Image rewardIconImage = _completeRewardIconRect.gameObject.AddComponent<Image>();
        rewardIconImage.sprite = GetCircleSprite();
        rewardIconImage.color = new Color(0.99f, 0.75f, 0.02f, 1f);
        rewardIconImage.raycastTarget = false;

        Shadow rewardIconShadow = _completeRewardIconRect.gameObject.AddComponent<Shadow>();
        rewardIconShadow.effectColor = new Color(0.2f, 0.16f, 0.05f, 0.2f);
        rewardIconShadow.effectDistance = new Vector2(0f, -4f);

        RectTransform rewardIconLabelRect = CreateRect(
            "Label",
            _completeRewardIconRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(48f, 30f));
        _completeRewardIconText = CreateText(rewardIconLabelRect, "PB", 18f, Color.white);
        _completeRewardIconText.fontStyle = FontStyles.Bold;

        RectTransform bestValueRect = CreateRect(
            "BestValue",
            _completeBestRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(36f, 0f),
            new Vector2(190f, 64f));
        _completeBestTimeText = CreateText(bestValueRect, "00:00", 42f, new Color(0.54f, 0.3f, 0.06f));
        _completeBestTimeText.fontStyle = FontStyles.Bold;
        _completeBestTimeText.alignment = TextAlignmentOptions.Left;
        _completeBestRect.gameObject.SetActive(false);

        CreateActionButton(_completeContentRect, "MapButton", "Harita", new Vector2(-118f, -118f), HandleMapPressed, out _completeMapButtonRect, out _completeMapButtonText);
        CreateActionButton(_completeContentRect, "ReplayButton", "Tekrar", new Vector2(0f, -118f), HandleReplayPressed, out _completeReplayButtonRect, out _completeReplayButtonText);
        _nextButton = CreateActionButton(_completeContentRect, "NextButton", "Sonraki", new Vector2(118f, -118f), HandleNextPressed, out _completeNextButtonRect, out _completeNextButtonText);
    }

    private Button CreateActionButton(
        RectTransform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction action,
        out RectTransform buttonRect,
        out TextMeshProUGUI labelText)
    {
        buttonRect = CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            anchoredPosition,
            new Vector2(104f, 62f));
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        GetCompleteButtonStyle(label, out Color fillColor, out Color outlineColor, out Color textColor);
        ApplyPanelSprite(buttonImage, fillColor);

        Shadow buttonShadow = buttonRect.gameObject.AddComponent<Shadow>();
        buttonShadow.effectColor = new Color(0.18f, 0.14f, 0.08f, 0.22f);
        buttonShadow.effectDistance = new Vector2(0f, -7f);

        Outline buttonOutline = buttonRect.gameObject.AddComponent<Outline>();
        buttonOutline.effectColor = outlineColor;
        buttonOutline.effectDistance = new Vector2(3f, -3f);

        RectTransform glossRect = CreateRect(
            "Gloss",
            buttonRect,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        Image glossImage = glossRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(glossImage, new Color(1f, 1f, 1f, 0.18f));
        glossImage.raycastTarget = false;

        RectTransform shadeRect = CreateRect(
            "BottomShade",
            buttonRect,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 12f));
        Image shadeImage = shadeRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(shadeImage, new Color(0f, 0f, 0f, 0.11f));
        shadeImage.raycastTarget = false;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(action);

        RectTransform labelRect = CreateRect(
            "Label",
            buttonRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(92f, 32f));
        labelText = CreateText(labelRect, label, 22f, textColor);
        labelText.fontStyle = FontStyles.Bold;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMax = 22f;
        labelText.fontSizeMin = 14f;
        labelText.enableWordWrapping = false;

        return button;
    }

    private static void GetCompleteButtonStyle(string label, out Color fillColor, out Color outlineColor, out Color textColor)
    {
        switch (label)
        {
            case "Map":
            case "Harita":
                fillColor = new Color(0.89f, 0.87f, 0.75f, 1f);
                outlineColor = new Color(0.48f, 0.47f, 0.39f, 0.96f);
                textColor = new Color(0.2f, 0.18f, 0.13f, 1f);
                return;

            case "Retry":
            case "Tekrar":
                fillColor = new Color(0.99f, 0.75f, 0.02f, 1f);
                outlineColor = new Color(0.33f, 0.24f, 0f, 0.96f);
                textColor = Color.white;
                return;

            default:
                fillColor = new Color(0.36f, 0.9f, 0.34f, 1f);
                outlineColor = new Color(0f, 0.37f, 0.09f, 0.96f);
                textColor = Color.white;
                return;
        }
    }

    private void ShowIntro()
    {
        RefreshMapPanel();
        SetOverlayState(OverlayState.Intro);
    }

    private void ShowLevelMap()
    {
        RefreshMapPanel();
        SetOverlayState(OverlayState.Map);
    }

    private void SetOverlayState(OverlayState state)
    {
        _overlayState = state;
        if (state != OverlayState.Complete)
            StopCompletePanelIntro();

        // Map'i her açışta yeniden kurma; guard (boyut değişti / henüz kurulmadı) karar verir.
        RefreshResponsiveLayout(forceRouteRebuild: false);

        bool overlayVisible = state != OverlayState.Hidden;
        if (_overlayRoot != null)
            _overlayRoot.SetActive(overlayVisible);

        if (_introPanel != null)
            _introPanel.SetActive(state == OverlayState.Intro);

        if (_mapPanel != null)
            _mapPanel.SetActive(state == OverlayState.Map);

        if (_completePanel != null)
        {
            _completePanel.SetActive(state == OverlayState.Complete);
            if (state == OverlayState.Complete)
                PlayCompletePanelIntro();
        }

        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);

        bool showSettings = state == OverlayState.Intro || state == OverlayState.Map;
        if (_settingsButton != null)
            _settingsButton.gameObject.SetActive(showSettings);

        if (_characterPanel != null)
            _characterPanel.SetActive(state == OverlayState.CharacterSelect);

        switch (state)
        {
            case OverlayState.Intro:
            case OverlayState.Map:
            case OverlayState.CharacterSelect:
            {
                _shouldBlockAutoStart = true;
                SetGameplayVisible(false);
                var menuClip = Resources.Load<AudioClip>("MenuMusic");
                if (menuClip != null)
                    AudioManager.Instance?.PlayMusicClip(menuClip);
                else
                    AudioManager.Instance?.PlayMainMenuMusic();
                break;
            }

            case OverlayState.Complete:
                _shouldBlockAutoStart = true;
                SetGameplayVisible(true);
                break;

            case OverlayState.Hidden:
            {
                _shouldBlockAutoStart = false;
                SetGameplayVisible(true);
                var gp1 = Resources.Load<AudioClip>("Gameplay1");
                var gp2 = Resources.Load<AudioClip>("Gameplay2");
                if (gp1 != null && gp2 != null)
                    AudioManager.Instance?.StartGameplayLoop(gp1, gp2);
                else
                    AudioManager.Instance?.PlayGameplayMusic();
                break;
            }
        }
    }

    private void HandlePlayPressed()
    {
        ShowLevelMap();
    }

    private void HandleChooseCharacterPressed()
    {
        SetOverlayState(OverlayState.CharacterSelect);
    }

    private void HandleCharacterSelected(string character)
    {
        CharacterManager.CharacterType type = CharacterManager.CharacterType.Dog;
        if (character == "Rabbit") type = CharacterManager.CharacterType.Rabbit;
        else if (character == "Cat") type = CharacterManager.CharacterType.Cat;

        CharacterManager.Select(type);

        // Oyun zaten çalışıyorsa görselleri hemen güncelle
        if (_playerToken != null)
        {
            _playerToken.ApplyCharacter();
            if (_gridManager != null)
                _playerToken.FitToCell(_gridManager.CellSize);
        }

        if (_gridManager != null)
            _gridManager.RefreshEndCellItem();

        SetOverlayState(OverlayState.Intro);
    }

    public void OpenLevelMap()
    {
        CacheSceneReferences();
        if (_canvas == null)
            return;

        BuildOverlay();
        ShowLevelMap();
    }

    private void HandleLevelSelected(int levelIndex)
    {
        if (LevelManager.Instance == null || !LevelManager.Instance.IsLevelUnlocked(levelIndex))
            return;

        SetGameplayVisible(true);
        if (!LevelManager.Instance.LoadLevel(levelIndex))
        {
            SetGameplayVisible(false);
            return;
        }

        SetOverlayState(OverlayState.Hidden);
    }

    private void HandleMapPressed()
    {
        ShowLevelMap();
    }

    private void HandleReplayPressed()
    {
        SetGameplayVisible(true);
        if (LevelManager.Instance != null && LevelManager.Instance.ReloadActiveLevel())
        {
            SetOverlayState(OverlayState.Hidden);
            return;
        }

        SetGameplayVisible(false);
    }

    private void HandleNextPressed()
    {
        if (LevelManager.Instance == null)
            return;

        if (!LevelManager.Instance.TryGetNextUnlockedLevelIndex(_lastResult.LevelIndex, out int nextLevelIndex))
            return;

        SetGameplayVisible(true);
        if (LevelManager.Instance.LoadLevel(nextLevelIndex))
        {
            SetOverlayState(OverlayState.Hidden);
            return;
        }

        SetGameplayVisible(false);
    }

    private void HandleLevelResult(LevelCompletionResult result)
    {
        _lastResult = result;
        RefreshMapPanel();
        UpdateCompletePanel(result);
        SetOverlayState(OverlayState.Complete);
    }

    private void UpdateCompletePanel(LevelCompletionResult result)
    {
        string bestTime = FormatTime(float.IsPositiveInfinity(result.BestTimeSeconds)
            ? result.ElapsedSeconds
            : result.BestTimeSeconds);
        string currentTime = FormatTime(result.ElapsedSeconds);

        if (_completeTitleText != null)
            _completeTitleText.text = $"KİŞİSEL REKOR: {bestTime}";

        if (_completeTimeText != null)
            _completeTimeText.text = $"ŞU ANKİ SÜRE: {currentTime}";

        if (_completeTimeRect != null)
            _completeTimeRect.gameObject.SetActive(true);

        UpdateStarRow(_completeStarsWidget, result.StarsEarned, false);

        if (_nextButton != null)
            _nextButton.gameObject.SetActive(true);
    }

    private void RefreshMapPanel()
    {
        if (_levelButtons.Count == 0 || LevelManager.Instance == null)
            return;

        int highlightedLevelIndex = GetHighlightedLevelIndex();
        foreach (LevelButtonWidget widget in _levelButtons)
        {
            int levelIndex = widget.LevelIndex;
            bool unlocked = LevelManager.Instance.IsLevelUnlocked(levelIndex);
            int bestStars = LevelManager.Instance.GetBestStars(levelIndex);
            bool highlighted = levelIndex == highlightedLevelIndex;

            widget.Button.interactable = unlocked;
            widget.NumberText.text = unlocked ? (levelIndex + 1).ToString() : "?";
            UpdateStarRow(widget.Stars, bestStars, !unlocked);
            ApplyLevelNodeVisual(widget, levelIndex, unlocked, highlighted);
        }

    }

    private void SetGameplayVisible(bool visible)
    {
        if (_playAreaRoot != null)
            _playAreaRoot.gameObject.SetActive(visible);

        if (_gridManager != null)
            _gridManager.gameObject.SetActive(visible);

        if (_playerToken != null)
            _playerToken.gameObject.SetActive(visible);

        if (_swipeInput != null && !visible)
            _swipeInput.SetInputEnabled(false);
    }

    private void PlayCompletePanelIntro()
    {
        if (_completeCardRect == null || !isActiveAndEnabled)
            return;

        StopCompletePanelIntro();

        _completePanelAnimation = StartCoroutine(AnimateCompletePanelIntro());
    }

    private void StopCompletePanelIntro()
    {
        if (_completePanelAnimation == null)
            return;

        StopCoroutine(_completePanelAnimation);
        _completePanelAnimation = null;

        if (_completeCardRect != null)
            _completeCardRect.localScale = Vector3.one;
    }

    private IEnumerator AnimateCompletePanelIntro()
    {
        Vector2 targetPosition = _completeCardRect.anchoredPosition;
        Vector2 startPosition = targetPosition + new Vector2(0f, -180f);
        Vector3 startScale = new Vector3(0.88f, 0.88f, 1f);
        const float duration = 0.34f;
        float elapsed = 0f;

        _completeCardRect.anchoredPosition = startPosition;
        _completeCardRect.localScale = startScale;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t);
            _completeCardRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
            _completeCardRect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eased);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _completeCardRect.anchoredPosition = targetPosition;
        _completeCardRect.localScale = Vector3.one;
        _completePanelAnimation = null;
    }

    private void TeardownOverlay()
    {
        DetachGameManagerEvents();
        StopCompletePanelIntro();

        if (_overlayRoot != null)
            Destroy(_overlayRoot);

        _overlayRoot = null;
        _introPanel = null;
        _mapPanel = null;
        _completePanel = null;
        _characterPanel = null;
        _introPanelRect = null;
        _mapPanelRect = null;
        _completePanelRect = null;
        _characterPanelRect = null;
        _introContentRect = null;
        _characterContentRect = null;
        _mapCardRect = null;
        _mapRouteWindowRect = null;
        _mapRouteViewportRect = null;
        _mapRouteRect = null;
        _mapRouteScroll = null;
        _completeCardRect = null;
        _completeContentRect = null;
        _completeTitleRect = null;
        _completeStarsRect = null;
        _completeTimeRect = null;
        _completeBestRect = null;
        _completeRewardIconRect = null;
        _completeMapButtonRect = null;
        _completeReplayButtonRect = null;
        _completeNextButtonRect = null;
        _canvas = null;
        _playAreaRoot = null;
        _gridManager = null;
        _playerToken = null;
        _swipeInput = null;
        _gameManager = null;
        _completeTitleText = null;
        _completeTimeText = null;
        _completeBestText = null;
        _completeBestTimeText = null;
        _completeRewardIconText = null;
        _completeMapButtonText = null;
        _completeReplayButtonText = null;
        _completeNextButtonText = null;
        _nextButton = null;
        _completeStarsWidget = null;
        _completeBestStarsWidget = null;
        _settingsPanel = null;
        _settingsButton = null;
        _settingsButtonRect = null;
        _lastCanvasSize = Vector2.negativeInfinity;
        _lastRouteSize = Vector2.negativeInfinity;
        _levelButtons.Clear();
    }

    private void ApplyPanelSprite(Image image, Color color)
    {
        if (_panelSprite != null)
        {
            image.sprite = _panelSprite;
            image.type = Image.Type.Sliced;
        }

        image.color = color;
    }

    private void ApplyNodeSprite(Image image, Color color)
    {
        if (_roundButtonSprite != null)
        {
            image.sprite = _roundButtonSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
        else if (_panelSprite != null)
        {
            image.sprite = _panelSprite;
            image.type = Image.Type.Sliced;
        }

        image.color = color;
    }

    private Image CreateNodeLayer(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Sprite sprite, Color color)
    {
        RectTransform layerRect = CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            anchoredPosition,
            sizeDelta);

        Image image = layerRect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void ApplyLevelNodeVisual(LevelButtonWidget widget, int levelIndex, bool unlocked, bool highlighted)
    {
        if (widget == null)
            return;

        Color baseColor = GetNodeColor(levelIndex);
        Color faceColor = unlocked
            ? baseColor
            : new Color(0.98f, 0.99f, 1f, 1f);
        Color rimColor = unlocked
            ? Color.Lerp(baseColor, Color.white, 0.82f)
            : new Color(0.96f, 0.97f, 0.99f, 1f);
        Color pedestalColor = unlocked
            ? Color.Lerp(baseColor, new Color(1f, 0.95f, 0.84f, 1f), 0.78f)
            : new Color(0.98f, 0.95f, 0.88f, 1f);
        Color shadeColor = unlocked
            ? Color.Lerp(baseColor, new Color(0.28f, 0.12f, 0.22f, 1f), 0.28f)
            : new Color(0.88f, 0.9f, 0.94f, 0.95f);
        Color highlightColor = unlocked
            ? new Color(1f, 1f, 1f, 0.56f)
            : new Color(1f, 1f, 1f, 0.86f);
        Color glowColor = highlighted
            ? new Color(baseColor.r, baseColor.g, baseColor.b, unlocked ? 0.24f : 0.14f)
            : new Color(1f, 1f, 1f, 0f);
        Color numberColor = unlocked
            ? Color.white
            : new Color(0.68f, 0.73f, 0.79f, 1f);

        if (widget.NodeRimImage != null)
            widget.NodeRimImage.color = rimColor;

        if (widget.NodeImage != null)
            widget.NodeImage.color = faceColor;

        if (widget.NodePedestalImage != null)
            widget.NodePedestalImage.color = pedestalColor;

        if (widget.NodeShadeImage != null)
            widget.NodeShadeImage.color = shadeColor;

        if (widget.NodeHighlightImage != null)
            widget.NodeHighlightImage.color = highlightColor;

        if (widget.NodeGlowImage != null)
            widget.NodeGlowImage.color = glowColor;

        if (widget.NumberText != null)
            widget.NumberText.color = numberColor;
    }

    private int GetHighlightedLevelIndex()
    {
        if (LevelManager.Instance == null)
            return -1;

        if (LevelManager.Instance.ActiveLevelIndex >= 0 && LevelManager.Instance.IsLevelUnlocked(LevelManager.Instance.ActiveLevelIndex))
            return LevelManager.Instance.ActiveLevelIndex;

        int highestUnlocked = -1;
        for (int i = 0; i < LevelManager.Instance.SessionLevelCount; i++)
        {
            if (LevelManager.Instance.IsLevelUnlocked(i))
                highestUnlocked = i;
        }

        return highestUnlocked;
    }

    private void CreatePathRibbon(RectTransform parent, Vector2 from, Vector2 to, float thickness)
    {
        float ribbonLength = Vector2.Distance(from, to);
        RectTransform ribbonRect = CreateRect(
            "RouteRibbon",
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            (from + to) * 0.5f,
            new Vector2(ribbonLength, thickness * 1.45f));

        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        ribbonRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image ribbonShadowImage = CreateNodeLayer(ribbonRect, "Shadow", new Vector2(0f, -thickness * 0.16f), new Vector2(ribbonLength, thickness * 1.18f), GetSoftCircleSprite(), new Color(0f, 0f, 0f, 0.12f));
        ribbonShadowImage.rectTransform.localScale = new Vector3(1f, 0.56f, 1f);

        CreateNodeLayer(ribbonRect, "Base", Vector2.zero, new Vector2(ribbonLength, thickness * 1.04f), null, Color.white);
        CreateNodeLayer(ribbonRect, "Inner", Vector2.zero, new Vector2(ribbonLength, thickness * 0.72f), null, new Color(1f, 0.76f, 0.87f, 1f));

        float stripeSpacing = thickness * 1.08f;
        float stripeWidth = thickness * 0.38f;
        float stripeHeight = thickness * 1.34f;
        int stripeCount = Mathf.CeilToInt(ribbonLength / stripeSpacing) + 3;
        float startX = -(ribbonLength * 0.5f) - stripeSpacing;
        for (int i = 0; i < stripeCount; i++)
        {
            Image stripeImage = CreateNodeLayer(
                ribbonRect,
                $"Stripe_{i + 1}",
                new Vector2(startX + (stripeSpacing * i), 0f),
                new Vector2(stripeWidth, stripeHeight),
                null,
                new Color(0.98f, 0.45f, 0.72f, 0.96f));
            stripeImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        }

        float capSize = thickness * 1.06f;
        CreateNodeLayer(ribbonRect, "CapStartBase", new Vector2(-(ribbonLength * 0.5f), 0f), new Vector2(capSize, capSize), GetCircleSprite(), Color.white);
        CreateNodeLayer(ribbonRect, "CapEndBase", new Vector2(ribbonLength * 0.5f, 0f), new Vector2(capSize, capSize), GetCircleSprite(), Color.white);
        CreateNodeLayer(ribbonRect, "CapStartInner", new Vector2(-(ribbonLength * 0.5f), 0f), new Vector2(thickness * 0.72f, thickness * 0.72f), GetCircleSprite(), new Color(1f, 0.76f, 0.87f, 1f));
        CreateNodeLayer(ribbonRect, "CapEndInner", new Vector2(ribbonLength * 0.5f, 0f), new Vector2(thickness * 0.72f, thickness * 0.72f), GetCircleSprite(), new Color(1f, 0.76f, 0.87f, 1f));
    }

    /// <summary>
    /// Node'ları içerik (content) alanına yukarıdan aşağıya, sabit dikey adımla yerleştirir.
    /// Level 1 en üstte; yatayda yumuşak zikzak. Konumlar content merkezine görelidir.
    /// </summary>
    private static Vector2[] BuildNodePositions(int levelCount, Vector2 contentSize, float nodeSize, float verticalStep, float topMargin)
    {
        var result = new Vector2[levelCount];
        float horizontalLimit = Mathf.Max(0f, (contentSize.x * 0.5f) - (nodeSize * 0.72f));
        float top = contentSize.y * 0.5f - topMargin; // content merkezine göre üst nokta

        for (int i = 0; i < levelCount; i++)
        {
            float wave = Mathf.Sin(i * 0.62f);
            float x = Mathf.Clamp(wave * contentSize.x * 0.34f, -horizontalLimit, horizontalLimit);
            float y = top - (verticalStep * i);
            result[i] = new Vector2(x, y);
        }

        return result;
    }

    private static float CalculateNodeSize(Vector2 viewportSize, int levelCount)
    {
        // Sadece genişliğe bağlı — kaydırma sayesinde yüksekliğe sıkıştırmaya gerek yok.
        float widthDriven = viewportSize.x * 0.27f;
        return Mathf.Clamp(widthDriven, 80f, 132f);
    }

    private void RefreshResponsiveLayoutIfNeeded()
    {
        if (_canvas == null || _overlayRoot == null)
            return;

        RectTransform canvasRect = _canvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Vector2 canvasSize = canvasRect.rect.size;
        if (canvasSize.x <= 0f || canvasSize.y <= 0f)
            return;

        if ((_lastCanvasSize - canvasSize).sqrMagnitude > 1f)
            RefreshResponsiveLayout(forceRouteRebuild: true);
    }

    private void RefreshResponsiveLayout(bool forceRouteRebuild)
    {
        if (_canvas == null)
            return;

        RectTransform canvasRect = _canvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Vector2 canvasSize = canvasRect.rect.size;
        if (canvasSize.x <= 0f || canvasSize.y <= 0f)
            return;

        _lastCanvasSize = canvasSize;

        Rect safeAreaRect = GetSafeAreaRectInCanvasSpace(canvasRect);
        if (safeAreaRect.width <= 0f || safeAreaRect.height <= 0f)
            safeAreaRect = new Rect(Vector2.zero, canvasSize);

        ApplySafeAreaRect(_introPanelRect, safeAreaRect);
        ApplySafeAreaRect(_mapPanelRect, safeAreaRect);
        ApplySafeAreaRect(_completePanelRect, safeAreaRect);
        ApplySafeAreaRect(_characterPanelRect, safeAreaRect);
        LayoutSettingsButton(safeAreaRect);

        bool portrait = safeAreaRect.height >= safeAreaRect.width;
        LayoutIntroPanel(safeAreaRect.size);
        LayoutCharacterPanel(safeAreaRect.size);
        LayoutMapPanel(safeAreaRect.size, portrait, forceRouteRebuild);
        LayoutCompletePanel(safeAreaRect.size);
    }

    private void LayoutIntroPanel(Vector2 safeAreaSize)
    {
        if (_introContentRect == null)
            return;

        float introScale = ComputeResponsiveScale(safeAreaSize, new Vector2(360f, 660f), 1f, 1.85f, 0.62f);
        float introFitScale = ComputeUniformScale(safeAreaSize, new Vector2(360f, 640f), 0.85f, 2f);
        introScale = Mathf.Min(introScale, introFitScale);
        _introContentRect.anchoredPosition = new Vector2(0f, safeAreaSize.y * 0.015f);
        _introContentRect.localScale = new Vector3(introScale, introScale, 1f);
    }

    private void LayoutCharacterPanel(Vector2 safeAreaSize)
    {
        if (_characterContentRect == null)
            return;

        float characterScale = ComputeResponsiveScale(safeAreaSize, new Vector2(360f, 660f), 1f, 1.22f, 0.62f);
        _characterContentRect.anchoredPosition = new Vector2(0f, safeAreaSize.y * 0.01f);
        _characterContentRect.localScale = new Vector3(characterScale, characterScale, 1f);
    }

    private void LayoutMapPanel(Vector2 safeAreaSize, bool portrait, bool forceRouteRebuild)
    {
        if (_mapCardRect == null || _mapRouteWindowRect == null || _mapRouteRect == null)
            return;

        float layoutScale = ComputeResponsiveScale(safeAreaSize, new Vector2(360f, 700f), 1f, 1.8f, 0.58f);
        float cardWidth = safeAreaSize.x;
        float cardHeight = safeAreaSize.y;

        _mapCardRect.anchoredPosition = Vector2.zero;
        _mapCardRect.sizeDelta = safeAreaSize;

        float sidePadding = Mathf.Clamp(cardWidth * (portrait ? 0.08f : 0.07f), 16f * layoutScale, 44f * layoutScale);
        float routeTopPadding = Mathf.Clamp(36f * layoutScale, 24f, 60f);
        float routeBottomPadding = Mathf.Clamp(42f * layoutScale, 28f, 72f);

        // Görünür kaydırma penceresini kart içine yerleştir.
        _mapRouteWindowRect.anchoredPosition = new Vector2(0f, (routeBottomPadding - routeTopPadding) * 0.5f);
        _mapRouteWindowRect.sizeDelta = new Vector2(
            Mathf.Max(240f, cardWidth - (sidePadding * 2f)),
            Mathf.Max(320f, cardHeight - routeTopPadding - routeBottomPadding));

        Canvas.ForceUpdateCanvases();

        Vector2 viewportSize = _mapRouteViewportRect.rect.size;
        if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            return;

        int expectedCount = LevelManager.Instance != null ? LevelManager.Instance.SessionLevelCount : 0;
        bool routeSizeChanged = (_lastRouteSize - viewportSize).sqrMagnitude > 1f;
        bool needBuild = forceRouteRebuild || routeSizeChanged || _levelButtons.Count != expectedCount;
        if (!needBuild)
        {
            // Yeniden kurmaya gerek yok; sadece aktif level'a kaydır (ör. map'e geri dönüş).
            ScrollToActiveLevel();
            return;
        }

        RebuildMapRoute(viewportSize);
    }

    private void LayoutCompletePanel(Vector2 safeAreaSize)
    {
        if (_completeCardRect == null || _completeContentRect == null)
            return;

        float cardScale = ComputeResponsiveScale(safeAreaSize, new Vector2(390f, 780f), 1.72f, 2.65f, 0.64f);
        float width = Mathf.Clamp(safeAreaSize.x * 1.34f, 540f, 820f);
        float height = Mathf.Clamp(520f * cardScale, 640f, 900f);
        float innerWidth = Mathf.Max(430f, width - (76f * cardScale));
        float innerHeight = Mathf.Max(470f, height - (76f * cardScale));
        float typographyScale = ComputeResponsiveScale(new Vector2(innerWidth, innerHeight), new Vector2(430f, 470f), 1.44f, 2.25f, 0.58f);
        float buttonGap = Mathf.Clamp(16f * typographyScale, 18f, 34f);
        float buttonWidth = Mathf.Clamp((innerWidth - (buttonGap * 2f)) / 3f, 148f, 230f);
        float buttonHeight = Mathf.Clamp(84f * typographyScale, 108f, 150f);
        float buttonY = -(innerHeight * 0.36f);

        _completeCardRect.anchoredPosition = Vector2.zero;
        _completeCardRect.sizeDelta = new Vector2(width, height);
        _completeContentRect.anchoredPosition = Vector2.zero;
        _completeContentRect.sizeDelta = new Vector2(innerWidth, innerHeight);
        _completeContentRect.localScale = Vector3.one;

        if (_completeTitleRect != null)
        {
            _completeTitleRect.anchoredPosition = new Vector2(0f, -130f * typographyScale);
            _completeTitleRect.sizeDelta = new Vector2(innerWidth * 0.96f, 92f * typographyScale);
        }

        if (_completeTitleText != null)
        {
            _completeTitleText.fontSize = Mathf.Clamp(36f * typographyScale, 46f, 74f);
            _completeTitleText.fontSizeMax = Mathf.Clamp(36f * typographyScale, 46f, 74f);
        }

        if (_completeStarsRect != null)
        {
            _completeStarsRect.anchoredPosition = new Vector2(0f, Mathf.Clamp(86f * typographyScale, 108f, 160f));
            _completeStarsRect.sizeDelta = new Vector2(width, Mathf.Clamp(182f * typographyScale, 210f, 320f));
        }

        ApplyCompleteStarClusterLayout(_completeStarsWidget, Mathf.Clamp(164f * typographyScale, 190f, 280f), Mathf.Clamp(12f * typographyScale, 14f, 24f));

        if (_completeTimeRect != null)
        {
            _completeTimeRect.anchoredPosition = new Vector2(0f, (innerHeight * 0.5f) - (196f * typographyScale));
            _completeTimeRect.sizeDelta = new Vector2(innerWidth * 0.96f, 76f * typographyScale);
        }

        if (_completeTimeText != null)
        {
            _completeTimeText.fontSize = Mathf.Clamp(30f * typographyScale, 38f, 62f);
            _completeTimeText.fontSizeMax = Mathf.Clamp(30f * typographyScale, 38f, 62f);
        }

        if (_completeBestRect != null)
        {
            _completeBestRect.anchoredPosition = new Vector2(0f, -(innerHeight * 0.145f));
            _completeBestRect.sizeDelta = new Vector2(innerWidth * 0.86f, 74f * typographyScale);
        }

        float iconSize = Mathf.Clamp(52f * typographyScale, 52f, 82f);
        if (_completeRewardIconRect != null)
        {
            _completeRewardIconRect.anchoredPosition = new Vector2(-(iconSize * 0.82f), 0f);
            _completeRewardIconRect.sizeDelta = new Vector2(iconSize, iconSize);
        }

        if (_completeRewardIconText != null)
            _completeRewardIconText.fontSize = Mathf.Clamp(18f * typographyScale, 18f, 30f);

        if (_completeBestTimeText != null)
        {
            _completeBestTimeText.fontSize = Mathf.Clamp(43f * typographyScale, 43f, 72f);
            _completeBestTimeText.rectTransform.anchoredPosition = new Vector2(iconSize * 0.34f, 0f);
            _completeBestTimeText.rectTransform.sizeDelta = new Vector2(innerWidth * 0.5f, 76f * typographyScale);
        }

        ApplyCompleteButtonLayout(_completeMapButtonRect, _completeMapButtonText, new Vector2(-(buttonWidth + buttonGap), buttonY), buttonWidth, buttonHeight, typographyScale);
        ApplyCompleteButtonLayout(_completeReplayButtonRect, _completeReplayButtonText, new Vector2(0f, buttonY), buttonWidth, buttonHeight, typographyScale);
        ApplyCompleteButtonLayout(_completeNextButtonRect, _completeNextButtonText, new Vector2(buttonWidth + buttonGap, buttonY), buttonWidth, buttonHeight, typographyScale);
    }

    private void RebuildMapRoute(Vector2 viewportSize)
    {
        if (_mapRouteRect == null)
            return;

        for (int i = _mapRouteRect.childCount - 1; i >= 0; i--)
            Destroy(_mapRouteRect.GetChild(i).gameObject);

        _levelButtons.Clear();
        _lastRouteSize = viewportSize;
        BuildMapRoute(_mapRouteRect, viewportSize);
        RefreshMapPanel();
        ScrollToActiveLevel();
    }

    /// <summary>
    /// İçeriği, oynanacak (aktif / en son açılmış) level görünür olacak şekilde kaydırır.
    /// </summary>
    private void ScrollToActiveLevel()
    {
        if (_mapRouteScroll == null || _mapRouteViewportRect == null || _mapRouteRect == null)
            return;

        int levelCount = _levelButtons.Count;
        if (levelCount <= 0) return;

        // Hedef level: aktif varsa o, yoksa en son açılmış (kilitsiz) level.
        int target = 0;
        if (LevelManager.Instance != null)
        {
            if (LevelManager.Instance.ActiveLevelIndex >= 0)
                target = LevelManager.Instance.ActiveLevelIndex;
            else
                for (int i = 0; i < levelCount; i++)
                    if (LevelManager.Instance.IsLevelUnlocked(i)) target = i;
        }

        Canvas.ForceUpdateCanvases();

        float contentH  = _mapRouteRect.rect.height;
        float viewportH = _mapRouteViewportRect.rect.height;
        float scrollable = contentH - viewportH;
        if (scrollable <= 1f)
        {
            _mapRouteScroll.verticalNormalizedPosition = 1f;
            return;
        }

        // Node'un content üstünden uzaklığı → normalize edilmiş scroll konumu.
        // BuildNodePositions ile aynı geometri: üstten verticalStep adımlarla.
        float nodeSize     = CalculateNodeSize(_mapRouteViewportRect.rect.size, levelCount);
        float verticalStep = nodeSize * 1.85f;
        float topMargin    = nodeSize * 1.0f;
        float distFromTop  = topMargin + target * verticalStep;          // content üstünden px
        float desiredTop   = Mathf.Clamp(distFromTop - viewportH * 0.5f, 0f, scrollable);
        // verticalNormalizedPosition: 1 = üst, 0 = alt
        _mapRouteScroll.verticalNormalizedPosition = 1f - (desiredTop / scrollable);
    }

    private static Color GetNodeColor(int levelIndex)
    {
        Color[] palette =
        {
            new Color(0.96f, 0.34f, 0.68f),
            new Color(0.29f, 0.58f, 0.98f),
            new Color(0.99f, 0.65f, 0.24f),
            new Color(0.66f, 0.42f, 0.98f),
            new Color(0.31f, 0.82f, 0.64f)
        };

        return palette[levelIndex % palette.Length];
    }

    private StarRowWidget CreateStarRow(Transform parent, string name, Vector2 anchoredPosition, float starSize, float spacing)
    {
        RectTransform rowRect = CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            anchoredPosition,
            Vector2.zero);

        var widget = new StarRowWidget
        {
            Rect = rowRect,
            Stars = new Image[3]
        };

        for (int i = 0; i < 3; i++)
        {
            RectTransform starRect = CreateRect(
                $"Star_{i + 1}",
                rowRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            Image image = starRect.gameObject.AddComponent<Image>();
            image.sprite = GetStarSprite();
            image.preserveAspect = true;
            image.raycastTarget = false;

            Shadow shadow = starRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.42f, 0.26f, 0.02f, 0.26f);
            shadow.effectDistance = new Vector2(0f, -2f);

            widget.Stars[i] = image;
        }

        ApplyStarRowLayout(widget, starSize, spacing);
        UpdateStarRow(widget, 0, false);
        return widget;
    }

    private StarRowWidget CreateCompleteStarCluster(Transform parent)
    {
        RectTransform rowRect = CreateRect(
            "StarCluster",
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);

        var widget = new StarRowWidget
        {
            Rect = rowRect,
            Stars = new Image[3]
        };

        for (int i = 0; i < widget.Stars.Length; i++)
        {
            RectTransform starRect = CreateRect(
                $"BigStar_{i + 1}",
                rowRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            Image image = starRect.gameObject.AddComponent<Image>();
            image.sprite = GetStarSprite();
            image.preserveAspect = true;
            image.raycastTarget = false;

            Shadow shadow = starRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.2f, 0.16f, 0.05f, 0.26f);
            shadow.effectDistance = new Vector2(0f, -7f);

            Outline outline = starRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.99f, 0.95f, 0.92f);
            outline.effectDistance = new Vector2(3f, -3f);

            widget.Stars[i] = image;
        }

        ApplyCompleteStarClusterLayout(widget, 112f, 10f);
        UpdateStarRow(widget, 0, false);
        return widget;
    }

    private static void ApplyStarRowLayout(StarRowWidget widget, float starSize, float spacing)
    {
        if (widget?.Rect == null || widget.Stars == null)
            return;

        float width = (starSize * widget.Stars.Length) + (spacing * Mathf.Max(0, widget.Stars.Length - 1));
        widget.Rect.sizeDelta = new Vector2(width, starSize);

        float startX = -width * 0.5f + starSize * 0.5f;
        for (int i = 0; i < widget.Stars.Length; i++)
        {
            if (widget.Stars[i] == null)
                continue;

            RectTransform starRect = widget.Stars[i].rectTransform;
            starRect.anchoredPosition = new Vector2(startX + (i * (starSize + spacing)), 0f);
            starRect.sizeDelta = new Vector2(starSize, starSize);
        }
    }

    private static void ApplyCompleteStarClusterLayout(StarRowWidget widget, float starSize, float spacing)
    {
        if (widget?.Rect == null || widget.Stars == null || widget.Stars.Length < 3)
            return;

        float sideSize = starSize * 0.72f;
        float width = (sideSize * 2f) + starSize - (spacing * 1.4f);
        float height = starSize * 1.08f;
        widget.Rect.sizeDelta = new Vector2(width, height);

        Vector2[] positions =
        {
            new Vector2(-(starSize * 0.46f), -(starSize * 0.02f)),
            new Vector2(0f, starSize * 0.08f),
            new Vector2(starSize * 0.46f, -(starSize * 0.02f))
        };
        float[] sizes = { sideSize, starSize, sideSize };
        float[] rotations = { -10f, 0f, 10f };

        for (int i = 0; i < widget.Stars.Length; i++)
        {
            if (widget.Stars[i] == null)
                continue;

            RectTransform starRect = widget.Stars[i].rectTransform;
            starRect.anchoredPosition = positions[i];
            starRect.sizeDelta = new Vector2(sizes[i], sizes[i]);
            starRect.localRotation = Quaternion.Euler(0f, 0f, rotations[i]);
        }
    }

    private static void UpdateStarRow(StarRowWidget widget, int filledStars, bool locked)
    {
        if (widget?.Stars == null)
            return;

        Color filledColor = new Color(1f, 0.75f, 0.05f, 1f);
        Color emptyColor = locked
            ? new Color(0.45f, 0.52f, 0.58f, 0.58f)
            : new Color(0.72f, 0.78f, 0.84f, 0.76f);

        for (int i = 0; i < widget.Stars.Length; i++)
        {
            if (widget.Stars[i] == null)
                continue;

            widget.Stars[i].color = i < filledStars ? filledColor : emptyColor;
        }
    }

    private Sprite GetStarSprite()
    {
        if (_starSprite != null)
            return _starSprite;

        const int size = 128;
        const int samples = 4;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float outerRadius = size * 0.46f;
        float innerRadius = size * 0.22f;
        var points = new Vector2[10];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = (90f + (36f * i)) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        Color[] pixels = new Color[size * size];
        float sampleStep = 1f / samples;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        Vector2 point = new Vector2(x + ((sx + 0.5f) * sampleStep), y + ((sy + 0.5f) * sampleStep));
                        if (IsPointInPolygon(point, points))
                            hits++;
                    }
                }

                float alpha = hits / (float)(samples * samples);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        _starSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        _starSprite.name = "RuntimeJuicyStar";
        return _starSprite;
    }

    private Sprite GetCircleSprite()
    {
        if (_circleSprite != null)
            return _circleSprite;

        _circleSprite = CreateRadialSprite(size: 128, feather: 0.08f, softInterior: false, "RuntimeCircle");
        return _circleSprite;
    }

    private Sprite GetSoftCircleSprite()
    {
        if (_softCircleSprite != null)
            return _softCircleSprite;

        _softCircleSprite = CreateRadialSprite(size: 128, feather: 0.36f, softInterior: true, "RuntimeSoftCircle");
        return _softCircleSprite;
    }

    private static Sprite CreateRadialSprite(int size, float feather, bool softInterior, string spriteName)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        float featherWidth = Mathf.Max(1f, size * feather);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - distance) / featherWidth);
                if (softInterior)
                    alpha *= Mathf.SmoothStep(0f, 1f, alpha);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = spriteName;
        return sprite;
    }

    private static bool IsPointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            bool intersects = ((a.y > point.y) != (b.y > point.y))
                && point.x < ((b.x - a.x) * (point.y - a.y) / (b.y - a.y)) + a.x;
            if (intersects)
                inside = !inside;
        }

        return inside;
    }

    private static string FormatTime(float elapsedSeconds)
    {
        int wholeSeconds = Mathf.Max(0, Mathf.RoundToInt(elapsedSeconds));
        int minutes = wholeSeconds / 60;
        int seconds = wholeSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = CreateUiObject(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
        return rect;
    }

    private TextMeshProUGUI CreateText(RectTransform rect, string content, float fontSize, Color color)
    {
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (_textTemplate != null)
        {
            text.font = _textTemplate.font;
            text.fontSharedMaterial = _textTemplate.fontSharedMaterial;
        }

        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static float ComputeUniformScale(Vector2 currentSize, Vector2 referenceSize, float minScale, float maxScale)
    {
        if (referenceSize.x <= 0f || referenceSize.y <= 0f)
            return 1f;

        float scale = Mathf.Min(currentSize.x / referenceSize.x, currentSize.y / referenceSize.y);
        return Mathf.Clamp(scale, minScale, maxScale);
    }

    private static float ComputeResponsiveScale(Vector2 currentSize, Vector2 referenceSize, float minScale, float maxScale, float widthBias)
    {
        if (referenceSize.x <= 0f || referenceSize.y <= 0f)
            return 1f;

        float widthScale = currentSize.x / referenceSize.x;
        float heightScale = currentSize.y / referenceSize.y;
        float scale = Mathf.Lerp(heightScale, widthScale, Mathf.Clamp01(widthBias));
        return Mathf.Clamp(scale, minScale, maxScale);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + (c3 * p * p * p) + (c1 * p * p);
    }

    private static void ApplyCompleteButtonLayout(
        RectTransform buttonRect,
        TextMeshProUGUI labelText,
        Vector2 anchoredPosition,
        float width,
        float height,
        float scale)
    {
        if (buttonRect == null || labelText == null)
            return;

        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(width, height);
        float fontSize = Mathf.Clamp(28f * scale, 30f, 46f);
        labelText.fontSize = fontSize;
        labelText.fontSizeMax = fontSize;
        labelText.fontSizeMin = Mathf.Clamp(18f * scale, 20f, 30f);
        labelText.rectTransform.sizeDelta = new Vector2(width * 0.9f, height * 0.58f);

        if (buttonRect.Find("BottomShade") is RectTransform shadeRect)
            shadeRect.sizeDelta = new Vector2(0f, height * 0.2f);
    }

    private static void ApplySafeAreaRect(RectTransform rect, Rect safeAreaRect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = safeAreaRect.center;
        rect.sizeDelta = safeAreaRect.size;
        rect.localScale = Vector3.one;
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    private void BuildSettingsButton()
    {
        RectTransform btnRect = CreateRect("SettingsButton", _overlayRoot.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-44f, -44f), new Vector2(56f, 56f));
        _settingsButtonRect = btnRect;

        var btnImg = btnRect.gameObject.AddComponent<Image>();
        var gearSprite = Resources.Load<Sprite>("SettingsIcon");
        if (gearSprite != null)
        {
            btnImg.sprite = gearSprite;
            btnImg.preserveAspect = true;
            btnImg.color = Color.white;
        }
        else
        {
            btnImg.color = new Color(0.7f, 0.7f, 0.8f, 0.9f);
        }

        _settingsButton = btnRect.gameObject.AddComponent<Button>();
        _settingsButton.targetGraphic = btnImg;
        ColorBlock cb = _settingsButton.colors;
        cb.normalColor    = Color.white;
        cb.highlightedColor = new Color(0.85f, 0.85f, 1f);
        cb.pressedColor   = new Color(0.7f, 0.7f, 0.9f);
        _settingsButton.colors = cb;
        _settingsButton.onClick.AddListener(ToggleSettingsPanel);
    }

    private void LayoutSettingsButton(Rect safeAreaRect)
    {
        if (_settingsButtonRect == null && _settingsButton != null)
            _settingsButtonRect = _settingsButton.GetComponent<RectTransform>();

        if (_settingsButtonRect == null || safeAreaRect.width <= 0f || safeAreaRect.height <= 0f)
            return;

        float size = Mathf.Clamp(Mathf.Min(safeAreaRect.width, safeAreaRect.height) * 0.08f, 44f, 62f);
        float horizontalMargin = Mathf.Clamp(size * 0.55f, 24f, 34f);
        float topMargin = Mathf.Clamp(size * 0.78f, 34f, 48f);

        _settingsButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
        _settingsButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
        _settingsButtonRect.pivot = new Vector2(1f, 1f);
        _settingsButtonRect.anchoredPosition = new Vector2(safeAreaRect.xMax - horizontalMargin, safeAreaRect.yMax - topMargin);
        _settingsButtonRect.sizeDelta = new Vector2(size, size);
        _settingsButtonRect.localScale = Vector3.one;
    }

    private void BuildSettingsPanel()
    {
        var panelRoot = CreateUiObject("SettingsPanel", _overlayRoot.transform);
        _settingsPanel = panelRoot;
        RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
        StretchToParent(rootRect);

        var dim = panelRoot.AddComponent<Image>();
        dim.color = new Color(0.03f, 0.04f, 0.08f, 0.38f);
        dim.raycastTarget = true;
        var dimBtn = panelRoot.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        ColorBlock dimCb = dimBtn.colors;
        dimCb.normalColor = dimCb.highlightedColor = dimCb.pressedColor = Color.white;
        dimBtn.colors = dimCb;
        dimBtn.onClick.AddListener(ToggleSettingsPanel);

        RectTransform cardRect = CreateRect("Card", panelRoot.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(660f, 880f));
        var cardImg = cardRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(cardImg, new Color(1f, 0.99f, 0.95f, 0.98f));

        Shadow cardShadow = cardRect.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0.2f, 0.18f, 0.12f, 0.2f);
        cardShadow.effectDistance = new Vector2(0f, -12f);

        var cardBtn = cardRect.gameObject.AddComponent<Button>();
        cardBtn.targetGraphic = cardImg;
        ColorBlock cardCb = cardBtn.colors;
        cardCb.normalColor = cardCb.highlightedColor = cardCb.pressedColor = Color.white;
        cardBtn.colors = cardCb;

        RectTransform titleRect = CreateRect("Title", cardRect,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -92f), new Vector2(520f, 86f));
        var title = CreateText(titleRect, "AYARLAR", 66, new Color(0.53f, 0.3f, 0f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;
        title.alignment = TextAlignmentOptions.Center;

        RectTransform musicLblRect = CreateRect("MusicLabel", cardRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 120f), new Vector2(520f, 52f));
        var musicLbl = CreateText(musicLblRect, "Müzik", 40, new Color(0.53f, 0.3f, 0f));
        musicLbl.fontStyle = FontStyles.Bold;
        musicLbl.alignment = TextAlignmentOptions.Left;

        float initMusic = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.5f;
        CreateVolumeSlider(cardRect, new Vector2(0f, 58f), new Vector2(520f, 58f),
            initMusic, v => AudioManager.Instance?.SetMusicVolume(v), new Color(0.99f, 0.75f, 0.02f, 1f));

        RectTransform sfxLblRect = CreateRect("SfxLabel", cardRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -60f), new Vector2(520f, 52f));
        var sfxLbl = CreateText(sfxLblRect, "Ses Efekti", 40, new Color(0.53f, 0.3f, 0f));
        sfxLbl.fontStyle = FontStyles.Bold;
        sfxLbl.alignment = TextAlignmentOptions.Left;

        float initSfx = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.8f;
        CreateVolumeSlider(cardRect, new Vector2(0f, -122f), new Vector2(520f, 58f),
            initSfx, v => AudioManager.Instance?.SetSfxVolume(v), new Color(0.36f, 0.9f, 0.34f, 1f));

        CreateSettingsActionButton(
            cardRect,
            "MainMenuButton",
            "Ana Menüye Dön",
            new Vector2(0f, -250f),
            new Vector2(500f, 86f),
            "Next",
            HandleSettingsMainMenuPressed,
            38f);

        CreateSettingsActionButton(
            cardRect,
            "CloseButton",
            "Kapat",
            new Vector2(0f, -356f),
            new Vector2(340f, 86f),
            "Map",
            ToggleSettingsPanel,
            38f);

        _settingsPanel.SetActive(false);
    }

    private Button CreateSettingsActionButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        string styleKey,
        UnityEngine.Events.UnityAction action,
        float fontSize)
    {
        RectTransform buttonRect = CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            anchoredPosition,
            size);

        GetCompleteButtonStyle(styleKey, out Color fillColor, out Color outlineColor, out Color textColor);

        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(buttonImage, fillColor);

        Shadow buttonShadow = buttonRect.gameObject.AddComponent<Shadow>();
        buttonShadow.effectColor = new Color(0.18f, 0.14f, 0.08f, 0.22f);
        buttonShadow.effectDistance = new Vector2(0f, -7f);

        Outline buttonOutline = buttonRect.gameObject.AddComponent<Outline>();
        buttonOutline.effectColor = outlineColor;
        buttonOutline.effectDistance = new Vector2(3f, -3f);

        RectTransform glossRect = CreateRect(
            "Gloss",
            buttonRect,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        Image glossImage = glossRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(glossImage, new Color(1f, 1f, 1f, 0.18f));
        glossImage.raycastTarget = false;

        RectTransform shadeRect = CreateRect(
            "BottomShade",
            buttonRect,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            Vector2.zero,
            new Vector2(0f, 14f));
        Image shadeImage = shadeRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(shadeImage, new Color(0f, 0f, 0f, 0.11f));
        shadeImage.raycastTarget = false;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(action);

        RectTransform labelRect = CreateRect(
            "Label",
            buttonRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(size.x - 46f, size.y * 0.62f));
        TextMeshProUGUI labelText = CreateText(labelRect, label, fontSize, textColor);
        labelText.fontStyle = FontStyles.Bold;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMax = fontSize;
        labelText.fontSizeMin = 20f;
        labelText.enableWordWrapping = false;
        return button;
    }

    private void HandleSettingsMainMenuPressed()
    {
        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);

        SetOverlayState(OverlayState.Intro);
    }

    private void ToggleSettingsPanel()
    {
        if (_settingsPanel == null) return;
        _settingsPanel.SetActive(!_settingsPanel.activeSelf);
    }

    private Slider CreateVolumeSlider(Transform parent, Vector2 anchoredPos, Vector2 size,
        float initialValue, UnityEngine.Events.UnityAction<float> onChanged, Color fillColor)
    {
        var go   = CreateUiObject("Slider", parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = size;
        rect.localScale       = Vector3.one;

        // Background track
        var bg = CreateUiObject("Background", go.transform);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        bgRect.localScale = Vector3.one;
        var bgImg = bg.AddComponent<Image>();
        ApplyPanelSprite(bgImg, new Color(0.89f, 0.87f, 0.75f, 1f));
        bgImg.raycastTarget = true;

        // Fill area
        var fillArea = CreateUiObject("Fill Area", go.transform);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-18f, 0f);
        fillAreaRect.localScale = Vector3.one;

        var fill = CreateUiObject("Fill", fillArea.transform);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fillRect.localScale = Vector3.one;
        var fillImg = fill.AddComponent<Image>();
        ApplyPanelSprite(fillImg, fillColor);
        fillImg.raycastTarget = false;

        // Handle area
        var handleArea = CreateUiObject("Handle Slide Area", go.transform);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);
        handleAreaRect.localScale = Vector3.one;

        var handle = CreateUiObject("Handle", handleArea.transform);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.sizeDelta = new Vector2(34f, 0f);
        handleRect.localScale = Vector3.one;
        var handleImg = handle.AddComponent<Image>();
        handleImg.sprite = GetCircleSprite();
        handleImg.color = Color.white;

        Shadow handleShadow = handle.AddComponent<Shadow>();
        handleShadow.effectColor = new Color(0.2f, 0.16f, 0.05f, 0.22f);
        handleShadow.effectDistance = new Vector2(0f, -4f);

        var slider = go.AddComponent<Slider>();
        slider.fillRect      = fillRect;
        slider.handleRect    = handleRect;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = 0f;
        slider.maxValue      = 1f;
        slider.value         = initialValue;
        slider.onValueChanged.AddListener(onChanged);

        return slider;
    }

    private static Rect GetSafeAreaRectInCanvasSpace(RectTransform canvasRect)
    {
        Rect canvasBounds = canvasRect.rect;
        if (canvasBounds.width <= 0f || canvasBounds.height <= 0f)
            return default;

        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);
        Rect safeArea = Screen.safeArea;

        float xScale = canvasBounds.width / screenWidth;
        float yScale = canvasBounds.height / screenHeight;
        float safeWidth = safeArea.width * xScale;
        float safeHeight = safeArea.height * yScale;
        float safeCenterX = ((safeArea.x + safeArea.width * 0.5f) - (screenWidth * 0.5f)) * xScale;
        float safeCenterY = ((safeArea.y + safeArea.height * 0.5f) - (screenHeight * 0.5f)) * yScale;

        return new Rect(
            safeCenterX - safeWidth * 0.5f,
            safeCenterY - safeHeight * 0.5f,
            safeWidth,
            safeHeight);
    }
}
