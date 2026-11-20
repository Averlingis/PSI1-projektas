import { useState, useEffect } from "react";
import { getCategories, getLanguages, getQuizzes, getMyQuizzes } from "../api";

// List of quizzes with language and category filters
export default function QuizList({
  language,
  category,
  onLanguageChange,
  onCategoryChange,
  onSelectQuiz,
  showMine,
  onToggleMine,
}) {
  const [languages, setLanguages] = useState([]);
  const [categories, setCategories] = useState([]);
  // null until the first response arrives
  const [quizzes, setQuizzes] = useState(null);
  const [error, setError] = useState("");

  // options for the dropdowns, loaded once
  useEffect(() => {
    getLanguages().then(setLanguages).catch(() => { });
    getCategories().then(setCategories).catch(() => { });
  }, []);

  // reload the list whenever a filter or the list type changes
  useEffect(() => {
    let cancelled = false;
    const load = showMine ? getMyQuizzes : getQuizzes;

    load({ language, category })
      .then((list) => {
        if (cancelled) return;
        setQuizzes(list);
        setError("");
      })
      .catch((err) => {
        if (cancelled) return;
        setQuizzes(null);
        setError(err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [language, category, showMine]);

  return (
    <div className="quiz-list">
      <div className="quiz-list-header">
        <h2>{showMine ? "My Quizzes" : "Quizzes"}</h2>
        <button type="button" className="secondary-button" onClick={onToggleMine}>
          {showMine ? "Public Quizzes" : "My Quizzes"}
        </button>
      </div>

      <div className="quiz-filters">
        <select
          value={language}
          onChange={(e) => onLanguageChange(e.target.value)}
        >
          <option value="">All languages</option>
          {languages.map((lang) => (
            <option key={lang} value={lang}>{lang}</option>
          ))}
        </select>

        <select
          value={category}
          onChange={(e) => onCategoryChange(e.target.value)}
        >
          <option value="">All categories</option>
          {categories.map((cat) => (
            <option key={cat} value={cat}>{cat}</option>
          ))}
        </select>
      </div>

      <button
        type="button"
        className="secondary-button quiz-reset-button"
        onClick={() => {
          onLanguageChange(null);
          onCategoryChange(null);
        }}
      >
        Reset filters
      </button>

      {quizzes?.length === 0 && (
        <p>
          {showMine && !language && !category
            ? "You have not created any quizzes yet."
            : "No quizzes match these filters."}
        </p>
      )}

      {error && <p className="auth-error">{error}</p>}

      {quizzes === null && !error && <p>Loading quizzes...</p>}

      {quizzes?.map((quiz) => (
        <button
          key={quiz.id}
          type="button"
          className="quiz-item quiz-item-button"
          onClick={() => onSelectQuiz(quiz)}
        >
          <h3>{quiz.title}</h3>
          {quiz.description && <p>{quiz.description}</p>}
          <p className="quiz-meta">
            {quiz.language} · {quiz.category} · {quiz.questionCount} questions
            {showMine && (
              <span className="quiz-visibility">
                {quiz.isPublic ? "Public" : "Private"}
              </span>
            )}
          </p>
        </button>
      ))}
    </div >
  );
}