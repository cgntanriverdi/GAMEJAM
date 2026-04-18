using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StartupMenuUI : MonoBehaviour
{
    private static StartupMenuUI _instance;
    private static bool _shouldBlockAutoStart = true;

    private Canvas _canvas;
    private RectTransform _playAreaRoot;
    private GridManager _gridManager;
    private PlayerToken _playerToken;
    private SwipeInputController _swipeInput;

    private GameObject _overlayRoot;
    private TextMeshProUGUI _textTemplate;
    private Sprite _panelSprite;

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
    }

    private void Update()
    {
        if (_overlayRoot != null || !_shouldBlockAutoStart || GameManager.Instance == null)
            return;

        CacheSceneReferences();
        if (_canvas == null)
            return;

        BuildOverlay();
        ShowMenu();
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
        ShowMenu();
    }

    private void CacheSceneReferences()
    {
        _canvas = FindObjectOfType<Canvas>();
        _gridManager = FindObjectOfType<GridManager>();
        _playerToken = FindObjectOfType<PlayerToken>();
        _swipeInput = FindObjectOfType<SwipeInputController>();

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

    }

    private void BuildOverlay()
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.transform.SetParent(_canvas.transform, false);
            return;
        }

        _overlayRoot = CreateUiObject("StartupOverlay", _canvas.transform);
        RectTransform overlayRect = _overlayRoot.GetComponent<RectTransform>();
        StretchToParent(overlayRect);

        Image overlayImage = _overlayRoot.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.08f);
        overlayImage.raycastTarget = true;

        RectTransform ropeRect = CreateRect("Rope", _overlayRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(8f, 160f));
        Image ropeImage = ropeRect.gameObject.AddComponent<Image>();
        ropeImage.sprite = _panelSprite;
        ropeImage.type = Image.Type.Sliced;
        ropeImage.color = new Color(0.44f, 0.29f, 0.18f, 0.95f);
        ropeImage.raycastTarget = false;

        RectTransform cardRect = CreateRect("IntroCard", _overlayRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(320f, 260f));
        Image cardImage = cardRect.gameObject.AddComponent<Image>();
        cardImage.sprite = _panelSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = new Color(0.98f, 0.93f, 0.78f, 0.96f);
        cardImage.raycastTarget = false;

        Shadow cardShadow = cardRect.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        cardShadow.effectDistance = new Vector2(0f, -8f);

        Outline cardOutline = cardRect.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.68f, 0.52f, 0.23f, 0.75f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform holeRect = CreateRect("CardHole", cardRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(26f, 26f));
        Image holeImage = holeRect.gameObject.AddComponent<Image>();
        holeImage.sprite = _panelSprite;
        holeImage.type = Image.Type.Sliced;
        holeImage.color = new Color(0.45f, 0.32f, 0.2f, 0.95f);
        holeImage.raycastTarget = false;

        RectTransform titleRect = CreateRect("Title", cardRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(260f, 120f));
        TextMeshProUGUI titleText = CreateText(titleRect, "Chrome\nPath", 54, new Color(0.74f, 0.49f, 0.12f));
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.lineSpacing = -12f;

        RectTransform subtitleRect = CreateRect("Subtitle", cardRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(240f, 40f));
        TextMeshProUGUI subtitleText = CreateText(subtitleRect, "Hidden route puzzle", 24, new Color(0.42f, 0.28f, 0.16f));
        subtitleText.alignment = TextAlignmentOptions.Center;

        RectTransform buttonRect = CreateRect("PlayButton", _overlayRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -142f), new Vector2(248f, 74f));
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.sprite = _panelSprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = new Color(0.9f, 0.24f, 0.56f, 1f);

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

        RectTransform playLabelRect = CreateRect("Label", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 44f));
        TextMeshProUGUI playLabel = CreateText(playLabelRect, "Play", 34, Color.white);
        playLabel.fontStyle = FontStyles.Bold;
        playLabel.alignment = TextAlignmentOptions.Center;

        RectTransform hintRect = CreateRect("Hint", _overlayRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -204f), new Vector2(250f, 30f));
        TextMeshProUGUI hintText = CreateText(hintRect, "Tap Play to open the puzzle board", 18, new Color(1f, 1f, 1f, 0.9f));
        hintText.alignment = TextAlignmentOptions.Center;
    }

    private void ShowMenu()
    {
        _shouldBlockAutoStart = true;

        if (_overlayRoot != null)
            _overlayRoot.SetActive(true);

        SetGameplayVisible(false);

        var menuClip = Resources.Load<AudioClip>("OyunMenu");
        if (menuClip != null)
            AudioManager.Instance?.PlayMusicClip(menuClip);
        else
            AudioManager.Instance?.PlayMainMenuMusic();
    }

    private void HandlePlayPressed()
    {
        _shouldBlockAutoStart = false;

        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);

        SetGameplayVisible(true);
        GameManager.Instance?.BeginGameplay();
        AudioManager.Instance?.StopMusic();
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
        if (_overlayRoot != null)
            Destroy(_overlayRoot);

        _overlayRoot = null;
        _canvas = null;
        _playAreaRoot = null;
        _gridManager = null;
        _playerToken = null;
        _swipeInput = null;
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
}
