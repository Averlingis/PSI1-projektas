import ProfilePicture from "./ProfilePicture";

// the profile and picture are loaded by App before this page is shown
export default function ProfilePage({ profile, picture, onBack }) {
  return (
    <div className="profile-page">
      <h2>Your profile</h2>

      <ProfilePicture initialPicture={picture} />

      {profile ? (
        <div className="quiz-detail-info">
          <p><strong>Username:</strong> {profile.username}</p>
          <p><strong>Email:</strong> {profile.email}</p>
        </div>
      ) : (
        <p className="auth-error">Could not load your profile details.</p>
      )}

      <div className="step-actions">
        <button className="secondary-button" onClick={onBack}>
          Back
        </button>
      </div>
    </div>
  );
}