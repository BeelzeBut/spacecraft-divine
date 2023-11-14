using System.Collections;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using TMPro;

public class Emailer : MonoBehaviour
{
    public TextMeshProUGUI errorMessage;
    public TMP_InputField feedbackInput;
    public TMP_InputField feedbackName;
    [SerializeField] UnityEngine.UI.Button btnSubmit;
    [SerializeField] bool sendDirect;

    const string kSenderEmailAddress = "gamefeedback07@gmail.com";
    const string kSenderPassword = "Kodita0007";
    const string kReceiverEmailAddress = "bugamarco07@gmail.com";

    void Start()
    {
        Reset();
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
        else {
            btnSubmit.interactable = false;
            // Create mail
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(kSenderEmailAddress);
            mail.To.Add(kReceiverEmailAddress);
            mail.Subject = "New game feedback!";
            mail.Body = feedbackName.text + " - " + feedbackInput.text;

            // Setup server 
            SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
            smtpServer.Timeout = 10000;
            smtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpServer.UseDefaultCredentials = false;
            smtpServer.Port = 587;
            smtpServer.Credentials = new NetworkCredential(
                kSenderEmailAddress, kSenderPassword) as ICredentialsByHost;
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