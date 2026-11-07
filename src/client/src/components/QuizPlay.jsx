import { useState, useEffect } from "react";
import { getQuestion, answerQuestion, resetQuizScore } from "../api";

// Shows a question at a time with feedback after answer
export default function QuizPlay({ quiz, onFinish, onQuit }) {
  const [index, setIndex] = useState(0);
  const [question, setQuestion] = useState(null);
  // the option that a user clicked, and the backend return
  const [selectedIndex, setSelectedIndex] = useState(null);
  const [result, setResult] = useState(null); // { isCorrect, correctOptionText }
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const isLastQuestion = index === quiz.questionCount - 1;

  // clear answers from an earlier play, then load q
  useEffect(() => {
    let cancelled = false;

    resetQuizScore(quiz.id)
      .then(() => getQuestion(quiz.id, 0))
      .then((first) => {
        if (!cancelled) setQuestion(first);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [quiz.id]);

  async function handleAnswer(optionIndex) {
    // only one answer per q
    if (result || submitting) return;

    setSelectedIndex(optionIndex);
    setSubmitting(true);
    setError("");

    try {
      const data = await answerQuestion(quiz.id, index, optionIndex);
      setResult(data);
    } catch (err) {
      setError(err.message);
      setSelectedIndex(null);
    } finally {
      setSubmitting(false);
    }
  }

  async function handleNext() {
    if (isLastQuestion) {
      onFinish();
      return;
    }

    const nextIndex = index + 1;
    setError("");

    try {
      const next = await getQuestion(quiz.id, nextIndex);
      setIndex(nextIndex);
      setQuestion(next);
      setSelectedIndex(null);
      setResult(null);
    } catch (err) {
      setError(err.message);
    }
  }

  // the backend returns the correct answer as text
  function optionClass(option, optionIndex) {
    if (!result) return "quiz-option";
    if (option === result.correctOptionText) return "quiz-option correct";
    if (optionIndex === selectedIndex) return "quiz-option wrong";
    return "quiz-option";
  }

  if (!question) {
    return (
      <div className="quiz-play">
        {error ? <p className="auth-error">{error}</p> : <p>Loading question...</p>}
        <div className="step-actions">
          <button className="secondary-button" onClick={onQuit}>
            Back to details
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="quiz-play">
      <p className="quiz-progress">
        {quiz.title} · Question {index + 1} of {quiz.questionCount}
      </p>

      <h2>{question.questionText}</h2>

      <div className="quiz-options">
        {question.options.map((option, optionIndex) => (
          <button
            key={optionIndex}
            type="button"
            className={optionClass(option, optionIndex)}
            disabled={result !== null || submitting}
            onClick={() => handleAnswer(optionIndex)}
          >
            {option}
          </button>
        ))}
      </div>

      {result && (
        <p
          className={`quiz-feedback ${result.isCorrect ? "correct" : "wrong"}`}
          aria-live="polite"
        >
          {result.isCorrect
            ? "Correct!"
            : `Not quite. The correct answer is "${result.correctOptionText}".`}
        </p>
      )}

      {error && <p className="auth-error">{error}</p>}

      <div className="step-actions">
        <button className="secondary-button" onClick={onQuit}>
          Quit
        </button>
        {result && (
          <button onClick={handleNext}>
            {isLastQuestion ? "Finish" : "Next"}
          </button>
        )}
      </div>
    </div>
  );
}