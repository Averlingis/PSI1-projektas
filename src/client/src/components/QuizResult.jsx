import { useState, useEffect } from "react";
import { getQuizScore } from "../api";

// Shows the user's final score after finishing a quiz
export default function QuizResult({ quiz, onPlayAgain, onBackToQuizzes }) {
  const [score, setScore] = useState(null);
  const [error, setError] = useState("");

  // load the score, ignore the response if the screen was already left
  useEffect(() => {
    let cancelled = false;

    getQuizScore(quiz.id)
      .then((data) => {
        if (!cancelled) setScore(data);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [quiz.id]);
  
  return (
    <div className="quiz-detail">
      <h2>{quiz.title}</h2>

      {error && <p className="auth-error">{error}</p>}

      {!score && !error && <p>Loading your score...</p>}

      {score && (
        <>
          <p className="quiz-score">
            {score.correctAnswers} out of {score.totalAnswered} correct
          </p>
          <p>
            {score.correctAnswers === score.totalAnswered
              ? "Perfect score! Congratulations"
              : score.correctAnswers >= score.totalAnswered / 2
                ? "Well done!"
                : "😞"}
          </p>
        </>
      )}

      <div className="step-actions">
        <button className="secondary-button" onClick={onBackToQuizzes}>
          Back to quizzes
        </button>
        <button onClick={onPlayAgain}>
          Play again
        </button>
      </div>
    </div>
  );
}