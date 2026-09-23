import { useState, useEffect } from "react";
import RegisterForm from "./components/registerForm";
import LoginForm from "./components/loginForm";
import { getLanguages, selectLanguage } from "./api";
import "./App.css";

export default function App() {
  // tracks which screen the user is on before log in
  const [screen, setScreen] = useState("register");
  // jw token is saved into localstorage so it doesn't disappear on refresh
  const [token, setToken] = useState(localStorage.getItem("token"));
  // list of languages fetched from the backend, and whichever one is selected
  const [languages, setLanguages] = useState([]);
  const [selected, setSelected] = useState(null);
  const [languageError, setLanguageError] = useState("");

  // once logged in, load the list of languages to choose from
  useEffect(() => {
    if (!token) return;
    getLanguages().then(setLanguages);
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

  // if the token is found, login successful with a message
  if (token) {
    return (
      <div className="app-shell">
        <div className="status-card">
          <p>You're logged in.</p>

          <p>Selected language: {selected || "none yet"}</p>

          <div className="language-buttons">
            {languages.map((lang) => (
              <button key={lang} onClick={() => handleSelectLanguage(lang)}>
                {lang}
              </button>
            ))}
          </div>

          {languageError && <p className="auth-error">{languageError}</p>}

          <button onClick={handleLogout}>Log out</button>
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