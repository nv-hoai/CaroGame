using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : MonoBehaviour
{
    public GameObject registerButton;
    public GameObject email;

    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField emailInput;
    
    public TextMeshProUGUI errorText;

    private bool isWaitingForResponse = false;
    private bool isLoginSuccess = false;
    private float serverResponseTimeout = 5f;

    void Start()
    {
        CanvasManager.Instance.panelDict.Add("LoginPanel", gameObject);

        GameManager.Instance.Client.OnLoginFailed += (message) =>
        {
            errorText.color = Color.red;
            errorText.text = message;
            isWaitingForResponse = false;
        };

        GameManager.Instance.Client.OnRegisterFailed += (message) =>
        {
            errorText.color = Color.red;
            errorText.text = message;
            isWaitingForResponse = false;
        };

        GameManager.Instance.Client.OnLoginSuccess += (loginResponse) =>
        {
            isWaitingForResponse = false;
            isLoginSuccess = true;
        };

        GameManager.Instance.Client.OnRegisterSuccess += (message) =>
        {
            isWaitingForResponse = false;
            errorText.color = Color.green;
            errorText.text = message;
            LoginForm();
        };

        registerButton.SetActive(false);
        email.SetActive(false);
    }

    public void RegisterForm()
    {
        errorText.text = "";
        errorText.color = Color.red;
        registerButton.SetActive(true);
        email.SetActive(true);
    }

    public void LoginForm()
    {
        errorText.text = "";
        errorText.color = Color.red;
        registerButton.SetActive(false);
        email.SetActive(false);
    }

    public void Login()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            errorText.text = "Username and Password cannot be empty.";
            return;
        }

        _ = GameManager.Instance.Client.Login(usernameInput.text, passwordInput.text);
        isWaitingForResponse = true;
        StartCoroutine(WaitingForServerRespond());
    }

    public void Register()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text) || string.IsNullOrEmpty(emailInput.text))
        {
            errorText.text = "Username, Password and Email cannot be empty.";
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(emailInput.text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            errorText.text = "Invalid email format.";
            return;
        }

        int randomNumber = Random.Range(1000, 9999);
        string playerName = "Player" + randomNumber;

        _ = GameManager.Instance.Client.Register(usernameInput.text, passwordInput.text, emailInput.text, playerName);
        isWaitingForResponse = true;
        StartCoroutine(WaitingForServerRespond());
    }

    public IEnumerator WaitingForServerRespond()
    {
        CanvasManager.Instance.OpenPanel("WaitingPanel");
        yield return new WaitForSeconds(1);

        float elapsedTime = 0f;
        while (isWaitingForResponse && elapsedTime < serverResponseTimeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        if (isWaitingForResponse)
        {
            errorText.color = Color.red;
            errorText.text = "Server request timed out. Please try again.";
            isWaitingForResponse = false;
        }

        CanvasManager.Instance.ClosePanel("WaitingPanel");
        if (isLoginSuccess) GotoScene("StartScene");
    }

    private void OnDestroy()
    {
        CanvasManager.Instance.panelDict.Remove("LoginPanel");
    }

    private void GotoScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
