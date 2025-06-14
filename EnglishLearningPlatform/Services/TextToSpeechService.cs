using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;

namespace EnglishLearningPlatform.Services
{
    public class TextToSpeechService
    {
        private readonly string _subscriptionKey;
        private readonly string _region;

        public TextToSpeechService(IConfiguration configuration)
        {
            _subscriptionKey = configuration["AzureSpeech:SubscriptionKey"]
                ?? throw new ArgumentNullException(nameof(configuration), "AzureSpeech:SubscriptionKey is not configured.");
            _region = configuration["AzureSpeech:Region"]
                ?? throw new ArgumentNullException(nameof(configuration), "AzureSpeech:Region is not configured.");
        }

        public async Task<string> GenerateAudioFromTextAsync(string text, string fileName)
        {
            var outputPath = Path.Combine("wwwroot/audio", fileName + ".wav");

            var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

            using var audioConfig = AudioConfig.FromWavFileOutput(outputPath);
            using var synthesizer = new SpeechSynthesizer(config, audioConfig);

            var result = await synthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                return "/audio/" + fileName + ".wav";
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                var errorMsg = $"Speech synthesis canceled: Reason={cancellation.Reason}, ErrorDetails={cancellation.ErrorDetails}";
                throw new Exception(errorMsg);
            }
            else
            {
                throw new Exception("Speech synthesis failed: " + result.Reason);
            }
        }
    }
}
