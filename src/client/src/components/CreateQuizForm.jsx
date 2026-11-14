import { useState, useEffect } from "react";
import { createQuiz, getCategories, getLanguages } from "../api";

const emptyOption = () => ({ text: "", isCorrect: false });
const emptyQuestion = () => ({
  text: "",
  options: [emptyOption(), emptyOption()],
});

export default function CreateQuizForm({ onBack, onCreated }) {
  const [quiz, setQuiz] = useState({
    title: "",
    description: "",
    category: "",
    language: "",
    questions: [emptyQuestion()],
  });
  const [isPublic, setIsPublic] = useState(true);
  const [categories, setCategories] = useState([]);
  const [languages, setLanguages] = useState([]);
  // [{ location: number | null, message: string }]
  const [errors, setErrors] = useState([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getCategories().then(setCategories).catch(() => { });
    getLanguages().then(setLanguages).catch(() => { });
  }, []);

  function setField(field, value) {
    setQuiz((q) => ({ ...q, [field]: value }));
  }

  function updateQuestion(qi, changes) {
    setQuiz((q) => ({
      ...q,
      questions: q.questions.map((question, i) =>
        i === qi ? { ...question, ...changes } : question
      ),
    }));
  }

  function addQuestion() {
    setQuiz((q) => ({ ...q, questions: [...q.questions, emptyQuestion()] }));
  }

  function removeQuestion(qi) {
    setQuiz((q) => ({
      ...q,
      questions: q.questions.filter((_, i) => i !== qi),
    }));
  }

  function updateOptionText(qi, oi, text) {
    updateQuestion(qi, {
      options: quiz.questions[qi].options.map((o, i) =>
        i === oi ? { ...o, text } : o
      ),
    });
  }

  function markCorrect(qi, oi) {
    updateQuestion(qi, {
      options: quiz.questions[qi].options.map((o, i) => ({
        ...o,
        isCorrect: i === oi,
      })),
    });
  }

  function addOption(qi) {
    updateQuestion(qi, {
      options: [...quiz.questions[qi].options, emptyOption()],
    });
  }

  function removeOption(qi, oi) {
    updateQuestion(qi, {
      options: quiz.questions[qi].options.filter((_, i) => i !== oi),
    });
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setErrors([]);
    setSaving(true);

    try {
      const created = await createQuiz(
        { ...quiz, description: quiz.description || undefined },
        isPublic
      );
      onCreated(created);
    } catch (err) {
      setErrors(err.errors ?? [{ location: null, message: err.message }]);
      setSaving(false);
    }
  }

  return (
    <form className="create-quiz" onSubmit={handleSubmit} noValidate>
      <h2>Create quiz</h2>

      <label className="field">
        Title
        <input
          type="text"
          value={quiz.title}
          onChange={(e) => setField("title", e.target.value)}
        />
      </label>

      <label className="field">
        Description (optional)
        <textarea
          rows={2}
          value={quiz.description}
          onChange={(e) => setField("description", e.target.value)}
        />
      </label>

      <div className="create-quiz-row">
        <label className="field">
          Category
          <select
            value={quiz.category}
            onChange={(e) => setField("category", e.target.value)}
          >
            <option value="">Select...</option>
            {categories.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </label>

        <label className="field">
          Language
          <select
            value={quiz.language}
            onChange={(e) => setField("language", e.target.value)}
          >
            <option value="">Select...</option>
            {languages.map((l) => (
              <option key={l} value={l}>{l}</option>
            ))}
          </select>
        </label>
      </div>

      <fieldset className="visibility">
        <legend>Visibility</legend>
        <label>
          <input
            type="radio"
            name="visibility"
            checked={isPublic}
            onChange={() => setIsPublic(true)}
          />
          Public
        </label>
        <label>
          <input
            type="radio"
            name="visibility"
            checked={!isPublic}
            onChange={() => setIsPublic(false)}
          />
          Private
        </label>
      </fieldset>

      {quiz.questions.map((question, qi) => (
        <fieldset key={qi} className="question-editor">
          <legend>Question {qi + 1}</legend>

          <input
            type="text"
            placeholder="Question text"
            aria-label={`Question ${qi + 1} text`}
            value={question.text}
            onChange={(e) => updateQuestion(qi, { text: e.target.value })}
          />

          {question.options.map((option, oi) => (
            <div key={oi} className="option-row">
              <input
                type="radio"
                name={`correct-${qi}`}
                checked={option.isCorrect}
                onChange={() => markCorrect(qi, oi)}
                aria-label={`Mark option ${oi + 1} of question ${qi + 1} as correct`}
              />
              <input
                type="text"
                placeholder={`Option ${oi + 1}`}
                aria-label={`Question ${qi + 1} option ${oi + 1} text`}
                value={option.text}
                onChange={(e) => updateOptionText(qi, oi, e.target.value)}
              />
              <button
                type="button"
                className="secondary-button"
                onClick={() => removeOption(qi, oi)}
                aria-label={`Remove option ${oi + 1} of question ${qi + 1}`}
              >
                ✕
              </button>
            </div>
          ))}

          <div className="step-actions">
            <button
              type="button"
              className="secondary-button"
              onClick={() => addOption(qi)}
            >
              Add option
            </button>
            <button
              type="button"
              className="secondary-button"
              onClick={() => removeQuestion(qi)}
            >
              Remove question
            </button>
          </div>
        </fieldset>
      ))}

      <button type="button" className="secondary-button" onClick={addQuestion}>
        Add question
      </button>

      {errors.length > 0 && (
        <div className="auth-error" role="alert">
          {errors.map((error, i) => (
            <p key={i}>
              {error.location != null && `Question ${error.location}: `}
              {error.message}
            </p>
          ))}
        </div>
      )}

      <div className="step-actions">
        <button
          type="button"
          className="secondary-button"
          onClick={onBack}
          disabled={saving}
        >
          Back
        </button>
        <button type="submit" disabled={saving}>
          {saving ? "Creating..." : "Create"}
        </button>
      </div>
    </form>
  );
}
