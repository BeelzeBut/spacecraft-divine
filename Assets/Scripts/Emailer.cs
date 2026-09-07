using System.Collections;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using TMPro;

// Feedback form. Sending is only enabled when an SMTP config asset is present at
// Resources/smtp_config.json (see smtp_config.example.json). That file is intentionally
// git-ignored: shipping SMTP credentials inside a client build exposes them to anyone who
// unpacks the APK, so they must never live in source control. When the config is absent the
// form still opens and validates input, but sending is disabled.
public class Emailer : MonoBehaviour
{
    public TextMeshProUGUI errorMessage;
    public TMP_InputField feedbackInput;
    public TMP_InputField feedbackName;
    [SerializeField] UnityEngine.UI.Button btnSubmit;
    [SerializeField] bool sendDirect;

    [System.Serializable]
    private class SmtpConfig
    {
        public string senderAddress;
        public string senderPassword;
        public string receiverAddress;
        public string host = "smtp.gmail.com";
        public int port = 587;
    }

    private SmtpConfig config;

    void Start()
    {
        LoadConfig();
        Reset();
    }

    private void LoadConfig()
    {
        TextAsset asset = Resources.Load<TextAsset>("smtp_config");
        if (asset == null)
            return;

        SmtpConfig loaded = JsonUtility.FromJson<SmtpConfig>(asset.text);
        if (loaded != null && !string.IsNullOrWhiteSpace(loaded.senderAddress))
            config = loaded;
    }

    // Method 1: Direct message
    public void SendAnEmail()
    {
        errorMessage.text = "";
        if (string.IsNullOrWhiteSpace(feedbackInput.text))
        {
            errorMessage.text = "You haven't typed anything!";
            return;
        }
        if (string.IsNullOrWhiteSpace(feedbackName.text))
        {
            errorMessage.text = "Please fill in your name so I know who helps me make this game better!";
            return;
        }
        if (config == null)
        {
            errorMessage.text = "Feedback sending is disabled in this build.";
            return;
        }
        else {
            btnSubmit.interactable = false;
            // Create mail
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(config.senderAddress);
            mail.To.Add(config.receiverAddress);
            mail.Subject = "New game feedback!";
            mail.Body = feedbackName.text + " - " + feedbackInput.text;

            // Setup server
            SmtpClient smtpServer = new SmtpClient(config.host);
            smtpServer.Timeout = 10000;
            smtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpServer.UseDefaultCredentials = false;
            smtpServer.Port = config.port;
            smtpServer.Credentials = new NetworkCredential(
                config.senderAddress, config.senderPassword) as ICredentialsByHost;
            smtpServer.EnableSsl = true;
            ServicePointManager.ServerCertificateValidationCallback =
                delegate (object s, X509Certificate certificate,
                X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };
            mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
            smtpServer.Send(mail);
            CloseFeedbackForm();
            btnSubmit.interactable = true;
        }
    }
    public void CloseFeedbackForm()
    {
        StartCoroutine(CloseFeedbackFormC());
        if(DataHolder.instance.isInMenu)
            MainMenu.instance.mainMenu.SetActive(true);
    }
    IEnumerator CloseFeedbackFormC()
    {
        gameObject.GetComponent<Animator>().SetTrigger("close");
        yield return new WaitForSecondsRealtime(gameObject.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + .05f);
        gameObject.SetActive(false);
        Reset();
    }

    private void Reset()
    {
        feedbackInput.text = "";
        errorMessage.text = "";
    }
}
