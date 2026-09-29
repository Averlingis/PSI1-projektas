import { useState, useEffect } from "react";
import RegisterForm from "./components/registerForm";
import LoginForm from "./components/loginForm";
import CategorySelector from "./components/categorySelector";
import { getLanguages, getSelectedLanguage, selectLanguage } from "./api";
import "./App.css";

export default function App() {
  // tracks which screen the user is on before log in
  const [screen, setScreen] = useState("register");
  // jw token is saved into localstorage so it doesn't disappear on refresh
  const [token, setToken] = useState(localStorage.getItem("token"));
  // after log in the user goes through: "language" -> "category" -> "ready"
  const [step, setStep] = useState("language");
  // list of languages fetched from the backend, and whichever one is selected
  const [languages, setLanguages] = useState([]);
  const [selected, setSelected] = useState(null);
  const [languageError, setLanguageError] = useState("");
  // category confirmed by the backend
  const [category, setCategory] = useState(null);

  // once logged in, load the list of languages to choose from
  useEffect(() => {
    if (!token) return;
    getLanguages().then(setLanguages);
  }, [token]);

  // also load the language the user saved earlier, so it shows as selected
  useEffect(() => {
    if (!token) return;
    let cancelled = false;

    getSelectedLanguage()
      .then((data) => {
        if (!cancelled && data.language) setSelected(data.language);
      })
      // a failure here shouldn't block choosing a language
      .catch(() => {});

    return () => {
      cancelled = true;
    };
  }, [token]);

  // called after successful login, saves the jw token
  function handleLoginSuccess(newToken) {
    localStorage.setItem("token", newToken);
    setToken(newToken);
    setScreen("loggedIn");
  }

  // removes jwtoken, returns to login page
  function handleLogout() {
    localStorage.removeItem("token");
    setToken(null);
    setScreen("login");
    setStep("language");
    setSelected(null);
    setCategory(null);
  }

  // sends the chosen language to the backend, using the saved jw token
  async function handleSelectLanguage(language) {
    setLanguageError("");
    try {
      const data = await selectLanguage(language);
      setSelected(data.language);
    } catch (err) {
      setLanguageError(err.message);
    }
  }

  // if the token is found, the user is logged in and goes through the steps
  if (token) {
    return (
      <div className="app-shell">
        <div className="status-card">
          {step === "language" && (
            <>
              <h2>Choose a language</h2>
              <p>Selected language: {selected || "none yet"}</p>

              <div className="language-buttons">
                {languages.map((lang) => (
                  <button
                    key={lang}
                    className={lang === selected ? "selected" : ""}
                    aria-pressed={lang === selected}
                    onClick={() => handleSelectLanguage(lang)}
                  >
                    {lang}
                  </button>
                ))}
              </div>

              {languageError && <p className="auth-error">{languageError}</p>}

              <div className="step-actions">
                <button disabled={!selected} onClick={() => setStep("category")}>
                  Continue
                </button>
              </div>
            </>
          )}

          {step === "category" && (
            <CategorySelector
              onBack={() => setStep("language")}
              onContinue={(saved) => {
                setCategory(saved);
                setStep("ready");
              }}
            />
          )}

          {step === "ready" && (
            <>
              <h2>You're all set</h2>
              <p>Language: {selected || "none yet"}</p>
              <p>Category: {category}</p>

              <div className="step-actions">
                <button
                  className="secondary-button"
                  onClick={() => setStep("category")}
                >
                  Change category
                </button>
              </div>
            </>
          )}

          <button className="secondary-button logout" onClick={handleLogout}>
            Log out
          </button>
        </div>
      </div>
    );
  }

  // shows either register or login forms based on the screen the user is
  return (
    <div className="app-shell">
      {screen === "register" && (
        <RegisterForm
          onSuccess={() => setScreen("login")}
          onSwitchToLogin={() => setScreen("login")}
        />
      )}
      {screen === "login" && (
        <LoginForm
          onSuccess={handleLoginSuccess}
          onSwitchToRegister={() => setScreen("register")}
        />
      )}
    </div>
  );
}
