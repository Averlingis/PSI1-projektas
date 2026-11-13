import { useState } from "react";

export default function QuizDetail({ quiz, onBack, onPlay }) {
  const [starting, setStarting] = useState(false);
  const [error, setError] = useState("");

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

        <button onClick={handlePlay} disabled={starting}>
          {starting ? "Starting..." : "Play"}
        </button>
      </div>
    </div>
  );
}