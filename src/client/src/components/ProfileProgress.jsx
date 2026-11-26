import { useEffect, useState } from "react";
import { getStats } from "../api";

export default function ProfileProgress({ languages, initialLanguage, children }) {
    const [isOpen, setIsOpen] = useState(false);
    const [language, setLanguage] = useState(initialLanguage ?? "");
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        setLanguage(initialLanguage ?? "");
    }, [initialLanguage]);

    useEffect(() => {
        if (!isOpen || !language) {
            setStats(null);
            return;
        }

        let cancelled = false;

        setLoading(true);
        setError("");
        setStats(null);

        getStats(language)
            .then((data) => {
                if (!cancelled) setStats(data);
            })
            .catch((err) => {
                if (!cancelled) setError(err.message);
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [isOpen, language]);

    return (
        <section className="profile-progress">
            <div className="step-actions">
                <button
                    type="button"
                    className="secondary-button"
                    onClick={() => setIsOpen((previous) => !previous)}
                    aria-expanded={isOpen}
                >
                    Your progress {isOpen ? "−" : "+"}
                </button>

                {children}
            </div>

            {isOpen && (
                <div className="progress-panel">
                    <label htmlFor="progress-language">Language:</label>

                    <select
                        id="progress-language"
                        value={language}
                        onChange={(e) => setLanguage(e.target.value)}
                    >
                        {!language && <option value="">Choose a language</option>}
                        {languages.map((lang) => (
                            <option key={lang} value={lang}>
                                {lang}
                            </option>
                        ))}
                    </select>

                    {!language && <p>Choose a language to view your progress.</p>}
                    {loading && <p>Loading statistics...</p>}
                    {error && <p className="auth-error">{error}</p>}

                    {stats && (
                        <>
                            <h3>Statistics by category</h3>

                            {stats.categories.map((item) => (
                                <p key={item.category}>
                                    <strong>{item.category}:</strong>{" "}
                                    {item.hasEnoughData && item.answered > 0
                                        ? `${item.correct} of ${item.answered} answered correctly (${Math.round(
                                            (item.correct / item.answered) * 100
                                        )}%)`
                                        : "Not enough data"}
                                </p>
                            ))}

                            {stats.strongest != null && (
                                <p>
                                    <strong>Strongest category:</strong> {stats.strongest}
                                </p>
                            )}

                            {stats.weakest != null && (
                                <p>
                                    <strong>Weakest category:</strong> {stats.weakest}
                                </p>
                            )}
                        </>
                    )}
                </div>
            )}
        </section>
    );
}