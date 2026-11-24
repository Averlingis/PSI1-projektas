import { useState, useEffect } from "react";
import { getStreak } from "../api";

// shows the current streak as 🔥 N
export default function StreakIcon() {
  const [streak, setStreak] = useState(0);

  useEffect(() => {
    let cancelled = false;

    getStreak()
      .then((data) => {
        if (!cancelled) setStreak(data.currentStreak);
      })
      .catch(() => {
        // an error leaves icon hidden
      });

    return () => {
      cancelled = true;
    };
  }, []);

  if (streak === 0) return null;

  return (
    <span className="streak-icon" title={`${streak} day streak`} aria-label={`${streak} day streak`}>
      <span aria-hidden="true">🔥</span>
      <span>{streak}</span>
    </span>
  );
}