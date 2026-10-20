import { useState, useEffect } from "react";
import { getCategories, getLanguages, getQuizzes } from "../api";

// List of quizzes with language and category filters
export default function QuizList({ initialLanguage, initialCategory }) {
  const [language, setLanguage] = useState(initialLanguage ?? "");
  const [category, setCategory] = useState(initialCategory ?? "");
  const [languages, setLanguages] = useState([]);
  const [categories, setCategories] = useState([]);
  // null until the first response arrives
  const [quizzes, setQuizzes] = useState(null);
  const [error, setError] = useState("");

  // options for the dropdowns, loaded once
  useEffect(() => {
    getLanguages().then(setLanguages).catch(() => {});
    getCategories().then(setCategories).catch(() => {});
  }, []);

  // reload the list whenever a filter change
  useEffect(() => {
    getQuizzes({ language, category })
      .then((list) => {
        setQuizzes(list);
        setError("");
      })
      .catch((err) => setError(err.message));
  }, [language, category]);

  return (
    <div className="quiz-list">
      <h2>Quizzes</h2>

      <div className="quiz-filters">
        <select value={language} onChange={(e) => setLanguage(e.target.value)}>
          <option value="">All languages</option>
          {languages.map((lang) => (
            <option key={lang} value={lang}>{lang}</option>
          ))}
        </select>

        <select value={category} onChange={(e) => setCategory(e.target.value)}>
          <option value="">All categories</option>
          {categories.map((cat) => (
            <option key={cat} value={cat}>{cat}</option>
          ))}
        </select>
      </div>

      {error && <p className="auth-error">{error}</p>}

      {quizzes === null && !error && <p>Loading quizzes...</p>}

      {quizzes?.length === 0 && <p>No quizzes match these filters.</p>}

      {quizzes?.map((quiz) => (
        <div key={quiz.id} className="quiz-item">
          <h3>{quiz.title}</h3>
          {quiz.description && <p>{quiz.description}</p>}
          <p className="quiz-meta">
            {quiz.language} · {quiz.category} · {quiz.questionCount} questions
          </p>
        </div>
      ))}
    </div>
  );
}