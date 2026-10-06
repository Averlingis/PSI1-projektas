import { useState, useEffect, useRef } from "react";
import { getProfilePicture, uploadProfilePicture } from "../api";

// same limits as the backend
const MAX_SIZE_BYTES = 2 * 1024 * 1024;
const ALLOWED_TYPES = ["image/jpeg", "image/png"];

export default function ProfilePicture() {
  const [imageUrl, setImageUrl] = useState(null);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);
  const fileInputRef = useRef(null);

  // load the saved picture from the backend
  useEffect(() => {
    let cancelled = false;

    getProfilePicture()
      .then((blob) => {
        if (cancelled) return;
        setImageUrl(blob ? URL.createObjectURL(blob) : null);
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
  }, [reloadKey]);

  // frees the memory of object's URL once it's replaced or the component no longer available
  useEffect(() => {
    return () => {
      if (imageUrl) URL.revokeObjectURL(imageUrl);
    };
  }, [imageUrl]);

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
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err.message);
    } finally {
      setUploading(false);
    }
  }

  return (
    <div className="profile-picture">
      <div className="profile-avatar">
        {imageUrl ? (
          <img src={imageUrl} alt="Your profile picture" />
        ) : (
          <svg viewBox="0 0 24 24" aria-hidden="true">
            <circle cx="12" cy="8" r="4" />
            <path d="M4 21c0-4.4 3.6-8 8-8s8 3.6 8 8z" />
          </svg>
        )}
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

      {error && (
        <p className="auth-error" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}