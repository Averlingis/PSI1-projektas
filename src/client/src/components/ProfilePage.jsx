import ProfilePicture from "./ProfilePicture";
import { useEffect, useState } from "react";
import { changeUsername } from "../api";

// the profile and picture are loaded by App before this page is shown
export default function ProfilePage({ profile, picture, onBack, onUsernameChanged }) {
  const [username, setUsername] = useState(profile?.username ?? "");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setUsername(profile?.username ?? "");
  }, [profile?.username]);

  async function handleSave(e) {
    e.preventDefault();
    setError("");
    setSuccess("");
    setSaving(true);

    try {
      const result = await changeUsername(username);

      setUsername(result.username);
      onUsernameChanged?.(result.username);
      setSuccess("Username updated successfully.");
    } catch (err) {
      setError(err.message || "Could not update username.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="profile-page">
      <h2>Your profile</h2>

      <ProfilePicture initialPicture={picture} />

      {profile ? (
        <div className="quiz-detail-info">
          <form onSubmit={handleSave}>
            <label htmlFor="profile-username">
              <strong>Username:</strong>
            </label>

            <div className="username-row">
              <input
                id="profile-username"
                type="text"
                value={username}
                onChange={(e) => {
                  setUsername(e.target.value);
                  setError("");
                  setSuccess("");
                }}
                minLength={3}
                maxLength={30}
                required
              />

              <button type="submit" disabled={saving} className="username-save-button">
                {saving ? "Saving..." : "Save"}
              </button>
            </div>

            {error && <p className="auth-error">{error}</p>}
            {success && <p role="status">{success}</p>}

          </form>

          <p><strong>Email:</strong> {profile.email}</p>
        </div>
      ) : (
        <p className="auth-error">
          Could not load your profile details.
        </p>
      )}

      <div className="step-actions">
        <button className="secondary-button" onClick={onBack}>
          Back
        </button>
      </div>
    </div>
  );
}