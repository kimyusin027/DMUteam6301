using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AuthenticationManager : MonoBehaviour
{
    private FirebaseAuth auth;
    private FirebaseUser user;

    public static event Action<bool> OnAuthStateChanged;
    public static event Action<string> OnAuthError;

    public static AuthenticationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Root GameObject로 만들기
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;

                // 자동 로그인 방지: 기존 세션 삭제
                if (auth.CurrentUser != null)
                {
                    Debug.Log("Clearing existing session to prevent auto-login");
                    auth.SignOut();
                }

                auth.StateChanged += AuthStateChanged;
                Debug.Log("Firebase initialized successfully - Auto-login disabled");
            }
            else
            {
                Debug.LogError("Firebase initialization failed: " + dependencyStatus);
                OnAuthError?.Invoke("Firebase initialization failed.");
            }
        });
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("User signed out");
            }
            user = auth.CurrentUser;
            OnAuthStateChanged?.Invoke(signedIn);

            if (signedIn)
            {
                Debug.Log("User signed in: " + (user.DisplayName ?? user.Email));

                // 메인 스레드에서 씬 전환 실행
                StartCoroutine(LoadLobbySceneCoroutine());
            }
        }
    }

    private IEnumerator LoadLobbySceneCoroutine()
    {
        // 한 프레임 기다린 후 씬 전환
        yield return null;

        try
        {
            if (Application.CanStreamedLevelBeLoaded("LobbyScene"))
            {
                Debug.Log("Loading LobbyScene...");
                SceneManager.LoadScene("LobbyScene");
            }
            else if (Application.CanStreamedLevelBeLoaded(1)) // Index로 시도
            {
                Debug.Log("Loading LobbyScene by index...");
                SceneManager.LoadScene(1);
            }
            else
            {
                Debug.LogError("LobbyScene is not in Build Settings! Please add it to File -> Build Settings");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to load LobbyScene: " + ex.Message);
        }
    }

    private string HashPassword(string password, string salt)
    {
        using (var sha256 = SHA256.Create())
        {
            var saltedPassword = password + salt;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    private string GenerateSalt()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var saltBytes = new byte[32];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
    }

    public async Task<bool> RegisterAsync(string email, string password, string displayName)
    {
        try
        {
            string salt = GenerateSalt();
            string hashedPassword = HashPassword(password, salt);

            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

            if (result.User != null)
            {
                var profile = new UserProfile
                {
                    DisplayName = displayName
                };

                await result.User.UpdateUserProfileAsync(profile);

                Debug.Log("Registration successful: " + displayName);
                return true;
            }
        }
        catch (FirebaseException ex)
        {
            Debug.LogError("Registration failed: " + ex.Message);
            string errorMessage = GetFirebaseErrorMessage(ex);
            OnAuthError?.Invoke(errorMessage);
        }
        catch (Exception ex)
        {
            Debug.LogError("Registration error: " + ex.Message);
            OnAuthError?.Invoke("An error occurred during registration.");
        }

        return false;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);
            Debug.Log("Login successful");
            return true;
        }
        catch (FirebaseException ex)
        {
            Debug.LogError("Login failed: " + ex.Message);
            string errorMessage = GetFirebaseErrorMessage(ex);
            OnAuthError?.Invoke(errorMessage);
        }
        catch (Exception ex)
        {
            Debug.LogError("Login error: " + ex.Message);
            OnAuthError?.Invoke("An error occurred during login.");
        }

        return false;
    }

    public void Logout()
    {
        if (auth != null)
        {
            auth.SignOut();
            SceneManager.LoadScene("LoginScene");
        }
    }

    private string GetFirebaseErrorMessage(FirebaseException ex)
    {
        AuthError errorCode = (AuthError)ex.ErrorCode;

        switch (errorCode)
        {
            case AuthError.EmailAlreadyInUse:
                return "This email is already in use.";
            case AuthError.InvalidEmail:
                return "Invalid email format.";
            case AuthError.WeakPassword:
                return "Password is too weak. (minimum 6 characters)";
            case AuthError.WrongPassword:
                return "Incorrect password.";
            case AuthError.UserNotFound:
                return "User not found.";
            case AuthError.TooManyRequests:
                return "Too many requests. Please try again later.";
            default:
                return "Authentication error occurred: " + ex.Message;
        }
    }

    public string GetCurrentUserName()
    {
        return user?.DisplayName ?? user?.Email ?? "Unknown";
    }

    public string GetCurrentUserId()
    {
        return user?.UserId ?? "";
    }

    public bool IsLoggedIn()
    {
        return user != null;
    }

    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }
}