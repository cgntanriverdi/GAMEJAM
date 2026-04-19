using System.Collections.Generic;
using System.Text;
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
        public TextMeshProUGUI NumberText;
        public TextMeshProUGUI StarsText;
    }

    private enum OverlayState
    {
        Hidden,
        Intro,
        Map,
        Complete
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
    private RectTransform _introPanelRect;
    private RectTransform _mapPanelRect;
    private RectTransform _completePanelRect;
    private RectTransform _introContentRect;
    private RectTransform _mapTitleBadgeRect;
    private RectTransform _mapCardRect;
    private RectTransform _mapRouteRect;
    private RectTransform _mapFooterRect;
    private RectTransform _completeCardRect;
    private RectTransform _completeContentRect;
    private RectTransform _completeTitleRect;
    private RectTransform _completeStarsRect;
    private RectTransform _completeTimeRect;
    private RectTransform _completeBestRect;
    private RectTransform _completeMapButtonRect;
    private RectTransform _completeReplayButtonRect;
    private RectTransform _completeNextButtonRect;
    private TextMeshProUGUI _textTemplate;
    private Sprite _panelSprite;
    private Sprite _roundButtonSprite;

    private TextMeshProUGUI _mapTitleText;
    private TextMeshProUGUI _mapTitleSubText;
    private TextMeshProUGUI _mapHintText;
    private TextMeshProUGUI _completeTitleText;
    private TextMeshProUGUI _completeStarsText;
    private TextMeshProUGUI _completeTimeText;
    private TextMeshProUGUI _completeBestText;
    private TextMeshProUGUI _completeMapButtonText;
    private TextMeshProUGUI _completeReplayButtonText;
    private TextMeshProUGUI _completeNextButtonText;
    private Button _nextButton;
    private Vector2 _lastCanvasSize = Vector2.negativeInfinity;
    private Vector2 _lastRouteSize = Vector2.negativeInfinity;

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
        TextMeshProUGUI titleText = CreateText(titleRect, "Chrome\nPath", 54f, new Color(0.73f, 0.48f, 0.12f));
        titleText.fontStyle = FontStyles.Bold;
        titleText.lineSpacing = -12f;

        RectTransform subtitleRect = CreateRect(
            "Subtitle",
            cardRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -52f),
            new Vector2(280f, 40f));
        TextMeshProUGUI subtitleText = CreateText(subtitleRect, "Pick a route, clear it fast, earn stars.", 22f, new Color(0.42f, 0.29f, 0.16f));
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
        TextMeshProUGUI playLabel = CreateText(playLabelRect, "Play", 34f, Color.white);
        playLabel.fontStyle = FontStyles.Bold;

        RectTransform hintRect = CreateRect(
            "Hint",
            _introContentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -174f),
            new Vector2(300f, 34f));
        TextMeshProUGUI hintText = CreateText(hintRect, "Play opens a session map before the puzzle board.", 18f, new Color(1f, 1f, 1f, 0.9f));
        hintText.enableWordWrapping = true;
    }

    private void BuildMapPanel()
    {
        _mapPanel = CreateUiObject("MapPanel", _overlayRoot.transform);
        _mapPanelRect = _mapPanel.GetComponent<RectTransform>();

        _mapTitleBadgeRect = CreateRect(
            "TitleBadge",
            _mapPanel.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -92f),
            new Vector2(320f, 84f));
        Image titleBadgeImage = _mapTitleBadgeRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(titleBadgeImage, new Color(0.97f, 0.74f, 0.86f, 0.95f));

        RectTransform titleRect = CreateRect(
            "Title",
            _mapTitleBadgeRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 10f),
            new Vector2(280f, 30f));
        _mapTitleText = CreateText(titleRect, "Pick A Route", 30f, new Color(0.52f, 0.18f, 0.32f));
        _mapTitleText.fontStyle = FontStyles.Bold;

        RectTransform titleSubRect = CreateRect(
            "TitleSub",
            _mapTitleBadgeRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -18f),
            new Vector2(280f, 24f));
        _mapTitleSubText = CreateText(titleSubRect, "Stars live only for this app session.", 15f, new Color(0.46f, 0.2f, 0.28f));

        _mapCardRect = CreateRect(
            "MapCard",
            _mapPanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -10f),
            new Vector2(680f, 1080f));
        Image mapCardImage = _mapCardRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(mapCardImage, new Color(0.88f, 0.97f, 1f, 0.92f));

        Shadow mapCardShadow = _mapCardRect.gameObject.AddComponent<Shadow>();
        mapCardShadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
        mapCardShadow.effectDistance = new Vector2(0f, -10f);

        _mapRouteRect = CreateRect(
            "Route",
            _mapCardRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 24f),
            new Vector2(580f, 860f));

        _mapFooterRect = CreateRect(
            "Footer",
            _mapCardRect,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 68f),
            new Vector2(520f, 50f));
        _mapHintText = CreateText(_mapFooterRect, "Level 1 starts unlocked. Faster clears give more stars.", 18f, new Color(0.24f, 0.33f, 0.45f));
        _mapHintText.enableWordWrapping = true;
    }

    private void BuildMapRoute(RectTransform routeRect, Vector2 routeSize)
    {
        int levelCount = LevelManager.Instance != null
            ? LevelManager.Instance.SessionLevelCount
            : 1;

        float nodeSize = CalculateNodeSize(routeSize, levelCount);
        float starOffset = Mathf.Clamp(nodeSize * 0.82f, 46f, 78f);
        float ribbonThickness = Mathf.Clamp(nodeSize * 0.17f, 12f, 22f);
        Vector2[] positions = BuildNodePositions(levelCount, routeSize, nodeSize, starOffset);

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
                new Vector2(nodeSize, nodeSize));

            Image nodeImage = nodeRect.gameObject.AddComponent<Image>();
            ApplyNodeSprite(nodeImage, GetNodeColor(i));

            Shadow nodeShadow = nodeRect.gameObject.AddComponent<Shadow>();
            nodeShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            nodeShadow.effectDistance = new Vector2(0f, -5f);

            Button nodeButton = nodeRect.gameObject.AddComponent<Button>();
            nodeButton.targetGraphic = nodeImage;

            int capturedIndex = i;
            nodeButton.onClick.AddListener(() => HandleLevelSelected(capturedIndex));

            RectTransform numberRect = CreateRect(
                "Number",
                nodeRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, nodeSize * 0.1f),
                new Vector2(nodeSize * 0.82f, nodeSize * 0.36f));
            TextMeshProUGUI numberText = CreateText(numberRect, (i + 1).ToString(), Mathf.Clamp(nodeSize * 0.36f, 24f, 40f), Color.white);
            numberText.fontStyle = FontStyles.Bold;

            RectTransform starsRect = CreateRect(
                "Stars",
                routeRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position + new Vector2(0f, -starOffset),
                new Vector2(nodeSize * 1.6f, nodeSize * 0.34f));
            TextMeshProUGUI starsText = CreateText(starsRect, BuildStarMarkup(0), Mathf.Clamp(nodeSize * 0.2f, 14f, 26f), Color.white);
            starsText.richText = true;

            _levelButtons.Add(new LevelButtonWidget
            {
                LevelIndex = i,
                Button = nodeButton,
                NodeImage = nodeImage,
                NumberText = numberText,
                StarsText = starsText
            });
        }
    }

    private void BuildCompletePanel()
    {
        _completePanel = CreateUiObject("CompletePanel", _overlayRoot.transform);
        _completePanelRect = _completePanel.GetComponent<RectTransform>();

        _completeCardRect = CreateRect(
            "CompleteCard",
            _completePanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(420f, 360f));
        Image cardImage = _completeCardRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(cardImage, new Color(0.98f, 0.95f, 0.84f, 0.98f));

        Shadow cardShadow = _completeCardRect.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        cardShadow.effectDistance = new Vector2(0f, -10f);

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
        _completeTitleText = CreateText(_completeTitleRect, "Level Complete", 34f, new Color(0.58f, 0.22f, 0.31f));
        _completeTitleText.fontStyle = FontStyles.Bold;

        _completeStarsRect = CreateRect(
            "Stars",
            _completeContentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 34f),
            new Vector2(260f, 36f));
        _completeStarsText = CreateText(_completeStarsRect, BuildStarMarkup(3), 30f, Color.white);
        _completeStarsText.richText = true;

        _completeTimeRect = CreateRect(
            "Time",
            _completeContentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -18f),
            new Vector2(320f, 32f));
        _completeTimeText = CreateText(_completeTimeRect, "Time 00:00", 24f, new Color(0.29f, 0.31f, 0.39f));

        _completeBestRect = CreateRect(
            "Best",
            _completeContentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -58f),
            new Vector2(340f, 48f));
        _completeBestText = CreateText(_completeBestRect, "Best * * *  00:00", 20f, new Color(0.41f, 0.31f, 0.24f));
        _completeBestText.richText = true;
        _completeBestText.enableWordWrapping = true;

        CreateActionButton(_completeContentRect, "MapButton", "Map", new Vector2(-118f, -118f), HandleMapPressed, out _completeMapButtonRect, out _completeMapButtonText);
        CreateActionButton(_completeContentRect, "ReplayButton", "Replay", new Vector2(0f, -118f), HandleReplayPressed, out _completeReplayButtonRect, out _completeReplayButtonText);
        _nextButton = CreateActionButton(_completeContentRect, "NextButton", "Next", new Vector2(118f, -118f), HandleNextPressed, out _completeNextButtonRect, out _completeNextButtonText);
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
        ApplyPanelSprite(buttonImage, new Color(0.9f, 0.24f, 0.56f, 1f));

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
        labelText = CreateText(labelRect, label, 22f, Color.white);
        labelText.fontStyle = FontStyles.Bold;

        return button;
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
        RefreshResponsiveLayout(forceRouteRebuild: state == OverlayState.Map);

        bool overlayVisible = state != OverlayState.Hidden;
        if (_overlayRoot != null)
            _overlayRoot.SetActive(overlayVisible);

        if (_introPanel != null)
            _introPanel.SetActive(state == OverlayState.Intro);

        if (_mapPanel != null)
            _mapPanel.SetActive(state == OverlayState.Map);

        if (_completePanel != null)
            _completePanel.SetActive(state == OverlayState.Complete);

        switch (state)
        {
            case OverlayState.Intro:
            case OverlayState.Map:
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
        if (_completeTitleText != null)
            _completeTitleText.text = $"Level {result.LevelIndex + 1} Clear";

        if (_completeStarsText != null)
            _completeStarsText.text = BuildStarMarkup(result.StarsEarned);

        if (_completeTimeText != null)
            _completeTimeText.text = $"Time {FormatTime(result.ElapsedSeconds)}";

        if (_completeBestText != null)
        {
            string bestLabel = result.IsNewBest ? "New Best" : "Best";
            _completeBestText.text = $"{bestLabel} {BuildStarMarkup(result.BestStars)}  {FormatTime(result.BestTimeSeconds)}";
        }

        if (_nextButton != null)
            _nextButton.gameObject.SetActive(
                LevelManager.Instance != null
                && LevelManager.Instance.TryGetNextUnlockedLevelIndex(result.LevelIndex, out _));
    }

    private void RefreshMapPanel()
    {
        if (_levelButtons.Count == 0 || LevelManager.Instance == null)
            return;

        foreach (LevelButtonWidget widget in _levelButtons)
        {
            int levelIndex = widget.LevelIndex;
            bool unlocked = LevelManager.Instance.IsLevelUnlocked(levelIndex);
            int bestStars = LevelManager.Instance.GetBestStars(levelIndex);

            widget.Button.interactable = unlocked;
            widget.NumberText.text = unlocked ? (levelIndex + 1).ToString() : "?";
            widget.StarsText.text = unlocked
                ? BuildStarMarkup(bestStars)
                : BuildStarMarkup(0, "#5E6786", "#5E6786");

            Color nodeColor = unlocked
                ? GetNodeColor(levelIndex)
                : new Color(0.4f, 0.45f, 0.58f, 0.9f);
            ApplyNodeSprite(widget.NodeImage, nodeColor);
        }

        if (_mapHintText != null)
            _mapHintText.text = "Level 1 starts unlocked. Stars reset when the app closes.";
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

    private void TeardownOverlay()
    {
        DetachGameManagerEvents();

        if (_overlayRoot != null)
            Destroy(_overlayRoot);

        _overlayRoot = null;
        _introPanel = null;
        _mapPanel = null;
        _completePanel = null;
        _introPanelRect = null;
        _mapPanelRect = null;
        _completePanelRect = null;
        _introContentRect = null;
        _mapTitleBadgeRect = null;
        _mapCardRect = null;
        _mapRouteRect = null;
        _mapFooterRect = null;
        _completeCardRect = null;
        _completeContentRect = null;
        _completeTitleRect = null;
        _completeStarsRect = null;
        _completeTimeRect = null;
        _completeBestRect = null;
        _completeMapButtonRect = null;
        _completeReplayButtonRect = null;
        _completeNextButtonRect = null;
        _canvas = null;
        _playAreaRoot = null;
        _gridManager = null;
        _playerToken = null;
        _swipeInput = null;
        _gameManager = null;
        _mapTitleText = null;
        _mapTitleSubText = null;
        _mapHintText = null;
        _completeTitleText = null;
        _completeStarsText = null;
        _completeTimeText = null;
        _completeBestText = null;
        _completeMapButtonText = null;
        _completeReplayButtonText = null;
        _completeNextButtonText = null;
        _nextButton = null;
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

    private void CreatePathRibbon(RectTransform parent, Vector2 from, Vector2 to, float thickness)
    {
        RectTransform ribbonRect = CreateRect(
            "RouteRibbon",
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            (from + to) * 0.5f,
            new Vector2(Vector2.Distance(from, to), thickness));

        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        ribbonRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image ribbonImage = ribbonRect.gameObject.AddComponent<Image>();
        ApplyPanelSprite(ribbonImage, new Color(1f, 0.88f, 0.95f, 0.9f));
        ribbonImage.raycastTarget = false;
    }

    private static Vector2[] BuildNodePositions(int levelCount, Vector2 routeSize, float nodeSize, float starOffset)
    {
        var result = new Vector2[levelCount];
        float[] xPattern = { -0.32f, 0.31f, -0.13f, 0.28f, -0.25f, 0.15f };
        float horizontalLimit = Mathf.Max(0f, (routeSize.x * 0.5f) - (nodeSize * 0.58f));
        float top = routeSize.y * 0.5f - (nodeSize * 0.55f);
        float bottom = -routeSize.y * 0.5f + (nodeSize * 0.82f) + (starOffset * 0.12f);
        float spacing = levelCount > 1 ? (top - bottom) / (levelCount - 1) : 0f;

        for (int i = 0; i < levelCount; i++)
        {
            float x = Mathf.Clamp(routeSize.x * xPattern[i % xPattern.Length], -horizontalLimit, horizontalLimit);
            float y = top - (spacing * i);
            result[i] = new Vector2(x, y);
        }

        return result;
    }

    private static float CalculateNodeSize(Vector2 routeSize, int levelCount)
    {
        float widthDriven = routeSize.x * 0.23f;
        float heightDriven = routeSize.y / Mathf.Max(levelCount, 1) * 0.92f;
        return Mathf.Clamp(Mathf.Min(widthDriven, heightDriven), 60f, 132f);
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

        bool portrait = safeAreaRect.height >= safeAreaRect.width;
        LayoutIntroPanel(safeAreaRect.size);
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

    private void LayoutMapPanel(Vector2 safeAreaSize, bool portrait, bool forceRouteRebuild)
    {
        if (_mapTitleBadgeRect == null || _mapCardRect == null || _mapRouteRect == null || _mapFooterRect == null)
            return;

        float layoutScale = ComputeResponsiveScale(safeAreaSize, new Vector2(360f, 700f), 1f, 1.8f, 0.58f);
        float badgeWidth = Mathf.Clamp(safeAreaSize.x * (portrait ? 0.66f : 0.44f), 250f * layoutScale, 500f);
        float badgeHeight = Mathf.Clamp(72f * layoutScale, 72f, 118f);
        float badgeTopPadding = 10f * layoutScale;
        _mapTitleBadgeRect.anchoredPosition = new Vector2(0f, -(badgeTopPadding + badgeHeight * 0.5f));
        _mapTitleBadgeRect.sizeDelta = new Vector2(badgeWidth, badgeHeight);

        if (_mapTitleText != null)
            _mapTitleText.fontSize = Mathf.Clamp(28f * layoutScale, 28f, 46f);

        if (_mapTitleSubText != null)
            _mapTitleSubText.fontSize = Mathf.Clamp(14f * layoutScale, 14f, 22f);

        float horizontalMargin = portrait
            ? Mathf.Clamp(safeAreaSize.x * 0.035f, 10f, 24f * layoutScale)
            : Mathf.Clamp(safeAreaSize.x * 0.055f, 24f, 64f * layoutScale);
        float topMargin = badgeTopPadding + badgeHeight + (12f * layoutScale);
        float bottomMargin = 12f * layoutScale;
        float cardWidth = Mathf.Min(safeAreaSize.x - (horizontalMargin * 2f), 860f);
        float cardHeight = Mathf.Min(safeAreaSize.y - topMargin - bottomMargin, 1320f);

        _mapCardRect.anchoredPosition = new Vector2(0f, (bottomMargin - topMargin) * 0.5f);
        _mapCardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

        float sidePadding = Mathf.Clamp(cardWidth * (portrait ? 0.1f : 0.08f), 16f * layoutScale, 44f * layoutScale);
        float routeTopPadding = Mathf.Clamp(24f * layoutScale, 20f, 48f);
        float routeBottomPadding = Mathf.Clamp(38f * layoutScale, 30f, 74f);
        _mapRouteRect.anchoredPosition = new Vector2(0f, (routeBottomPadding - routeTopPadding) * 0.5f + (6f * layoutScale));
        _mapRouteRect.sizeDelta = new Vector2(
            Mathf.Max(240f, cardWidth - (sidePadding * 2f)),
            Mathf.Max(320f, cardHeight - routeTopPadding - routeBottomPadding));

        _mapFooterRect.anchoredPosition = new Vector2(0f, 24f * layoutScale);
        _mapFooterRect.sizeDelta = new Vector2(cardWidth - (sidePadding * 1.1f), 44f * layoutScale);

        if (_mapHintText != null)
            _mapHintText.fontSize = Mathf.Clamp(15f * layoutScale, 15f, 24f);

        Canvas.ForceUpdateCanvases();

        Vector2 routeSize = _mapRouteRect.rect.size;
        if (routeSize.x <= 0f || routeSize.y <= 0f)
            return;

        bool routeSizeChanged = (_lastRouteSize - routeSize).sqrMagnitude > 1f;
        if (!forceRouteRebuild && !routeSizeChanged)
            return;

        RebuildMapRoute(routeSize);
    }

    private void LayoutCompletePanel(Vector2 safeAreaSize)
    {
        if (_completeCardRect == null || _completeContentRect == null)
            return;

        float cardScale = ComputeResponsiveScale(safeAreaSize, new Vector2(360f, 760f), 1f, 1.75f, 0.66f);
        float width = Mathf.Clamp(safeAreaSize.x * 0.95f, 340f, 760f);
        float height = Mathf.Clamp(safeAreaSize.y * 0.58f, 380f, 720f);
        float innerWidth = Mathf.Max(300f, width - (28f * cardScale));
        float innerHeight = Mathf.Max(320f, height - (24f * cardScale));
        float typographyScale = ComputeResponsiveScale(new Vector2(innerWidth, innerHeight), new Vector2(340f, 320f), 1f, 1.7f, 0.64f);
        float buttonGap = Mathf.Clamp(12f * typographyScale, 12f, 24f);
        float buttonWidth = Mathf.Clamp((innerWidth - (buttonGap * 2f)) / 3f, 96f, 190f);
        float buttonHeight = Mathf.Clamp(62f * typographyScale, 62f, 102f);
        float buttonY = -(innerHeight * 0.31f);

        _completeCardRect.sizeDelta = new Vector2(width, height);
        _completeContentRect.anchoredPosition = Vector2.zero;
        _completeContentRect.sizeDelta = new Vector2(innerWidth, innerHeight);
        _completeContentRect.localScale = Vector3.one;

        if (_completeTitleRect != null)
        {
            _completeTitleRect.anchoredPosition = new Vector2(0f, -42f * typographyScale);
            _completeTitleRect.sizeDelta = new Vector2(innerWidth * 0.86f, 60f * typographyScale);
        }

        if (_completeTitleText != null)
            _completeTitleText.fontSize = Mathf.Clamp(36f * typographyScale, 36f, 62f);

        if (_completeStarsRect != null)
        {
            _completeStarsRect.anchoredPosition = new Vector2(0f, innerHeight * 0.15f);
            _completeStarsRect.sizeDelta = new Vector2(innerWidth * 0.56f, 42f * typographyScale);
        }

        if (_completeStarsText != null)
            _completeStarsText.fontSize = Mathf.Clamp(34f * typographyScale, 34f, 56f);

        if (_completeTimeRect != null)
        {
            _completeTimeRect.anchoredPosition = new Vector2(0f, innerHeight * 0.03f);
            _completeTimeRect.sizeDelta = new Vector2(innerWidth * 0.72f, 40f * typographyScale);
        }

        if (_completeTimeText != null)
            _completeTimeText.fontSize = Mathf.Clamp(28f * typographyScale, 28f, 46f);

        if (_completeBestRect != null)
        {
            _completeBestRect.anchoredPosition = new Vector2(0f, -(innerHeight * 0.11f));
            _completeBestRect.sizeDelta = new Vector2(innerWidth * 0.82f, 54f * typographyScale);
        }

        if (_completeBestText != null)
            _completeBestText.fontSize = Mathf.Clamp(21f * typographyScale, 21f, 34f);

        ApplyCompleteButtonLayout(_completeMapButtonRect, _completeMapButtonText, new Vector2(-(buttonWidth + buttonGap), buttonY), buttonWidth, buttonHeight, typographyScale);
        ApplyCompleteButtonLayout(_completeReplayButtonRect, _completeReplayButtonText, new Vector2(0f, buttonY), buttonWidth, buttonHeight, typographyScale);
        ApplyCompleteButtonLayout(_completeNextButtonRect, _completeNextButtonText, new Vector2(buttonWidth + buttonGap, buttonY), buttonWidth, buttonHeight, typographyScale);
    }

    private void RebuildMapRoute(Vector2 routeSize)
    {
        if (_mapRouteRect == null)
            return;

        for (int i = _mapRouteRect.childCount - 1; i >= 0; i--)
            Destroy(_mapRouteRect.GetChild(i).gameObject);

        _levelButtons.Clear();
        _lastRouteSize = routeSize;
        BuildMapRoute(_mapRouteRect, routeSize);
        RefreshMapPanel();
    }

    private static Color GetNodeColor(int levelIndex)
    {
        Color[] palette =
        {
            new Color(0.96f, 0.34f, 0.63f),
            new Color(0.3f, 0.62f, 0.98f),
            new Color(0.98f, 0.62f, 0.24f),
            new Color(0.63f, 0.43f, 0.96f),
            new Color(0.32f, 0.78f, 0.66f)
        };

        return palette[levelIndex % palette.Length];
    }

    private static string BuildStarMarkup(int stars, string filledColor = "#F3C552", string emptyColor = "#7A839C")
    {
        var builder = new StringBuilder();
        for (int i = 0; i < 3; i++)
        {
            string color = i < stars ? filledColor : emptyColor;
            builder.Append("<color=");
            builder.Append(color);
            builder.Append(">*</color>");

            if (i < 2)
                builder.Append(" ");
        }

        return builder.ToString();
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
        labelText.fontSize = Mathf.Clamp(23f * scale, 23f, 36f);
        labelText.rectTransform.sizeDelta = new Vector2(width * 0.9f, height * 0.58f);
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
