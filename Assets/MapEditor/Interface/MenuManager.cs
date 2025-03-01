using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RustMapEditor.Variables;

public class MenuManager : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public Toggle lockToggle;
    public GameObject MenuPanel;
    public Button scaleButton;
    public Button closeButton;
    public GameObject confirmationPanel; // Reference to the confirmation panel
    public Button quitButton;           // Button to confirm quitting
    public Button cancelButton;         // Button to cancel quitting
    private bool isScaling = false;
    private RectTransform menuRectTransform;
    private RectTransform confirmationRectTransform;

    public static MenuManager Instance { get; private set; } // Singleton

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        menuRectTransform = GetComponent<RectTransform>();
        confirmationRectTransform = confirmationPanel.GetComponent<RectTransform>();

        // Hook up the close button to show the confirmation panel
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ShowConfirmationPanel);
        }
        else
        {
            Debug.LogWarning("Close button is not assigned in the Inspector.");
        }

        // Hook up confirmation panel buttons
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(CloseApplication);
        }
        else
        {
            Debug.LogWarning("Quit button is not assigned in the Inspector.");
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(HideConfirmationPanel);
        }
        else
        {
            Debug.LogWarning("Cancel button is not assigned in the Inspector.");
        }

        // Ensure confirmation panel is hidden at start
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
		
		LoadMenuState();
    }
	
	public Vector3 GetMenuScale()
    {
        return menuRectTransform.localScale;
    }

    public void SaveMenuState()
    {
        SettingsManager.menuState = new MenuState(menuRectTransform.localScale, menuRectTransform.position);
        SettingsManager.SaveSettings();
    }
	
	public void LoadMenuState()
	{
		Vector3 loadedScale = SettingsManager.menuState.scale;
		Vector3 loadedPosition = SettingsManager.menuState.position;

    if (loadedPosition == Vector3.zero)
    {
        Vector3 parentOff = menuRectTransform.parent != null ? menuRectTransform.parent.position : Vector3.zero;
        loadedPosition = new Vector3(parentOff.x*2, parentOff.y*2, 0);
    }

		if (loadedScale == Vector3.zero || loadedScale.x <= 0 || loadedScale.y <= 0)
		{
			loadedScale = Vector3.one * 1.5f;
		}

		loadedScale.x = Mathf.Clamp(loadedScale.x, 1f, 3f);
		loadedScale.y = Mathf.Clamp(loadedScale.y, 1f, 3f);
		loadedScale.z = 1f;

		// Adjust for parent offset to achieve desired WORLD position
		Vector3 parentOffset = menuRectTransform.parent != null ? menuRectTransform.parent.position : Vector3.zero;
		

		// Apply scale and adjusted local position
		menuRectTransform.localScale = loadedScale;
		menuRectTransform.position = loadedPosition;
        if (Compass.Instance != null)
        {
            Compass.Instance.SyncScaleWithMenu();
        }
	}
	
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!lockToggle.isOn)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(scaleButton.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))
            {
                isScaling = true;
                ScaleMenu(eventData);
            }
            else
            {
                isScaling = false;
                transform.localPosition += new Vector3(eventData.delta.x, eventData.delta.y, 0f);
            }

            transform.SetAsLastSibling();
            MenuPanel.transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (lockToggle.isOn)
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (isScaling)
        {
            ScaleMenu(eventData);
            return;
        }

        transform.localPosition += new Vector3(eventData.delta.x, eventData.delta.y, 0f);
		SaveMenuState();
    }

    public void ScaleMenu(PointerEventData eventData)
    {
        // Invert scaling: down/left increases scale, up/right decreases scale
        float scaleChange = (eventData.delta.x * -0.004f) + (eventData.delta.y * -0.004f);
        Vector3 newScale = menuRectTransform.localScale + new Vector3(scaleChange, scaleChange, 0f);

        // Clamp scale between sensible limits
        newScale.x = Mathf.Clamp(newScale.x, 1f, 3f);
        newScale.y = Mathf.Clamp(newScale.y, 1f, 3f);



        // Apply adjusted scale to menu
        menuRectTransform.localScale = newScale;
		
		        // Subtract 1 from the scale for application (effective range: 0 to 2)
        Vector3 adjustedScale = newScale - Vector3.one;

        // Scale all windows via AppManager singleton with adjusted scale
        if (AppManager.Instance != null)
        {
            AppManager.Instance.ScaleAllWindows(adjustedScale);
        }
		
		if (Compass.Instance != null)
        {
            Compass.Instance.SyncScaleWithMenu();
        }
		
		SaveMenuState();
    }

    public void ShowConfirmationPanel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            confirmationRectTransform.localScale = menuRectTransform.localScale- Vector3.one; // Match menu scale
            confirmationPanel.transform.SetAsLastSibling(); // Bring to front
        }
    }

    public void HideConfirmationPanel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }

    public void CloseApplication()
    {
        // Exit the application
        Application.Quit();

        // For testing in the Unity Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}