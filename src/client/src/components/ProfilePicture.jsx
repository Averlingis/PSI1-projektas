import { useState, useRef } from "react";
import { uploadProfilePicture } from "../api";
import useProfilePicture from "../hooks/useProfilePicture";
import AvatarImage from "./AvatarImage";
import StreakIcon from "./StreakIcon";

// same limits as the backend
const MAX_SIZE_BYTES = 2 * 1024 * 1024;
const ALLOWED_TYPES = ["image/jpeg", "image/png"];

// initialPicture: undefined = load it here, null = preloaded and there's no picture, Blob = preloaded picture
// onPictureChange is called with the new picture after an upload
export default function ProfilePicture({ initialPicture, onPictureChange }) {
  const { imageUrl, loading, error: loadError, reload } = useProfilePicture(initialPicture);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState("");
  const fileInputRef = useRef(null);

  async function handleFileChange(event) {
    const file = event.target.files[0];
    // reset the input so picking the same file again still triggers onChange
    event.target.value = "";
    if (!file) return;

    setError("");

    if (!ALLOWED_TYPES.includes(file.type)) {
      setError("Only JPEG and PNG images are allowed.");
      return;
    }

    if (file.size > MAX_SIZE_BYTES) {
      setError("The image must be 2 MB or smaller.");
      return;
    }

    setUploading(true);
    try {
      await uploadProfilePicture(file);
      onPictureChange?.(file);
      reload();
    } catch (err) {
      setError(err.message);
    } finally {
      setUploading(false);
    }
  }

  return (
    <div className="profile-picture">
      <div className="profile-avatar-wrapper">
        <div className="profile-avatar">
          <AvatarImage imageUrl={imageUrl} loading={loading} />
        </div>
        <StreakIcon />
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept={ALLOWED_TYPES.join(",")}
        onChange={handleFileChange}
        hidden
      />

      <button
        type="button"
        className="secondary-button"
        onClick={() => fileInputRef.current.click()}
        disabled={loading || uploading}
      >
        {uploading ? "Uploading..." : imageUrl ? "Change picture" : "Upload picture"}
      </button>

      {(error || loadError) && (
        <p className="auth-error" role="alert">
          {error || loadError}
        </p>
      )}
    </div>
  );
}