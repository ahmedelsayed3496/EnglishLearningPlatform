using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text.Json;

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
            // Call the enhanced method and return just the audio path
            var (audioPath, _) = await GenerateAudioWithTimingsAsync(text, fileName);
            return audioPath;
        }

        public async Task<(string AudioPath, string TimingsJson)> GenerateAudioWithTimingsAsync(string text, string fileName)
        {
            var outputPath = Path.Combine("wwwroot/audio", fileName + ".wav");
            var timingsOutputPath = Path.Combine("wwwroot/audio", fileName + ".json");

            var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

            // Generate SSML with word boundary information
            string ssml = GenerateSsmlWithWordBoundary(text);

            using var audioConfig = AudioConfig.FromWavFileOutput(outputPath);
            using var synthesizer = new SpeechSynthesizer(config, audioConfig);

            var wordTimings = new List<WordTiming>();
            var wordIndex = 0;
            string[] words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // Subscribe to word boundary event
            synthesizer.WordBoundary += (s, e) =>
            {
                // Only process if we haven't exceeded our word count
                if (wordIndex < words.Length)
                {
                    var timing = new WordTiming
                    {
                        Index = wordIndex,
                        Word = words[wordIndex],
                        OffsetMs = (long)(e.AudioOffset / 10000), // Convert from ticks (100 nanoseconds) to milliseconds
                        DurationMs = e.Duration.TotalMilliseconds
                    };
                    wordTimings.Add(timing);
                    wordIndex++;
                }
            };

            // Use SpeakSsmlAsync instead of SpeakTextAsync to get word boundaries
            var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                // Save word timings to JSON file
                string timingsJson = JsonSerializer.Serialize(wordTimings,
                    new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(timingsOutputPath, timingsJson);

                return ("/audio/" + fileName + ".wav", timingsJson);
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

        public async Task<string> GenerateFlashcardAudioAsync(string text)
        {
            // Create a unique file name based on the content
            string fileName = "flashcard_" + Guid.NewGuid().ToString();
            return await GenerateAudioFromTextAsync(text, fileName);
        }

        private string GenerateSsmlWithWordBoundary(string text)
        {
            // Escape the text for XML
            text = System.Security.SecurityElement.Escape(text) ?? "";

            // Create SSML with word boundary enabled
            return $@"
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'>
    <voice name='en-US-SaraNeural'>
        <mstts:prosody rate='0.95'>
            <mstts:silence type='Tailing' value='0ms'/>
            <prosody rate='0.95'>
                {text}
            </prosody>
        </mstts:prosody>
    </voice>
</speak>";
        }
    }

    
    public class WordTiming
    {
        public int Index { get; set; }         // Index of the word in the text
        public string Word { get; set; } = "";  // The actual word
        public long OffsetMs { get; set; }     // Time offset in milliseconds
        public double DurationMs { get; set; } // Duration in milliseconds
    }



}
