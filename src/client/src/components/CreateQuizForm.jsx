import { useState, useEffect, useRef } from "react";
import { createQuiz, getCategories, getLanguages } from "../api";

const emptyOption = () => ({ text: "", isCorrect: false });
const emptyQuestion = () => ({
  text: "",
  options: [emptyOption(), emptyOption()],
});

const isObject = (v) => typeof v === "object" && v !== null && !Array.isArray(v);
const asText = (v) => (typeof v === "string" ? v : "");

// returns the dropdown's own value if the file's value matches one of them (ignoring case), else ""
const matchDropdown = (value, allowed) =>
  (typeof value === "string"
    && allowed.find((a) => a.toLowerCase() === value.trim().toLowerCase()))
  || "";

function optionsFromFile(list) {
  if (!Array.isArray(list) || list.length === 0) {
    return [emptyOption(), emptyOption()];
  }

  // a radio group can show only one correct option, so only the first one is kept
  let foundCorrect = false;
  return list.map((item) => {
    const o = isObject(item) ? item : {};
    const isCorrect = o.isCorrect === true && !foundCorrect;
    if (isCorrect) foundCorrect = true;
    return { text: asText(o.text), isCorrect };
  });
}

// Turns anything JSON.parse can return into a valid form state.
// Values that do not fit are left empty.
function quizFromFile(data, categories, languages) {
  const file = isObject(data) ? data : {};

  const questions = Array.isArray(file.questions)
    ? file.questions.map((item) => {
        const q = isObject(item) ? item : {};
        return { text: asText(q.text), options: optionsFromFile(q.options) };
      })
    : [];

  return {
    title: asText(file.title),
    description: asText(file.description),
    category: matchDropdown(file.category, categories),
    language: matchDropdown(file.language, languages),
    questions: questions.length > 0 ? questions : [emptyQuestion()],
  };
}

// Form for creating a quiz by hand.
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
  const fileInputRef = useRef(null);
  const [importError, setImportError] = useState("");

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

  // radio button: exactly this option is correct, the others are not
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

  async function handleImport(event) {
    const file = event.target.files[0];
    // reset the input so choosing the same file again still triggers onChange
    event.target.value = "";
    if (!file) return;

    setImportError("");

    let data;
    try {
      data = JSON.parse(await file.text());
    } catch {
      setImportError("The file is not valid JSON.");
      return;
    }

    setQuiz(quizFromFile(data, categories, languages));
    setErrors([]);
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

      <div className="import-row">
        <input
          ref={fileInputRef}
          type="file"
          accept=".json,application/json"
          onChange={handleImport}
          hidden
        />
        <button
          type="button"
          className="secondary-button"
          onClick={() => fileInputRef.current.click()}
          disabled={saving}
        >
          Import from a file
        </button>
      </div>

      {importError && (
        <p className="auth-error" role="alert">
          {importError}
        </p>
      )}

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
