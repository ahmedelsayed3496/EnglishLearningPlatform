document.getElementById('navbar-toggler').onclick = function () {
    document.getElementById('navbar-menu').classList.toggle('show');
};


// Audio Progress Tracking
document.addEventListener('DOMContentLoaded', function () {
    // Check if we're on a lesson page with audio
    const audioElement = document.querySelector('.lesson-audio');
    if (!audioElement) return;

    const lessonId = document.getElementById('lesson-id')?.value;
    if (!lessonId) return;

    // Get or initialize progress data
    let audioProgress = localStorage.getItem('lesson_' + lessonId + '_progress');
    audioProgress = audioProgress ? JSON.parse(audioProgress) : {
        currentTime: 0,
        duration: 0,
        completed: false,
        percentCompleted: 0
    };

    // Set up progress tracking
    audioElement.addEventListener('loadedmetadata', function () {
        audioProgress.duration = audioElement.duration;
        updateProgressBar();

        // If we have stored progress, set the current time
        if (audioProgress.currentTime > 0) {
            audioElement.currentTime = audioProgress.currentTime;
        }
    });

    // Track progress during playback
    audioElement.addEventListener('timeupdate', function () {
        audioProgress.currentTime = audioElement.currentTime;
        audioProgress.percentCompleted = Math.floor((audioProgress.currentTime / audioProgress.duration) * 100);

        // Mark as completed if listened to at least 90% of audio
        if (audioProgress.percentCompleted >= 90 && !audioProgress.completed) {
            audioProgress.completed = true;

            // Send to server that lesson is completed
            updateLessonCompletionStatus(lessonId, true);
        }

        // Save progress to localStorage
        localStorage.setItem('lesson_' + lessonId + '_progress', JSON.stringify(audioProgress));

        // Update UI
        updateProgressBar();
    });

    function updateProgressBar() {
        const progressBar = document.querySelector('.audio-progress-bar');
        const progressPercentage = document.querySelector('.audio-progress-percentage');
        const currentTimeDisplay = document.querySelector('.audio-current-time');
        const totalTimeDisplay = document.querySelector('.audio-total-time');

        if (progressBar) {
            progressBar.style.width = audioProgress.percentCompleted + '%';
        }

        if (progressPercentage) {
            progressPercentage.textContent = audioProgress.percentCompleted + '%';
        }

        if (currentTimeDisplay) {
            currentTimeDisplay.textContent = formatTime(audioProgress.currentTime);
        }

        if (totalTimeDisplay) {
            totalTimeDisplay.textContent = formatTime(audioProgress.duration);
        }
    }

    // Format time to MM:SS
    function formatTime(seconds) {
        if (isNaN(seconds) || seconds === 0) return '00:00';
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return (mins < 10 ? '0' : '') + mins + ':' + (secs < 10 ? '0' : '') + secs;
    }

    // Update server with lesson completion
    function updateLessonCompletionStatus(lessonId, completed) {
        // Get the anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        fetch('/Lessons/UpdateProgress', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                lessonId: lessonId,
                percentCompleted: audioProgress.percentCompleted,
                completed: completed
            })
        })
            .then(response => {
                if (!response.ok) {
                    console.error('Failed to update lesson progress');
                }
            })
            .catch(error => console.error('Error updating lesson progress:', error));
    }

    // Initialize custom audio player controls if needed
    const playPauseButton = document.querySelector('.audio-play-pause');
    if (playPauseButton) {
        playPauseButton.addEventListener('click', function () {
            if (audioElement.paused) {
                audioElement.play();
                this.innerHTML = '<i class="bi bi-pause-fill"></i>';
            } else {
                audioElement.pause();
                this.innerHTML = '<i class="bi bi-play-fill"></i>';
            }
        });
    }
});
