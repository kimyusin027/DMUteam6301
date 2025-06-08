using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button showRegisterButton;

    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private TMP_InputField displayNameInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button showLoginButton;

    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingPanel;

    [SerializeField] private TextMeshProUGUI passwordRequirements;

    private AuthenticationManager authManager;

    private void Start()
    {
        authManager = AuthenticationManager.Instance;
        SetupUI();
        SetupEvents();

        ShowLoginPanel();
    }

    private void SetupUI()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        showRegisterButton.onClick.AddListener(ShowRegisterPanel);
        showLoginButton.onClick.AddListener(ShowLoginPanel);

        registerPasswordInput.onValueChanged.AddListener(OnPasswordChanged);
        confirmPasswordInput.onValueChanged.AddListener(OnConfirmPasswordChanged);

        SetLoadingState(false);
        ClearErrorText();

        passwordRequirements.text = "Password requirements:\n* At least 8 characters";
    }

    private void SetupEvents()
    {
        AuthenticationManager.OnAuthStateChanged += OnAuthStateChanged;
        AuthenticationManager.OnAuthError += OnAuthError;
    }

    private void OnDestroy()
    {
        AuthenticationManager.OnAuthStateChanged -= OnAuthStateChanged;
        AuthenticationManager.OnAuthError -= OnAuthError;
    }

    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        ClearAllInputs();
        ClearErrorText();
    }

    private void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        ClearAllInputs();
        ClearErrorText();
    }

    private void ClearAllInputs()
    {
        loginEmailInput.text = "";
        loginPasswordInput.text = "";
        registerEmailInput.text = "";
        registerPasswordInput.text = "";
        confirmPasswordInput.text = "";
        displayNameInput.text = "";
    }

    private async void OnLoginButtonClicked()
    {
        string email = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;

        if (!ValidateLoginInput(email, password))
            return;

        SetLoadingState(true);

        bool success = await authManager.LoginAsync(email, password);

        SetLoadingState(false);

        if (success)
        {
            Debug.Log("Login successful!");
        }
    }

    private async void OnRegisterButtonClicked()
    {
        string email = registerEmailInput.text.Trim();
        string password = registerPasswordInput.text;
        string confirmPassword = confirmPasswordInput.text;
        string displayName = displayNameInput.text.Trim();

        if (!ValidateRegisterInput(email, password, confirmPassword, displayName))
            return;

        SetLoadingState(true);

        bool success = await authManager.RegisterAsync(email, password, displayName);

        SetLoadingState(false);

        if (success)
        {
            Debug.Log("Registration successful!");
            ShowSuccessMessage("Registration completed successfully!");
        }
    }

    private bool ValidateLoginInput(string email, string password)
    {
        if (string.IsNullOrEmpty(email))
        {
            ShowErrorMessage("Please enter your email.");
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowErrorMessage("Please enter your password.");
            return false;
        }

        if (!IsValidEmail(email))
        {
            ShowErrorMessage("Please enter a valid email address.");
            return false;
        }

        return true;
    }

    private bool ValidateRegisterInput(string email, string password, string confirmPassword, string displayName)
    {
        if (string.IsNullOrEmpty(email))
        {
            ShowErrorMessage("Please enter your email.");
            return false;
        }

        if (!IsValidEmail(email))
        {
            ShowErrorMessage("Please enter a valid email address.");
            return false;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            ShowErrorMessage("Please enter a display name.");
            return false;
        }

        if (displayName.Length < 2 || displayName.Length > 20)
        {
            ShowErrorMessage("Display name must be 2-20 characters.");
            return false;
        }

        if (!IsValidPassword(password))
        {
            ShowErrorMessage("Password must be at least 8 characters long.");
            return false;
        }

        if (password != confirmPassword)
        {
            ShowErrorMessage("Password confirmation does not match.");
            return false;
        }

        return true;
    }

    private bool IsValidEmail(string email)
    {
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    private bool IsValidPassword(string password)
    {
        return password.Length >= 8;
    }

    private void OnPasswordChanged(string password)
    {
        UpdatePasswordRequirements(password);
    }

    private void OnConfirmPasswordChanged(string confirmPassword)
    {
        if (string.IsNullOrEmpty(confirmPassword)) return;

        if (registerPasswordInput.text == confirmPassword)
        {
            confirmPasswordInput.GetComponent<Image>().color = Color.green;
        }
        else
        {
            confirmPasswordInput.GetComponent<Image>().color = Color.red;
        }
    }

    private void UpdatePasswordRequirements(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            passwordRequirements.color = Color.white;
            return;
        }

        bool lengthOk = password.Length >= 8;

        string requirements = "Password requirements:\n";
        requirements += (lengthOk ? "<color=green>✓</color>" : "<color=red>X</color>") + " At least 8 characters";

        passwordRequirements.text = requirements;

        if (IsValidPassword(password))
        {
            registerPasswordInput.GetComponent<Image>().color = Color.green;
        }
        else
        {
            registerPasswordInput.GetComponent<Image>().color = Color.red;
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        loadingPanel.SetActive(isLoading);

        loginButton.interactable = !isLoading;
        registerButton.interactable = !isLoading;
        showRegisterButton.interactable = !isLoading;
        showLoginButton.interactable = !isLoading;
    }

    private void ShowErrorMessage(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
        errorText.gameObject.SetActive(true);

        Invoke(nameof(ClearErrorText), 3f);
    }

    private void ShowSuccessMessage(string message)
    {
        errorText.text = message;
        errorText.color = Color.green;
        errorText.gameObject.SetActive(true);

        Invoke(nameof(ClearErrorText), 2f);
    }

    private void ClearErrorText()
    {
        errorText.gameObject.SetActive(false);
        errorText.text = "";
    }

    private void OnAuthStateChanged(bool isLoggedIn)
    {
        if (isLoggedIn)
        {
            Debug.Log("User authentication state changed: signed in");
        }
    }

    private void OnAuthError(string errorMessage)
    {
        ShowErrorMessage(errorMessage);
        SetLoadingState(false);
    }
}