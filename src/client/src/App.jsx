import { useState, useEffect } from "react";
import RegisterForm from "./components/RegisterForm";
import LoginForm from "./components/LoginForm";
import CategorySelector from "./components/CategorySelector";
import ProfilePicture from "./components/ProfilePicture";
import ProfilePage from "./components/ProfilePage";
import QuizList from "./components/QuizList";
import QuizDetail from "./components/QuizDetail";
import CreateQuizForm from "./components/CreateQuizForm";
import QuizPlay from "./components/QuizPlay";
import QuizResult from "./components/QuizResult";
import {
  getLanguages,
  getSelectedLanguage,
  getSelectedCategory,
  selectLanguage,
  resetQuizScore,
  getLatestInProgress,
  getQuizProgress,
  getProfile,
  getProfilePicture,
} from "./api";
import ProfileProgress from "./components/ProfileProgress";
import "./App.css";

const SESSION_EXPIRED_MESSAGE = "Your session has expired. Please log in again.";

export default function App() {
  // tracks which screen the user is on before log in
  const [screen, setScreen] = useState("register");
  // jw token is saved into localstorage so it doesn't disappear on refresh
  const [token, setToken] = useState(localStorage.getItem("token"));
  // after log in the user goes through: "loading" -> "language" -> "category" -> "ready"
  // "loading" checks the saved choices first, so returning users go straight to "ready"
  const [step, setStep] = useState("loading");
  // list of languages fetched from the backend, and whichever one is selected
  const [languages, setLanguages] = useState([]);
  const [selected, setSelected] = useState(null);
  const [languageError, setLanguageError] = useState("");
  // category confirmed by the backend
  const [category, setCategory] = useState(null);
  // Quiz navigation
  const [selectedQuiz, setSelectedQuiz] = useState(null);
  // progress of the selected quiz, loaded before its detail page is shown
  const [selectedProgress, setSelectedProgress] = useState(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [latestInProgress, setLatestInProgress] = useState(null);
  const [continueError, setContinueError] = useState("");
  const [isContinuing, setIsContinuing] = useState(false);
  const [initialQuestionIndex, setInitialQuestionIndex] = useState(0);
  // Quiz creating
  const [isCreating, setIsCreating] = useState(false);
  // true after the last question, shows the result screen
  const [isFinished, setIsFinished] = useState(false);
  // Keep quiz filters when navigating between list and details
  const [quizLanguage, setQuizLanguage] = useState(null);
  const [quizCategory, setQuizCategory] = useState(null);
  // true when the quiz list shows the user's own quizzes instead of the public ones
  const [showMyQuizzes, setShowMyQuizzes] = useState(false);
  const [isResuming, setIsResuming] = useState(false);
  // profile page, the details are loaded before the page is shown
  const [isViewingProfile, setIsViewingProfile] = useState(false);
  const [profile, setProfile] = useState(null);
  // the picture is loaded together with the profile, undefined means not preloaded
  const [profilePicture, setProfilePicture] = useState(undefined);

  // once logged in, load the list of languages to choose from
  useEffect(() => {
    if (!token) return;
    getLanguages()
      .then(setLanguages)
      .catch((err) => setLanguageError(err.message));
  }, [token]);

  // load the language and category the user saved earlier
  // and skip the setup steps if both are already saved
  useEffect(() => {
    if (!token) return;
    let cancelled = false;

    Promise.all([getSelectedLanguage(), getSelectedCategory()])
      .then(([languageData, categoryData]) => {
        if (cancelled) return;
        setSelected(languageData.language ?? null);
        setCategory(categoryData.category ?? null);
        setStep(languageData.language && categoryData.category ? "ready" : "language");
      })
      .catch((err) => {
        if (cancelled) return;
        if (err.message === SESSION_EXPIRED_MESSAGE) {
          handleLogout();
        } else {
          // saved choices couldn't be checked, so let the user pick them again
          setLanguageError(err.message);
          setStep("language");
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token]);

  // Load the latest unfinished quiz when the home screen is opened.
  useEffect(() => {
    if (!token || step !== "ready" || selectedQuiz || isCreating) return;

    let cancelled = false;

    getLatestInProgress()
      .then((data) => {
        if (!cancelled) {
          setLatestInProgress(data);
          setContinueError("");
        }
      })
      .catch((err) => {
        if (!cancelled) {
          if (err.message === SESSION_EXPIRED_MESSAGE) {
            handleLogout();
          } else {
            setContinueError(err.message);
          }
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token, step, selectedQuiz, isCreating]);

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
    setStep("loading");
    setSelected(null);
    setCategory(null);
    setSelectedQuiz(null);
    setSelectedProgress(null);
    setIsResuming(false);
    setIsPlaying(false);
    setIsCreating(false);
    setIsFinished(false);
    setQuizLanguage(null);
    setQuizCategory(null);
    setLanguageError("");
    setShowMyQuizzes(false);
    setLatestInProgress(null);
    setContinueError("");
    setIsContinuing(false);
    setInitialQuestionIndex(0);
    setIsViewingProfile(false);
    setProfile(null);
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


  // clears earlier answers, then opens the play screen
  async function startQuiz() {
    await resetQuizScore(selectedQuiz.id);
    setInitialQuestionIndex(0);
    setIsFinished(false);
    setIsPlaying(true);
    setIsResuming(false);
  }

  // Continues the latest unfinished quiz without clearing previous answers.
  async function handleContinue() {
    if (!latestInProgress || isContinuing) return;

    setIsContinuing(true);
    setContinueError("");

    try {
      const progress = await getQuizProgress(latestInProgress.quizId);

      if (progress.nextQuestionIndex == null) {
        setLatestInProgress(null);
        return;
      }
      setIsResuming(true);

      setSelectedQuiz({
        id: latestInProgress.quizId,
        title: latestInProgress.title,
        questionCount: progress.totalQuestions,
      });

      setInitialQuestionIndex(progress.nextQuestionIndex);
      setIsFinished(false);
      setIsPlaying(true);
    } catch (err) {
      if (err.message === SESSION_EXPIRED_MESSAGE) {
        handleLogout();
      } else {
        setContinueError(err.message);
      }
    } finally {
      setIsContinuing(false);
    }
  }

  // continues the selected quiz from the detail page, without clearing previous answers
  function continueFromDetail(questionIndex) {
    setInitialQuestionIndex(questionIndex);
    setIsFinished(false);
    setIsResuming(false);
    setIsPlaying(true);
  }

  // loads how far the user got in a quiz or null if it cant be loaded
  async function loadProgress(quizId) {
    try {
      return await getQuizProgress(quizId);
    } catch {
      // without progress the detail page shows only play, which still works
      return null;
    }
  }

  // loads the progress first, so the detail page opens with the right buttons
  async function openQuizDetail(quiz) {
    const progress = await loadProgress(quiz.id);
    setSelectedProgress(progress);
    setSelectedQuiz(quiz);
  }

  // leaves the play screen and returns to the detail page with up-to-date progress
  async function quitToDetail() {
    const progress = await loadProgress(selectedQuiz.id);
    setSelectedProgress(progress);
    setIsPlaying(false);
  }

  // leaves the quiz and goes back to the list
  function handleBackToQuizzes() {
    setSelectedQuiz(null);
    setSelectedProgress(null);
    setIsPlaying(false);
    setIsFinished(false);
    setIsResuming(false);
  }

  // loads the profile details and the picture first, so the profile page opens complete
  async function openProfile() {
    try {
      const [details, picture] = await Promise.all([
        getProfile(),
        // without the picture the placeholder is shown, so a failure here doesn't stop the page
        getProfilePicture().catch(() => null),
      ]);
      setProfile(details);
      setProfilePicture(picture);
    } catch (err) {
      if (err.message === SESSION_EXPIRED_MESSAGE) {
        handleLogout();
        return;
      }
      // the page still opens and shows that the details couldn't be loaded
      setProfile(null);
      setProfilePicture(undefined);
    }
    setIsViewingProfile(true);
  }

  // leaves the profile page and goes back to the home screen
  function closeProfile() {
    setIsViewingProfile(false);
  }

  // if the token is found, the user is logged in and goes through the steps
  if (token) {
    return (
      <div className="app-shell">
        <div className="status-card">
          {step === "loading" && <p>Loading your settings...</p>}

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
                <button
                  disabled={!selected}
                  onClick={() => setStep(category ? "ready" : "category")}
                >
                  Continue
                </button>
              </div>
            </>
          )}

          {step === "category" && (
            <CategorySelector
              onBack={category ? undefined : () => setStep("language")}
              onContinue={(saved) => {
                setCategory(saved);
                setStep("ready");
              }}
            />
          )}

          {step === "ready" && !selectedQuiz && !isCreating && !isViewingProfile && (
            <>
              <ProfilePicture />

              <h2>You're all set</h2>
              <p>Language: {selected || "none yet"}</p>
              <p>Category: {category}</p>

              <div className="step-actions">
                <button
                  className="secondary-button"
                  onClick={() => setStep("language")}
                >
                  Change language
                </button>
                <button
                  className="secondary-button"
                  onClick={() => setStep("category")}
                >
                  Change category
                </button>
              </div>

              <ProfileProgress
                languages={languages}
                initialLanguage={selected}
              >
                <button className="secondary-button" onClick={openProfile}>
                  Edit profile
                </button>

                <button onClick={() => setIsCreating(true)}>
                  Create quiz
                </button>
              </ProfileProgress>

              {latestInProgress && (
                <div className="step-actions">
                  <button
                    onClick={handleContinue}
                    disabled={isContinuing}
                  >
                    {isContinuing
                      ? "Loading..."
                      : `Continue: ${latestInProgress.title}`}
                  </button>
                </div>
              )}

              {continueError && (
                <p className="auth-error">{continueError}</p>
              )}

              <QuizList
                initialLanguage={selected}
                initialCategory={category}
                language={quizLanguage ?? selected ?? ""}
                category={quizCategory ?? category ?? ""}
                onLanguageChange={setQuizLanguage}
                onCategoryChange={setQuizCategory}
                onSelectQuiz={openQuizDetail}
                showMine={showMyQuizzes}
                onToggleMine={() => setShowMyQuizzes((v) => !v)}
              />
            </>
          )}

          {step === "ready" && isViewingProfile && (
            <ProfilePage profile={profile} picture={profilePicture} onBack={closeProfile} />
          )}

          {step === "ready" && isCreating && (
            <CreateQuizForm
              onBack={() => setIsCreating(false)}
              onCreated={async (quiz) => {
                await openQuizDetail(quiz);
                setIsCreating(false);
              }}
            />
          )}

          {step === "ready" && selectedQuiz && !isPlaying && (
            <QuizDetail
              quiz={selectedQuiz}
              progress={selectedProgress}
              onBack={() => setSelectedQuiz(null)}
              onPlay={startQuiz}
              onContinue={continueFromDetail}
            />
          )}

          {step === "ready" && selectedQuiz && isPlaying && !isFinished && (
            <QuizPlay
              quiz={selectedQuiz}
              initialQuestionIndex={initialQuestionIndex}
              onQuit={() => {
                if (isResuming) {
                  setSelectedQuiz(null);
                  setIsPlaying(false);
                  setIsResuming(false);
                } else {
                  quitToDetail();
                }
              }}
              onFinish={() => setIsFinished(true)}
            />
          )}

          {step === "ready" && selectedQuiz && isPlaying && isFinished && (
            <QuizResult
              quiz={selectedQuiz}
              onPlayAgain={startQuiz}
              onBackToQuizzes={handleBackToQuizzes}
            />
          )}

          {/* only on the main page, not during setup or inside a quiz */}
          {step === "ready" && !selectedQuiz && !isCreating && !isViewingProfile && (
            <button className="secondary-button logout" onClick={handleLogout}>
              Log out
            </button>
          )}

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