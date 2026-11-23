import { useState, useEffect } from "react";
import { getProfilePicture } from "../api";

// loads the user's profile picture and turns it into a URL an <img> can show
export default function useProfilePicture(initialPicture) {
  const preloaded = initialPicture !== undefined;
  const [imageUrl, setImageUrl] = useState(() =>
    initialPicture ? URL.createObjectURL(initialPicture) : null
  );
  const [loading, setLoading] = useState(!preloaded);
  const [error, setError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);

  // load the saved picture from the backend
  useEffect(() => {
    // a preloaded picture is already shown, so it's only loaded again after an upload
    if (preloaded && reloadKey === 0) return;

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
  }, [reloadKey, preloaded]);

  // frees the memory of object's URL once it's replaced or the component no longer available
  useEffect(() => {
    return () => {
      if (imageUrl) URL.revokeObjectURL(imageUrl);
    };
  }, [imageUrl]);

  // loads the picture again after a new one was uploaded
  function reload() {
    setReloadKey((k) => k + 1);
  }

  return { imageUrl, loading, error, reload };
}