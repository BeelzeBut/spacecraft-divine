using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Amazon.SimpleNotificationService;
using Amazon.Runtime;

public class Feedback : MonoBehaviour
{
    public TMP_InputField feedbackInput; // where the user's message will go
    public TMP_Text errorMessage; // we'll display any errors here
    public Button sendButton, cancelButton;
    // fill these out with your IAM user keys and SNS topic ARN.  you can also find your AWS region code
    // in the topic ARN, if you're not in us-west-2.
    public string SNSFeedbackUserKey, SNSFeedbackUserSecret, SNSRegion = "eu-central-1", SNSTopicARN;

    void Start()
    {
        Reset();
    }

    void Reset()
    {
        feedbackInput.text = "";
        errorMessage.text = "";
    }

    public void OnClickCancel()
    {
        Reset();
        gameObject.SetActive(false);
    }

    // note `async` here which lets us use the `await` keyword in this method
    public async void OnClickSend()
    {
        // if the user hasn't entered any text, show an error
        if (string.IsNullOrWhiteSpace(feedbackInput.text))
        {
            errorMessage.text = "You haven't typed anything!";
            return;
        }
        // reset the error message in case we got an error last time
        errorMessage.text = "";
        // build our SNS credentials
        var credentials = new BasicAWSCredentials(SNSFeedbackUserKey, SNSFeedbackUserSecret);
        // build an SNS client (this also converts your region code into an Amazon.RegionEndpoint)
        var client = new AmazonSimpleNotificationServiceClient(credentials,
                Amazon.RegionEndpoint.GetBySystemName(SNSRegion));
        // disable the buttons to prevent double clicks
        sendButton.interactable = false;
        cancelButton.interactable = false;
        /*try
        {
            // build an SNS request that will publish the player's message to our topic 
            // using the email subject line "New player feedback!"
            var request = new Amazon.SimpleNotificationService.Model.PublishRequest(
                    SNSTopicARN, feedbackInput.text, "New player feedback!");
            // publish our SNS message.  the AWS SDK uses async Tasks which are sort of like 
            // Unity coroutines.  our method will suspend execution until this network request is 
            // finished, but it won't block other code from running in the meantime.  if anything
            // goes wrong, the SNS client will throw an exception.
            var result = await client.PublishAsync(request);
            if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new System.Exception($"Bad HTTP status code: {result.HttpStatusCode}");
            // everything worked, so hide this window
            OnClickCancel();
        }
        catch (System.Exception e)
        {
            errorMessage.text = $"Error sending feedback: {e.Message}";
        }*/
        //finally
        {
            sendButton.interactable = true;
            cancelButton.interactable = true;
        }
    }
}
