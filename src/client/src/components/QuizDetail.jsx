import { useState } from "react";

// progress is loaded by App before this page is shown,
export default function QuizDetail({ quiz, progress, onBack, onPlay, onContinue }) {
  const [starting, setStarting] = useState(false);
  const [error, setError] = useState("");

  // partly answered: at least one answer, but not finished
  const canContinue =
    progress != null && progress.answeredCount > 0 && progress.nextQuestionIndex !== null;

  // onPlay resets the old score first, so wait for it and show any error here
  async function handlePlay() {
    setError("");
    setStarting(true);
    try {
      await onPlay();
    } catch (err) {
      setError(err.message);
      setStarting(false);
    }
  }

  return (
    <div className="quiz-detail">
      <h2>{quiz.title}</h2>

      <p>{quiz.description || "No description available."}</p>

      <div className="quiz-detail-info">
        <p><strong>Category:</strong> {quiz.category}</p>
        <p><strong>Language:</strong> {quiz.language}</p>
        <p><strong>Questions:</strong> {quiz.questionCount}</p>
      </div>

      {error && <p className="auth-error">{error}</p>}

      {canContinue && (
        <p className="quiz-meta">
          You stopped at question {progress.nextQuestionIndex + 1} of {progress.totalQuestions}.
        </p>
      )}

      <div className="step-actions">
        <button className="secondary-button" onClick={onBack}>
          Back
        </button>

        {canContinue ? (
          <>
            <button className="secondary-button" onClick={handlePlay} disabled={starting}>
              {starting ? "Starting..." : "Start over"}
            </button>
            <button onClick={() => onContinue(progress.nextQuestionIndex)} disabled={starting}>
              Continue
            </button>
          </>
        ) : (
          <button onClick={handlePlay} disabled={starting}>
            {starting ? "Starting..." : "Play"}
          </button>
        )}
      </div>
    </div>
  );
}