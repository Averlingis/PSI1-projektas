import { useState, useEffect } from "react";
import { getQuizProgress } from "../api";

export default function QuizDetail({ quiz, onBack, onPlay, onContinue }) {
  const [starting, setStarting] = useState(false);
  const [error, setError] = useState("");
  // { nextQuestionIndex, answeredCount, totalQuestions }, null until loaded
  const [progress, setProgress] = useState(null);

  // load how far the user got, to decide which buttons to show
  useEffect(() => {
    let cancelled = false;

    getQuizProgress(quiz.id)
      .then((data) => {
        if (!cancelled) setProgress(data);
      })
      .catch(() => {
        // without progress only "Play" is shown, which still works
      });

    return () => {
      cancelled = true;
    };
  }, [quiz.id]);

  // partly answered: at least one answer, but not finished
  const canContinue =
    progress !== null && progress.answeredCount > 0 && progress.nextQuestionIndex !== null;

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