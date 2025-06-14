using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;

namespace EnglishLearningPlatform.Services
{
    public class SpeechToTextService
    {
        private readonly string _subscriptionKey;
        private readonly string _region;

        public SpeechToTextService(IConfiguration configuration)
        {
            _subscriptionKey = configuration["AzureSpeech:SubscriptionKey"]
                ?? throw new ArgumentNullException(nameof(configuration), "AzureSpeech:SubscriptionKey is not configured.");
            _region = configuration["AzureSpeech:Region"]
                ?? throw new ArgumentNullException(nameof(configuration), "AzureSpeech:Region is not configured.");
        }

        public async Task<string> RecognizeSpeechAsync(string audioFilePath)
        {
            var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);

            using var audioInput = AudioConfig.FromWavFileInput(audioFilePath);
            using var recognizer = new SpeechRecognizer(config, audioInput);

            var allText = new System.Text.StringBuilder();

            var stopRecognition = new TaskCompletionSource<int>();

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    allText.AppendLine(e.Result.Text);
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                stopRecognition.TrySetResult(0);
            };

            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            await stopRecognition.Task;
            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            var resultText = allText.ToString().Trim();
            if (string.IsNullOrWhiteSpace(resultText))
                throw new Exception("Speech could not be recognized.");

            return resultText;
        }
    }
}
