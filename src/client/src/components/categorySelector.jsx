import { useState, useEffect } from "react";
import { getCategories, getSelectedCategory, selectCategory } from "../api";

export default function CategorySelector({ onContinue, onBack }) {
  const [categories, setCategories] = useState([]);
  const [selected, setSelected] = useState(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState("");
  // bumping this re-runs the loading effect (used by the "Try again" button)
  const [reloadKey, setReloadKey] = useState(0);

  // load the available categories, and pre-select the one saved earlier (if any)
  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const list = await getCategories();
        // a failure here should not block choosing, so it's ignored
        const saved = await getSelectedCategory().catch(() => null);
        if (cancelled) return;
        setCategories(list);
        setSelected(saved?.category ?? null);
        setLoadError("");
      } catch (err) {
        if (!cancelled) setLoadError(err.message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    load();
    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  function handleRetry() {
    setLoading(true);
    setLoadError("");
    setReloadKey((k) => k + 1);
  }

  // sends the chosen category to the backend, moves on only if it was saved
  async function handleContinue() {
    if (!selected) return;

    setSaveError("");
    setSaving(true);

    try {
      const data = await selectCategory(selected);
      onContinue(data.category);
    } catch (err) {
      setSaveError(err.message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="category-selector">
      <h2>Choose a category</h2>

      {loading && <p className="category-hint">Loading categories...</p>}

      {loadError && (
        <div className="auth-error" role="alert">
          <p>{loadError}</p>
          <button type="button" className="link-button" onClick={handleRetry}>
            Try again
          </button>
        </div>
      )}

      {!loading && !loadError && (
        <>
          <p className="category-hint" aria-live="polite">
            {selected ? (
              <>
                Selected category: <strong>{selected}</strong>
              </>
            ) : (
              "Pick the topic you want to practise."
            )}
          </p>

          <div className="category-grid" role="radiogroup" aria-label="Categories">
            {categories.map((category) => {
              const isSelected = category === selected;
              return (
                <label
                  key={category}
                  className={`category-option${isSelected ? " selected" : ""}`}
                >
                  <input
                    type="radio"
                    name="category"
                    value={category}
                    checked={isSelected}
                    disabled={saving}
                    onChange={() => {
                      setSelected(category);
                      setSaveError("");
                    }}
                  />
                  <span>{category}</span>
                  {isSelected && (
                    <span className="category-check" aria-hidden="true">
                      ✓
                    </span>
                  )}
                </label>
              );
            })}
          </div>
        </>
      )}

      {saveError && (
        <p className="auth-error" role="alert">
          {saveError}
        </p>
      )}

      <div className="step-actions">
        {onBack && (
          <button
            type="button"
            className="secondary-button"
            onClick={onBack}
            disabled={saving}
          >
            Back
          </button>
        )}
        <button
          type="button"
          onClick={handleContinue}
          disabled={!selected || saving || loading || !!loadError}
        >
          {saving ? "Saving..." : "Continue"}
        </button>
      </div>
    </div>
  );
}
