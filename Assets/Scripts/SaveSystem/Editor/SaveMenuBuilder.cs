using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Baut das Speichern/Laden-Menü als normale UI-Objekte in der Szene – im Stil des Pause-Menüs:
/// schwarzes, leicht transparentes Fenster, weiße abgerundete Buttons mit dunkler Schrift,
/// Titel in PixelFraktur. Danach kann alles im Editor frei angepasst werden.
/// Die Bausteine (internal) nutzt auch <see cref="DifficultyMenuBuilder"/>, damit alle Menüs gleich aussehen.
/// </summary>
public static class SaveMenuBuilder
{
    private const string TextFontPath = "Assets/Font/Sources/PressStart2P-Regular SDF.asset";
    private const string TitleFontPath = "Assets/Font/Sources/PixelFraktur SDF.asset";
    private const string ButtonSpritePath = "UI/Skin/UISprite.psd";

    // Farben wie im Pause-Menü.
    internal static readonly Color OverlayColor = new(0f, 0f, 0f, 0.5f);
    private static readonly Color WindowColor = new(0f, 0f, 0f, 0.88f);
    private static readonly Color RowColor = new(1f, 1f, 1f, 0.06f);
    private static readonly Color ButtonColor = Color.white;
    internal static readonly Color ButtonTextColor = new(0.196f, 0.196f, 0.196f, 1f);
    private static readonly Color DeleteTextColor = new(0.6f, 0.1f, 0.12f, 1f);
    private static readonly Color TextColor = Color.white;
    internal static readonly Color InfoTextColor = new(0.78f, 0.78f, 0.78f, 1f);

    private static TMP_FontAsset textFont;
    private static TMP_FontAsset titleFont;
    private static Sprite buttonSprite;

    public static SaveSlotMenu Build()
    {
        LoadStyle();

        GameObject root = CreateCanvasRoot("SaveSlotMenu");
        SaveSlotMenu menu = root.AddComponent<SaveSlotMenu>();

        GameObject panel = CreateStretched("Panel", root.transform, OverlayColor);
        GameObject window = CreateWindow(panel.transform, new Vector2(1250f, 820f));

        TMP_Text title = CreateTitle(window.transform, "Save Game");

        Transform slotList = CreateScrollList(window.transform);
        SaveSlotView[] slotViews = new SaveSlotView[SaveManager.TotalSlotCount];

        for (int slot = 0; slot < SaveManager.TotalSlotCount; slot++)
        {
            slotViews[slot] = CreateSlotRow(slotList, slot);
        }

        TMP_Text feedback = CreateText("Feedback", window.transform, string.Empty, 20, TextAlignmentOptions.Center, 40f);
        Button closeButton = CreateButton("CloseButton", window.transform, "Back", ButtonTextColor, 0f, 55f);

        GameObject confirmPanel = CreateConfirmDialog(
            panel.transform,
            out TMP_Text confirmText,
            out Button yesButton,
            out Button noButton);

        SerializedObject serialized = new(menu);
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("feedbackText").objectReferenceValue = feedback;
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        serialized.FindProperty("confirmPanel").objectReferenceValue = confirmPanel;
        serialized.FindProperty("confirmText").objectReferenceValue = confirmText;
        serialized.FindProperty("confirmYesButton").objectReferenceValue = yesButton;
        serialized.FindProperty("confirmNoButton").objectReferenceValue = noButton;

        SerializedProperty slotViewsProperty = serialized.FindProperty("slotViews");
        slotViewsProperty.arraySize = slotViews.Length;

        for (int i = 0; i < slotViews.Length; i++)
        {
            slotViewsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        confirmPanel.SetActive(false);

        return menu;
    }

    internal static void LoadStyle()
    {
        textFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFontPath);
        titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
        buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(ButtonSpritePath);
    }

    internal static TMP_Text CreateTitle(Transform parent, string text)
    {
        TMP_Text title = CreateText("Title", parent, text, 64, TextAlignmentOptions.Center, 90f);

        if (titleFont != null)
        {
            title.font = titleFont;
        }

        return title;
    }

    internal static GameObject CreateCanvasRoot(string name)
    {
        GameObject root = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // über dem Pause-Menü

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // passt auf jedes Seitenverhältnis

        return root;
    }

    private static SaveSlotView CreateSlotRow(Transform parent, int slot)
    {
        GameObject row = CreateImage($"Slot{slot + 1}", parent, RowColor);
        AddLayoutElement(row, preferredHeight: 150f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(25, 25, 25, 25);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject texts = new("Texts", typeof(RectTransform));
        texts.transform.SetParent(row.transform, false);
        AddLayoutElement(texts, flexibleWidth: 1f);

        VerticalLayoutGroup textLayout = texts.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 10f;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandHeight = false;

        TMP_Text title = CreateText("Title", texts.transform, $"Slot {slot + 1}", 28, TextAlignmentOptions.Left, 40f);
        TMP_Text info = CreateText("Info", texts.transform, "- Empty -", 18, TextAlignmentOptions.TopLeft, 60f);
        info.color = InfoTextColor;

        Button actionButton = CreateButton("ActionButton", row.transform, "Save", ButtonTextColor, 230f, 0f);
        Button deleteButton = CreateButton("DeleteButton", row.transform, "Delete", DeleteTextColor, 230f, 0f);

        SaveSlotView view = row.AddComponent<SaveSlotView>();

        SerializedObject serialized = new(view);
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("infoText").objectReferenceValue = info;
        serialized.FindProperty("actionButton").objectReferenceValue = actionButton;
        serialized.FindProperty("actionLabel").objectReferenceValue = actionButton.GetComponentInChildren<TMP_Text>();
        serialized.FindProperty("deleteButton").objectReferenceValue = deleteButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return view;
    }

    /// <summary>Scrollbare Liste (Viewport + Content + Scrollbar rechts). Gibt den Content zurück.</summary>
    private static Transform CreateScrollList(Transform parent)
    {
        const float ScrollbarWidth = 20f;

        GameObject scrollView = CreateImage("SlotList", parent, Color.clear);
        LayoutElement scrollElement = scrollView.AddComponent<LayoutElement>();
        scrollElement.flexibleHeight = 1f;

        // Transparentes Image, damit das Mausrad auch zwischen den Zeilen scrollt.
        GameObject viewport = CreateStretched("Viewport", scrollView.transform, Color.clear);
        viewport.AddComponent<RectMask2D>();

        GameObject content = new("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Scrollbar scrollbar = CreateScrollbar(scrollView.transform, ScrollbarWidth);

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = 10f;

        return content.transform;
    }

    private static Scrollbar CreateScrollbar(Transform parent, float width)
    {
        GameObject track = CreateImage("Scrollbar", parent, RowColor);

        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = Vector2.one;
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(width, 0f);

        GameObject slidingArea = new("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(track.transform, false);

        RectTransform areaRect = slidingArea.GetComponent<RectTransform>();
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.one;
        areaRect.offsetMin = Vector2.zero;
        areaRect.offsetMax = Vector2.zero;

        GameObject handle = CreateImage("Handle", slidingArea.transform, ButtonColor);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = buttonSprite;
        handleImage.type = Image.Type.Sliced;

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        Scrollbar scrollbar = track.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;

        return scrollbar;
    }

    private static GameObject CreateConfirmDialog(
        Transform parent,
        out TMP_Text message,
        out Button yesButton,
        out Button noButton)
    {
        GameObject overlay = CreateStretched("ConfirmDialog", parent, OverlayColor);
        GameObject box = CreateWindow(overlay.transform, new Vector2(900f, 380f));

        message = CreateText("Message", box.transform, "Are you sure?", 26, TextAlignmentOptions.Center, 180f);

        GameObject buttons = new("Buttons", typeof(RectTransform));
        buttons.transform.SetParent(box.transform, false);
        AddLayoutElement(buttons, preferredHeight: 80f);

        HorizontalLayoutGroup layout = buttons.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        yesButton = CreateButton("YesButton", buttons.transform, "Yes", ButtonTextColor, 250f, 0f);
        noButton = CreateButton("NoButton", buttons.transform, "No", ButtonTextColor, 250f, 0f);

        return overlay;
    }

    #region UI Helpers

    internal static GameObject CreateWindow(Transform parent, Vector2 size)
    {
        GameObject window = CreateImage("Window", parent, WindowColor);

        RectTransform rect = window.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        VerticalLayoutGroup layout = window.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 35, 35);
        layout.spacing = 20f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return window;
    }

    internal static GameObject CreateStretched(string name, Transform parent, Color color)
    {
        GameObject gameObject = CreateImage(name, parent, color);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return gameObject;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        gameObject.GetComponent<Image>().color = color;

        return gameObject;
    }

    internal static TMP_Text CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        float preferredHeight)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = TextColor;
        label.textWrappingMode = TextWrappingModes.Normal;

        if (textFont != null)
        {
            label.font = textFont;
        }

        if (preferredHeight > 0f)
        {
            AddLayoutElement(gameObject, preferredHeight: preferredHeight);
        }

        return label;
    }

    /// <summary>Weißer, abgerundeter Button wie im Pause-Menü.</summary>
    internal static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color textColor,
        float preferredWidth,
        float preferredHeight)
    {
        GameObject gameObject = CreateImage(name, parent, ButtonColor);

        Image image = gameObject.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;

        Button button = gameObject.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.784f, 0.784f, 0.784f, 1f);
        colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.disabledColor = new Color(0.784f, 0.784f, 0.784f, 0.5f);
        button.colors = colors;

        AddLayoutElement(gameObject, preferredWidth: preferredWidth, preferredHeight: preferredHeight);

        TMP_Text text = CreateText("Label", gameObject.transform, label, 18, TextAlignmentOptions.Center, 0f);
        text.color = textColor;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private static void AddLayoutElement(
        GameObject gameObject,
        float preferredWidth = 0f,
        float preferredHeight = 0f,
        float flexibleWidth = 0f)
    {
        LayoutElement element = gameObject.AddComponent<LayoutElement>();

        if (preferredWidth > 0f) element.preferredWidth = preferredWidth;
        if (preferredHeight > 0f) element.preferredHeight = preferredHeight;
        if (flexibleWidth > 0f) element.flexibleWidth = flexibleWidth;
    }

    #endregion
}
