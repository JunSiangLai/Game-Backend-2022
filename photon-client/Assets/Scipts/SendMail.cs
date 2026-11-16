using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public class SendMail : MonoBehaviour
{
    public SendMessage sendMessage;
    public InputField Username;
    public InputField recipientEmail;
    public InputField Code;
    public InputField NewPassword;
    public GameObject request;
    public GameObject result;
    private char[] codeSent;
    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private void Awake()
    {
        codeSent = new char[5];
    }

    public void Reset()
    {
        recipientEmail.text = "";
        Code.text = "";
        NewPassword.text = "";
        codeSent = new char[5];
    }

    public void SendEmail()
    {
        if (string.IsNullOrEmpty(recipientEmail.text) || !recipientEmail.text.Contains("@gmail.com"))
            return;

        MailMessage mail = new MailMessage();
        SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
        SmtpServer.Timeout = 10000;
        SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
        SmtpServer.UseDefaultCredentials = false;
        SmtpServer.Port = 587;

        mail.From = new MailAddress("YOUR_SENDER_EMAIL@gmail.com");
        mail.To.Add(new MailAddress(recipientEmail.text));

        mail.Subject = "Chad Station Password Reset";
        for (int i = 0; i < 5; i++)
        {
            codeSent[i] = chars[Random.Range(0, chars.Length)];
        }
        mail.Body = "Code: " + new string(codeSent);


        SmtpServer.Credentials = new System.Net.NetworkCredential("YOUR_SENDER_EMAIL@gmail.com", "YOUR_GMAIL_APP_PASSWORD") as ICredentialsByHost; SmtpServer.EnableSsl = true;
        ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        };

        mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
        SmtpServer.Send(mail);
    }

    public void CheckCode()
    {
        if (string.IsNullOrEmpty(Code.text) || string.IsNullOrEmpty(NewPassword.text))
            return;

        if (Code.text.Equals(new string(codeSent)))
        {
            sendMessage.OnSendMessageButtonClicked(8);
        }
    }

    public void CodeAccepted()
    {
        request.gameObject.SetActive(false);
        result.gameObject.SetActive(true);
    }

    public void ResultClose()
    {
        Reset();
        request.gameObject.SetActive(true);
        result.gameObject.SetActive(false);
    }
}
