import ProfilePicture from "./ProfilePicture";

// the profile and picture are loaded by App before this page is shown
export default function ProfilePage({
  profile,
  picture,
  language,
  category,
  onPictureChange,
  onChangeLanguage,
  onChangeCategory,
  onLogout,
  onBack,
}) {
  return (
    <div className="profile-page">
      <h2>Your profile</h2>

      <ProfilePicture initialPicture={picture} onPictureChange={onPictureChange} />

      <div className="quiz-detail-info">
        {profile ? (
          <>
            <p><strong>Username:</strong> {profile.username}</p>
            <p><strong>Email:</strong> {profile.email}</p>
          </>
        ) : (
          <p className="auth-error">Could not load your profile details.</p>
        )}
        <p><strong>Language:</strong> {language || "none yet"}</p>
        <p><strong>Category:</strong> {category || "none yet"}</p>
      </div>

      <div className="step-actions">
        <button className="secondary-button" onClick={onChangeLanguage}>
          Change language
        </button>
        <button className="secondary-button" onClick={onChangeCategory}>
          Change category
        </button>
      </div>

      <div className="profile-actions">
        <button className="secondary-button" onClick={onLogout}>
          Log out
        </button>
        <button className="secondary-button" onClick={onBack}>
          Back
        </button>
      </div>
    </div>
  );
}